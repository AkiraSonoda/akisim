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
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Server.Base;
using OpenSim.Services.Interfaces;
using PresenceInfo = OpenSim.Services.Interfaces.PresenceInfo;

namespace OpenSim.Region.CoreModules.ServiceConnectorsOut.Presence
{
    public class BasePresenceServiceConnector : IPresenceService
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected bool m_Enabled;

        protected PresenceDetector m_PresenceDetector;

        /// <summary>
        /// Underlying presence service.  Do not use directly.
        /// </summary>
        public IPresenceService m_PresenceService;

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void AddRegion(Scene scene)
        {
            if (!m_Enabled)
            {
                if (m_log.IsDebugEnabled)
                    m_log.Debug($"Not adding to region {scene.Name} - connector disabled");
                return;
            }

            scene.RegisterModuleInterface<IPresenceService>(this);
            m_PresenceDetector.AddRegion(scene);

            m_log.InfoFormat("Added to region {0} and registered IPresenceService interface", scene.Name);
            if (m_log.IsDebugEnabled)
                m_log.Debug($"PresenceDetector added to region {scene.Name}");
        }

        public void RemoveRegion(Scene scene)
        {
            if (!m_Enabled)
                return;

            m_PresenceDetector.RemoveRegion(scene);
            if (m_log.IsDebugEnabled)
                m_log.Debug($"Removed from region {scene.Name}, PresenceDetector cleaned up");
        }

        public void RegionLoaded(Scene scene)
        {
            if (!m_Enabled)
                return;

            if (m_log.IsDebugEnabled)
                m_log.Debug($"Region {scene.Name} loaded successfully");
        }

        public void PostInitialise()
        {
        }

        public void Close()
        {
        }

        #region IPresenceService

        public bool LoginAgent(string userID, UUID sessionID, UUID secureSessionID)
        {
            m_log.Warn("LoginAgent connector not implemented at the simulators");
            return false;
        }

        public bool LogoutAgent(UUID sessionID)
        {
            if (m_log.IsDebugEnabled)
                m_log.Debug($"LogoutAgent for session {sessionID}");
                
            bool result = m_PresenceService.LogoutAgent(sessionID);
            
            if (m_log.IsDebugEnabled)
                m_log.Debug($"LogoutAgent result: {result}");
            
            return result;
        }

        public bool LogoutRegionAgents(UUID regionID)
        {
            if (m_log.IsDebugEnabled)
                m_log.Debug($"LogoutRegionAgents for region {regionID}");
                
            bool result = m_PresenceService.LogoutRegionAgents(regionID);
            
            if (m_log.IsDebugEnabled)
                m_log.Debug($"LogoutRegionAgents result: {result}");
            
            return result;
        }

        public bool ReportAgent(UUID sessionID, UUID regionID)
        {
            if (m_log.IsDebugEnabled)
                m_log.Debug($"ReportAgent for session {sessionID} in region {regionID}");
                
            bool result = m_PresenceService.ReportAgent(sessionID, regionID);
            
            if (m_log.IsDebugEnabled)
                m_log.Debug($"ReportAgent result: {result}");
            
            return result;
        }

        public PresenceInfo GetAgent(UUID sessionID)
        {
            if (m_log.IsDebugEnabled)
                m_log.Debug($"GetAgent for session {sessionID}");
                
            PresenceInfo result = m_PresenceService.GetAgent(sessionID);
            
            if (m_log.IsDebugEnabled)
            {
                if (result != null)
                    m_log.Debug($"GetAgent successful - user: {result.UserID}, region: {result.RegionID}");
                else
                    m_log.Debug("GetAgent returned null");
            }
            
            return result;
        }

        public PresenceInfo[] GetAgents(string[] userIDs)
        {
            // Don't bother potentially making a useless network call if we not going to ask for any users anyway.
            if (userIDs.Length == 0)
            {
                if (m_log.IsDebugEnabled)
                    m_log.Debug("GetAgents called with empty userIDs array, returning empty result");
                return new PresenceInfo[0];
            }

            if (m_log.IsDebugEnabled)
                m_log.Debug($"GetAgents for {userIDs.Length} users");
                
            PresenceInfo[] result = m_PresenceService.GetAgents(userIDs);
            
            if (m_log.IsDebugEnabled)
                m_log.Debug($"GetAgents returned {result?.Length ?? 0} presence records");
            
            return result;
        }

        #endregion
    }
}