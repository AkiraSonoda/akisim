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
using OpenSim.Region.Framework.Interfaces;
using Nini.Config;
using log4net;

using OpenSim.Region.CoreModules.Agent.AssetTransaction;
using OpenSim.Region.CoreModules.Agent.IPBan;
using OpenSim.Region.CoreModules.Agent.TextureSender;
using OpenSim.Region.CoreModules.Framework.UserManagement;
using OpenSim.Region.CoreModules.Framework;
using OpenSim.Region.CoreModules.Agent.Xfer;
using OpenSim.Region.CoreModules.Avatar.Attachments;
using OpenSim.Region.CoreModules.Avatar.AvatarFactory;
using OpenSim.Region.CoreModules.Avatar.Chat;
using OpenSim.Region.CoreModules.Avatar.Dialog;
using OpenSim.Region.CoreModules.Scripting.EmailModules;
using OpenSim.Region.CoreModules.Avatar.Friends;
using OpenSim.Region.CoreModules.Avatar.Gestures;
using OpenSim.Region.CoreModules.Avatar.Gods;
using OpenSim.Region.CoreModules.Avatar.InstantMessage;
using OpenSim.Region.CoreModules.Avatar.Lure;
using OpenSim.Region.CoreModules.Avatar.Inventory.Archiver;
using OpenSim.Region.CoreModules.Avatar.Inventory.Transfer;
using OpenSim.Region.CoreModules.Avatar.UserProfiles;
using OpenSim.Region.CoreModules.Framework.InventoryAccess;
// BinaryLoggingModule not found in this project
using OpenSim.Region.CoreModules.ServiceConnectorsOut.Asset;
using OpenSim.Region.CoreModules.ServiceConnectorsOut.Authentication;
using OpenSim.Region.CoreModules.ServiceConnectorsOut.Authorization;
using OpenSim.Region.CoreModules.ServiceConnectorsOut.Avatar;
using OpenSim.Region.CoreModules.ServiceConnectorsOut.Grid;
using OpenSim.Region.CoreModules.ServiceConnectorsOut.GridUser;
using OpenSim.Region.CoreModules.ServiceConnectorsOut.Inventory;
using OpenSim.Region.CoreModules.ServiceConnectorsOut.Land;
using OpenSim.Region.CoreModules.ServiceConnectorsOut.Neighbour;
using OpenSim.Region.CoreModules.ServiceConnectorsOut.Presence;
using OpenSim.Region.ClientStack.LindenUDP;
using OpenSim.Region.CoreModules.ServiceConnectorsOut.Simulation;
using OpenSim.Region.CoreModules.ServiceConnectorsOut.UserAccounts;
using OpenSim.Region.CoreModules.ServiceConnectorsOut.MuteList;
using OpenSim.Region.CoreModules.ServiceConnectorsOut.AgentPreferences;
using OpenSim.Region.CoreModules.World.Archiver;
using OpenSim.Region.CoreModules.World.Estate;
using OpenSim.Region.CoreModules.World.Land;
using OpenSim.Region.CoreModules.World.Objects.BuySell;
using OpenSim.Region.CoreModules.World.Objects.Commands;
using OpenSim.Region.CoreModules.World.Permissions;
using OpenSim.Region.CoreModules.World.Region;
using OpenSim.Region.CoreModules.World.Serialiser;
using OpenSim.Region.CoreModules.World.Sound;
using OpenSim.Region.CoreModules.World.Media.Moap;
using OpenSim.Region.CoreModules.Scripting.LoadImageURL;
using OpenSim.Region.CoreModules.Scripting.VectorRender;
using OpenSim.Region.CoreModules.World.LightShare;
using OpenSim.Region.CoreModules.World.Terrain;
using OpenSim.Region.CoreModules.World.Vegetation;
using OpenSim.Region.CoreModules.World.Wind;
using OpenSim.Region.CoreModules.World.WorldMap;
using OpenSim.Region.CoreModules.World.Warp3DMap;
using OpenSim.Region.CoreModules.Hypergrid;
using OpenSim.Region.CoreModules.Framework.Library;
using OpenSim.Region.CoreModules.Framework.EntityTransfer;
using OpenSim.Region.CoreModules.ServiceConnectorsIn.Simulation;
using OpenSim.Region.CoreModules.ServiceConnectorsIn.Land;
using OpenSim.Region.CoreModules.ServiceConnectorsIn.Neighbour;
using OpenSim.Region.CoreModules.Scripting.ScriptModuleComms;
using OpenSim.Region.CoreModules.Scripting.WorldComm;
using OpenSim.Region.CoreModules.Scripting.HttpRequest;
using OpenSim.Region.CoreModules.Scripting.XMLRPC;

// Physics modules
using OpenSim.Region.PhysicsModule.BulletS;
using OpenSim.Region.PhysicsModule.ubOde;
using OpenSim.Region.PhysicsModule.Meshing;
using OpenSim.Region.PhysicsModule.ubODEMeshing;

// Caps modules
using OpenSim.Region.ClientStack.Linden;
using OpenSim.Region.ClientStack.LindenCaps;




namespace OpenSim.Region.CoreModules
{
    /// <summary>
    /// Factory for creating core region modules without relying on Mono.Addins
    /// This provides a .NET 8 compatible alternative to the plugin system
    /// </summary>
    public static class CoreModuleFactory
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        /// <summary>
        /// Creates all essential core modules for basic OpenSim functionality
        /// </summary>
        public static IEnumerable<INonSharedRegionModule> CreateNonSharedModules(IConfigSource configSource = null)
        {
            // Essential non-shared region modules
            yield return new AssetTransactionModule();
            yield return new XferModule();
            yield return new AttachmentsModule();
            yield return new AvatarFactoryModule();
            yield return new DialogModule();
            yield return new GesturesModule();
            yield return new BasicInventoryAccessModule();
            yield return new HGInventoryAccessModule();
            yield return new LandManagementModule();
            yield return new PrimCountModule();
            yield return new DefaultPermissionsModule();
            yield return new SoundModule();
            
            // Load LocalAuthorizationServicesConnector based on configuration
            if (configSource != null)
            {
                var modulesConfig = configSource.Configs["Modules"];
                string authorizationServicesModule = modulesConfig?.GetString("AuthorizationServices", "");
                if (authorizationServicesModule == "LocalAuthorizationServicesConnector")
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("Loading LocalAuthorizationServicesConnector");
                    yield return new LocalAuthorizationServicesConnector();
                    m_log.Info("LocalAuthorizationServicesConnector loaded for local authorization handling");
                }
            }
            
