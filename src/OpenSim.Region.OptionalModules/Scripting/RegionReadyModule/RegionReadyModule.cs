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
using System.Reflection;
using System.Runtime;
using System.Net;
using System.IO;
using System.Text;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using System.Net.Http;
using System.Threading;

namespace OpenSim.Region.OptionalModules.Scripting.RegionReady
{
    public class RegionReadyModule : IRegionReadyModule, INonSharedRegionModule
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private IConfig m_config = null;
        private bool m_firstEmptyCompileQueue;
        private bool m_oarFileLoading;
        private bool m_lastOarLoadedOk;
        private int m_channelNotify = -1000;
        private bool m_enabled = false;
        private bool m_disable_logins;
        private string m_uri = string.Empty;

        Scene m_scene;

        #region INonSharedRegionModule interface

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void Initialise(IConfigSource config)
        {
            if(m_log.IsDebugEnabled) m_log.Debug("Initializing RegionReadyModule for region startup coordination and readiness notifications");

            m_config = config.Configs["RegionReady"];
            if (m_config != null)
            {
                m_enabled = m_config.GetBoolean("enabled", false);

                if (m_enabled)
                {
                    m_channelNotify = m_config.GetInt("channel_notify", m_channelNotify);
                    m_disable_logins = m_config.GetBoolean("login_disable", false);
                    m_uri = m_config.GetString("alert_uri",string.Empty);

                    if(m_log.IsDebugEnabled) m_log.DebugFormat("RegionReadyModule configuration: channel_notify={0}, login_disable={1}, alert_uri={2}",
                        m_channelNotify, m_disable_logins, !string.IsNullOrEmpty(m_uri) ? "configured" : "not configured");
                }
                else
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("RegionReadyModule disabled - set enabled = true in [RegionReady] to enable region readiness coordination");
                }
            }
            else
            {
                if(m_log.IsDebugEnabled) m_log.Debug("RegionReadyModule disabled - no [RegionReady] configuration section found");
            }

