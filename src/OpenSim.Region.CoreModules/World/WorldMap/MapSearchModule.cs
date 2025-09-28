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
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
// AKIDO: clean

namespace OpenSim.Region.CoreModules.World.WorldMap
{
    public class MapSearchModule : ISharedRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        IGridService m_gridservice = null;
        UUID m_stupidScope = UUID.Zero;

        #region ISharedRegionModule Members
        public void Initialise(IConfigSource source)
        {
            if(m_log.IsDebugEnabled) m_log.Debug("Initializing MapSearchModule for region search functionality");
            if(m_log.IsDebugEnabled) m_log.Debug("MapSearchModule initialization completed successfully");
        }

        public void AddRegion(Scene scene)
        {
            if(m_log.IsDebugEnabled) m_log.DebugFormat("Adding MapSearchModule to region {0} - registering client event handlers", scene.RegionInfo.RegionName);

            scene.EventManager.OnNewClient += OnNewClient;

            if(m_log.IsInfoEnabled) m_log.InfoFormat("MapSearchModule added to region {0} - map search functionality available", scene.RegionInfo.RegionName);
        }

        public void RegionLoaded(Scene scene)
        {
            if (m_gridservice == null)
            {
                if(m_log.IsDebugEnabled) m_log.DebugFormat("Setting up grid service connection for region {0}", scene.RegionInfo.RegionName);
                m_gridservice = scene.GridService;
                m_stupidScope = scene.RegionInfo.ScopeID;
                if(m_log.IsDebugEnabled) m_log.DebugFormat("Grid service initialized for region {0} with scope {1}", scene.RegionInfo.RegionName, m_stupidScope);
            }
            else
            {
                if(m_log.IsDebugEnabled) m_log.DebugFormat("Grid service already initialized for region {0}", scene.RegionInfo.RegionName);
            }
        }

        public void RemoveRegion(Scene scene)
        {
            if(m_log.IsDebugEnabled) m_log.DebugFormat("Removing MapSearchModule from region {0} - unregistering client event handlers", scene.RegionInfo.RegionName);

            scene.EventManager.OnNewClient -= OnNewClient;

            if(m_log.IsDebugEnabled) m_log.DebugFormat("MapSearchModule removed from region {0}", scene.RegionInfo.RegionName);
        }

        public void PostInitialise()
        {
        }

        public void Close()
        {
            if(m_log.IsDebugEnabled) m_log.Debug("Closing MapSearchModule - cleaning up grid service connection");
            m_gridservice = null;
            if(m_log.IsDebugEnabled) m_log.Debug("MapSearchModule shutdown completed");
        }

