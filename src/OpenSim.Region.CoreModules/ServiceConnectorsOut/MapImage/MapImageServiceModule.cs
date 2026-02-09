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
using System.Reflection;
using System.IO;
using System.Timers;
using System.Drawing;
using System.Drawing.Imaging;
using log4net;
using Nini.Config;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using OpenSim.Server.Base;
using OpenMetaverse;
using ThreadedClasses;
using SkiaSharp;
// AKIDO: clean

namespace OpenSim.Region.CoreModules.ServiceConnectorsOut.MapImage
{
    /// <summary>
    /// </summary>
    /// <remarks>
    /// </remarks>


    public class MapImageServiceModule : IMapImageUploadModule, ISharedRegionModule
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static string LogHeader = "[MAP IMAGE SERVICE MODULE]:";

        private bool m_enabled = false;
        private IMapImageService m_MapService;

        private RwLockedDictionary<UUID, Scene> m_scenes = new RwLockedDictionary<UUID, Scene>(); // AKIDO

        private int m_refreshtime = 0;
        private int m_lastrefresh = 0;
        private System.Timers.Timer m_refreshTimer;

        #region ISharedRegionModule

        public Type ReplaceableInterface { get { return null; } }
        public string Name { get { return "MapImageServiceModule"; } }
        public void RegionLoaded(Scene scene) { }
        public void Close()
        {
            if(m_log.IsDebugEnabled) m_log.Debug("Closing MapImageServiceModule - stopping map tile refresh timer and cleaning up resources");

            if (m_refreshTimer != null)
            {
                m_refreshTimer.Stop();
                m_refreshTimer.Dispose();
                m_refreshTimer = null;
                if(m_log.IsDebugEnabled) m_log.Debug("Map tile refresh timer stopped and disposed");
            }

            if(m_log.IsDebugEnabled) m_log.Debug("MapImageServiceModule shutdown completed");
        }
        public void PostInitialise() { }

        ///<summary>
        ///
        ///</summary>
        public void Initialise(IConfigSource source)
        {
            if(m_log.IsDebugEnabled) m_log.Debug("Initializing MapImageServiceModule for map tile upload and management services");

            IConfig moduleConfig = source.Configs["Modules"];
            if (moduleConfig != null)
            {
                string name = moduleConfig.GetString("MapImageService", "");
                if (name != Name)
                {
                    if(m_log.IsDebugEnabled) m_log.DebugFormat("MapImageServiceModule disabled - configured service is '{0}', expected '{1}'", name, Name);
                    return;
                }
            }
            else
            {
                if(m_log.IsDebugEnabled) m_log.Debug("No [Modules] configuration section found - MapImageServiceModule may not be properly configured");
            }

            IConfig config = source.Configs["MapImageService"];
            if (config == null)
            {
                if(m_log.IsDebugEnabled) m_log.Debug("MapImageServiceModule disabled - no [MapImageService] configuration section found");
                return;
            }

            string refreshTimeStr = config.GetString("RefreshTime", "0");
            int refreshminutes;
            if (!int.TryParse(refreshTimeStr, out refreshminutes))
            {
                m_log.WarnFormat("Invalid refresh time '{0}' given in config. Module disabled.", refreshTimeStr);
                return;
            }
            if (refreshminutes < 0)
            {
                m_log.WarnFormat("Negative refresh time {0} given in config. Module disabled.", refreshminutes);
                return;
            }

            string service = config.GetString("LocalServiceModule", string.Empty);
            if (service.Length == 0)
            {
                m_log.WarnFormat("No LocalServiceModule dll given in config. Unable to proceed.");
                return;
            }
            if(m_log.IsDebugEnabled) m_log.DebugFormat("Loading map image service from: {0}", service);

            Object[] args = new Object[] { source };
            m_MapService = ServerUtils.LoadPlugin<IMapImageService>(service, args);
            if (m_MapService == null)
            {
                m_log.WarnFormat("Unable to load LocalServiceModule from {0}. MapService module disabled. Please fix the configuration.", service);
                return;
            }
            if(m_log.IsDebugEnabled) m_log.DebugFormat("Successfully loaded map image service from: {0}", service);

            // we don't want the timer if the interval is zero, but we still want this module enables
            if(refreshminutes > 0)
            {
                m_refreshtime = refreshminutes * 60 * 1000; // convert from minutes to ms

                if(m_log.IsDebugEnabled) m_log.DebugFormat("Setting up map tile refresh timer with interval: {0} minutes ({1} ms)", refreshminutes, m_refreshtime);

                m_refreshTimer = new System.Timers.Timer();
                m_refreshTimer.Enabled = true;
                m_refreshTimer.AutoReset = true;
                m_refreshTimer.Interval = m_refreshtime;
                m_refreshTimer.Elapsed += new ElapsedEventHandler(HandleMaptileRefresh);

                if(m_log.IsInfoEnabled) m_log.InfoFormat("MapImageServiceModule enabled with refresh time {0} min and service object {1}",
                             refreshminutes, service);
            }
            else
            {
                if(m_log.IsInfoEnabled) m_log.InfoFormat("MapImageServiceModule enabled with no automatic refresh and service object {0}", service);
            }
            m_enabled = true;
            if(m_log.IsDebugEnabled) m_log.Debug("MapImageServiceModule initialization completed successfully");
        }

