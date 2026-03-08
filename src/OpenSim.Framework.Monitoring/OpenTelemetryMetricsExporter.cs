/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using log4net;

namespace OpenSim.Framework.Monitoring
{
    /// <summary>
    /// Exports StatsManager statistics as OpenTelemetry metrics.
    /// </summary>
    public class OpenTelemetryMetricsExporter
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private Meter m_meter;
        private Dictionary<string, ObservableGauge<double>> m_gauges;
        private static readonly Regex s_invalidCharsRegex = new Regex("[^a-zA-Z0-9_.]", RegexOptions.Compiled);

        public OpenTelemetryMetricsExporter()
        {
            m_gauges = new Dictionary<string, ObservableGauge<double>>();
        }

        /// <summary>
        /// Registers all existing StatsManager statistics as OpenTelemetry metrics.
        /// </summary>
        public void RegisterStatsManagerMetrics()
        {
            if (!OpenTelemetryManager.IsInitialized || !OpenTelemetryManager.Instance.MetricsEnabled)
            {
                m_log.Info("[OpenTelemetry] Metrics registration skipped - OpenTelemetry not initialized or metrics disabled");
                return;
            }

            m_meter = OpenTelemetryManager.Instance.GetMeter();
            if (m_meter == null)
            {
                m_log.Warn("[OpenTelemetry] Failed to get Meter instance");
                return;
            }

            m_log.Info("[OpenTelemetry] Starting StatsManager metrics registration...");
            int registeredCount = 0;
            int failedCount = 0;

            lock (StatsManager.RegisteredStats)
            {
                foreach (var categoryPair in StatsManager.RegisteredStats)
                {
                    string category = categoryPair.Key;

                    foreach (var containerPair in categoryPair.Value)
                    {
                        string container = containerPair.Key;

                        foreach (var statPair in containerPair.Value)
                        {
                            Stat stat = statPair.Value;
                            try
                            {
                                RegisterStat(category, container, stat);
                                registeredCount++;
                            }
                            catch (Exception ex)
                            {
                                failedCount++;
                                m_log.WarnFormat("[OpenTelemetry] Failed to register stat {0}.{1}.{2}: {3}",
                                    category, container, stat.ShortName, ex.Message);
                            }
                        }
                    }
                }
            }

            m_log.InfoFormat("[OpenTelemetry] Metrics registration complete: {0} registered, {1} failed",
                registeredCount, failedCount);
        }

        /// <summary>
        /// Registers a single Stat as an OpenTelemetry metric.
        /// </summary>
        private void RegisterStat(string category, string container, Stat stat)
        {
            // Create metric name: category_container_shortname
            string metricName = SanitizeMetricName($"{category}_{container}_{stat.ShortName}");

            // Avoid duplicate registration
            if (m_gauges.ContainsKey(metricName))
                return;

            // Determine description
            string description = string.IsNullOrWhiteSpace(stat.Description)
                ? stat.Name
                : stat.Description;

            // Determine unit
            string unit = string.IsNullOrWhiteSpace(stat.UnitName)
                ? "1"
                : stat.UnitName.Trim();

            // Create observable gauge
            // ObservableGauge is appropriate because StatsManager stats are pull-based
            var gauge = m_meter.CreateObservableGauge(
                metricName,
                () =>
                {
                    try
                    {
                        // Get current value (this will trigger PullAction if it's a Pull stat)
                        double value = stat.Value;

                        // Create measurement with tags
                        return new Measurement<double>(
                            value,
                            new KeyValuePair<string, object>("category", category),
                            new KeyValuePair<string, object>("container", container),
                            new KeyValuePair<string, object>("stat_name", stat.Name),
                            new KeyValuePair<string, object>("short_name", stat.ShortName)
                        );
                    }
                    catch
                    {
                        // If stat throws exception, return 0
                        return new Measurement<double>(0);
                    }
                },
                unit,
                description
            );

            m_gauges[metricName] = gauge;
        }

        /// <summary>
        /// Sanitizes metric names to conform to OpenTelemetry naming conventions.
        /// Replaces invalid characters with underscores and converts to lowercase.
        /// </summary>
        private string SanitizeMetricName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "unknown";

            // Replace invalid characters with underscore
            string sanitized = s_invalidCharsRegex.Replace(name, "_");

            // Convert to lowercase
            sanitized = sanitized.ToLowerInvariant();

            // Remove consecutive underscores
            while (sanitized.Contains("__"))
            {
                sanitized = sanitized.Replace("__", "_");
            }

            // Trim leading/trailing underscores
            sanitized = sanitized.Trim('_');

            // Ensure it starts with a letter
            if (sanitized.Length > 0 && char.IsDigit(sanitized[0]))
            {
                sanitized = "metric_" + sanitized;
            }

            return sanitized;
        }
    }
}
