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
using OpenSim.Framework.Monitoring;

namespace OpenSim.Framework.Console
{
    /// <summary>
    /// Bridges log4net logging to OpenTelemetry logs.
    /// </summary>
    public class OpenTelemetryAppender : AppenderSkeleton
    {
        protected override void Append(LoggingEvent loggingEvent)
        {
            if (loggingEvent == null)
                return;

            // Skip if OpenTelemetry is not initialized or logs are disabled
            if (!OpenTelemetryManager.IsInitialized || !OpenTelemetryManager.Instance.LogsEnabled)
                return;

            try
            {
                // Create logger for this category
                string loggerName = loggingEvent.LoggerName ?? "Unknown";
                var logger = OpenTelemetryManager.Instance.CreateLogger(loggerName);

                if (logger == null)
                    return;

                // Map log4net level to Microsoft.Extensions.Logging.LogLevel
                LogLevel level = MapLogLevel(loggingEvent.Level);

                // Build log message
                string message = loggingEvent.RenderedMessage ?? string.Empty;

                // Create state dictionary with log context
                var state = new List<KeyValuePair<string, object>>
                {
                    new KeyValuePair<string, object>("Timestamp", loggingEvent.TimeStamp),
                    new KeyValuePair<string, object>("ThreadName", loggingEvent.ThreadName ?? string.Empty),
                    new KeyValuePair<string, object>("LoggerName", loggerName),
                    new KeyValuePair<string, object>("Level", loggingEvent.Level.Name)
                };

                // Add location information if available
                if (loggingEvent.LocationInformation != null)
                {
                    state.Add(new KeyValuePair<string, object>("SourceFile", loggingEvent.LocationInformation.FileName ?? string.Empty));
                    state.Add(new KeyValuePair<string, object>("SourceLine", loggingEvent.LocationInformation.LineNumber ?? string.Empty));
                    state.Add(new KeyValuePair<string, object>("MethodName", loggingEvent.LocationInformation.MethodName ?? string.Empty));
                    state.Add(new KeyValuePair<string, object>("ClassName", loggingEvent.LocationInformation.ClassName ?? string.Empty));
                }

                // Add properties if present
                if (loggingEvent.Properties != null && loggingEvent.Properties.Count > 0)
                {
                    foreach (string key in loggingEvent.Properties.GetKeys())
                    {
                        object value = loggingEvent.Properties[key];
                        if (value != null)
                        {
                            state.Add(new KeyValuePair<string, object>($"Property.{key}", value));
                        }
                    }
                }

                // Log with or without exception
                Exception exception = loggingEvent.ExceptionObject;
                if (exception != null)
                {
                    logger.Log(level, exception, message);
                }
                else
                {
                    logger.Log(level, message);
                }
            }
            catch (Exception ex)
            {
                // Don't throw exceptions from the appender - just write to console
                System.Console.WriteLine($"OpenTelemetryAppender error: {ex.Message}");
            }
        }

        /// <summary>
        /// Maps log4net Level to Microsoft.Extensions.Logging.LogLevel
        /// </summary>
        private static LogLevel MapLogLevel(Level level)
        {
            if (level == null)
                return LogLevel.None;

            if (level >= Level.Fatal)
                return LogLevel.Critical;
            if (level >= Level.Error)
                return LogLevel.Error;
            if (level >= Level.Warn)
                return LogLevel.Warning;
            if (level >= Level.Info)
                return LogLevel.Information;
            if (level >= Level.Debug)
                return LogLevel.Debug;

            return LogLevel.Trace;
        }
    }
}
