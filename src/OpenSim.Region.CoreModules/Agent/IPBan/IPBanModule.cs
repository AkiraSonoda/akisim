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
using System.IO;
using System.Reflection;
using System.Text;
using log4net;
using Nini.Config;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.Region.CoreModules.Agent.IPBan
{
    public class IPBanModule : ISharedRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region Implementation of ISharedRegionModule

        private List<string> m_bans = new List<string>();

        public void Initialise(IConfigSource source)
        {
            if(m_log.IsDebugEnabled) m_log.Debug("Initializing IPBanModule for IP and hostname based access control");
            if(m_log.IsDebugEnabled) m_log.Debug("IPBanModule initialization completed - ready for region registration");
        }

        public void AddRegion(Scene scene)
        {
            if(m_log.IsDebugEnabled) m_log.DebugFormat("Adding IPBanModule to region {0} - setting up IP ban enforcement", scene.RegionInfo.RegionName);

            new SceneBanner(scene, m_bans);

            int banCount = 0;
            lock (m_bans)
            {
                foreach (EstateBan ban in scene.RegionInfo.EstateSettings.EstateBans)
                {
                    if (!String.IsNullOrEmpty(ban.BannedHostIPMask))
                    {
                        m_bans.Add(ban.BannedHostIPMask);
                        banCount++;
                        if(m_log.IsDebugEnabled) m_log.DebugFormat("Added IP ban mask: {0}", ban.BannedHostIPMask);
                    }
                    if (!String.IsNullOrEmpty(ban.BannedHostNameMask))
                    {
                        m_bans.Add(ban.BannedHostNameMask);
                        banCount++;
                        if(m_log.IsDebugEnabled) m_log.DebugFormat("Added hostname ban mask: {0}", ban.BannedHostNameMask);
                    }
                }
            }

            if(m_log.IsInfoEnabled) m_log.InfoFormat("IPBanModule added to region {0} with {1} estate bans loaded", scene.RegionInfo.RegionName, banCount);
        }

        public void RemoveRegion(Scene scene)
        {
            if(m_log.IsDebugEnabled) m_log.DebugFormat("Removing IPBanModule from region {0} - IP ban enforcement will be disabled", scene.RegionInfo.RegionName);
        }

        public void RegionLoaded(Scene scene)
        {
            if(m_log.IsDebugEnabled) m_log.DebugFormat("IPBanModule region loaded for {0} - ban enforcement is active", scene.RegionInfo.RegionName);
        }

        public void PostInitialise()
        {
            if(m_log.IsDebugEnabled) m_log.Debug("Post-initializing IPBanModule - checking for global ban file");

            if (File.Exists("bans.txt"))
            {
                try
                {
                    string[] bans = File.ReadAllLines("bans.txt");
                    int loadedBans = 0;
                    foreach (string ban in bans)
                    {
                        if (!string.IsNullOrWhiteSpace(ban) && !ban.StartsWith("#"))
                        {
                            m_bans.Add(ban.Trim());
                            loadedBans++;
                            if(m_log.IsDebugEnabled) m_log.DebugFormat("Loaded global ban: {0}", ban.Trim());
                        }
                    }
                    if(m_log.IsInfoEnabled) m_log.InfoFormat("Loaded {0} global bans from bans.txt", loadedBans);
                }
                catch (Exception ex)
                {
                    m_log.ErrorFormat("Failed to load bans.txt: {0}", ex.Message);
                }
            }
            else
            {
                if(m_log.IsDebugEnabled) m_log.Debug("No global bans.txt file found - using only estate bans");
            }
        }

        public void Close()
        {
            if(m_log.IsDebugEnabled) m_log.Debug("Closing IPBanModule - clearing ban list and shutting down");
            lock (m_bans)
            {
                m_bans.Clear();
            }
            if(m_log.IsDebugEnabled) m_log.Debug("IPBanModule shutdown completed");
        }

        public string Name
        {
            get { return "IPBanModule"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        #endregion

        /// <summary>
        /// Bans all users from the specified network from connecting.
        /// DNS bans are in the form "somewhere.com" will block ANY
        /// matching domain (including "betasomewhere.com", "beta.somewhere.com",
        /// "somewhere.com.beta") - make sure to be reasonably specific in DNS
        /// bans.
        ///
        /// IP address bans match on first characters, so,
        /// "127.0.0.1" will ban only that address,
        /// "127.0.1" will ban "127.0.10.0"
        /// but "127.0.1." will ban only the "127.0.1.*" network
        /// </summary>
        /// <param name="host">See summary for explanation of parameter</param>
        public void Ban(string host)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                m_log.Warn("Attempted to add empty or null ban - ignoring");
                return;
            }

            lock (m_bans)
            {
                if (!m_bans.Contains(host))
                {
                    m_bans.Add(host);
                    if(m_log.IsInfoEnabled) m_log.InfoFormat("Added runtime ban for: {0}", host);
                }
                else
                {
                    if(m_log.IsDebugEnabled) m_log.DebugFormat("Ban already exists for: {0}", host);
                }
            }
        }
    }
}
