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

// Dedicated to Quill Littlefeather

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Caps = OpenSim.Framework.Capabilities.Caps;

namespace OpenSim.Region.ClientStack.LindenCaps
{
    public class ServerReleaseNotesModule : ISharedRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private bool m_enabled;
        private string m_ServerReleaseNotesURL;

        public string Name { get { return "ServerReleaseNotesModule"; } }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void Initialise(IConfigSource source)
        {
            if(m_log.IsDebugEnabled) m_log.Debug("Initializing ServerReleaseNotesModule for client release notes capability");

            m_enabled = false;
            IConfig config = source.Configs["ClientStack.LindenCaps"];
            if (config == null)
            {
                if(m_log.IsDebugEnabled) m_log.Debug("ServerReleaseNotesModule disabled - no [ClientStack.LindenCaps] configuration section found");
                return;
            }

            string capURL = config.GetString("Cap_ServerReleaseNotes", string.Empty);
            if (string.IsNullOrEmpty(capURL) || capURL != "localhost")
            {
                if(m_log.IsDebugEnabled) m_log.DebugFormat("ServerReleaseNotesModule disabled - Cap_ServerReleaseNotes must be set to 'localhost', current value: '{0}'", capURL);
                return;
            }

            config = source.Configs["ServerReleaseNotes"];
            if (config == null)
            {
                if(m_log.IsDebugEnabled) m_log.Debug("ServerReleaseNotesModule disabled - no [ServerReleaseNotes] configuration section found");
                return;
            }

            m_ServerReleaseNotesURL = config.GetString("ServerReleaseNotesURL", m_ServerReleaseNotesURL);
            if (string.IsNullOrEmpty(m_ServerReleaseNotesURL))
            {
                if(m_log.IsDebugEnabled) m_log.Debug("ServerReleaseNotesModule disabled - ServerReleaseNotesURL not configured");
                return;
            }

            Uri dummy;
            if(!Uri.TryCreate(m_ServerReleaseNotesURL,UriKind.Absolute, out dummy))
            {
                m_log.Error("ServerReleaseNotesModule invalid ServerReleaseNotesURL - capability disabled");
                return;
            }

            m_enabled = true;
            if(m_log.IsDebugEnabled) m_log.DebugFormat("ServerReleaseNotesModule initialized successfully - will redirect to: {0}", m_ServerReleaseNotesURL);
        }

        public void AddRegion(Scene scene)
        {
            if (!m_enabled)
                return;

            if(m_log.IsDebugEnabled) m_log.DebugFormat("Adding ServerReleaseNotesModule to region {0} - registering capability handlers", scene.RegionInfo.RegionName);

            scene.EventManager.OnRegisterCaps += RegisterCaps;

            if(m_log.IsInfoEnabled) m_log.InfoFormat("ServerReleaseNotesModule added to region {0} - ServerReleaseNotes capability available", scene.RegionInfo.RegionName);
        }

        public void RegionLoaded(Scene scene) { }

        public void RemoveRegion(Scene scene)
        {
            if (!m_enabled)
                return;

            if(m_log.IsDebugEnabled) m_log.DebugFormat("Removing ServerReleaseNotesModule from region {0} - unregistering capability handlers", scene.RegionInfo.RegionName);

            scene.EventManager.OnRegisterCaps -= RegisterCaps;

            if(m_log.IsDebugEnabled) m_log.DebugFormat("ServerReleaseNotesModule removed from region {0}", scene.RegionInfo.RegionName);
        }

        public void PostInitialise() { }

        public void Close() { }

        public void RegisterCaps(UUID agentID, Caps caps)
        {
            if(m_log.IsDebugEnabled) m_log.DebugFormat("ServerReleaseNotesModule registering capability for agent {0}", agentID);

            string capPath = "/" + UUID.Random();
            caps.RegisterSimpleHandler("ServerReleaseNotes", new SimpleStreamHandler(capPath, ProcessServerReleaseNotes));

            if(m_log.IsDebugEnabled) m_log.DebugFormat("ServerReleaseNotesModule registered capability at path {0} for agent {1}", capPath, agentID);
        }

        public void ProcessServerReleaseNotes(IOSHttpRequest httpRequest, IOSHttpResponse httpResponse)
        {
            if(m_log.IsDebugEnabled) m_log.DebugFormat("ServerReleaseNotesModule processing request from {0} - redirecting to {1}", httpRequest.RemoteIPEndPoint, m_ServerReleaseNotesURL);

            httpResponse.Redirect(m_ServerReleaseNotesURL);

            if(m_log.IsDebugEnabled) m_log.DebugFormat("ServerReleaseNotesModule redirect sent to {0}", m_ServerReleaseNotesURL);
        }
    }
}