        public string Name
        {
            get { return "MapSearchModule"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        #endregion

        private void OnNewClient(IClientAPI client)
        {
            if(m_log.IsDebugEnabled) m_log.DebugFormat("Registering map search handler for client {0}", client.Name);
            client.OnMapNameRequest += OnMapNameRequestHandler;
        }

        private void OnMapNameRequestHandler(IClientAPI remoteClient, string mapName, uint flags)
        {
            if(m_log.IsDebugEnabled) m_log.DebugFormat("Processing map search request from {0} for '{1}' with flags {2}", remoteClient.Name, mapName, flags);

            if (m_gridservice == null)
            {
                if(m_log.IsWarnEnabled) m_log.WarnFormat("Map search request from {0} failed - no grid service available", remoteClient.Name);
                return;
            }

            try
            {
                List<MapBlockData> blocks = new List<MapBlockData>();
                if (mapName.Length < 3 || (mapName.EndsWith("#") && mapName.Length < 4))
                {
                    if(m_log.IsDebugEnabled) m_log.DebugFormat("Map search request from {0} rejected - search string '{1}' too short", remoteClient.Name, mapName);

                    // final block, closing the search result
                    AddFinalBlock(blocks, mapName);

                    // flags are agent flags sent from the viewer.
                    // they have different values depending on different viewers, apparently
                    remoteClient.SendMapBlock(blocks, flags);
                    remoteClient.SendAlertMessage("Use a search string with at least 3 characters");
                    return;
                }

                if(m_log.IsDebugEnabled) m_log.DebugFormat("Processing map search for '{0}' from client {1}", mapName, remoteClient.Name);
                string mapNameOrig = mapName;
                int indx = mapName.IndexOfAny(new char[] {'.', '!','+','|',':','%'});
                bool needOriginalName = indx >= 0;

                // try to fetch from GridServer
                if(m_log.IsDebugEnabled) m_log.DebugFormat("Querying grid service for regions matching '{0}' with scope {1}", mapName, m_stupidScope);
                List<GridRegion> regionInfos = m_gridservice.GetRegionsByName(m_stupidScope, mapName, 20);

                if (!remoteClient.IsActive)
                {
                    if(m_log.IsDebugEnabled) m_log.DebugFormat("Client {0} is no longer active - aborting map search", remoteClient.Name);
                    return;
                }

                if(m_log.IsDebugEnabled) m_log.DebugFormat("Grid search for '{0}' returned {1} regions", mapName, regionInfos?.Count ?? 0);

                MapBlockData data;
                if (regionInfos != null && regionInfos.Count > 0)
                {
                    if(m_log.IsDebugEnabled) m_log.DebugFormat("Processing {0} region results for search '{1}'", regionInfos.Count, mapName);
                    foreach (GridRegion info in regionInfos)
                    {
                        data = new MapBlockData();
                        data.Agents = 0;
                        data.Access = info.Access;
                        MapBlockData block = new MapBlockData();
                        MapBlockFromGridRegion(block, info, flags);

                        if (needOriginalName && flags == 2 &&  regionInfos.Count == 1)
                        {
                            if(m_log.IsDebugEnabled) m_log.DebugFormat("Using original search name '{0}' for single result", mapNameOrig);
                            block.Name = mapNameOrig;
                        }
                        blocks.Add(block);
                        if(m_log.IsDebugEnabled) m_log.DebugFormat("Added region '{0}' at {1},{2} to search results", info.RegionName, info.RegionLocX, info.RegionLocY);
                    }
                }
                else
                {
                    if(m_log.IsDebugEnabled) m_log.DebugFormat("No regions found matching search '{0}'", mapName);
                }

                // final block, closing the search result
                AddFinalBlock(blocks, mapNameOrig);

                // flags are agent flags sent from the viewer.
                // they have different values depending on different viewers, apparently
                if(m_log.IsDebugEnabled) m_log.DebugFormat("Sending {0} map blocks to client {1} for search '{2}'", blocks.Count, remoteClient.Name, mapName);
                remoteClient.SendMapBlock(blocks, flags);

                // send extra user messages for V3
                // because the UI is very confusing
                // while we don't fix the hard-coded urls
                if (flags == 2)
                {
                    if (regionInfos == null || regionInfos.Count == 0)
                    {
                        if(m_log.IsDebugEnabled) m_log.DebugFormat("Sending 'no regions found' alert to client {0} for search '{1}'", remoteClient.Name, mapName);
                        remoteClient.SendAgentAlertMessage("No regions found with that name.", true);
                    }
                }

                if(m_log.IsInfoEnabled) m_log.InfoFormat("Map search completed for '{0}' by {1} - {2} results found", mapName, remoteClient.Name, regionInfos?.Count ?? 0);
            }
            catch(Exception ex)
            {
                m_log.ErrorFormat("Error processing map search request from {0} for '{1}': {2}", remoteClient.Name, mapName, ex.Message);
                if(m_log.IsDebugEnabled) m_log.DebugFormat("Map search exception details: {0}", ex);
            }
        }

        private static void MapBlockFromGridRegion(MapBlockData block, GridRegion r, uint flag)
        {
            if (r == null)
            {
                block.Access = (byte)SimAccess.NonExistent;
                block.MapImageId = UUID.Zero;
                return;
            }

            block.Access = r.Access;
            switch (flag)
            {
                case 0:
                    block.MapImageId = r.TerrainImage;
                    break;
                case 2:
                    block.MapImageId = r.ParcelImage;
                    break;
                default:
                    block.MapImageId = UUID.Zero;
                    break;
            }
            block.Name = r.RegionName;
            block.X = (ushort)(r.RegionLocX / Constants.RegionSize);
            block.Y = (ushort)(r.RegionLocY / Constants.RegionSize);
            block.SizeX = (ushort)r.RegionSizeX;
            block.SizeY = (ushort)r.RegionSizeY;
        }

        private void AddFinalBlock(List<MapBlockData> blocks,string name)
        {
                // final block, closing the search result
                MapBlockData data = new MapBlockData()
                {
                    Agents = 0,
                    Access = (byte)SimAccess.NonExistent,
                    MapImageId = UUID.Zero,
                    Name = name,
                    RegionFlags = 0,
                    WaterHeight = 0, // not used
                    X = 0,
                    Y = 0
                };
                blocks.Add(data);
        }
    }
}
