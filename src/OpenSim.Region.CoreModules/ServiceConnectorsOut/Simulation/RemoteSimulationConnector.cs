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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Scenes.Serialization;
using OpenSim.Services.Interfaces;
using OpenSim.Services.Connectors.Simulation;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace OpenSim.Region.CoreModules.ServiceConnectorsOut.Simulation
{
    public class RemoteSimulationConnectorModule : ISharedRegionModule, ISimulationService
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private bool initialized = false;
        protected bool m_enabled = false;
        protected Scene m_aScene;
        // AKIDO: Removed LocalSimulationConnectorModule dependency - operating in remote-only mode
        protected SimulationServiceConnector m_remoteConnector;

        protected bool m_safemode;

        #region Region Module interface

        public virtual void Initialise(IConfigSource configSource)
        {
            IConfig moduleConfig = configSource.Configs["Modules"];
            if (moduleConfig != null)
            {
                string name = moduleConfig.GetString("SimulationServices", "");
                if (name == Name)
                {
                    // AKIDO: Removed LocalSimulationConnectorModule instantiation - remote-only operation
                    m_remoteConnector = new SimulationServiceConnector();

                    m_enabled = true;

                    m_log.Info("Remote simulation connector enabled for distributed simulation services");
                    m_log.Debug("Using SimulationServiceConnector for remote service communication");
                    m_log.Debug("Operating in remote-only mode without local simulation fallback");
                }
                else
                {
                    if (m_log.IsDebugEnabled)
                        m_log.Debug($"Module disabled. SimulationServices = '{name}', expected '{Name}'");
                }
            }
            else
            {
                if (m_log.IsDebugEnabled)
                    m_log.Debug("No [Modules] configuration section found, connector disabled");
            }
        }

        public virtual void PostInitialise()
        {
        }

        public virtual void Close()
        {
        }

        public void AddRegion(Scene scene)
        {
            if (!m_enabled)
            {
                if (m_log.IsDebugEnabled)
                    m_log.Debug($"Not adding to region {scene.Name} - connector disabled");
                return;
            }

            if (!initialized)
            {
                InitOnce(scene);
                initialized = true;
                if (m_log.IsDebugEnabled)
                    m_log.Debug("Remote simulation connector initialized for first region");
            }
            InitEach(scene);
            
            if (m_log.IsDebugEnabled)
                m_log.Debug($"Added to region {scene.Name} and registered ISimulationService interface");
        }

        public void RemoveRegion(Scene scene)
        {
            if (m_enabled)
            {
                // AKIDO: Removed m_localBackend.RemoveScene(scene) call - no local backend to clean up
                scene.UnregisterModuleInterface<ISimulationService>(this);
                if (m_log.IsDebugEnabled)
                    m_log.Debug($"Removed from region {scene.Name} and unregistered ISimulationService interface");
            }
        }

        public void RegionLoaded(Scene scene)
        {
            if (!m_enabled)
                return;
                
            if (m_log.IsDebugEnabled)
                m_log.Debug($"Region {scene.Name} loaded successfully");
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public virtual string Name
        {
            get { return "RemoteSimulationConnectorModule"; }
        }

        protected virtual void InitEach(Scene scene)
        {
            // AKIDO: Removed m_localBackend.Init(scene) call - remote-only operation
            scene.RegisterModuleInterface<ISimulationService>(this);
        }

        protected virtual void InitOnce(Scene scene)
        {
            m_aScene = scene;
            //m_regionClient = new RegionToRegionClient(m_aScene, m_hyperlinkService);
        }

        #endregion

        #region ISimulationService

        public IScene GetScene(UUID regionId)
        {
            // AKIDO: Removed local backend dependency - returning null for remote-only operation
            return null;
        }

        public ISimulationService GetInnerService()
        {
            // AKIDO: Removed local backend dependency - returning remote connector
            return m_remoteConnector;
        }

        /**
         * Agent-related communications
         */

        public bool CreateAgent(GridRegion source, GridRegion destination, AgentCircuitData aCircuit, uint teleportFlags, EntityTransferContext ctx, out string reason)
        {
            if (destination == null)
            {
                reason = "Given destination was null";
                m_log.Debug("CreateAgent was given a null destination");
                return false;
            }

            if (m_log.IsDebugEnabled)
                m_log.Debug($"CreateAgent for {aCircuit.AgentID} from {source?.RegionName ?? "unknown"} to {destination.RegionName}");

            // AKIDO: Removed local backend fallback - going directly to remote connector
            bool result = m_remoteConnector.CreateAgent(source, destination, aCircuit, teleportFlags, ctx, out reason);
            
            if (m_log.IsDebugEnabled)
                m_log.Debug($"CreateAgent result: {result}, reason: {reason ?? "none"}");
                
            return result;
        }

        public bool UpdateAgent(GridRegion destination, AgentData cAgentData, EntityTransferContext ctx)
        {
            if (destination == null)
            {
                if (m_log.IsDebugEnabled)
                    m_log.Debug("UpdateAgent was given a null destination");
                return false;
            }

            if (m_log.IsDebugEnabled)
                m_log.Debug($"UpdateAgent for {cAgentData.AgentID} to {destination.RegionName}");

            // AKIDO: Removed local backend fallback - going directly to remote connector
            bool result = m_remoteConnector.UpdateAgent(destination, cAgentData, ctx);
            
            if (m_log.IsDebugEnabled)
                m_log.Debug($"UpdateAgent result: {result}");
                
            return result;
        }

        public bool UpdateAgent(GridRegion destination, AgentPosition cAgentData)
        {
            if (destination == null)
                return false;

            // AKIDO: Removed local backend fallback - going directly to remote connector
            return m_remoteConnector.UpdateAgent(destination, cAgentData);
        }

        public bool QueryAccess(GridRegion destination, UUID agentID, string agentHomeURI, bool viaTeleport, Vector3 position, List<UUID> features, EntityTransferContext ctx, out string reason)
        {
            reason = "Communications failure";

            if (destination == null)
            {
                if (m_log.IsDebugEnabled)
                    m_log.Debug("QueryAccess was given a null destination");
                return false;
            }

            if (m_log.IsDebugEnabled)
                m_log.Debug($"QueryAccess for agent {agentID} to {destination.RegionName}, viaTeleport: {viaTeleport}");

            // AKIDO: Removed local backend fallback - going directly to remote connector
            bool result = m_remoteConnector.QueryAccess(destination, agentID, agentHomeURI, viaTeleport, position, features, ctx, out reason);
            
            if (m_log.IsDebugEnabled)
                m_log.Debug($"QueryAccess result: {result}, reason: {reason ?? "none"}");
                
            return result;
        }

        public bool ReleaseAgent(UUID origin, UUID id, string uri)
        {
            // AKIDO: Removed local backend fallback - going directly to remote connector
            return m_remoteConnector.ReleaseAgent(origin, id, uri);
        }

        public bool CloseAgent(GridRegion destination, UUID id, string auth_token)
        {
            if (destination == null)
                return false;

            // AKIDO: Removed local backend fallback - going directly to remote connector
            return m_remoteConnector.CloseAgent(destination, id, auth_token);
        }

        /**
         * Object-related communications
         */

        public bool CreateObject(GridRegion destination, Vector3 newPosition, ISceneObject sog, bool isLocalCall)
        {
            if (destination == null)
                return false;

            // AKIDO: Removed local backend fallback - going directly to remote connector
            return m_remoteConnector.CreateObject(destination, newPosition, sog, isLocalCall);
        }

        #endregion
    }
}