            // Load WorldMap module based on configuration
            if (configSource != null)
            {
                string[] configSections = new string[] { "Map", "Startup" };
                string worldMapModule = "";
                if(m_log.IsDebugEnabled) m_log.Debug("Checking WorldMapModule configuration...");

                foreach (var sectionName in configSections)
                {
                    var section = configSource.Configs[sectionName];
                    if (section != null)
                    {
                        worldMapModule = section.GetString("WorldMapModule", "");
                        if(m_log.IsDebugEnabled) m_log.DebugFormat("Section [{0}] WorldMapModule = '{1}'", sectionName, worldMapModule);
                        if (!string.IsNullOrEmpty(worldMapModule))
                            break;
                    }
                    else
                    {
                        if(m_log.IsDebugEnabled) m_log.DebugFormat("Section [{0}] not found", sectionName);
                    }
                }

                if (worldMapModule == "HGWorldMap")
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("Loading HGWorldMapModule");
                    yield return new HGWorldMapModule();
                }
                else if (worldMapModule == "WorldMap")
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("Loading WorldMapModule");
                    yield return new WorldMapModule();
                }
                else
                {
                    if(m_log.IsDebugEnabled) m_log.DebugFormat("No WorldMapModule loaded - configured value: '{0}'", worldMapModule);
                }
            }
            else
            {
                if(m_log.IsDebugEnabled) m_log.Debug("No config source provided for WorldMapModule");
            }

            // Load Warp3DImageModule for 3D map tile generation
            if (configSource != null)
            {
                string[] configSections = new string[] { "Map", "Startup" };
                string mapImageModule = "";
                if(m_log.IsDebugEnabled) m_log.Debug("Checking MapImageModule configuration...");

                foreach (var sectionName in configSections)
                {
                    var section = configSource.Configs[sectionName];
                    if (section != null)
                    {
                        mapImageModule = section.GetString("MapImageModule", "");
                        if(m_log.IsDebugEnabled) m_log.DebugFormat("Section [{0}] MapImageModule = '{1}'", sectionName, mapImageModule);
                        if (!string.IsNullOrEmpty(mapImageModule))
                            break;
                    }
                    else
                    {
                        if(m_log.IsDebugEnabled) m_log.DebugFormat("Section [{0}] not found", sectionName);
                    }
                }

                if (mapImageModule == "Warp3DImageModule")
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("Loading Warp3DImageModule for 3D map tile generation");
                    yield return new Warp3DImageModule();
                    if(m_log.IsInfoEnabled) m_log.Info("Warp3DImageModule loaded for 3D map tile generation with terrain texturing and prim rendering");
                }
                else if (!string.IsNullOrEmpty(mapImageModule))
                {
                    if(m_log.IsDebugEnabled) m_log.DebugFormat("MapImageModule configured as '{0}' but not 'Warp3DImageModule', skipping", mapImageModule);
                }
                else
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("No MapImageModule configured");
                }
            }
            else
            {
                if(m_log.IsDebugEnabled) m_log.Debug("No config source provided for MapImageModule");
            }
            
            // Load TerrainModule if enabled for terrain editing and management functionality
            if (configSource != null)
            {
                var modulesConfig = configSource.Configs["Modules"];
                if (modulesConfig?.GetBoolean("TerrainModule", true) == true)  // Default to true as it's essential for terrain functionality
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("Loading TerrainModule for terrain editing, height maps, and land management");
                    yield return new TerrainModule();
                    if(m_log.IsInfoEnabled) m_log.Info("TerrainModule loaded for terrain editing, RAW/R32/JPG/BMP/TIFF file support, and console commands");
                }
                else
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("TerrainModule disabled - terrain editing functionality will be limited");
                }
            }
            else
            {
                // Default behavior when no config source - load TerrainModule as it's essential
                if(m_log.IsDebugEnabled) m_log.Debug("No config source provided, loading TerrainModule by default");
                yield return new TerrainModule();
                if(m_log.IsInfoEnabled) m_log.Info("TerrainModule loaded by default for essential terrain functionality");
            }

            // Load VegetationModule if enabled for tree and grass creation functionality
            if (configSource != null)
            {
                var modulesConfig = configSource.Configs["Modules"];
                if (modulesConfig?.GetBoolean("VegetationModule", true) == true)  // Default to true as it's essential for vegetation functionality
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("Loading VegetationModule for tree and grass creation");
                    yield return new VegetationModule();
                    if(m_log.IsInfoEnabled) m_log.Info("VegetationModule loaded for tree and grass creation, phantom vegetation objects, and PCode support");
                }
                else
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("VegetationModule disabled - vegetation creation functionality will be limited");
                }
            }
            else
            {
                // Default behavior when no config source - load VegetationModule as it's essential
                if(m_log.IsDebugEnabled) m_log.Debug("No config source provided, loading VegetationModule by default");
                yield return new VegetationModule();
                if(m_log.IsInfoEnabled) m_log.Info("VegetationModule loaded by default for essential vegetation functionality");
            }

            // Load WindModule if enabled for wind simulation and environmental effects
            if (configSource != null)
            {
                var windConfig = configSource.Configs["Wind"];
                if (windConfig?.GetBoolean("enabled", true) == true)  // Default to true as wind is part of environmental simulation
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("Loading WindModule for wind simulation and environmental effects");
                    yield return new WindModule();
                    if(m_log.IsInfoEnabled) m_log.Info("WindModule loaded for wind simulation, environmental effects, and pluggable wind models");
                }
                else
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("WindModule disabled - wind simulation functionality will be unavailable");
                }
            }
            else
            {
                // Default behavior when no config source - load WindModule as it's part of environmental simulation
                if(m_log.IsDebugEnabled) m_log.Debug("No config source provided, loading WindModule by default");
                yield return new WindModule();
                if(m_log.IsInfoEnabled) m_log.Info("WindModule loaded by default for essential environmental wind simulation");
            }

            // Load EnvironmentModule if enabled for EEP and Windlight environment settings
            if (configSource != null)
            {
                var clientStackConfig = configSource.Configs["ClientStack.LindenCaps"];
                if (clientStackConfig?.GetString("Cap_EnvironmentSettings", string.Empty).Equals("localhost") == true)
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("Loading EnvironmentModule for EEP and Windlight environment settings");
                    yield return new EnvironmentModule();
                    if(m_log.IsInfoEnabled) m_log.Info("EnvironmentModule loaded for EEP environment settings, day/night cycles, and legacy Windlight support");
                }
                else
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("EnvironmentModule disabled - Cap_EnvironmentSettings not set to localhost");
                }
            }
            else
            {
                // Default behavior when no config source - EnvironmentModule disabled by default due to capability requirement
                if(m_log.IsDebugEnabled) m_log.Debug("No config source provided, EnvironmentModule not loaded (requires Cap_EnvironmentSettings = localhost)");
            }

            // Load DefaultDwellModule if enabled for parcel dwell (popularity) tracking
            if (configSource != null)
            {
                var dwellConfig = configSource.Configs["Dwell"];
                if (dwellConfig?.GetString("DwellModule", "DefaultDwellModule") == "DefaultDwellModule")
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("Loading DefaultDwellModule for parcel dwell tracking and land popularity");
                    yield return new DefaultDwellModule();
                    if(m_log.IsInfoEnabled) m_log.Info("DefaultDwellModule loaded for parcel dwell tracking, land popularity metrics, and client dwell requests");
                }
                else
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("DefaultDwellModule disabled - DwellModule not set to DefaultDwellModule");
                }
            }
            else
            {
                // Default behavior when no config source - DefaultDwellModule disabled by default
                if(m_log.IsDebugEnabled) m_log.Debug("No config source provided, DefaultDwellModule not loaded (requires [Dwell] section)");
            }

            // Essential capabilities module for viewer functionality
            yield return new CapabilitiesModule();
            
            // Essential UDP server for client communication
            yield return new LLUDPServerShim();

            // Load UserProfiles module based on configuration
            if (configSource != null)
            {
                var userProfilesConfig = configSource.Configs["UserProfiles"];
                if (userProfilesConfig != null)
                {
                    string profileServiceURL = userProfilesConfig.GetString("ProfileServiceURL", "");
                    if (!string.IsNullOrEmpty(profileServiceURL))
                    {
                        yield return new UserProfileModule();
                        m_log.Info("UserProfileModule loaded (advanced profile system)");
                    }
                    else
                    {
                        m_log.Info("UserProfileModule not loaded - ProfileServiceURL not configured");
                    }
                }
                else
                {
                    m_log.Info("UserProfileModule not loaded - no [UserProfiles] configuration section");
                }
            }

            // Essential caps modules for viewer functionality
            yield return new BunchOfCapsModule();
            yield return new EventQueueGetModule();
            yield return new SimulatorFeaturesModule();
            yield return new WebFetchInvDescModule();
            yield return new GetAssetsModule();
            yield return new FetchLibDescModule();
            yield return new MeshUploadFlagModule();
            yield return new RegionConsoleModule();
            yield return new ObjectAddModule();
            yield return new UploadObjectAssetModule();
            // Estate management - required by estate caps modules
            yield return new EstateManagementModule();
            
            if(m_log.IsDebugEnabled) m_log.Debug("Creating EstateAccessCapModule...");
            yield return new EstateAccessCapModule();
            yield return new EstateChangeInfoCapModule();

            // Load EntityTransferModule based on configuration
            if (configSource != null)
            {
                var modulesConfig = configSource.Configs["Modules"];
                string entityTransferModule = modulesConfig?.GetString("EntityTransferModule", "");
                if (entityTransferModule == "HGEntityTransferModule")
                {
                    yield return new HGEntityTransferModule();
                }
                else if (entityTransferModule == "BasicEntityTransferModule")
                {
                    yield return new EntityTransferModule();
                }
                
                // Load meshing module based on configuration - use same key as physics modules
                var startupConfig = configSource.Configs["Startup"];
                string meshingModule = startupConfig?.GetString("meshing", "Meshmerizer") ?? "Meshmerizer";
                switch (meshingModule.ToLowerInvariant())
                {
                    case "meshmerizer":
                        yield return new Meshmerizer();
                        break;
                    case "ubmeshmerizer":
                    case "ubodemeshmerizer":
                    case "ubODEMeshmerizer":
                        yield return new ubMeshmerizer();
                        break;
                    default:
                        // Default to Meshmerizer if unknown meshing module specified
                        yield return new Meshmerizer();
                        break;
                }
            }
            else
            {
                // Default to Meshmerizer if no config provided
                yield return new Meshmerizer();
            }

            // Physics modules - load based on configuration
            if (configSource != null)
            {
                var startupConfig = configSource.Configs["Startup"];
                string physics = startupConfig?.GetString("physics", "BulletSim") ?? "BulletSim";
                
                switch (physics.ToLowerInvariant())
                {
                    case "bulletsim":
                        yield return new BSScene();
                        yield return new ExtendedPhysics();
                        break;
                    case "ubode":
                        yield return new ubOdeModule();
                        break;
                    default:
                        // Default to BulletSim if unknown physics engine specified
                        yield return new BSScene();
                        yield return new ExtendedPhysics();
                        break;
                }
            }
            else
            {
                // Default to BulletSim if no config provided
                yield return new BSScene();
                yield return new ExtendedPhysics();
            }

            // Load MoapModule if enabled for Media on a Prim functionality
            if (configSource != null)
            {
                var mediaConfig = configSource.Configs["MediaOnAPrim"];
                if (mediaConfig != null)
                {
                    bool enableMediaOnAPrim = mediaConfig.GetBoolean("Enabled", false);
                    if (enableMediaOnAPrim)
                    {
                        if(m_log.IsDebugEnabled) m_log.Debug("Loading MoapModule for Media on a Prim functionality");
                        yield return new MoapModule();
                        if(m_log.IsInfoEnabled) m_log.Info("MoapModule loaded for Media on a Prim (MOAP) functionality");
                    }
                    else
                    {
                        if(m_log.IsDebugEnabled) m_log.Debug("MoapModule disabled in configuration");
                    }
                }
                else
                {
                    // Default disabled if no MediaOnAPrim config section (matches original behavior)
                    if(m_log.IsDebugEnabled) m_log.Debug("MoapModule not loaded - no [MediaOnAPrim] configuration section found");
                }
            }
            else
            {
                // Default disabled if no config source (matches original behavior)
                if(m_log.IsDebugEnabled) m_log.Debug("MoapModule not loaded - no config source provided");
            }

            // Load ScriptModuleCommsModule if enabled for script-to-module communication and function registration
            if (configSource != null)
            {
                var modulesConfig = configSource.Configs["Modules"];
                if (modulesConfig?.GetBoolean("ScriptModuleCommsModule", true) == true)  // Default to true as it's essential for script interop
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("Loading ScriptModuleCommsModule for script-to-module communication");
                    yield return new ScriptModuleCommsModule();
                    if(m_log.IsInfoEnabled) m_log.Info("ScriptModuleCommsModule loaded for script-to-module communication and function registration");
                }
                else
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("ScriptModuleCommsModule disabled - set ScriptModuleCommsModule = true in [Modules] to enable script-to-module communication");
                }
            }
            else
            {
                // Default to loading if no config source (essential module)
                if(m_log.IsDebugEnabled) m_log.Debug("Loading ScriptModuleCommsModule (no config source - using default)");
                yield return new ScriptModuleCommsModule();
                if(m_log.IsInfoEnabled) m_log.Info("ScriptModuleCommsModule loaded for script-to-module communication and function registration");
            }

            // Load WorldCommModule if enabled for LSL script communication and llListen functionality
            if (configSource != null)
            {
                var modulesConfig = configSource.Configs["Modules"];
                if (modulesConfig?.GetBoolean("WorldCommModule", true) == true)  // Default to true as it's essential for LSL chat functionality
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("Loading WorldCommModule for LSL script communication and chat listening");
                    yield return new WorldCommModule();
                    if(m_log.IsInfoEnabled) m_log.Info("WorldCommModule loaded for LSL script communication, llListen, and chat filtering");
                }
                else
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("WorldCommModule disabled - set WorldCommModule = true in [Modules] to enable LSL script communication");
                }
            }
            else
            {
                // Default to loading if no config source (essential module for script functionality)
                if(m_log.IsDebugEnabled) m_log.Debug("Loading WorldCommModule (no config source - using default)");
                yield return new WorldCommModule();
                if(m_log.IsInfoEnabled) m_log.Info("WorldCommModule loaded for LSL script communication, llListen, and chat filtering");
            }

            // Load HttpRequestModule if enabled for LSL HTTP request functionality and llHTTPRequest
            if (configSource != null)
            {
                var modulesConfig = configSource.Configs["Modules"];
                if (modulesConfig?.GetBoolean("HttpRequestModule", true) == true)  // Default to true as it's essential for LSL HTTP functionality
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("Loading HttpRequestModule for LSL HTTP requests and web service integration");
                    yield return new HttpRequestModule();
                    if(m_log.IsInfoEnabled) m_log.Info("HttpRequestModule loaded for LSL HTTP requests, llHTTPRequest, and web service integration");
                }
                else
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("HttpRequestModule disabled - set HttpRequestModule = true in [Modules] to enable LSL HTTP functionality");
                }
            }
            else
            {
                // Default to loading if no config source (essential module for script HTTP functionality)
                if(m_log.IsDebugEnabled) m_log.Debug("Loading HttpRequestModule (no config source - using default)");
                yield return new HttpRequestModule();
                if(m_log.IsInfoEnabled) m_log.Info("HttpRequestModule loaded for LSL HTTP requests, llHTTPRequest, and web service integration");
            }


        }

        /// <summary>
        /// Creates shared modules that are instantiated once per OpenSim instance
        /// Reads configuration to determine which modules to load
        /// </summary>
        public static IEnumerable<ISharedRegionModule> CreateSharedModules(IConfigSource configSource)
        {
            // Load modules based on configuration
            var modulesConfig = configSource.Configs["Modules"];
            var messagingConfig = configSource.Configs["Messaging"];

            // Load J2KDecoderModule first - essential for texture decoding functionality
            if (modulesConfig?.GetBoolean("J2KDecoderModule", true) == true)  // Default to true as it's essential
            {
                if(m_log.IsDebugEnabled) m_log.Debug("Loading J2KDecoderModule for JPEG2000 texture decoding");
                yield return new J2KDecoderModule();
                if(m_log.IsInfoEnabled) m_log.Info("J2KDecoderModule loaded for JPEG2000 texture decoding and layer boundary analysis");
            }
            else
            {
                if(m_log.IsDebugEnabled) m_log.Debug("J2KDecoderModule disabled - texture decoding functionality will be limited");
            }

            // Load AssetService first - required by LibraryModule and other services
            string assetServicesModule = modulesConfig?.GetString("AssetServices", "");
            if (assetServicesModule == "RegionAssetConnector")
            {
                yield return new RegionAssetConnector();
            }

            // Load LibraryModule after AssetService - it provides LibraryService needed by other modules
            if (modulesConfig?.GetBoolean("LibraryModule", false) == true)
            {
                yield return new LibraryModule();
            }

            // Always load core modules that don't have configuration options
            yield return new ChatModule();
            yield return new InventoryTransferModule();
            yield return new InstantMessageModule();
            yield return new MuteListModule();
            yield return new PresenceModule();

            // Load XMLRPCModule if enabled for XMLRPC functionality and llRemoteData
            if (modulesConfig?.GetBoolean("XMLRPCModule", true) == true)  // Default to true as it's essential for LSL XMLRPC functionality
            {
                if(m_log.IsDebugEnabled) m_log.Debug("Loading XMLRPCModule for LSL XMLRPC support and remote data channels");
                yield return new XMLRPCModule();
                if(m_log.IsInfoEnabled) m_log.Info("XMLRPCModule loaded for LSL XMLRPC support, llRemoteData functions, and external API integration");
            }
            else
            {
                if(m_log.IsDebugEnabled) m_log.Debug("XMLRPCModule disabled - set XMLRPCModule = true in [Modules] to enable LSL XMLRPC functionality");
            }

            // Load EmailModule based on configuration
            var startupConfig = configSource?.Configs["Startup"];
            string emailModule = startupConfig?.GetString("emailmodule", "");
            if (emailModule == "DefaultEmailModule")
            {
                if(m_log.IsDebugEnabled) m_log.Debug("Loading EmailModule (DefaultEmailModule)");
                yield return new EmailModule();
            }
            else if (!string.IsNullOrEmpty(emailModule))
            {
                m_log.InfoFormat("EmailModule configured as '{0}' but not 'DefaultEmailModule', skipping", emailModule);
            }
            else
            {
                if(m_log.IsDebugEnabled) m_log.Debug("No emailmodule configured, skipping email functionality");
            }
            
            // Load VivoxVoiceModule based on configuration
            var vivoxConfig = configSource?.Configs["VivoxVoice"];
            if (vivoxConfig?.GetBoolean("enabled", false) == true)
            {
                if(m_log.IsDebugEnabled) m_log.Debug("Loading VivoxVoiceModule");
                var vivoxModuleInstance = LoadVivoxVoiceModule();
                if (vivoxModuleInstance != null)
                {
                    yield return vivoxModuleInstance;
                    m_log.Info("VivoxVoiceModule loaded successfully");
                }
                else
                {
                    m_log.Warn("VivoxVoiceModule was configured ([VivoxVoice] enabled = true) but could not be loaded. Check that OpenSim.Region.OptionalModules.dll is available.");
                }
            }
            else if (vivoxConfig != null)
            {
                if(m_log.IsDebugEnabled) m_log.Debug("VivoxVoiceModule disabled by configuration ([VivoxVoice] enabled = false)");
            }
            else
            {
                if(m_log.IsDebugEnabled) m_log.Debug("No VivoxVoice configuration section found, skipping VivoxVoiceModule");
            }
            
            // Load AgentPreferencesModule directly for reliable loading
            yield return new AgentPreferencesModule();
            
            var groupsConfig = configSource.Configs["Groups"];

            // Load Friends module based on configuration
            if (modulesConfig?.GetString("FriendsModule", "") == "HGFriendsModule")
                yield return new HGFriendsModule();
            else
                yield return new FriendsModule();

            // Note: FriendsCommandsModule is in OptionalModules and cannot be loaded from CoreModules
            // It requires manual loading or a separate OptionalModules factory

            // Load HGMessageTransferModule (supports both hypergrid and local scenarios)
            yield return new HGMessageTransferModule();

            // Load HGLureModule - supports both local and cross-grid lures
            yield return new HGLureModule();

            // Load Offline IM module based on configuration
            string offlineIMModule = messagingConfig?.GetString("OfflineMessageModule", "");
            if(m_log.IsDebugEnabled) m_log.DebugFormat("OfflineMessageModule configuration: '{0}'", offlineIMModule);
            
            if (offlineIMModule == "Offline Message Module V2")
            {
                if(m_log.IsDebugEnabled) m_log.Debug("Offline Message Module V2 configured, attempting to load");
                // Try to load OfflineIMRegionModule using reflection to avoid hard dependency
                var offlineIMModuleInstance = LoadOfflineIMModuleV2();
                if (offlineIMModuleInstance != null)
                {
                    yield return offlineIMModuleInstance;
                }
                else
                {
                    m_log.Warn("Offline Message Module V2 was configured but could not be loaded. Check that OpenSim.Addons.OfflineIM.dll is available.");
                }
            }
            else if (!string.IsNullOrEmpty(offlineIMModule))
            {
                if(m_log.IsDebugEnabled) m_log.DebugFormat("OfflineMessageModule configured as '{0}' but not 'Offline Message Module V2', skipping", offlineIMModule);
            }
            else
            {
                if(m_log.IsDebugEnabled) m_log.Debug("No OfflineMessageModule configured, skipping offline IM loading");
            }

            // Load UserManagement module based on configuration
            if (modulesConfig?.GetString("UserManagementModule", "") == "HGUserManagementModule")
            {
                yield return new HGUserManagementModule();
                m_log.Info("HGUserManagementModule loaded");
            }
            else
            {
                yield return new UserManagementModule();
                m_log.Info("UserManagementModule loaded");
            }


            // Groups Module V2 loading is handled later in the factory with proper configuration checks

            // Load GridService module based on configuration
            string gridServicesModule = modulesConfig?.GetString("GridServices", "");
            if (gridServicesModule == "RegionGridServicesConnector")
            {
                yield return new RegionGridServicesConnector();
            }

            // Load UserAccountService module based on configuration
            string userAccountServicesModule = modulesConfig?.GetString("UserAccountServices", "");
            if (userAccountServicesModule == "RemoteUserAccountServicesConnector")
            {
                yield return new RemoteUserAccountServicesConnector();
            }

            // Load NeighbourServices module based on configuration
            string neighbourServicesModule = modulesConfig?.GetString("NeighbourServices", "");
            if (neighbourServicesModule == "NeighbourServicesOutConnector")
            {
                yield return new NeighbourServicesOutConnector();
            }

            // Load SimulationServices module based on configuration
            string simulationServicesModule = modulesConfig?.GetString("SimulationServices", "");
            if (simulationServicesModule == "RemoteSimulationConnectorModule")
            {
                yield return new RemoteSimulationConnectorModule();
            }

            // Load SimulationServiceInConnector if enabled
            if (modulesConfig?.GetBoolean("SimulationServiceInConnector", false) == true)
            {
                if(m_log.IsDebugEnabled) m_log.Debug("Loading SimulationServiceInConnectorModule");
                yield return new SimulationServiceInConnectorModule();
                m_log.Info("SimulationServiceInConnectorModule loaded for incoming simulation requests (agent/object transfers)");
            }

            // Load LandServiceInConnector if enabled
            if (modulesConfig?.GetBoolean("LandServiceInConnector", false) == true)
            {
                if(m_log.IsDebugEnabled) m_log.Debug("Loading LandServiceInConnectorModule");
                yield return new LandServiceInConnectorModule();
                m_log.Info("LandServiceInConnectorModule loaded for incoming land data requests");
            }

            // Load NeighbourServiceInConnector if enabled
            if (modulesConfig?.GetBoolean("NeighbourServiceInConnector", false) == true)
            {
                if(m_log.IsDebugEnabled) m_log.Debug("Loading NeighbourServiceInConnectorModule");
                yield return new NeighbourServiceInConnectorModule();
                m_log.Info("NeighbourServiceInConnectorModule loaded for incoming neighbour region requests");
            }







            // Load InventoryServices module based on configuration
            string inventoryServicesModule = modulesConfig?.GetString("InventoryServices", "");
            if (inventoryServicesModule == "HGInventoryBroker")
            {
                yield return new HGInventoryBroker();
            }
            else if (inventoryServicesModule == "RemoteXInventoryServicesConnector")
            {
                yield return new RemoteXInventoryServicesConnector();
            }

            // Load PresenceServices module based on configuration
            string presenceServicesModule = modulesConfig?.GetString("PresenceServices", "");
            if (presenceServicesModule == "RemotePresenceServicesConnector")
            {
                yield return new RemotePresenceServicesConnector();
            }

            // Load AuthenticationServices module based on configuration
            string authenticationServicesModule = modulesConfig?.GetString("AuthenticationServices", "");
            if (authenticationServicesModule == "RemoteAuthenticationServicesConnector")
            {
                if(m_log.IsDebugEnabled) m_log.Debug("Loading RemoteAuthenticationServicesConnector");
                yield return new RemoteAuthenticationServicesConnector();
                m_log.Info("RemoteAuthenticationServicesConnector loaded for remote authentication handling");
            }

            // Load AuthorizationServices module based on configuration
            string authorizationServicesModule = modulesConfig?.GetString("AuthorizationServices", "");
            if (authorizationServicesModule == "RemoteAuthorizationServicesConnector")
            {
                yield return new RemoteAuthorizationServicesConnector();
            }

            // Load AvatarServices module based on configuration
            string avatarServicesModule = modulesConfig?.GetString("AvatarServices", "");
            if (avatarServicesModule == "RemoteAvatarServicesConnector")
            {
                if(m_log.IsDebugEnabled) m_log.Debug("Loading RemoteAvatarServicesConnector");
                yield return new RemoteAvatarServicesConnector();
                m_log.Info("RemoteAvatarServicesConnector loaded for remote avatar data handling");
            }

            // Load GridUserServices module based on configuration
            string gridUserServicesModule = modulesConfig?.GetString("GridUserServices", "");
            if (gridUserServicesModule == "RemoteGridUserServicesConnector")
            {
                if(m_log.IsDebugEnabled) m_log.Debug("Loading RemoteGridUserServicesConnector");
                yield return new RemoteGridUserServicesConnector();
                m_log.Info("RemoteGridUserServicesConnector loaded for remote grid user data handling");
            }

            // Load LandServices module based on configuration
            string landServicesModule = modulesConfig?.GetString("LandServices", "");
            if (landServicesModule == "RemoteLandServicesConnector")
            {
                yield return new RemoteLandServicesConnector();
            }

            // Load MuteListServices module based on configuration
            string muteListServicesModule = modulesConfig?.GetString("MuteListService", "");
            if (muteListServicesModule == "RemoteMuteListServicesConnector")
            {
                yield return new RemoteMuteListServicesConnector();
            }

            // Load AgentPreferencesServices module based on configuration
            string agentPreferencesServicesModule = modulesConfig?.GetString("AgentPreferencesServices", "");
            if (agentPreferencesServicesModule == "RemoteAgentPreferencesServicesConnector")
            {
                if(m_log.IsDebugEnabled) m_log.Debug("Loading RemoteAgentPreferencesServicesConnector");
                yield return new RemoteAgentPreferencesServicesConnector();
                m_log.Info("RemoteAgentPreferencesServicesConnector loaded for remote agent preferences handling");
            }

            // Load ViewerStatsModule (WebStatsModule) based on configuration
            // WebStatsModule checks for [WebStats] enabled = true in its own Initialise method
            var webStatsConfig = configSource?.Configs["WebStats"];
            if (webStatsConfig?.GetBoolean("enabled", false) == true)
            {
                var viewerStatsModuleInstance = LoadViewerStatsModule();
                if (viewerStatsModuleInstance != null)
                {
                    yield return viewerStatsModuleInstance;
                    m_log.Info("ViewerStatsModule (WebStatsModule) loaded");
                }
                else
                {
                    m_log.Warn("ViewerStatsModule was configured ([WebStats] enabled = true) but could not be loaded. Check that OpenSim.Region.OptionalModules.dll is available.");
                }
            }
            else
            {
                m_log.Info("ViewerStatsModule (WebStatsModule) disabled by configuration ([WebStats] enabled = false)");
            }

            // Load External Data Generator (DataSnapshotManager) based on configuration
            // DataSnapshotManager checks for [DataSnapshot] index_sims = true in its own Initialise method
            var dataSnapshotConfig = configSource?.Configs["DataSnapshot"];
            if (dataSnapshotConfig?.GetBoolean("index_sims", false) == true)
            {
                var dataSnapshotModuleInstance = LoadDataSnapshotModule();
                if (dataSnapshotModuleInstance != null)
                {
                    yield return dataSnapshotModuleInstance;
                    m_log.Info("DataSnapshotManager (External Data Generator) loaded");
                }
                else
                {
                    m_log.Warn("DataSnapshot was configured ([DataSnapshot] index_sims = true) but could not be loaded. Check that OpenSim.Region.OptionalModules.dll is available.");
                }
            }
            else
            {
                m_log.Info("DataSnapshotManager (External Data Generator) disabled by configuration ([DataSnapshot] index_sims = false)");
            }

            // Load NPCModule based on configuration
            if (modulesConfig?.GetBoolean("NPCModule", false) == true)
            {
                var npcModuleInstance = LoadNPCModule();
                if (npcModuleInstance != null)
                {
                    yield return npcModuleInstance;
                }
                else
                {
                    m_log.Warn("NPCModule was configured but could not be loaded. Check that OpenSim.Region.OptionalModules.dll is available.");
                }
            }

            // Load BetaGridLikeMoneyModule (SampleMoneyModule) - dynamic loading with fallback behavior
            string economyModule = modulesConfig?.GetString("economymodule", "");
            if (economyModule == "BetaGridLikeMoneyModule")
            {
                m_log.Info("Loading BetaGridLikeMoneyModule (SampleMoneyModule) as explicitly requested by configuration");
                var moneyModuleInstance = LoadBetaGridLikeMoneyModule();
                if (moneyModuleInstance != null)
                {
                    yield return moneyModuleInstance;
                    m_log.Info("BetaGridLikeMoneyModule loaded successfully");
                }
                else
                {
                    m_log.Warn("BetaGridLikeMoneyModule was configured but could not be loaded. Check that OpenSim.Region.OptionalModules.dll is available.");
                }
            }
            else if (!string.IsNullOrEmpty(economyModule))
            {
                m_log.InfoFormat("BetaGridLikeMoneyModule not loaded - economymodule configured as '{0}'", economyModule);
            }
            else
            {
                // Dynamic loading: attempt to load BetaGridLikeMoneyModule when no economy module is configured
                m_log.Info("No economymodule configured - attempting dynamic loading of BetaGridLikeMoneyModule (SampleMoneyModule)");
                var moneyModuleInstance = LoadBetaGridLikeMoneyModule();
                if (moneyModuleInstance != null)
                {
                    yield return moneyModuleInstance;
                    m_log.Info("BetaGridLikeMoneyModule loaded successfully via dynamic loading");
                }
                else
                {
                    m_log.Info("BetaGridLikeMoneyModule not available for dynamic loading - no economy module will be active");
                }
            }

            // Load Groups Module V2 based on configuration
            // Groups Module V2 checks for [Groups] Enabled = true and Module = "Groups Module V2" in its own Initialise method
            var groupsV2Config = configSource?.Configs["Groups"];
            if (groupsV2Config?.GetBoolean("Enabled", false) == true && 
                groupsV2Config?.GetString("Module", "") == "Groups Module V2")
            {
                var groupsV2ModuleInstance = LoadGroupsModuleV2();
                if (groupsV2ModuleInstance != null)
                {
                    yield return groupsV2ModuleInstance;
                    m_log.Info("Groups Module V2 loaded");
                }
                else
                {
                    m_log.Warn("Groups Module V2 was configured ([Groups] Enabled = true, Module = \"Groups Module V2\") but could not be loaded. Check that OpenSim.Addons.Groups.dll is available.");
                }

                // Load Groups Messaging Module V2 when Groups V2 is enabled
                var groupsMessagingV2ModuleInstance = LoadGroupsMessagingModuleV2();
                if (groupsMessagingV2ModuleInstance != null)
                {
                    yield return groupsMessagingV2ModuleInstance;
                    m_log.Info("Groups Messaging Module V2 loaded");
                }
                else
                {
                    m_log.Warn("Groups Messaging Module V2 could not be loaded. Check that OpenSim.Addons.Groups.dll is available.");
                }

                // Load GroupsServiceHGConnectorModule when Groups V2 is enabled
                string groupsConnectorModule = groupsV2Config?.GetString("ServicesConnectorModule", "");
                if (groupsConnectorModule == "Groups HG Service Connector")
                {
                    var groupsHGConnectorInstance = LoadGroupsServiceHGConnectorModule();
                    if (groupsHGConnectorInstance != null)
                    {
                        yield return groupsHGConnectorInstance;
                        m_log.Info("GroupsServiceHGConnectorModule loaded");
                    }
                    else
                    {
                        m_log.Warn("GroupsServiceHGConnectorModule was configured but could not be loaded. Check that OpenSim.Addons.Groups.dll is available.");
                    }
                }
            }
            else if (groupsV2Config?.GetBoolean("Enabled", false) == true)
            {
                m_log.Info("Groups Module V2 disabled by configuration ([Groups] Module != \"Groups Module V2\")");
            }
            else
            {
                m_log.Info("Groups Module V2 disabled by configuration ([Groups] Enabled = false)");
            }

            // Load PhysicsParameters module based on configuration using reflection
            if (modulesConfig?.GetBoolean("PhysicsParametersModule", false) == true)
            {
                var physicsParametersModule = LoadPhysicsParametersModule();
                if (physicsParametersModule != null)
                {
                    yield return physicsParametersModule;
                }
                else
                {
                    m_log.Warn("PhysicsParametersModule was configured but could not be loaded. Check that OpenSim.Region.OptionalModules.dll is available.");
                }
            }

            // Estate management module - always load as it's essential for estate management

            // Essential shared caps modules - loaded at runtime to avoid circular dependencies
            foreach (var capsModule in CreateCapsModules())
            {
                if (capsModule != null)
                    yield return capsModule;
            }

            // Load LoadImageURLModule for dynamic texture loading from URLs
            if (modulesConfig?.GetBoolean("LoadImageURLModule", true) == true)  // Default to true for backward compatibility
            {
                if(m_log.IsDebugEnabled) m_log.Debug("Loading LoadImageURLModule for dynamic texture image loading from URLs");
                yield return new LoadImageURLModule();
                if(m_log.IsInfoEnabled) m_log.Info("LoadImageURLModule loaded for dynamic texture image loading from URLs");
            }
            else
            {
                if(m_log.IsDebugEnabled) m_log.Debug("LoadImageURLModule disabled - set LoadImageURLModule = true in [Modules] to enable dynamic texture image loading");
            }

            // Load VectorRenderModule for dynamic texture generation using vector graphics
            if (modulesConfig?.GetBoolean("VectorRenderModule", true) == true)  // Default to true for backward compatibility
            {
                if(m_log.IsDebugEnabled) m_log.Debug("Loading VectorRenderModule for dynamic texture generation using vector graphics");
                yield return new VectorRenderModule();
                if(m_log.IsInfoEnabled) m_log.Info("VectorRenderModule loaded for dynamic texture generation using vector graphics");
            }
            else
            {
                if(m_log.IsDebugEnabled) m_log.Debug("VectorRenderModule disabled - set VectorRenderModule = true in [Modules] to enable vector graphics rendering");
            }
        }

        /// <summary>
        /// Loads BetaGridLikeMoneyModule (SampleMoneyModule) using reflection to avoid hard dependency
        /// </summary>
        private static ISharedRegionModule LoadBetaGridLikeMoneyModule()
        {
            try
            {
                m_log.Debug("Attempting to load BetaGridLikeMoneyModule (SampleMoneyModule)");
                
                // First, try to explicitly load the OptionalModules assembly
                System.Reflection.Assembly optionalModulesAssembly = null;
                try
                {
                    optionalModulesAssembly = System.Reflection.Assembly.LoadFrom("OpenSim.Region.OptionalModules.dll");
                    m_log.DebugFormat("Successfully loaded OptionalModules assembly: {0}", optionalModulesAssembly.FullName);
                }
                catch (Exception ex)
                {
                    m_log.DebugFormat("Could not explicitly load OptionalModules assembly: {0}", ex.Message);
                }
                
                // Debug: List all loaded assemblies
                var loadedAssemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                m_log.DebugFormat("Total loaded assemblies: {0}", loadedAssemblies.Length);
                foreach (var assembly in loadedAssemblies)
                {
                    if (assembly.FullName.Contains("OptionalModules"))
                    {
                        m_log.DebugFormat("Found OptionalModules assembly: {0}", assembly.FullName);
                    }
                }
                
                // Try to find the SampleMoneyModule type in any loaded assembly
                Type moneyModuleType = null;
                foreach (var assembly in loadedAssemblies)
                {
                    moneyModuleType = assembly.GetType("OpenSim.Region.OptionalModules.World.MoneyModule.SampleMoneyModule");
                    if (moneyModuleType != null)
                    {
                        m_log.DebugFormat("Found SampleMoneyModule type in assembly: {0}", assembly.FullName);
                        break;
                    }
                }

                if (moneyModuleType != null)
                {
                    m_log.Debug("Creating instance of SampleMoneyModule");
                    var moduleInstance = Activator.CreateInstance(moneyModuleType) as ISharedRegionModule;
                    if (moduleInstance != null)
                    {
                        m_log.InfoFormat("Successfully created BetaGridLikeMoneyModule instance: {0}", moduleInstance.Name);
                        return moduleInstance;
                    }
                    else
                    {
                        m_log.Error("Failed to cast SampleMoneyModule instance to ISharedRegionModule");
                    }
                }
                else
                {
                    m_log.Warn("SampleMoneyModule type not found in any loaded assembly. Ensure OpenSim.Region.OptionalModules.dll is loaded.");
                }
            }
            catch (Exception ex)
            {
                m_log.ErrorFormat("Exception while loading BetaGridLikeMoneyModule: {0}", ex);
            }

            return null;
        }

        /// <summary>
        /// Loads NPCModule using reflection to avoid hard dependency
        /// </summary>
        private static ISharedRegionModule LoadNPCModule()
        {
            try
            {
                // Try to find the NPCModule type in any loaded assembly
                Type npcModuleType = null;
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    npcModuleType = assembly.GetType("OpenSim.Region.OptionalModules.World.NPC.NPCModule");
                    if (npcModuleType != null)
                        break;
                }

                if (npcModuleType != null)
                {
                    var moduleInstance = Activator.CreateInstance(npcModuleType) as ISharedRegionModule;
                    return moduleInstance;
                }
            }
            catch (Exception ex)
            {
                m_log.WarnFormat("Could not load NPCModule: {0}", ex.Message);
            }

            return null;
        }

        /// <summary>
        /// Loads External Data Generator (DataSnapshotManager) using reflection to avoid hard dependency
        /// </summary>
        private static ISharedRegionModule LoadDataSnapshotModule()
        {
            try
            {
                // Try to find the DataSnapshotManager type in any loaded assembly
                Type dataSnapshotModuleType = null;
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    dataSnapshotModuleType = assembly.GetType("OpenSim.Region.DataSnapshot.DataSnapshotManager");
                    if (dataSnapshotModuleType != null)
                        break;
                }

                if (dataSnapshotModuleType != null)
                {
                    var moduleInstance = Activator.CreateInstance(dataSnapshotModuleType) as ISharedRegionModule;
                    return moduleInstance;
                }
            }
            catch (Exception ex)
            {
                m_log.WarnFormat("Could not load DataSnapshotManager: {0}", ex.Message);
            }

            return null;
        }

        /// <summary>
        /// Loads ViewerStatsModule (WebStatsModule) using reflection to avoid hard dependency
        /// </summary>
        private static ISharedRegionModule LoadViewerStatsModule()
        {
            try
            {
                // Try to find the WebStatsModule type in any loaded assembly
                Type viewerStatsModuleType = null;
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    viewerStatsModuleType = assembly.GetType("OpenSim.Region.UserStatistics.WebStatsModule");
                    if (viewerStatsModuleType != null)
                        break;
                }

                if (viewerStatsModuleType != null)
                {
                    var moduleInstance = Activator.CreateInstance(viewerStatsModuleType) as ISharedRegionModule;
                    return moduleInstance;
                }
            }
            catch (Exception ex)
            {
                m_log.WarnFormat("Could not load ViewerStatsModule: {0}", ex.Message);
            }

            return null;
        }

        /// <summary>
        /// Loads OfflineIMRegionModule V2 using reflection to avoid hard dependency
        /// </summary>
        private static ISharedRegionModule LoadOfflineIMModuleV2()
        {
            try
            {
                if(m_log.IsDebugEnabled) m_log.Debug("Attempting to load OfflineIMRegionModule V2 via reflection");
                
                // First, try to load the assembly if it's not already loaded
                try
                {
                    var assemblyPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "OpenSim.Addons.OfflineIM.dll");
                    if(m_log.IsDebugEnabled) m_log.DebugFormat("Looking for assembly at: {0}", assemblyPath);
                    
                    if (System.IO.File.Exists(assemblyPath))
                    {
                        var assembly = System.Reflection.Assembly.LoadFrom(assemblyPath);
                        if(m_log.IsDebugEnabled) m_log.DebugFormat("Successfully loaded assembly: {0}", assembly.FullName);
                    }
                    else
                    {
                        if(m_log.IsDebugEnabled) m_log.Debug("OpenSim.Addons.OfflineIM.dll not found in base directory");
                    }
                }
                catch (Exception loadEx)
                {
                    if(m_log.IsDebugEnabled) m_log.DebugFormat("Could not load OpenSim.Addons.OfflineIM.dll: {0}", loadEx.Message);
                }
                
                // Try to find the OfflineIMRegionModule type in any loaded assembly
                Type offlineIMModuleType = null;
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    if(m_log.IsDebugEnabled) m_log.DebugFormat("Checking assembly: {0}", assembly.FullName);
                    
                    offlineIMModuleType = assembly.GetType("OpenSim.OfflineIM.OfflineIMRegionModule");
                    if (offlineIMModuleType != null)
                    {
                        if(m_log.IsDebugEnabled) m_log.DebugFormat("Found OfflineIMRegionModule V2 in assembly: {0}", assembly.FullName);
                        break;
                    }
                }

                if (offlineIMModuleType != null)
                {
                    var moduleInstance = Activator.CreateInstance(offlineIMModuleType) as ISharedRegionModule;
                    if (moduleInstance != null)
                    {
                        if(m_log.IsDebugEnabled) m_log.Debug("Successfully loaded OfflineIMRegionModule V2");
                        return moduleInstance;
                    }
                    else
                    {
                        m_log.Warn("OfflineIMRegionModule V2 type found but could not cast to ISharedRegionModule");
                    }
                }
                else
                {
                    m_log.Warn("OfflineIMRegionModule V2 type not found in any loaded assembly");
                    if(m_log.IsDebugEnabled) 
                    {
                        m_log.Debug("Available assemblies:");
                        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                        {
                            m_log.DebugFormat("  - {0}", assembly.FullName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                m_log.WarnFormat("Could not load OfflineIMRegionModule V2: {0}", ex.Message);
            }

            return null;
        }

        /// <summary>
        /// Loads PhysicsParameters module using reflection to avoid hard dependency
        /// </summary>
        private static ISharedRegionModule LoadPhysicsParametersModule()
        {
            try
            {
                // Try to find the PhysicsParameters type in any loaded assembly
                Type physicsParametersType = null;
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    physicsParametersType = assembly.GetType("OpenSim.Region.OptionalModules.PhysicsParameters.PhysicsParameters");
                    if (physicsParametersType != null)
                        break;
                }

                if (physicsParametersType != null)
                {
                    var moduleInstance = Activator.CreateInstance(physicsParametersType) as ISharedRegionModule;
                    return moduleInstance;
                }
            }
            catch (Exception ex)
            {
                m_log.WarnFormat("Could not load PhysicsParameters module: {0}", ex.Message);
            }

            return null;
        }

        /// <summary>
        /// Loads Groups Module V2 using reflection to avoid hard dependency
        /// </summary>
        private static ISharedRegionModule LoadGroupsModuleV2()
        {
            try
            {
                // Try to load OpenSim.Addons.Groups.dll if not already loaded
                try
                {
                    Assembly.LoadFrom("OpenSim.Addons.Groups.dll");
                }
                catch (Exception loadEx)
                {
                    if(m_log.IsDebugEnabled) m_log.DebugFormat("Could not load OpenSim.Addons.Groups.dll: {0}", loadEx.Message);
                }
                
                // Try to find the GroupsModule type in any loaded assembly
                Type groupsModuleV2Type = null;
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    if(m_log.IsDebugEnabled) m_log.DebugFormat("Checking assembly: {0}", assembly.FullName);
                    
                    groupsModuleV2Type = assembly.GetType("OpenSim.Groups.GroupsModule");
                    if (groupsModuleV2Type != null)
                    {
                        if(m_log.IsDebugEnabled) m_log.DebugFormat("Found Groups Module V2 in assembly: {0}", assembly.FullName);
                        break;
                    }
                }

                if (groupsModuleV2Type != null)
                {
                    var moduleInstance = Activator.CreateInstance(groupsModuleV2Type) as ISharedRegionModule;
                    if (moduleInstance != null)
                    {
                        if(m_log.IsDebugEnabled) m_log.Debug("Successfully loaded Groups Module V2");
                        return moduleInstance;
                    }
                    else
                    {
                        m_log.Warn("Groups Module V2 type found but could not cast to ISharedRegionModule");
                    }
                }
                else
                {
                    m_log.Warn("Groups Module V2 type not found in any loaded assembly");
                    if(m_log.IsDebugEnabled) 
                    {
                        m_log.Debug("Available assemblies:");
                        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                        {
                            m_log.DebugFormat("  - {0}", assembly.FullName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                m_log.WarnFormat("Could not load Groups Module V2: {0}", ex.Message);
            }

            return null;
        }

        /// <summary>
        /// Loads Groups Messaging Module V2 using reflection to avoid hard dependency
        /// </summary>
        private static ISharedRegionModule LoadGroupsMessagingModuleV2()
        {
            try
            {
                // Try to load OpenSim.Addons.Groups.dll if not already loaded
                try
                {
                    Assembly.LoadFrom("OpenSim.Addons.Groups.dll");
                }
                catch (Exception loadEx)
                {
                    if(m_log.IsDebugEnabled) m_log.DebugFormat("Could not load OpenSim.Addons.Groups.dll: {0}", loadEx.Message);
                }
                
                // Try to find the GroupsMessagingModule type in any loaded assembly
                Type groupsMessagingModuleV2Type = null;
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    if(m_log.IsDebugEnabled) m_log.DebugFormat("Checking assembly for Groups Messaging Module V2: {0}", assembly.FullName);
                    
                    groupsMessagingModuleV2Type = assembly.GetType("OpenSim.Groups.GroupsMessagingModule");
                    if (groupsMessagingModuleV2Type != null)
                    {
                        if(m_log.IsDebugEnabled) m_log.DebugFormat("Found Groups Messaging Module V2 in assembly: {0}", assembly.FullName);
                        break;
                    }
                }

                if (groupsMessagingModuleV2Type != null)
                {
                    var moduleInstance = Activator.CreateInstance(groupsMessagingModuleV2Type) as ISharedRegionModule;
                    if (moduleInstance != null)
                    {
                        if(m_log.IsDebugEnabled) m_log.Debug("Successfully loaded Groups Messaging Module V2");
                        return moduleInstance;
                    }
                    else
                    {
                        m_log.Warn("Groups Messaging Module V2 type found but could not cast to ISharedRegionModule");
                    }
                }
                else
                {
                    m_log.Warn("Groups Messaging Module V2 type not found in any loaded assembly");
                    if(m_log.IsDebugEnabled) 
                    {
                        m_log.Debug("Available assemblies:");
                        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                        {
                            m_log.DebugFormat("  - {0}", assembly.FullName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                m_log.WarnFormat("Could not load Groups Messaging Module V2: {0}", ex.Message);
            }

            return null;
        }

        /// <summary>
        /// Loads GroupsServiceHGConnectorModule using reflection to avoid hard dependency
        /// </summary>
        private static ISharedRegionModule LoadGroupsServiceHGConnectorModule()
        {
            try
            {
                // Try to load OpenSim.Addons.Groups.dll if not already loaded
                try
                {
                    Assembly.LoadFrom("OpenSim.Addons.Groups.dll");
                }
                catch (Exception loadEx)
                {
                    if(m_log.IsDebugEnabled) m_log.DebugFormat("Could not load OpenSim.Addons.Groups.dll: {0}", loadEx.Message);
                }
                
                // Try to find the GroupsServiceHGConnectorModule type in any loaded assembly
                Type groupsHGConnectorType = null;
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    if(m_log.IsDebugEnabled) m_log.DebugFormat("Checking assembly for GroupsServiceHGConnectorModule: {0}", assembly.FullName);
                    
                    groupsHGConnectorType = assembly.GetType("OpenSim.Groups.GroupsServiceHGConnectorModule");
                    if (groupsHGConnectorType != null)
                    {
                        if(m_log.IsDebugEnabled) m_log.DebugFormat("Found GroupsServiceHGConnectorModule in assembly: {0}", assembly.FullName);
                        break;
                    }
                }

                if (groupsHGConnectorType != null)
                {
                    var moduleInstance = Activator.CreateInstance(groupsHGConnectorType) as ISharedRegionModule;
                    if (moduleInstance != null)
                    {
                        if(m_log.IsDebugEnabled) m_log.Debug("Successfully loaded GroupsServiceHGConnectorModule");
                        return moduleInstance;
                    }
                    else
                    {
                        m_log.Warn("GroupsServiceHGConnectorModule type found but could not cast to ISharedRegionModule");
                    }
                }
                else
                {
                    m_log.Warn("GroupsServiceHGConnectorModule type not found in any loaded assembly");
                    if(m_log.IsDebugEnabled) 
                    {
                        m_log.Debug("Available assemblies:");
                        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                        {
                            m_log.DebugFormat("  - {0}", assembly.FullName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                m_log.WarnFormat("Could not load GroupsServiceHGConnectorModule: {0}", ex.Message);
            }

            return null;
        }

        /// <summary>
        /// Loads VivoxVoiceModule using reflection to avoid hard dependency
        /// </summary>
        private static ISharedRegionModule LoadVivoxVoiceModule()
        {
            try
            {
                if(m_log.IsDebugEnabled) m_log.Debug("Attempting to load VivoxVoiceModule via reflection");
                
                // First, try to explicitly load the OptionalModules assembly if not already loaded
                try
                {
                    var assemblyPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "OpenSim.Region.OptionalModules.dll");
                    if(m_log.IsDebugEnabled) m_log.DebugFormat("Looking for OptionalModules assembly at: {0}", assemblyPath);
                    
                    if (System.IO.File.Exists(assemblyPath))
                    {
                        var assembly = System.Reflection.Assembly.LoadFrom(assemblyPath);
                        if(m_log.IsDebugEnabled) m_log.DebugFormat("Successfully loaded OptionalModules assembly: {0}", assembly.FullName);
                    }
                    else
                    {
                        if(m_log.IsDebugEnabled) m_log.Debug("OpenSim.Region.OptionalModules.dll not found in base directory");
                    }
                }
                catch (Exception loadEx)
                {
                    if(m_log.IsDebugEnabled) m_log.DebugFormat("Could not load OpenSim.Region.OptionalModules.dll: {0}", loadEx.Message);
                }
                
                // Try to find the VivoxVoiceModule type in any loaded assembly
                Type vivoxVoiceModuleType = null;
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    vivoxVoiceModuleType = assembly.GetType("OpenSim.Region.OptionalModules.Avatar.Voice.VivoxVoice.VivoxVoiceModule");
                    if (vivoxVoiceModuleType != null)
                    {
                        if(m_log.IsDebugEnabled) m_log.DebugFormat("Found VivoxVoiceModule type in assembly: {0}", assembly.FullName);
                        break;
                    }
                }

                if (vivoxVoiceModuleType != null)
                {
                    var moduleInstance = Activator.CreateInstance(vivoxVoiceModuleType) as ISharedRegionModule;
                    if (moduleInstance != null)
                    {
                        if(m_log.IsDebugEnabled) m_log.Debug("Successfully created VivoxVoiceModule instance");
                        return moduleInstance;
                    }
                    else
                    {
                        m_log.Warn("VivoxVoiceModule type found but could not cast to ISharedRegionModule");
                    }
                }
                else
                {
                    m_log.Warn("VivoxVoiceModule type not found in any loaded assembly");
                    if(m_log.IsDebugEnabled) 
                    {
                        m_log.Debug("Available assemblies:");
                        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                        {
                            if (assembly.FullName.Contains("Optional"))
                                m_log.DebugFormat("  - {0}", assembly.FullName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                m_log.ErrorFormat("Failed to load VivoxVoiceModule: {0}", ex.Message);
                if(m_log.IsDebugEnabled) m_log.DebugFormat("VivoxVoiceModule loading exception details: {0}", ex.ToString());
            }

            return null;
        }

        /// <summary>
        /// Creates caps modules using runtime reflection to avoid circular dependencies
        /// </summary>
        private static IEnumerable<ISharedRegionModule> CreateCapsModules()
        {
            var capsModuleTypes = new[]
            {
                "OpenSim.Region.ClientStack.Linden.FetchInventory2Module",
                "OpenSim.Region.ClientStack.Linden.UploadBakedTextureModule",
                "OpenSim.Region.ClientStack.LindenCaps.ServerReleaseNotesModule",
                "OpenSim.Region.ClientStack.Linden.AvatarPickerSearchModule"
            };

            var modules = new List<ISharedRegionModule>();

            foreach (var typeName in capsModuleTypes)
            {
                try
                {
                    // Try to find the type in any loaded assembly
                    Type moduleType = null;
                    foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                    {
                        moduleType = assembly.GetType(typeName);
                        if (moduleType != null)
                            break;
                    }

                    if (moduleType != null)
                    {
                        var moduleInstance = Activator.CreateInstance(moduleType) as ISharedRegionModule;
                        if (moduleInstance != null)
                            modules.Add(moduleInstance);
                    }
                }
                catch (Exception ex)
                {
                    // Log but don't fail - caps modules are optional for basic functionality
                    m_log.WarnFormat("Could not load caps module {0}: {1}", typeName, ex.Message);
                }
            }

            return modules;
        }
    }
}