        ///<summary>
        ///
        ///</summary>
        public void AddRegion(Scene scene)
        {
            if (!m_enabled)
                return;

            if(m_log.IsDebugEnabled) m_log.DebugFormat("Adding MapImageServiceModule to region {0} - registering map upload interface", scene.RegionInfo.RegionName);

            // Every shared region module has to maintain an indepedent list of
            // currently running regions
            // AKIDO
            m_scenes[scene.RegionInfo.RegionID] = scene;

            // v2 Map generation on startup is now handled by scene to allow bmp to be shared with
            // v1 service and not generate map tiles twice as was previous behavior
            // AKIDO re-adding the old Handling because we require the static maptiles
            // Use OnRegionStarted which fires after grid registration is complete
            scene.EventManager.OnRegionStarted += s =>
            {
                Util.FireAndForget(delegate(object o)
                {
                    UploadMapTile(s);
                }, null, "MapImageServiceModule.UploadOnStartup");
            };
            // AKIDO

            scene.RegisterModuleInterface<IMapImageUploadModule>(this);

            if(m_log.IsInfoEnabled) m_log.InfoFormat("MapImageServiceModule added to region {0} - map tile upload services available", scene.RegionInfo.RegionName);
        }

        ///<summary>
        ///
        ///</summary>
        public void RemoveRegion(Scene scene)
        {
            if (! m_enabled)
                return;

            if(m_log.IsDebugEnabled) m_log.DebugFormat("Removing MapImageServiceModule from region {0} - unregistering map upload interface", scene.RegionInfo.RegionName);

            // AKIDO
            m_scenes.Remove(scene.RegionInfo.RegionID);

            if(m_log.IsDebugEnabled) m_log.DebugFormat("MapImageServiceModule removed from region {0}", scene.RegionInfo.RegionName);
        }

        #endregion ISharedRegionModule

        ///<summary>
        ///
        ///</summary>
        private void HandleMaptileRefresh(object sender, EventArgs ea)
        {
            // this approach is a bit convoluted becase we want to wait for the
            // first upload to happen on startup but after all the objects are
            // loaded and initialized
            if (m_lastrefresh > 0 && Util.EnvironmentTickCountSubtract(m_lastrefresh) < m_refreshtime)
                return;

            if(m_log.IsDebugEnabled) m_log.DebugFormat("Starting automatic map tile refresh for {0} regions", m_scenes.Count);

            // AKIDO
            int successCount = 0;
            int errorCount = 0;
            foreach (IScene scene in m_scenes.Values)
            {
                try
                {
                    UploadMapTile(scene);
                    successCount++;
                }
                catch (Exception ex)
                {
                    errorCount++;
                    m_log.WarnFormat("Map tile refresh failed for region {0}: {1}", scene.RegionInfo.RegionName, ex.Message);
                    if(m_log.IsDebugEnabled) m_log.DebugFormat("Map tile refresh exception details: {0}", ex);
                }
            }
            // AKIDO

            if(m_log.IsInfoEnabled) m_log.InfoFormat("Map tile refresh completed - {0} successful, {1} errors", successCount, errorCount);
            m_lastrefresh = Util.EnvironmentTickCount();
        }

