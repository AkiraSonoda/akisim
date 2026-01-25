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
using log4net.Appender;
using log4net.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

namespace OpenSim.Framework.Console
{
    /// <summary>
    /// Writes log information to OpenTelemetry
    /// </summary>
    public class OpenTelemetryAppender : AppenderSkeleton
    {
        private static ILoggerFactory s_loggerFactory;
        private static Microsoft.Extensions.Logging.ILogger s_logger;
        private static bool s_initialized = false;
        private static readonly object s_lock = new object();

        public string Endpoint { get; set; } = "http://localhost:4317";
        public string ServiceName { get; set; } = "OpenSimulator";
        public string ServiceVersion { get; set; } = "1.0.0";

        public override void ActivateOptions()
        {
            base.ActivateOptions();
            InitializeOpenTelemetry();
        }

        private void InitializeOpenTelemetry()
        {
            lock (s_lock)
            {
                if (s_initialized)
                    return;

                try
                {
                    var serviceCollection = new ServiceCollection();

                    serviceCollection.AddLogging(builder =>
                    {
                        builder.AddOpenTelemetry(options =>
                        {
                            options.SetResourceBuilder(
                                ResourceBuilder.CreateDefault()
                                    .AddService(serviceName: ServiceName, serviceVersion: ServiceVersion));

                            options.AddOtlpExporter(exporterOptions =>
                            {
                                exporterOptions.Endpoint = new Uri(Endpoint);
                            });
                        });
                    });

                    var serviceProvider = serviceCollection.BuildServiceProvider();
                    s_loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                    s_logger = s_loggerFactory.CreateLogger("OpenSimulator");

                    s_initialized = true;
                }
                catch (Exception ex)
                {
                    ErrorHandler.Error($"Failed to initialize OpenTelemetry: {ex.Message}", ex);
                }
            }
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            if (!s_initialized || s_logger == null)
                return;

            try
            {
                var logLevel = MapLogLevel(loggingEvent.Level);
                var message = RenderLoggingEvent(loggingEvent);
                var exception = loggingEvent.ExceptionObject;

                // Build state dictionary with location information
                var state = new Dictionary<string, object>
                {
                    ["Message"] = message,
                    ["LoggerName"] = loggingEvent.LoggerName ?? "Unknown"
                };

                var locationInfo = loggingEvent.LocationInformation;
                if (locationInfo != null)
                {
                    if (!string.IsNullOrEmpty(locationInfo.ClassName))
                        state["SourceContext"] = locationInfo.ClassName;
                    if (!string.IsNullOrEmpty(locationInfo.MethodName))
                        state["MethodName"] = locationInfo.MethodName;
                    if (!string.IsNullOrEmpty(locationInfo.LineNumber))
                        state["LineNumber"] = locationInfo.LineNumber;
                }

                // Add custom properties from log4net
                var properties = loggingEvent.GetProperties();
                if (properties != null)
                {
                    foreach (var key in properties.GetKeys())
                    {
                        var value = properties[key];
                        if (value != null && !state.ContainsKey(key))
                        {
                            state[key] = value.ToString();
                        }
                    }
                }

                // Log using Microsoft.Extensions.Logging
                s_logger.Log(logLevel, exception, message, state);
            }
            catch (Exception ex)
            {
                ErrorHandler.Error($"Error appending to OpenTelemetry: {ex.Message}", ex);
            }
        }

        private Microsoft.Extensions.Logging.LogLevel MapLogLevel(Level level)
        {
            if (level >= Level.Fatal)
                return Microsoft.Extensions.Logging.LogLevel.Critical;
            if (level >= Level.Error)
                return Microsoft.Extensions.Logging.LogLevel.Error;
            if (level >= Level.Warn)
                return Microsoft.Extensions.Logging.LogLevel.Warning;
            if (level >= Level.Info)
                return Microsoft.Extensions.Logging.LogLevel.Information;
            if (level >= Level.Debug)
                return Microsoft.Extensions.Logging.LogLevel.Debug;
            return Microsoft.Extensions.Logging.LogLevel.Trace;
        }

        protected override void OnClose()
        {
            lock (s_lock)
            {
                if (s_loggerFactory != null)
                {
                    s_loggerFactory.Dispose();
                    s_loggerFactory = null;
                    s_logger = null;
                    s_initialized = false;
                }
            }
            base.OnClose();
        }
    }
}
