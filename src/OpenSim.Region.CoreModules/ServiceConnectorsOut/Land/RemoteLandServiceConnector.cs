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

using log4net;
using System;
using System.Collections.Generic;
using System.Reflection;
using Nini.Config;
using OpenSim.Framework;
using OpenSim.Services.Connectors;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using OpenSim.Server.Base;
using OpenMetaverse;


namespace OpenSim.Region.CoreModules.ServiceConnectorsOut.Land
{
    public class RemoteLandServicesConnector :
            LandServicesConnector, ISharedRegionModule, ILandService
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private bool m_Enabled = false;
        // AKIDO: Removed LocalLandServicesConnector dependency as it was deleted for GridHypergrid-only deployments

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public string Name
        {
            get { return "RemoteLandServicesConnector"; }
        }

        public void Initialise(IConfigSource source)
        {
            IConfig moduleConfig = source.Configs["Modules"];
            if (moduleConfig != null)
            {
                string name = moduleConfig.GetString("LandServices", "");
                if (name == Name)
                {
                    // AKIDO: Removed LocalLandServicesConnector instantiation - no longer needed for remote-only operation
                    m_Enabled = true;
                    m_log.Info("[REMOTE LAND CONNECTOR]: Remote Land connector enabled for grid-wide land services");
                    m_log.Debug("[REMOTE LAND CONNECTOR]: Operating in remote-only mode without local service fallback");
                }
                else
                {
                    if (m_log.IsDebugEnabled)
                        m_log.Debug($"[REMOTE LAND CONNECTOR]: Module disabled. LandServices = '{name}', expected '{Name}'");
                }
            }
            else
            {
                if (m_log.IsDebugEnabled)
                    m_log.Debug("[REMOTE LAND CONNECTOR]: No [Modules] configuration section found, connector disabled");
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
                    m_log.Debug($"[REMOTE LAND CONNECTOR]: Not adding to region {scene.Name} - connector disabled");
                return;
            }

            // AKIDO: Removed m_LocalService.AddRegion(scene) call - operating in remote-only mode
            scene.RegisterModuleInterface<ILandService>(this);
            if (m_log.IsDebugEnabled)
                m_log.Debug($"[REMOTE LAND CONNECTOR]: Added to region {scene.Name} and registered ILandService interface");
        }

        public void RemoveRegion(Scene scene)
        {
            // AKIDO: Removed m_LocalService.RemoveRegion(scene) call - no local service to clean up
            if (m_log.IsDebugEnabled)
                m_log.Debug($"[REMOTE LAND CONNECTOR]: Removed from region {scene.Name}");
        }

        public void RegionLoaded(Scene scene)
        {
            if (m_Enabled)
            {
                m_GridService = scene.GridService;
                if (m_log.IsDebugEnabled)
                    m_log.Debug($"[REMOTE LAND CONNECTOR]: Region {scene.Name} loaded, GridService configured");
            }
        }


        #region ILandService

        public override LandData GetLandData(UUID scopeID, ulong regionHandle, uint x, uint y, out byte regionAccess)
        {
            // AKIDO: Removed local service fallback - now operates purely as remote connector
            if (m_log.IsDebugEnabled)
                m_log.Debug($"[REMOTE LAND CONNECTOR]: GetLandData request - scopeID: {scopeID}, regionHandle: {regionHandle}, position: ({x},{y})");
            
            LandData result = base.GetLandData(scopeID, regionHandle, x, y, out regionAccess);
            
            if (m_log.IsDebugEnabled)
            {
                if (result != null)
                    m_log.Debug($"[REMOTE LAND CONNECTOR]: GetLandData successful - parcel: {result.Name}, owner: {result.OwnerID}");
                else
                    m_log.Debug("[REMOTE LAND CONNECTOR]: GetLandData returned null");
            }
            
            return result;
        }
        #endregion ILandService
    }
}