        public void UploadMapTile(IScene scene, SKBitmap mapTile)
        {
            m_log.InfoFormat("{0} Starting map tile upload for region {1}", LogHeader, scene.RegionInfo.RegionName);

            if (mapTile == null)
            {
                m_log.WarnFormat("{0} Cannot upload null image for region {1}", LogHeader, scene.RegionInfo.RegionName);
                return;
            }


            // mapTile.Save(   // DEBUG DEBUG
            //     String.Format("maptiles/raw-{0}-{1}-{2}.jpg", regionName, scene.RegionInfo.RegionLocX, scene.RegionInfo.RegionLocY),
            //     ImageFormat.Jpeg);
            // If the region/maptile is legacy sized, just upload the one tile like it has always been done
            if (mapTile.Width == Constants.RegionSize && mapTile.Height == Constants.RegionSize)
            {
                m_log.InfoFormat("{0} Uploading single legacy-sized map tile ({1}x{2}) for region {3}", LogHeader, mapTile.Width, mapTile.Height, scene.Name);
                bool success = ConvertAndUploadMaptile(scene, mapTile,
                                        scene.RegionInfo.RegionLocX, scene.RegionInfo.RegionLocY,
                                        scene.RegionInfo.RegionName);
                m_log.InfoFormat("{0} Single map tile upload for {1}: {2}", LogHeader, scene.Name, success ? "successful" : "failed");
            }
            else
            {
                if(m_log.IsDebugEnabled) m_log.DebugFormat("{0} Upload {1} maptiles for {2}", LogHeader,
                    (mapTile.Width * mapTile.Height) / (Constants.RegionSize * Constants.RegionSize),
                    scene.Name);

                // For larger regions (varregion) we must cut the region image into legacy sized
                //    pieces since that is how the maptile system works.
                // Note the assumption that varregions are always a multiple of legacy size.
                for (uint xx = 0; xx < mapTile.Width; xx += Constants.RegionSize)
                {
                    for (uint yy = 0; yy < mapTile.Height; yy += Constants.RegionSize)
                    {
                        // Images are addressed from the upper left corner so have to do funny
                        //     math to pick out the sub-tile since regions are numbered from
                        //     the lower left.
                        SKRectI rect = new SKRectI(
                            (int)xx,
                            mapTile.Height - (int)yy - (int)Constants.RegionSize,
                            (int)xx + (int)Constants.RegionSize,
                            mapTile.Height - (int)yy);

                        SKBitmap subMapTile = new SKBitmap((int)Constants.RegionSize, (int)Constants.RegionSize);
                        using (SKCanvas canvas = new SKCanvas(subMapTile))
                        {
                            canvas.DrawBitmap(mapTile, rect, new SKRect(0, 0, Constants.RegionSize, Constants.RegionSize));
                        }
                        using (subMapTile)
                        {
                            if(!ConvertAndUploadMaptile(scene, subMapTile,
                                                    scene.RegionInfo.RegionLocX + (xx / Constants.RegionSize),
                                                    scene.RegionInfo.RegionLocY + (yy / Constants.RegionSize),
                                                    scene.Name))
                            {
                                if(m_log.IsDebugEnabled) m_log.DebugFormat(
                                    "{0} Upload maptileS for {1} aborted!", LogHeader, scene.Name);
                                return; // abort rest;
                            }
                        }            
                    }
                }
            }
        }

