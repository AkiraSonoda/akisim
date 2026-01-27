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
using System.Linq;
using Nini.Config;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Exporter;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;

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
        private ILoggerFactory m_loggerFactory;
        private bool m_disposed = false;

        // Configuration properties
        public string Endpoint { get; set; } = "https://otlp-gateway-prod-us-central-0.grafana.net/otlp";
        public string ServiceName { get; set; } = "OpenSimulator";
        public string ServiceVersion { get; set; } = "1.0.0";
        public string GrafanaInstanceId { get; set; } = "";
        public string GrafanaApiKey { get; set; } = "";
        public string OriginalAuthToken { get; set; } = ""; // Store original base64 token
        public int ExportIntervalMilliseconds { get; set; } = 60000; // 60 seconds default
        public string Protocol { get; set; } = "Grpc"; // Grpc or HttpProtobuf
        public bool EnableConsoleExporter { get; set; } = false; // For debugging

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
            var logger = log4net.LogManager.GetLogger(GetType());
            
            if (configSource == null)
            {
                logger.Info("[OPENTELEMETRY]: No configuration source provided. Metrics disabled.");
                return;
            }

            var config = configSource.Configs["OpenTelemetry"];
            if (config == null)
            {
                logger.Info("[OPENTELEMETRY]: OpenTelemetry section not found in configuration. Metrics disabled.");
                return;
            }

            bool enabled = config.GetBoolean("Enabled", false);
            if (!enabled)
            {
                logger.Info("[OPENTELEMETRY]: OpenTelemetry is disabled in configuration.");
                return;
            }

            logger.Info("[OPENTELEMETRY]: Loading OpenTelemetry configuration...");

            // Read configuration - support both old and new key names for backward compatibility
            string oldEndpoint = config.GetString("Endpoint", null);
            string newEndpoint = config.GetString("OtlpEndpoint", null);
            Endpoint = newEndpoint ?? oldEndpoint ?? Endpoint;
            
            logger.InfoFormat("[OPENTELEMETRY]: Service Name: {0}, Version: {1}", 
                ServiceName = config.GetString("ServiceName", ServiceName),
                ServiceVersion = config.GetString("ServiceVersion", ServiceVersion));
            
            logger.InfoFormat("[OPENTELEMETRY]: OTLP Endpoint: {0}", Endpoint);
            
            // Read protocol configuration (Grpc or HttpProtobuf)
            string protocolStr = config.GetString("OtlpProtocol", "Grpc");
            logger.InfoFormat("[OPENTELEMETRY]: OTLP Protocol: {0}", protocolStr);
            
            // Handle authorization token - can be base64-encoded or plain text
            string authToken = config.GetString("AuthorizationToken", "");
            if (!string.IsNullOrEmpty(authToken))
            {
                logger.Info("[OPENTELEMETRY]: Using AuthorizationToken for authentication");

                // Check if it's base64 encoded (for Grafana Cloud Basic auth)
                // Base64 strings typically contain only A-Z, a-z, 0-9, +, /, and = for padding
                bool looksLikeBase64 = authToken.Length > 20 &&
                                       authToken.All(c => char.IsLetterOrDigit(c) || c == '+' || c == '/' || c == '=');

                if (looksLikeBase64 && !authToken.Contains(':'))
                {
                    // Likely base64-encoded, store original and decode for logging
                    try
                    {
                        byte[] data = Convert.FromBase64String(authToken);
                        string decoded = System.Text.Encoding.UTF8.GetString(data);
                        string[] parts = decoded.Split(new[] {':'}, 2);
                        if (parts.Length == 2)
                        {
                            // Store the original base64 token to use directly in Authorization header
                            OriginalAuthToken = authToken;
                            GrafanaInstanceId = parts[0];
                            GrafanaApiKey = parts[1];
                            logger.InfoFormat("[OPENTELEMETRY]: Decoded base64 token - Instance ID: {0}", GrafanaInstanceId);
                            logger.Info("[OPENTELEMETRY]: Will use original base64 token for authentication (no double-encoding)");
                        }
                        else
                        {
                            // Base64 but not in instanceId:apiKey format, use as-is
                            OriginalAuthToken = authToken;
                            GrafanaInstanceId = authToken;
                            GrafanaApiKey = "";
                            logger.Info("[OPENTELEMETRY]: Using base64-encoded token directly");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Not valid base64, treat as plain token
                        GrafanaInstanceId = authToken;
                        GrafanaApiKey = "";
                        logger.WarnFormat("[OPENTELEMETRY]: Failed to decode token as base64: {0}", ex.Message);
                    }
                }
                else if (authToken.Contains(':'))
                {
                    // Plain text instanceId:apiKey format - need to encode it
                    string[] parts = authToken.Split(new[] {':'}, 2);
                    GrafanaInstanceId = parts[0];
                    GrafanaApiKey = parts.Length > 1 ? parts[1] : "";
                    // Don't store OriginalAuthToken - we'll encode it later
                    logger.InfoFormat("[OPENTELEMETRY]: Plain text authentication - Instance ID: {0}", GrafanaInstanceId);
                }
                else
                {
                    // Single token format
                    GrafanaInstanceId = authToken;
                    GrafanaApiKey = "";
                    logger.Info("[OPENTELEMETRY]: Using single token authentication");
                }
            }
            else
            {
                // Fallback to old format if AuthorizationToken not present
                GrafanaInstanceId = config.GetString("GrafanaInstanceId", GrafanaInstanceId);
                GrafanaApiKey = config.GetString("GrafanaApiKey", GrafanaApiKey);
                if (!string.IsNullOrEmpty(GrafanaInstanceId))
                {
                    logger.Info("[OPENTELEMETRY]: Using legacy Grafana Cloud authentication");
                }
                else
                {
                    logger.Warn("[OPENTELEMETRY]: No authentication configured. Export may fail if endpoint requires authentication.");
                }
            }
            
            ExportIntervalMilliseconds = config.GetInt("ExportIntervalMilliseconds", ExportIntervalMilliseconds);
            logger.InfoFormat("[OPENTELEMETRY]: Metrics export interval: {0} ms", ExportIntervalMilliseconds);

            // Check if console exporter should be enabled (for debugging)
            EnableConsoleExporter = config.GetBoolean("EnableConsoleExporter", EnableConsoleExporter);
            if (EnableConsoleExporter)
            {
                logger.Warn("[OPENTELEMETRY]: Console exporter is ENABLED - metrics will be printed to console (for debugging only)");
            }

            // Store protocol for use in Start() method
            Protocol = protocolStr;

            if (autoStart)
            {
                logger.Info("[OPENTELEMETRY]: Starting OpenTelemetry metrics pipeline...");
                Start();
            }
            else
            {
                logger.Info("[OPENTELEMETRY]: Configuration loaded. Call Start() to begin metrics collection.");
            }
        }

        /// <summary>
        /// Initialize and start the OpenTelemetry metrics pipeline
        /// </summary>
        public void Start()
        {
            var logger = log4net.LogManager.GetLogger(GetType());
            
            if (m_meterProvider != null)
            {
                logger.Info("[OPENTELEMETRY]: Metrics pipeline already started.");
                return;
            }

            try
            {
                // Enable OpenTelemetry internal diagnostics
                logger.Info("[OPENTELEMETRY]: Enabling internal diagnostics...");
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2Support", true);

                // Set up diagnostic listener for OpenTelemetry internal events
                var listener = new ActivityListener
                {
                    ShouldListenTo = _ => true,
                    Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
                };
                ActivitySource.AddActivityListener(listener);

                logger.Info("[OPENTELEMETRY]: Building resource builder...");
                var resourceBuilder = ResourceBuilder.CreateDefault()
                    .AddService(
                        serviceName: ServiceName,
                        serviceVersion: ServiceVersion)
                    .AddAttributes(new[]
                    {
                        new KeyValuePair<string, object>("deployment.environment", Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "production"),
                        new KeyValuePair<string, object>("host.name", Environment.MachineName)
                    });

                logger.Info("[OPENTELEMETRY]: Configuring meter provider...");
                var builder = Sdk.CreateMeterProviderBuilder()
                    .SetResourceBuilder(resourceBuilder)
                    .AddRuntimeInstrumentation()
                    .AddMeter(m_meter.Name);

                // Add console exporter if enabled (for debugging)
                if (EnableConsoleExporter)
                {
                    logger.Info("[OPENTELEMETRY]: Configuring console exporter for debugging...");
                    builder = builder.AddConsoleExporter((exporterOptions, metricReaderOptions) =>
                    {
                        metricReaderOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 10000; // 10 seconds for console
                        logger.Info("[OPENTELEMETRY]: Console exporter configured (10 second intervals)");
                    });
                }

                logger.Info("[OPENTELEMETRY]: Configuring OTLP exporter...");
                builder = builder.AddOtlpExporter((exporterOptions, metricReaderOptions) =>
                {
                    // Adjust endpoint based on protocol
                    string endpoint = Endpoint;
                    if (Protocol.Equals("HttpProtobuf", StringComparison.OrdinalIgnoreCase))
                    {
                        // HttpProtobuf needs /v1/metrics path for Grafana Cloud
                        if (!endpoint.EndsWith("/v1/metrics"))
                        {
                            endpoint = endpoint.TrimEnd('/') + "/v1/metrics";
                            logger.InfoFormat("[OPENTELEMETRY]: Adjusted endpoint for HttpProtobuf: {0}", endpoint);
                        }
                    }

                    exporterOptions.Endpoint = new Uri(endpoint);
                    logger.InfoFormat("[OPENTELEMETRY]: OTLP Endpoint configured: {0}", endpoint);

                    // Add Grafana Cloud authentication headers if configured
                    if (!string.IsNullOrEmpty(OriginalAuthToken))
                    {
                        // Use the original base64 token directly (no double-encoding!)
                        string headerValue = "Authorization=Basic " + OriginalAuthToken;
                        exporterOptions.Headers = headerValue;
                        logger.InfoFormat("[OPENTELEMETRY]: Basic authentication configured for instance {0} (using original token)", GrafanaInstanceId);
                        logger.InfoFormat("[OPENTELEMETRY]: Auth header (first 50 chars): {0}...", headerValue.Substring(0, Math.Min(50, headerValue.Length)));
                    }
                    else if (!string.IsNullOrEmpty(GrafanaInstanceId) && !string.IsNullOrEmpty(GrafanaApiKey))
                    {
                        // Plain text credentials - encode them
                        string credentials = GrafanaInstanceId + ":" + GrafanaApiKey;
                        string base64Credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(credentials));
                        string headerValue = "Authorization=Basic " + base64Credentials;
                        exporterOptions.Headers = headerValue;
                        logger.InfoFormat("[OPENTELEMETRY]: Basic authentication configured for instance {0} (encoded)", GrafanaInstanceId);
                        logger.InfoFormat("[OPENTELEMETRY]: Auth header (first 50 chars): {0}...", headerValue.Substring(0, Math.Min(50, headerValue.Length)));
                    }
                    else if (!string.IsNullOrEmpty(GrafanaInstanceId))
                    {
                        // Single token - use as-is
                        string headerValue = "Authorization=Basic " + GrafanaInstanceId;
                        exporterOptions.Headers = headerValue;
                        logger.Info("[OPENTELEMETRY]: Single token authentication configured");
                        logger.InfoFormat("[OPENTELEMETRY]: Auth header (first 50 chars): {0}...", headerValue.Substring(0, Math.Min(50, headerValue.Length)));
                    }

                    // Set protocol based on configuration
                    if (Protocol.Equals("HttpProtobuf", StringComparison.OrdinalIgnoreCase))
                    {
                        exporterOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                    }
                    else
                    {
                        exporterOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                    }
                    logger.InfoFormat("[OPENTELEMETRY]: Using protocol: {0}", exporterOptions.Protocol);

                    // Configure export interval
                    metricReaderOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = ExportIntervalMilliseconds;
                    logger.InfoFormat("[OPENTELEMETRY]: Export interval: {0} ms", ExportIntervalMilliseconds);
                });

                logger.Info("[OPENTELEMETRY]: Building and starting meter provider...");
                m_meterProvider = builder.Build();

                // Configure OpenTelemetry Logging
                logger.Info("[OPENTELEMETRY]: Configuring log export...");
                m_loggerFactory = LoggerFactory.Create(loggingBuilder =>
                {
                    loggingBuilder.AddOpenTelemetry(options =>
                    {
                        options.SetResourceBuilder(resourceBuilder);

                        // Add OTLP exporter for logs
                        options.AddOtlpExporter((exporterOptions) =>
                        {
                            // Configure endpoint
                            string logEndpoint = Endpoint;
                            if (Protocol.Equals("HttpProtobuf", StringComparison.OrdinalIgnoreCase))
                            {
                                if (!logEndpoint.EndsWith("/v1/logs"))
                                {
                                    logEndpoint = logEndpoint.TrimEnd('/') + "/v1/logs";
                                }
                            }
                            exporterOptions.Endpoint = new Uri(logEndpoint);

                            // Set authentication
                            if (!string.IsNullOrEmpty(OriginalAuthToken))
                            {
                                string headerValue = "Authorization=Basic " + OriginalAuthToken;
                                exporterOptions.Headers = headerValue;
                            }
                            else if (!string.IsNullOrEmpty(GrafanaInstanceId) && !string.IsNullOrEmpty(GrafanaApiKey))
                            {
                                string credentials = GrafanaInstanceId + ":" + GrafanaApiKey;
                                string base64Credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(credentials));
                                exporterOptions.Headers = "Authorization=Basic " + base64Credentials;
                            }

                            // Set protocol
                            if (Protocol.Equals("HttpProtobuf", StringComparison.OrdinalIgnoreCase))
                            {
                                exporterOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                            }
                            else
                            {
                                exporterOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                            }

                            logger.InfoFormat("[OPENTELEMETRY]: Log exporter configured - Endpoint: {0}, Protocol: {1}",
                                logEndpoint, exporterOptions.Protocol);
                        });

                        // Configure logging behavior
                        options.IncludeScopes = true;
                        options.IncludeFormattedMessage = true;
                    });
                });

                // Share logger factory with log4net appender
                // The OpenTelemetryAppender in OpenSim.Framework.Console will use this
                try
                {
                    var appenderType = Type.GetType("OpenSim.Framework.Console.OpenTelemetryAppender, OpenSim.Framework.Console");
                    if (appenderType != null)
                    {
                        var setMethod = appenderType.GetMethod("SetLoggerFactory",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        if (setMethod != null)
                        {
                            setMethod.Invoke(null, new object[] { m_loggerFactory });
                            logger.Info("[OPENTELEMETRY]: ✓ Log export configured and shared with log4net appender");
                        }
                        else
                        {
                            logger.Warn("[OPENTELEMETRY]: OpenTelemetryAppender.SetLoggerFactory method not found");
                        }
                    }
                    else
                    {
                        logger.Warn("[OPENTELEMETRY]: OpenTelemetryAppender type not found - logs will not be exported");
                    }
                }
                catch (Exception appenderEx)
                {
                    logger.Warn("[OPENTELEMETRY]: Could not initialize log4net appender: " + appenderEx.Message);
                }

                logger.InfoFormat("[OPENTELEMETRY]: ✓ OpenTelemetry metrics initialized successfully!");
                logger.InfoFormat("[OPENTELEMETRY]: ✓ Service: {0} v{1}", ServiceName, ServiceVersion);
                logger.InfoFormat("[OPENTELEMETRY]: ✓ Exporting to: {0}", Endpoint);
                logger.InfoFormat("[OPENTELEMETRY]: ✓ Metrics will be exported every {0} ms", ExportIntervalMilliseconds);
                logger.Info("[OPENTELEMETRY]: ✓ Runtime metrics collection enabled");
                logger.Info("[OPENTELEMETRY]: ✓ Custom OpenSimulator metrics collection enabled");

                // Record a test metric immediately to verify pipeline is working
                logger.Info("[OPENTELEMETRY]: Recording test metric to verify pipeline...");
                m_avatarCount?.Add(1, new KeyValuePair<string, object>("test", "startup"));

                // Trigger immediate export for testing
                logger.Info("[OPENTELEMETRY]: Triggering immediate test export...");
                System.Threading.Tasks.Task.Run(async () =>
                {
                    await System.Threading.Tasks.Task.Delay(2000); // Wait 2 seconds
                    logger.Info("[OPENTELEMETRY]: Executing test export...");
                    try
                    {
                        m_meterProvider?.ForceFlush();
                        logger.Info("[OPENTELEMETRY]: ✓ Test export completed");
                    }
                    catch (Exception testEx)
                    {
                        logger.Error("[OPENTELEMETRY]: ✗ Test export failed", testEx);
                    }
                });
            }
            catch (Exception ex)
            {
                logger.Error("[OPENTELEMETRY]: ✗ Failed to initialize OpenTelemetry metrics", ex);
                logger.ErrorFormat("[OPENTELEMETRY]: ✗ Error: {0}", ex.Message);
                if (ex.InnerException != null)
                {
                    logger.ErrorFormat("[OPENTELEMETRY]: ✗ Inner exception: {0}", ex.InnerException.Message);
                }
            }
        }

        /// <summary>
        /// Manually trigger a metrics export (for testing/diagnostics)
        /// </summary>
        public void TestExport()
        {
            var logger = log4net.LogManager.GetLogger(GetType());
            
            if (m_meterProvider == null)
            {
                logger.Warn("[OPENTELEMETRY]: Cannot test export - meter provider not initialized");
                return;
            }
            
            try
            {
                logger.Info("[OPENTELEMETRY]: Manually triggering metrics export...");
                m_meterProvider.ForceFlush();
                logger.Info("[OPENTELEMETRY]: Export triggered successfully");
            }
            catch (Exception ex)
            {
                logger.Error("[OPENTELEMETRY]: Failed to export metrics", ex);
                logger.ErrorFormat("[OPENTELEMETRY]: Error details: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Stop the metrics and logging pipeline and flush any pending data
        /// </summary>
        public void Stop()
        {
            var logger = log4net.LogManager.GetLogger(GetType());

            if (m_loggerFactory != null)
            {
                logger.Info("[OPENTELEMETRY]: Disposing logger factory...");
                m_loggerFactory.Dispose();
                m_loggerFactory = null;
                logger.Info("[OPENTELEMETRY]: Logging pipeline stopped");
            }

            if (m_meterProvider != null)
            {
                logger.Info("[OPENTELEMETRY]: Flushing pending metrics before shutdown...");
                m_meterProvider.ForceFlush();
                m_meterProvider.Dispose();
                m_meterProvider = null;
                logger.Info("[OPENTELEMETRY]: Metrics pipeline stopped");
            }
        }

        // Public methods to record custom metrics
        public void RecordAvatarConnection()
        {
            var logger = log4net.LogManager.GetLogger(GetType());
            try
            {
                m_avatarCount?.Add(1);
                logger.Debug("[OPENTELEMETRY]: Recorded avatar connection metric");
            }
            catch (Exception ex)
            {
                logger.Error("[OPENTELEMETRY]: Failed to record avatar connection metric", ex);
            }
        }

        public void RecordScriptExecution()
        {
            var logger = log4net.LogManager.GetLogger(GetType());
            try
            {
                m_scriptExecutions?.Add(1);
                logger.Debug("[OPENTELEMETRY]: Recorded script execution metric");
            }
            catch (Exception ex)
            {
                logger.Error("[OPENTELEMETRY]: Failed to record script execution metric", ex);
            }
        }

        public void RecordFrameTime(double durationMs)
        {
            var logger = log4net.LogManager.GetLogger(GetType());
            try
            {
                m_frameTime?.Record(durationMs);
                logger.DebugFormat("[OPENTELEMETRY]: Recorded frame time metric: {0}ms", durationMs);
            }
            catch (Exception ex)
            {
                logger.Error("[OPENTELEMETRY]: Failed to record frame time metric", ex);
            }
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
