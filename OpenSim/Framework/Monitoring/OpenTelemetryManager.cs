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
using Microsoft.Extensions.Logging;
using Nini.Config;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace OpenSim.Framework.Monitoring
{
    /// <summary>
    /// Manages OpenTelemetry instrumentation for metrics and logs.
    /// Provides centralized configuration and lifecycle management.
    /// </summary>
    public class OpenTelemetryManager : IDisposable
    {
        private static OpenTelemetryManager s_instance;
        private static readonly object s_lock = new object();

        private MeterProvider m_meterProvider;
        private ILoggerFactory m_loggerFactory;
        private Meter m_meter;

        private bool m_enabled;
        private bool m_metricsEnabled;
        private bool m_logsEnabled;
        private string m_serviceName;
        private string m_otlpEndpoint;
        private OtlpExportProtocol m_protocol;
        private int m_metricsExportIntervalMs;
        private LogLevel m_minimumLogLevel;
        private Dictionary<string, string> m_resourceAttributes;
        private bool m_disposed;

        /// <summary>
        /// Gets whether OpenTelemetry has been initialized.
        /// </summary>
        public static bool IsInitialized => s_instance != null;

        /// <summary>
        /// Gets the singleton instance of OpenTelemetryManager.
        /// </summary>
        public static OpenTelemetryManager Instance
        {
            get
            {
                if (s_instance == null)
                    throw new InvalidOperationException("OpenTelemetryManager has not been initialized. Call Initialize() first.");
                return s_instance;
            }
        }

        /// <summary>
        /// Gets whether OpenTelemetry is enabled.
        /// </summary>
        public bool Enabled => m_enabled;

        /// <summary>
        /// Gets whether metrics export is enabled.
        /// </summary>
        public bool MetricsEnabled => m_enabled && m_metricsEnabled;

        /// <summary>
        /// Gets whether logs export is enabled.
        /// </summary>
        public bool LogsEnabled => m_enabled && m_logsEnabled;

        private OpenTelemetryManager()
        {
        }

        /// <summary>
        /// Initializes OpenTelemetry with configuration from the provided config source.
        /// </summary>
        /// <param name="config">Configuration source containing [OpenTelemetry] section</param>
        public static void Initialize(IConfigSource config)
        {
            lock (s_lock)
            {
                if (s_instance != null)
                {
                    throw new InvalidOperationException("OpenTelemetryManager has already been initialized.");
                }

                s_instance = new OpenTelemetryManager();
                s_instance.LoadConfiguration(config);

                if (s_instance.m_enabled)
                {
                    s_instance.InitializeMeterProvider();
                    s_instance.InitializeLoggerProvider();
                }
            }
        }

        /// <summary>
        /// Gets the Meter instance for creating metrics instruments.
        /// </summary>
        /// <returns>Meter instance</returns>
        public Meter GetMeter()
        {
            if (!m_enabled)
                return null;
            return m_meter;
        }

        /// <summary>
        /// Creates a logger for the specified category name.
        /// </summary>
        /// <param name="categoryName">Logger category name</param>
        /// <returns>ILogger instance</returns>
        public ILogger CreateLogger(string categoryName)
        {
            if (!m_enabled || !m_logsEnabled || m_loggerFactory == null)
                return null;
            return m_loggerFactory.CreateLogger(categoryName);
        }

        private void LoadConfiguration(IConfigSource config)
        {
            IConfig otelConfig = config.Configs["OpenTelemetry"];
            if (otelConfig == null)
            {
                m_enabled = false;
                return;
            }

            m_enabled = otelConfig.GetBoolean("Enabled", false);
            if (!m_enabled)
                return;

            m_serviceName = otelConfig.GetString("ServiceName", "Akisim");
            m_otlpEndpoint = otelConfig.GetString("OtlpEndpoint", "http://localhost:4317");
            m_metricsEnabled = otelConfig.GetBoolean("MetricsEnabled", true);
            m_logsEnabled = otelConfig.GetBoolean("LogsEnabled", true);
            m_metricsExportIntervalMs = otelConfig.GetInt("MetricsExportIntervalMs", 60000);

            // Parse OTLP protocol
            string protocolStr = otelConfig.GetString("OtlpProtocol", "Grpc");
            m_protocol = protocolStr.Equals("HttpProtobuf", StringComparison.OrdinalIgnoreCase)
                ? OtlpExportProtocol.HttpProtobuf
                : OtlpExportProtocol.Grpc;

            // Parse log level
            string logLevelStr = otelConfig.GetString("LogLevel", "Information");
            if (!Enum.TryParse(logLevelStr, true, out m_minimumLogLevel))
            {
                m_minimumLogLevel = LogLevel.Information;
            }

            // Parse resource attributes
            m_resourceAttributes = new Dictionary<string, string>();
            string resourceAttributesStr = otelConfig.GetString("ResourceAttributes", "");
            if (!string.IsNullOrWhiteSpace(resourceAttributesStr))
            {
                foreach (string pair in resourceAttributesStr.Split(';'))
                {
                    string[] parts = pair.Split('=');
                    if (parts.Length == 2)
                    {
                        m_resourceAttributes[parts[0].Trim()] = parts[1].Trim();
                    }
                }
            }
        }

        private void InitializeMeterProvider()
        {
            if (!m_metricsEnabled)
                return;

            try
            {
                var builder = Sdk.CreateMeterProviderBuilder()
                    .SetResourceBuilder(CreateResourceBuilder())
                    .AddMeter(m_serviceName);

                builder.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(m_otlpEndpoint);
                    options.Protocol = m_protocol;
                });

                // Set metric reader with export interval
                builder.AddOtlpExporter((exporterOptions, readerOptions) =>
                {
                    exporterOptions.Endpoint = new Uri(m_otlpEndpoint);
                    exporterOptions.Protocol = m_protocol;
                    readerOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = m_metricsExportIntervalMs;
                });

                m_meterProvider = builder.Build();
                m_meter = new Meter(m_serviceName, "1.0.0");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to initialize OpenTelemetry MeterProvider: {ex.Message}", ex);
            }
        }

        private void InitializeLoggerProvider()
        {
            if (!m_logsEnabled)
                return;

            try
            {
                m_loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.AddOpenTelemetry(logging =>
                    {
                        logging.SetResourceBuilder(CreateResourceBuilder());
                        logging.AddOtlpExporter(options =>
                        {
                            options.Endpoint = new Uri(m_otlpEndpoint);
                            options.Protocol = m_protocol;
                        });
                        logging.IncludeFormattedMessage = true;
                        logging.IncludeScopes = true;
                    });
                    builder.SetMinimumLevel(m_minimumLogLevel);
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to initialize OpenTelemetry LoggerProvider: {ex.Message}", ex);
            }
        }

        private ResourceBuilder CreateResourceBuilder()
        {
            var builder = ResourceBuilder.CreateDefault()
                .AddService(serviceName: m_serviceName, serviceVersion: "1.0.0");

            // Add custom resource attributes
            foreach (var kvp in m_resourceAttributes)
            {
                builder.AddAttributes(new[] { new KeyValuePair<string, object>(kvp.Key, kvp.Value) });
            }

            return builder;
        }

        /// <summary>
        /// Disposes of OpenTelemetry resources and flushes pending telemetry.
        /// </summary>
        public void Dispose()
        {
            if (m_disposed)
                return;

            m_disposed = true;

            try
            {
                // Dispose meter provider (flushes metrics)
                m_meterProvider?.Dispose();

                // Dispose logger factory (flushes logs)
                m_loggerFactory?.Dispose();

                // Dispose meter
                m_meter?.Dispose();
            }
            catch (Exception ex)
            {
                // Log disposal errors but don't throw
                Console.WriteLine($"Error disposing OpenTelemetryManager: {ex.Message}");
            }
        }
    }
}
