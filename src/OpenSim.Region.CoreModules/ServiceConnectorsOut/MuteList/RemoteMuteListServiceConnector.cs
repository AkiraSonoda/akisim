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
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Server.Base;
using OpenSim.Services.Interfaces;
using OpenSim.Services.Connectors;

using OpenMetaverse;
using log4net;
using Nini.Config;

namespace OpenSim.Region.CoreModules.ServiceConnectorsOut.MuteList
{
    public class RemoteMuteListServicesConnector : ISharedRegionModule, IMuteListService
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region ISharedRegionModule

        private bool m_Enabled = false;

        private IMuteListService m_remoteConnector;

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public string Name
        {
            get { return "RemoteMuteListServicesConnector"; }
        }

        public void Initialise(IConfigSource source)
        {
           // only active for core mute lists module
            IConfig moduleConfig = source.Configs["Messaging"];
            if (moduleConfig == null)
            {
                if (m_log.IsDebugEnabled)
                    m_log.Debug("[REMOTE MUTE LIST CONNECTOR]: No [Messaging] configuration section found, connector disabled");
                return;
            }

            if (moduleConfig.GetString("MuteListModule", "None") != "MuteListModule")
            {
                if (m_log.IsDebugEnabled)
                    m_log.Debug("[REMOTE MUTE LIST CONNECTOR]: MuteListModule not enabled in [Messaging] section, connector disabled");
                return;
            }
            
            moduleConfig = source.Configs["Modules"];
            if (moduleConfig != null)
            {
                string name = moduleConfig.GetString("MuteListService", "");
                if (name == Name)
                {
                    m_remoteConnector = new MuteListServicesConnector(source);
                    m_Enabled = true;
                    m_log.Info("[REMOTE MUTE LIST CONNECTOR]: Remote mute list connector enabled for distributed mute list services");
                    m_log.Debug("[REMOTE MUTE LIST CONNECTOR]: Using MuteListServicesConnector for remote service communication");
                }
                else
                {
                    if (m_log.IsDebugEnabled)
                        m_log.Debug($"[REMOTE MUTE LIST CONNECTOR]: Module disabled. MuteListService = '{name}', expected '{Name}'");
                }
            }
            else
            {
                if (m_log.IsDebugEnabled)
                    m_log.Debug("[REMOTE MUTE LIST CONNECTOR]: No [Modules] configuration section found, connector disabled");
            }
        }

        public void PostInitialise()
        {
        }

        public void Close()
        {
        }

        public void AddRegion(Scene scene)
        {
            if (!m_Enabled)
            {
                if (m_log.IsDebugEnabled)
                    m_log.Debug($"[REMOTE MUTE LIST CONNECTOR]: Not adding to region {scene.Name} - connector disabled");
                return;
            }

            scene.RegisterModuleInterface<IMuteListService>(this);
            m_log.InfoFormat("[REMOTE MUTE LIST CONNECTOR]: Added to region {0} and registered IMuteListService interface", scene.RegionInfo.RegionName);
        }

        public void RemoveRegion(Scene scene)
        {
            if (!m_Enabled)
                return;
                
            if (m_log.IsDebugEnabled)
                m_log.Debug($"[REMOTE MUTE LIST CONNECTOR]: Removed from region {scene.Name}");
        }

        public void RegionLoaded(Scene scene)
        {
            if (!m_Enabled)
                return;
                
            if (m_log.IsDebugEnabled)
                m_log.Debug($"[REMOTE MUTE LIST CONNECTOR]: Region {scene.Name} loaded successfully");
        }

        #endregion

        #region IMuteListService
        public Byte[] MuteListRequest(UUID agentID, uint crc)
        {
            if (!m_Enabled)
                return null;
                
            if (m_log.IsDebugEnabled)
                m_log.Debug($"[REMOTE MUTE LIST CONNECTOR]: MuteListRequest for agent {agentID}, CRC: {crc}");
                
            Byte[] result = m_remoteConnector.MuteListRequest(agentID, crc);
            
            if (m_log.IsDebugEnabled)
            {
                if (result != null)
                    m_log.Debug($"[REMOTE MUTE LIST CONNECTOR]: MuteListRequest successful - returned {result.Length} bytes");
                else
                    m_log.Debug("[REMOTE MUTE LIST CONNECTOR]: MuteListRequest returned null");
            }
            
            return result;
        }

        public bool UpdateMute(MuteData mute)
        {
            if (!m_Enabled)
                return false;
                
            if (m_log.IsDebugEnabled)
                m_log.Debug($"[REMOTE MUTE LIST CONNECTOR]: UpdateMute for agent {mute.AgentID}, mute: {mute.MuteID} ({mute.MuteName})");
                
            bool result = m_remoteConnector.UpdateMute(mute);
            
            if (m_log.IsDebugEnabled)
                m_log.Debug($"[REMOTE MUTE LIST CONNECTOR]: UpdateMute result: {result}");
            
            return result;
        }

        public bool RemoveMute(UUID agentID, UUID muteID, string muteName)
        {
            if (!m_Enabled)
                return false;
                
            if (m_log.IsDebugEnabled)
                m_log.Debug($"[REMOTE MUTE LIST CONNECTOR]: RemoveMute for agent {agentID}, mute: {muteID} ({muteName})");
                
            bool result = m_remoteConnector.RemoveMute(agentID, muteID, muteName);
            
            if (m_log.IsDebugEnabled)
                m_log.Debug($"[REMOTE MUTE LIST CONNECTOR]: RemoveMute result: {result}");
            
            return result;
        }

        #endregion IMuteListService

    }
}
