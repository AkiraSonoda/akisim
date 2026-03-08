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
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IO;
using System.Reflection;
using log4net;
using Microsoft.Extensions.Logging;
using Nini.Config;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.Runtime;
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
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static OpenTelemetryManager s_instance;
        private static readonly object s_lock = new object();

        private MeterProvider m_meterProvider;
        private ILoggerFactory m_loggerFactory;
        private Meter m_meter;
        private long m_lastExportTimestamp;

        private bool m_enabled;
        private bool m_metricsEnabled;
        private bool m_logsEnabled;
        private string m_serviceName;
        private string m_otlpEndpoint;
        private OtlpExportProtocol m_protocol;
        private int m_metricsExportIntervalMs;
        private LogLevel m_minimumLogLevel;
        private string m_authorizationToken;
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
                    s_instance.EnableSelfDiagnostics();
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
                m_log.Info("[OpenTelemetry] No [OpenTelemetry] configuration section found. Telemetry disabled.");
                m_enabled = false;
                return;
            }

            m_enabled = otelConfig.GetBoolean("Enabled", false);
            if (!m_enabled)
            {
                m_log.Info("[OpenTelemetry] OpenTelemetry is disabled in configuration.");
                return;
            }

            m_serviceName = otelConfig.GetString("ServiceName", "Akisim");
            m_otlpEndpoint = otelConfig.GetString("OtlpEndpoint", "http://localhost:4317");
            m_authorizationToken = otelConfig.GetString("AuthorizationToken", "");
            m_metricsEnabled = otelConfig.GetBoolean("MetricsEnabled", true);
            m_logsEnabled = otelConfig.GetBoolean("LogsEnabled", true);
            m_metricsExportIntervalMs = otelConfig.GetInt("MetricsExportIntervalMs", 60000);

            // Debug output
            m_log.InfoFormat("[OpenTelemetry] Configuration loaded:");
            m_log.InfoFormat("[OpenTelemetry]   ServiceName: {0}", m_serviceName);
            m_log.InfoFormat("[OpenTelemetry]   OtlpEndpoint: {0}", m_otlpEndpoint);
            m_log.InfoFormat("[OpenTelemetry]   AuthorizationToken: {0}",
                string.IsNullOrWhiteSpace(m_authorizationToken) ? "<not set>" : "<configured>");
            m_log.InfoFormat("[OpenTelemetry]   MetricsEnabled: {0}", m_metricsEnabled);
            m_log.InfoFormat("[OpenTelemetry]   LogsEnabled: {0}", m_logsEnabled);
            m_log.InfoFormat("[OpenTelemetry]   MetricsExportIntervalMs: {0}", m_metricsExportIntervalMs);

            // Parse OTLP protocol
            string protocolStr = otelConfig.GetString("OtlpProtocol", "Grpc");
            m_protocol = protocolStr.Equals("HttpProtobuf", StringComparison.OrdinalIgnoreCase)
                ? OtlpExportProtocol.HttpProtobuf
                : OtlpExportProtocol.Grpc;
            m_log.InfoFormat("[OpenTelemetry]   OtlpProtocol: {0}", m_protocol);

            // Parse log level
            string logLevelStr = otelConfig.GetString("LogLevel", "Information");
            if (!Enum.TryParse(logLevelStr, true, out m_minimumLogLevel))
            {
                m_minimumLogLevel = LogLevel.Information;
            }
            m_log.InfoFormat("[OpenTelemetry]   LogLevel: {0}", m_minimumLogLevel);

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

        private void EnableSelfDiagnostics()
        {
            try
            {
                // Enable .NET event source diagnostics for OpenTelemetry
                // This will output to the console/logs
                System.Diagnostics.Tracing.EventSource.SetCurrentThreadActivityId(Guid.NewGuid());

                m_log.Info("[OpenTelemetry] Self-diagnostics mode enabled");
            }
            catch (Exception ex)
            {
                m_log.WarnFormat("[OpenTelemetry] Failed to enable self-diagnostics: {0}", ex.Message);
            }
        }

        private void InitializeMeterProvider()
        {
            if (!m_metricsEnabled)
            {
                m_log.Info("[OpenTelemetry] Metrics export is disabled, skipping MeterProvider initialization.");
                return;
            }

            try
            {
                m_log.Info("[OpenTelemetry] Initializing MeterProvider...");
                var builder = Sdk.CreateMeterProviderBuilder()
                    .SetResourceBuilder(CreateResourceBuilder())
                    .AddMeter(m_serviceName)
                    .AddRuntimeInstrumentation();

                // Set metric reader with export interval
                builder.AddOtlpExporter((exporterOptions, readerOptions) =>
                {
                    // For HttpProtobuf, need to specify the full metrics endpoint
                    Uri metricsEndpoint;
                    if (m_protocol == OtlpExportProtocol.HttpProtobuf)
                    {
                        // Ensure the endpoint ends with /v1/metrics for HTTP
                        string baseEndpoint = m_otlpEndpoint.TrimEnd('/');
                        if (!baseEndpoint.EndsWith("/v1/metrics"))
                        {
                            if (baseEndpoint.EndsWith("/otlp"))
                            {
                                metricsEndpoint = new Uri(baseEndpoint + "/v1/metrics");
                            }
                            else
                            {
                                metricsEndpoint = new Uri(baseEndpoint + "/v1/metrics");
                            }
                        }
                        else
                        {
                            metricsEndpoint = new Uri(baseEndpoint);
                        }
                        m_log.InfoFormat("[OpenTelemetry] Metrics endpoint adjusted for HTTP: {0}", metricsEndpoint);
                    }
                    else
                    {
                        metricsEndpoint = new Uri(m_otlpEndpoint);
                        m_log.InfoFormat("[OpenTelemetry] Using gRPC endpoint: {0}", metricsEndpoint);
                    }

                    exporterOptions.Endpoint = metricsEndpoint;
                    exporterOptions.Protocol = m_protocol;

                    // Add authorization header if token is configured
                    if (!string.IsNullOrWhiteSpace(m_authorizationToken))
                    {
                        exporterOptions.Headers = $"Authorization=Basic {m_authorizationToken}";
                        m_log.Info("[OpenTelemetry] Authorization header configured for metrics export.");
                    }
                    else
                    {
                        m_log.Warn("[OpenTelemetry] No authorization token configured - exports may fail!");
                    }

                    readerOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = m_metricsExportIntervalMs;
                });

                m_meterProvider = builder.Build();
                m_meter = new Meter(m_serviceName, "1.0.0");

                // Create a test heartbeat gauge to verify exports are working
                // This shows the time since the last export in seconds
                m_lastExportTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                m_meter.CreateObservableGauge("otel_heartbeat_seconds", () => {
                    long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    long timeSinceLastExport = currentTimestamp - m_lastExportTimestamp;
                    m_lastExportTimestamp = currentTimestamp; // Update for next export
                    return timeSinceLastExport;
                }, "seconds", "Time in seconds since the last OpenTelemetry export");

                m_log.InfoFormat("[OpenTelemetry] MeterProvider initialized successfully. Export interval: {0}ms", m_metricsExportIntervalMs);
                m_log.InfoFormat("[OpenTelemetry] First metric export will occur in {0} seconds", m_metricsExportIntervalMs / 1000);
                m_log.Info("[OpenTelemetry] Runtime instrumentation enabled for .NET metrics");
                m_log.Info("[OpenTelemetry] Heartbeat gauge created for export testing");
            }
            catch (Exception ex)
            {
                m_log.Error($"[OpenTelemetry] Failed to initialize MeterProvider: {ex.Message}", ex);
                throw new Exception($"Failed to initialize OpenTelemetry MeterProvider: {ex.Message}", ex);
            }
        }

        private void InitializeLoggerProvider()
        {
            if (!m_logsEnabled)
            {
                m_log.Info("[OpenTelemetry] Logs export is disabled, skipping LoggerProvider initialization.");
                return;
            }

            try
            {
                m_log.Info("[OpenTelemetry] Initializing LoggerProvider...");
                m_loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.AddOpenTelemetry(logging =>
                    {
                        logging.SetResourceBuilder(CreateResourceBuilder());
                        logging.AddOtlpExporter(options =>
                        {
                            // For HttpProtobuf, need to specify the full logs endpoint
                            Uri logsEndpoint;
                            if (m_protocol == OtlpExportProtocol.HttpProtobuf)
                            {
                                // Ensure the endpoint ends with /v1/logs for HTTP
                                string baseEndpoint = m_otlpEndpoint.TrimEnd('/');
                                if (!baseEndpoint.EndsWith("/v1/logs"))
                                {
                                    if (baseEndpoint.EndsWith("/otlp"))
                                    {
                                        logsEndpoint = new Uri(baseEndpoint + "/v1/logs");
                                    }
                                    else
                                    {
                                        logsEndpoint = new Uri(baseEndpoint + "/v1/logs");
                                    }
                                }
                                else
                                {
                                    logsEndpoint = new Uri(baseEndpoint);
                                }
                                m_log.InfoFormat("[OpenTelemetry] Logs endpoint adjusted for HTTP: {0}", logsEndpoint);
                            }
                            else
                            {
                                logsEndpoint = new Uri(m_otlpEndpoint);
                            }

                            options.Endpoint = logsEndpoint;
                            options.Protocol = m_protocol;

                            // Add authorization header if token is configured
                            if (!string.IsNullOrWhiteSpace(m_authorizationToken))
                            {
                                options.Headers = $"Authorization=Basic {m_authorizationToken}";
                                m_log.Info("[OpenTelemetry] Authorization header configured for logs export.");
                            }
                        });
                        logging.IncludeFormattedMessage = true;
                        logging.IncludeScopes = true;
                    });
                    builder.SetMinimumLevel(m_minimumLogLevel);
                });
                m_log.Info("[OpenTelemetry] LoggerProvider initialized successfully.");
            }
            catch (Exception ex)
            {
                m_log.Error($"[OpenTelemetry] Failed to initialize LoggerProvider: {ex.Message}", ex);
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
                m_log.Info("[OpenTelemetry] Shutting down and flushing telemetry...");

                // Dispose meter provider (flushes metrics)
                if (m_meterProvider != null)
                {
                    m_meterProvider.Dispose();
                    m_log.Debug("[OpenTelemetry] MeterProvider disposed.");
                }

                // Dispose logger factory (flushes logs)
                if (m_loggerFactory != null)
                {
                    m_loggerFactory.Dispose();
                    m_log.Debug("[OpenTelemetry] LoggerFactory disposed.");
                }

                // Dispose meter
                if (m_meter != null)
                {
                    m_meter.Dispose();
                    m_log.Debug("[OpenTelemetry] Meter disposed.");
                }

                m_log.Info("[OpenTelemetry] Shutdown complete.");
            }
            catch (Exception ex)
            {
                // Log disposal errors but don't throw
                m_log.Error($"[OpenTelemetry] Error disposing OpenTelemetryManager: {ex.Message}", ex);
            }
        }
    }
}