        ///<summary>
        ///
        ///</summary>
        public void UploadMapTile(IScene scene)
        {
            if(m_log.IsDebugEnabled) m_log.DebugFormat(
                "{0} Starting map tile generation and upload for region {1}", LogHeader, scene.RegionInfo.RegionName);

            // Create a JPG map tile and upload it to the AddMapTile API
            IMapImageGenerator tileGenerator = scene.RequestModuleInterface<IMapImageGenerator>();
            if (tileGenerator == null)
            {
                m_log.WarnFormat("{0} Cannot upload map tile for region {1} - no IMapImageGenerator available", LogHeader, scene.RegionInfo.RegionName);
                return;
            }
            if(m_log.IsDebugEnabled) m_log.DebugFormat("{0} Found map image generator for region {1}", LogHeader, scene.RegionInfo.RegionName);

            using (SKBitmap mapTile = tileGenerator.CreateMapTile())
            {
                // The MapImageModule will return a null if the user has chosen not to create map tiles and there
                // is no static map tile.
                if (mapTile == null)
                {
                    if(m_log.IsDebugEnabled) m_log.DebugFormat("{0} No map tile generated for region {1} - tile generation may be disabled or failed", LogHeader, scene.RegionInfo.RegionName);
                    return;
                }

                if(m_log.IsDebugEnabled) m_log.DebugFormat("{0} Map tile generated successfully for region {1} - size: {2}x{3}", LogHeader, scene.RegionInfo.RegionName, mapTile.Width, mapTile.Height);
                UploadMapTile(scene, mapTile);
            }
        }

        private bool ConvertAndUploadMaptile(IScene scene, SKBitmap tileImage, uint locX, uint locY, string regionName)
        {
            m_log.InfoFormat("{0} Converting and uploading map tile for {1} at location {2},{3}", LogHeader, regionName, locX, locY);

            byte[] jpgData = Utils.EmptyBytes;

            try
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    tileImage.Encode(stream, SKEncodedImageFormat.Jpeg, 95);
                    jpgData = stream.ToArray();
                }
            }
            catch (Exception ex)
            {
                m_log.ErrorFormat("{0} Failed to convert tile image to JPEG for region {1}: {2}", LogHeader, regionName, ex.Message);
                return false;
            }

            if (jpgData == Utils.EmptyBytes)
            {
                m_log.WarnFormat("{0} Tile image JPEG conversion resulted in empty data for region {1}", LogHeader, regionName);
                return false;
            }

            if(m_log.IsDebugEnabled) m_log.DebugFormat("{0} JPEG conversion successful for {1} - size: {2} bytes", LogHeader, regionName, jpgData.Length);

            string reason = string.Empty;
            int maxRetries = 3;
            int retryDelay = 2000; // milliseconds

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                if (m_MapService.AddMapTile((int)locX, (int)locY, jpgData, scene.RegionInfo.ScopeID, out reason))
                {
                    m_log.InfoFormat("{0} Map tile upload successful for {1} at {2},{3}{4}",
                        LogHeader, regionName, locX, locY, attempt > 0 ? $" (retry {attempt})" : "");
                    return true;
                }

                // Check if error is due to region not yet registered
                if (reason == "No region at coordinates" && attempt < maxRetries - 1)
                {
                    if(m_log.IsDebugEnabled) m_log.DebugFormat("{0} Region {1} not yet registered, retrying in {2}ms (attempt {3}/{4})",
                        LogHeader, regionName, retryDelay, attempt + 1, maxRetries);
                    System.Threading.Thread.Sleep(retryDelay);
                    continue;
                }

                // For other errors or final attempt, break and report failure
                break;
            }

            m_log.WarnFormat("{0} Unable to upload tile image for {1} at {2}-{3}: {4}", LogHeader,
                regionName, locX, locY, reason);
            return false;
        }
    }
}