            if (m_enabled)
            {
                if(m_log.IsDebugEnabled) m_log.Debug("RegionReadyModule initialized successfully - region startup coordination enabled");
            }
        }

        public void AddRegion(Scene scene)
        {
            if (!m_enabled)
                return;

            if(m_log.IsDebugEnabled) m_log.DebugFormat("Adding RegionReadyModule to region {0} - setting up startup coordination and event handlers", scene.RegionInfo.RegionName);

            m_scene = scene;

            m_scene.RegisterModuleInterface<IRegionReadyModule>(this);

            m_firstEmptyCompileQueue = true;
            m_oarFileLoading = false;
            m_lastOarLoadedOk = true;

            m_scene.EventManager.OnOarFileLoaded += OnOarFileLoaded;

            if(m_log.IsDebugEnabled) m_log.DebugFormat("RegionReadyModule enabled for region {0}", scene.RegionInfo.RegionName);

            if (m_disable_logins)
            {
                m_scene.LoginLock = true;
                m_scene.EventManager.OnEmptyScriptCompileQueue += OnEmptyScriptCompileQueue;

                // This should always show up to the user but should not trigger warn/errors as these messages are
                // expected and are not simulator problems.  Ideally, there would be a status level in log4net but
                // failing that, we will print out to console instead.
                MainConsole.Instance.Output("Region {0} - LOGINS DISABLED DURING INITIALIZATION.", m_scene.Name);

                if (m_uri != string.Empty)
                {
                    if(m_log.IsDebugEnabled) m_log.DebugFormat("RegionReadyModule sending 'disabled' alert to URI: {0}", m_uri);
                    RRAlert("disabled");
                }

                if(m_log.IsInfoEnabled) m_log.InfoFormat("RegionReadyModule added to region {0} - logins disabled until scripts compile, monitoring script compilation queue", scene.RegionInfo.RegionName);
            }
            else
            {
                if(m_log.IsInfoEnabled) m_log.InfoFormat("RegionReadyModule added to region {0} - monitoring OAR loading events only", scene.RegionInfo.RegionName);
            }
        }

        public void RemoveRegion(Scene scene)
        {
            if (!m_enabled)
                return;

            if(m_log.IsDebugEnabled) m_log.DebugFormat("Removing RegionReadyModule from region {0} - cleaning up event handlers and notifications", scene.RegionInfo.RegionName);

            m_scene.EventManager.OnOarFileLoaded -= OnOarFileLoaded;

            if (m_disable_logins)
                m_scene.EventManager.OnEmptyScriptCompileQueue -= OnEmptyScriptCompileQueue;

            if (m_uri != string.Empty)
            {
                if(m_log.IsDebugEnabled) m_log.DebugFormat("RegionReadyModule sending 'shutdown' alert to URI: {0}", m_uri);
                RRAlert("shutdown");
            }

            if(m_log.IsDebugEnabled) m_log.DebugFormat("RegionReadyModule removed from region {0}", scene.RegionInfo.RegionName);
            m_scene = null;
        }

        public void Close()
        {
        }

        public void RegionLoaded(Scene scene)
        {
        }

        public string Name
        {
            get { return "RegionReadyModule"; }
        }

        #endregion

        void OnEmptyScriptCompileQueue(int numScriptsFailed, string message)
        {
            if(m_log.IsDebugEnabled) m_log.DebugFormat("RegionReadyModule script compile queue empty in region {0} - {1} scripts failed, message: {2}", m_scene.RegionInfo.RegionName, numScriptsFailed, message);

            if (m_firstEmptyCompileQueue || m_oarFileLoading)
            {
                OSChatMessage c = new()
                {
                    From = "RegionReady",
                    Message = (m_firstEmptyCompileQueue ? "server_startup," : ("oar_file_load," + (m_lastOarLoadedOk ? "1," : "0,"))) +
                        numScriptsFailed.ToString() + "," + message,
                    Channel = m_channelNotify,
                    Type = ChatTypeEnum.Region,
                    Scene = m_scene
                };

                m_firstEmptyCompileQueue = false;
                m_oarFileLoading = false;
                m_scene.Backup(false);

                if(m_log.IsInfoEnabled) m_log.InfoFormat("RegionReadyModule region {0} is ready - broadcasting message \"{1}\" on channel {2}",
                                 m_scene.RegionInfo.RegionName, c.Message, m_channelNotify);

                m_scene.EventManager.TriggerOnChatBroadcast(this, c);

                TriggerRegionReady(m_scene);
            }
            else
            {
                if(m_log.IsDebugEnabled) m_log.DebugFormat("RegionReadyModule ignoring script compile queue completion - region {0} already marked ready", m_scene.RegionInfo.RegionName);
            }
        }

        void OnOarFileLoaded(Guid requestId, List<UUID> loadedScenes, string message)
        {
            if(m_log.IsDebugEnabled) m_log.DebugFormat("RegionReadyModule OAR file loaded in region {0} - request ID: {1}, scenes loaded: {2}", m_scene.RegionInfo.RegionName, requestId, loadedScenes.Count);

            m_oarFileLoading = true;

            if (message.Length == 0)
            {
                m_lastOarLoadedOk = true;
                if(m_log.IsDebugEnabled) m_log.DebugFormat("RegionReadyModule OAR file loaded successfully - no errors reported");
            }
            else
            {
                m_log.WarnFormat("RegionReadyModule OAR file load errors in region {0}: {1}", m_scene.RegionInfo.RegionName, message);
                m_lastOarLoadedOk = false;
            }
        }

        /// <summary>
        /// This will be triggered by Scene directly if it contains no scripts on startup.  Otherwise it is triggered
        /// when the script compile queue is empty after initial region startup.
        /// </summary>
        /// <param name='scene'></param>
        public void TriggerRegionReady(IScene scene)
        {
            if(m_log.IsDebugEnabled) m_log.DebugFormat("RegionReadyModule triggering region ready for {0} - performing final initialization steps", m_scene.RegionInfo.RegionName);

            m_scene.EventManager.OnEmptyScriptCompileQueue -= OnEmptyScriptCompileQueue;
            m_scene.LoginLock = false;

            if(m_log.IsDebugEnabled) m_log.Debug("RegionReadyModule performing garbage collection and heap compaction");
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.Default;

            if (!m_scene.StartDisabled)
            {
                m_scene.LoginsEnabled = true;

                // Putting this out to console to make it eye-catching for people who are running OpenSimulator
                // without info log messages enabled.  Making this a warning is arguably misleading since it isn't a
                // warning, and monitor scripts looking for warn/error/fatal messages will received false positives.
                // Arguably, log4net needs a status log level (like Apache).
                MainConsole.Instance.Output("INITIALIZATION COMPLETE FOR {0} - LOGINS ENABLED", m_scene.Name);

                if(m_log.IsInfoEnabled) m_log.InfoFormat("RegionReadyModule enabled logins for region {0}", m_scene.RegionInfo.RegionName);
            }
            else
            {
                if(m_log.IsDebugEnabled) m_log.DebugFormat("RegionReadyModule region {0} marked ready but logins remain disabled (StartDisabled=true)", m_scene.RegionInfo.RegionName);
            }

            if(m_log.IsDebugEnabled) m_log.DebugFormat("RegionReadyModule informing neighbors that region {0} is up", m_scene.RegionInfo.RegionName);
            m_scene.SceneGridService.InformNeighborsThatRegionisUp(
                m_scene.RequestModuleInterface<INeighbourService>(), m_scene.RegionInfo);

            if (m_uri != string.Empty)
            {
                if(m_log.IsDebugEnabled) m_log.DebugFormat("RegionReadyModule sending 'enabled' alert to URI: {0}", m_uri);
                RRAlert("enabled");
            }

            m_scene.Ready = true;
            if(m_log.IsInfoEnabled) m_log.InfoFormat("RegionReadyModule region {0} is now fully ready and operational", m_scene.RegionInfo.RegionName);
        }

        public void OarLoadingAlert(string msg)
        {
            // Let's bypass this for now until some better feedback can be established
            //

//            if (msg == "load")
//            {
//                m_scene.EventManager.OnEmptyScriptCompileQueue += OnEmptyScriptCompileQueue;
//                m_scene.EventManager.OnOarFileLoaded += OnOarFileLoaded;
//                m_scene.EventManager.OnLoginsEnabled += OnLoginsEnabled;
//                m_scene.EventManager.OnRezScript  += OnRezScript;
//                m_oarFileLoading = true;
//                m_firstEmptyCompileQueue = true;
//
//                m_scene.LoginsDisabled = true;
//                m_scene.LoginLock = true;
//                if ( m_uri != string.Empty )
//                {
//                    RRAlert("loading oar");
//                    RRAlert("disabled");
//                }
//            }
        }

        public void RRAlert(string status)
        {
            if(m_log.IsDebugEnabled) m_log.DebugFormat("RegionReadyModule preparing alert for region {0} with status '{1}' to URI: {2}", m_scene.RegionInfo.RegionName, status, m_uri);

            OSDMap RRAlert = new()
            {
                ["alert"] = "region_ready",
                ["login"] = status,
                ["region_name"] = m_scene.RegionInfo.RegionName,
                ["region_id"] = m_scene.RegionInfo.RegionID
            };

            byte[] buffer;
            try
            {
                buffer = OSDParser.SerializeJsonToBytes(RRAlert); ;
                if(m_log.IsDebugEnabled) m_log.DebugFormat("RegionReadyModule serialized alert JSON: {0} bytes", buffer.Length);
            }
            catch (Exception e)
            {
                m_log.WarnFormat("RegionReadyModule exception thrown serializing alert for region {0}: {1}", m_scene.RegionInfo.RegionName, e.Message);
                return;
            }

            HttpResponseMessage responseMessage = null;
            HttpRequestMessage request = null;
            HttpClient client = null;
            try
            {
                client = WebUtil.GetNewGlobalHttpClient(-1);

                request = new(HttpMethod.Post, m_uri);
                request.Headers.ExpectContinue = false;
                request.Headers.TransferEncodingChunked = false;
                request.Headers.TryAddWithoutValidation("Connection", "close");

                request.Content = new ByteArrayContent(buffer);
                request.Content.Headers.TryAddWithoutValidation("Content-Type", "application/json");
                request.Content.Headers.TryAddWithoutValidation("Content-Length", buffer.Length.ToString());

                responseMessage = client.Send(request, HttpCompletionOption.ResponseContentRead);
                responseMessage.EnsureSuccessStatusCode();

                if(m_log.IsInfoEnabled) m_log.InfoFormat("RegionReadyModule successfully sent '{0}' alert for region {1} to external service", status, m_scene.RegionInfo.RegionName);
            }
            catch(Exception e)
            {
                m_log.WarnFormat("RegionReadyModule exception thrown sending alert for region {0}: {1}", m_scene.RegionInfo.RegionName, e.Message);
            }
            finally
            {
                request?.Dispose();
                responseMessage?.Dispose();
                client?.Dispose();
            }
        }
    }
}
