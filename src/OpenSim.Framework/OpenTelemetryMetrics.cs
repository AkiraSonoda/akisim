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
using Nini.Config;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace OpenSim.Framework
{
    /// <summary>
    /// Manages OpenTelemetry metrics collection and export for OpenSimulator
    /// </summary>
    public class OpenTelemetryMetrics : IDisposable
    {
        private static OpenTelemetryMetrics s_instance;
        private static readonly object s_lock = new object();

        private MeterProvider m_meterProvider;
        private readonly Meter m_meter;
        private bool m_disposed = false;

        // Configuration properties
        public string Endpoint { get; set; } = "https://otlp-gateway-prod-us-central-0.grafana.net/otlp";
        public string ServiceName { get; set; } = "OpenSimulator";
        public string ServiceVersion { get; set; } = "1.0.0";
        public string GrafanaInstanceId { get; set; } = "";
        public string GrafanaApiKey { get; set; } = "";
        public int ExportIntervalMilliseconds { get; set; } = 60000; // 60 seconds default

        // Custom metrics
        private Counter<long> m_avatarCount;
        private Counter<long> m_scriptExecutions;
        private Histogram<double> m_frameTime;
        private ObservableGauge<long> m_activeSessions;

        private OpenTelemetryMetrics()
        {
            m_meter = new Meter("OpenSimulator", "1.0.0");
            InitializeCustomMetrics();
        }

        public static OpenTelemetryMetrics Instance
        {
            get
            {
                if (s_instance == null)
                {
                    lock (s_lock)
                    {
                        if (s_instance == null)
                        {
                            s_instance = new OpenTelemetryMetrics();
                        }
                    }
                }
                return s_instance;
            }
        }

        private void InitializeCustomMetrics()
        {
            // Counter for tracking avatar connections
            m_avatarCount = m_meter.CreateCounter<long>(
                "opensim.avatar.connections",
                unit: "connections",
                description: "Number of avatar connections");

            // Counter for script executions
            m_scriptExecutions = m_meter.CreateCounter<long>(
                "opensim.script.executions",
                unit: "executions",
                description: "Number of script executions");

            // Histogram for frame time tracking
            m_frameTime = m_meter.CreateHistogram<double>(
                "opensim.frame.duration",
                unit: "ms",
                description: "Frame processing duration in milliseconds");

            // Observable gauge for active sessions
            m_activeSessions = m_meter.CreateObservableGauge<long>(
                "opensim.sessions.active",
                () => GetActiveSessionCount(),
                unit: "sessions",
                description: "Number of active sessions");
        }

        /// <summary>
        /// Configure and optionally start from Nini configuration source
        /// </summary>
        public void Configure(IConfigSource configSource, bool autoStart = false)
        {
            if (configSource == null)
                return;

            var config = configSource.Configs["OpenTelemetry"];
            if (config == null)
            {
                log4net.LogManager.GetLogger(GetType()).Info("OpenTelemetry section not found in configuration. Metrics disabled.");
                return;
            }

            bool enabled = config.GetBoolean("Enabled", false);
            if (!enabled)
            {
                log4net.LogManager.GetLogger(GetType()).Info("OpenTelemetry is disabled in configuration.");
                return;
            }

            // Read configuration - support both old and new key names for backward compatibility
            Endpoint = config.GetString("OtlpEndpoint", config.GetString("Endpoint", Endpoint));
            ServiceName = config.GetString("ServiceName", ServiceName);
            ServiceVersion = config.GetString("ServiceVersion", ServiceVersion);
            
            // Handle authorization token - can be in Grafana Cloud format (instanceId:apiKey) or just token
            string authToken = config.GetString("AuthorizationToken", "");
            if (!string.IsNullOrEmpty(authToken))
            {
                // Try to parse Grafana Cloud format: instanceId:apiKey
                string[] parts = authToken.Split(':');
                if (parts.Length == 2)
                {
                    GrafanaInstanceId = parts[0];
                    GrafanaApiKey = parts[1];
                }
                else
                {
                    // Single token format - use as GrafanaInstanceId with empty apiKey
                    GrafanaInstanceId = authToken;
                    GrafanaApiKey = "";
                }
            }
            else
            {
                // Fallback to old format if AuthorizationToken not present
                GrafanaInstanceId = config.GetString("GrafanaInstanceId", GrafanaInstanceId);
                GrafanaApiKey = config.GetString("GrafanaApiKey", GrafanaApiKey);
            }
            
            ExportIntervalMilliseconds = config.GetInt("ExportIntervalMilliseconds", ExportIntervalMilliseconds);

            if (autoStart)
                Start();
        }

        /// <summary>
        /// Initialize and start the OpenTelemetry metrics pipeline
        /// </summary>
        public void Start()
        {
            if (m_meterProvider != null)
                return;

            try
            {
                var resourceBuilder = ResourceBuilder.CreateDefault()
                    .AddService(
                        serviceName: ServiceName,
                        serviceVersion: ServiceVersion)
                    .AddAttributes(new[]
                    {
                        new KeyValuePair<string, object>("deployment.environment", Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "production"),
                        new KeyValuePair<string, object>("host.name", Environment.MachineName)
                    });

                var builder = Sdk.CreateMeterProviderBuilder()
                    .SetResourceBuilder(resourceBuilder)
                    // Add CLR Runtime instrumentation for .NET metrics
                    .AddRuntimeInstrumentation()
                    // Add our custom meter
                    .AddMeter(m_meter.Name)
                    // Configure periodic export
                    .AddOtlpExporter((exporterOptions, metricReaderOptions) =>
                    {
                        exporterOptions.Endpoint = new Uri(Endpoint);

                        // Add Grafana Cloud authentication headers if configured
                        if (!string.IsNullOrEmpty(GrafanaInstanceId) && !string.IsNullOrEmpty(GrafanaApiKey))
                        {
                            exporterOptions.Headers = $"Authorization=Bearer {GrafanaInstanceId}:{GrafanaApiKey}";
                        }

                        exporterOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;

                        // Configure export interval
                        metricReaderOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = ExportIntervalMilliseconds;
                    });

                m_meterProvider = builder.Build();

                log4net.LogManager.GetLogger(GetType()).Info($"OpenTelemetry metrics initialized. Exporting to: {Endpoint}");
            }
            catch (Exception ex)
            {
                log4net.LogManager.GetLogger(GetType()).Error($"Failed to initialize OpenTelemetry metrics: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Stop the metrics pipeline and flush any pending metrics
        /// </summary>
        public void Stop()
        {
            if (m_meterProvider != null)
            {
                m_meterProvider.ForceFlush();
                m_meterProvider.Dispose();
                m_meterProvider = null;
            }
        }

        // Public methods to record custom metrics
        public void RecordAvatarConnection()
        {
            m_avatarCount?.Add(1);
        }

        public void RecordScriptExecution()
        {
            m_scriptExecutions?.Add(1);
        }

        public void RecordFrameTime(double durationMs)
        {
            m_frameTime?.Record(durationMs);
        }

        private long GetActiveSessionCount()
        {
            // This would be implemented to return actual active session count
            // For now, return 0 as placeholder
            return 0;
        }

        public void Dispose()
        {
            if (!m_disposed)
            {
                Stop();
                m_meter?.Dispose();
                m_disposed = true;
            }
        }
    }
}
