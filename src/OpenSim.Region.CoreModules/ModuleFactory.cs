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
using OpenSim.Region.CoreModules.Avatar.BakedTextures;
using OpenSim.Region.CoreModules.Avatar.Chat;
using OpenSim.Region.CoreModules.Avatar.Combat.CombatModule;
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
using OpenSim.Region.CoreModules.Framework.Monitoring;
using OpenSim.Region.CoreModules.Framework.Search;
using OpenSim.Region.CoreModules.Framework.DynamicAttributes;
using OpenSim.Region.Framework.DynamicAttributes;
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
using OpenSim.Region.CoreModules.ServiceConnectorsOut.MapImage;
using OpenSim.Region.CoreModules.World;
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
using OpenSim.Region.CoreModules.World.LegacyMap;
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
using OpenSim.Region.CoreModules.Scripting.LSLHttp;

// Physics modules
using OpenSim.Region.PhysicsModule.BulletS;
using OpenSim.Region.PhysicsModule.ubOde;
using OpenSim.Region.PhysicsModule.Meshing;
using OpenSim.Region.PhysicsModule.ubODEMeshing;

// Caps modules
using OpenSim.Region.ClientStack.Linden;
using OpenSim.Region.ClientStack.LindenCaps;

// Note: Optional modules are loaded via reflection to avoid circular dependencies




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

            // Load XBakesModule based on configuration for external baked texture storage
            if (configSource != null)
            {
                var xBakesConfig = configSource.Configs["XBakes"];
                if (xBakesConfig != null)
                {
                    string xBakesURL = xBakesConfig.GetString("URL", String.Empty);
                    if (!string.IsNullOrEmpty(xBakesURL))
                    {
                        if(m_log.IsDebugEnabled) m_log.Debug("Loading XBakesModule for external baked texture storage and caching");
                        yield return new XBakesModule();
                        if(m_log.IsInfoEnabled) m_log.Info("XBakesModule loaded for external baked texture storage, avatar appearance caching, and texture optimization");
                    }
                    else
                    {
                        if(m_log.IsDebugEnabled) m_log.Debug("XBakesModule not loaded - no URL configured in [XBakes] section");
                    }
                }
                else
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("XBakesModule not loaded - no [XBakes] configuration section found");
                }
            }
            else
            {
                if(m_log.IsDebugEnabled) m_log.Debug("XBakesModule not loaded - no configuration source provided");
            }

            // Load DAExampleModule if enabled for dynamic attributes demonstration
            if (configSource != null)
            {
                var daExampleConfig = configSource.Configs["DAExampleModule"];
                if (daExampleConfig?.GetBoolean("enabled", false) == true)
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("Loading DAExampleModule for dynamic attributes demonstration");
                    yield return new OpenSim.Region.CoreModules.Framework.DynamicAttributes.DAExampleModule.DAExampleModule();
                    if(m_log.IsInfoEnabled) m_log.Info("DAExampleModule loaded for dynamic attributes tracking and object move counting");
                }
                else
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("DAExampleModule not loaded - set enabled = true in [DAExampleModule] section to enable dynamic attributes example");
                }
            }

            // Load DOExampleModule if enabled for dynamic objects demonstration
            if (configSource != null)
            {
                var doExampleConfig = configSource.Configs["DOExampleModule"];
                if (doExampleConfig?.GetBoolean("enabled", false) == true)
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("Loading DOExampleModule for dynamic objects demonstration");
                    yield return new OpenSim.Region.Framework.DynamicAttributes.DOExampleModule.DOExampleModule();
                    if(m_log.IsInfoEnabled) m_log.Info("DOExampleModule loaded for dynamic objects tracking and in-memory object management");
                }
                else
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("DOExampleModule not loaded - set enabled = true in [DOExampleModule] section to enable dynamic objects example");
                }
            }

            yield return new LandManagementModule();
            yield return new PrimCountModule();
            yield return new DefaultPermissionsModule();
            yield return new SoundModule();

            // Load EstateManagementModule for essential estate management functionality (always enabled)
            if(m_log.IsDebugEnabled) m_log.Debug("Loading EstateManagementModule for estate settings, access controls, and management functionality");
            yield return new EstateManagementModule();
            if(m_log.IsInfoEnabled) m_log.Info("EstateManagementModule loaded for estate settings, ban/access lists, teleport controls, and estate messaging");

            // Load PrimLimitsModule based on configuration
            if (configSource != null)
            {
                string permissionModules = Util.GetConfigVarFromSections<string>(configSource, "permissionmodules",
                    new string[] { "Startup", "Permissions" }, "DefaultPermissionsModule");

                if (!string.IsNullOrEmpty(permissionModules) && permissionModules.Contains("PrimLimitsModule"))
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("Loading PrimLimitsModule for parcel prim limits enforcement");
                    var primLimitsModuleInstance = LoadPrimLimitsModule();
                    if (primLimitsModuleInstance != null)
                    {
                        yield return primLimitsModuleInstance;
                        if(m_log.IsInfoEnabled) m_log.Info("PrimLimitsModule loaded for parcel prim limits, object rezzing restrictions, and capacity management");
                    }
                    else
                    {
                        m_log.Warn("PrimLimitsModule was configured in permissionmodules but could not be loaded. Check that OpenSim.Region.OptionalModules.dll is available.");
                    }
                }
                else
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("PrimLimitsModule not loaded - not specified in permissionmodules configuration");
                }
            }
            else
            {
                if(m_log.IsDebugEnabled) m_log.Debug("PrimLimitsModule not loaded - no configuration source provided");
            }

            // Load GodsModule for god powers and administrative controls (always enabled)
            if(m_log.IsDebugEnabled) m_log.Debug("Loading GodsModule for god powers, user kicking, and administrative controls");
            yield return new GodsModule();
            if(m_log.IsInfoEnabled) m_log.Info("GodsModule loaded for god powers, kick/freeze functionality, and grid-wide administrative controls");

            // Load ObjectCommandsModule for object manipulation console commands (always enabled)
            if(m_log.IsDebugEnabled) m_log.Debug("Loading ObjectCommandsModule for object manipulation and inspection console commands");
            yield return new ObjectCommandsModule();
            if(m_log.IsInfoEnabled) m_log.Info("ObjectCommandsModule loaded for object show, delete, dump commands and scene object management");

            // Load RegionCommandsModule for region management console commands (always enabled)
            if(m_log.IsDebugEnabled) m_log.Debug("Loading RegionCommandsModule for region information and management console commands");
            yield return new RegionCommandsModule();
            if(m_log.IsInfoEnabled) m_log.Info("RegionCommandsModule loaded for region show, set, neighbor commands and scene statistics");

            // Load RestartModule for region restart management (always enabled)
            if(m_log.IsDebugEnabled) m_log.Debug("Loading RestartModule for region restart scheduling, countdown timers, and shutdown coordination");
            yield return new RestartModule();
            if(m_log.IsInfoEnabled) m_log.Info("RestartModule loaded for region restart commands, scheduled shutdowns, and graceful restart coordination");

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
                    m_log.Info("[MODULE FACTORY] Loading HGWorldMapModule");
                    yield return new HGWorldMapModule();
                }
                else if (worldMapModule == "WorldMap")
                {
                    m_log.Info("[MODULE FACTORY] Loading WorldMapModule");
                    yield return new WorldMapModule();
                }
                else
                {
                    m_log.WarnFormat("[MODULE FACTORY] No WorldMapModule loaded - configured value: '{0}'", worldMapModule);
                }

                // Note: MapSearchModule is now loaded in CreateSharedModules as it implements ISharedRegionModule
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
                else if (mapImageModule == "MapImageModule")
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("Loading legacy MapImageModule for basic 2D map tile generation");
                    yield return new MapImageModule();
                    if(m_log.IsInfoEnabled) m_log.Info("MapImageModule loaded for legacy 2D map tile generation with basic terrain and prim rendering");
                }
                else if (!string.IsNullOrEmpty(mapImageModule))
                {
                    if(m_log.IsDebugEnabled) m_log.DebugFormat("MapImageModule configured as '{0}' but not recognized (supported: 'Warp3DImageModule', 'MapImageModule'), skipping", mapImageModule);
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

            // Load BuySellModule for object buying and selling functionality (always enabled)
            if(m_log.IsDebugEnabled) m_log.Debug("Loading BuySellModule for object transactions and marketplace functionality");
            yield return new BuySellModule();
            if(m_log.IsInfoEnabled) m_log.Info("BuySellModule loaded for object buying/selling, marketplace transactions, and permission handling");

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

            // Note: UrlModule is now loaded in CreateSharedModules as it implements ISharedRegionModule

            // Note: BasicSearchModule is now loaded in CreateSharedModules as it implements ISharedRegionModule

            // Load MonitorModule if enabled for region monitoring and statistics
            if (configSource != null)
            {
                var monitoringConfig = configSource.Configs["Monitoring"];
                if (monitoringConfig?.GetBoolean("Enabled", true) == true)  // Default to true as it's essential for monitoring
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("Loading MonitorModule for region health monitoring and statistics collection");
                    yield return new MonitorModule();
                    if(m_log.IsInfoEnabled) m_log.Info("MonitorModule loaded for region health monitoring, performance statistics, and console commands");
                }
                else
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("MonitorModule disabled - set Enabled = true in [Monitoring] to enable region monitoring");
                }
            }
            else
            {
                // Default to loading if no config source (essential module for monitoring)
                if(m_log.IsDebugEnabled) m_log.Debug("Loading MonitorModule (no config source - using default)");
                yield return new MonitorModule();
                if(m_log.IsInfoEnabled) m_log.Info("MonitorModule loaded for region health monitoring and performance statistics");
            }

            // Load EtcdMonitoringModule based on configuration
            if (configSource != null)
            {
                var etcdConfig = configSource.Configs["Etcd"];
                if (etcdConfig != null)
                {
                    string etcdUrls = etcdConfig.GetString("EtcdUrls", String.Empty);
                    if (!string.IsNullOrEmpty(etcdUrls))
                    {
                        if(m_log.IsDebugEnabled) m_log.Debug("Loading EtcdMonitoringModule for etcd-based monitoring data storage");
                        var etcdModuleInstance = LoadEtcdMonitoringModule();
                        if (etcdModuleInstance != null)
                        {
                            yield return etcdModuleInstance;
                            if(m_log.IsInfoEnabled) m_log.Info("EtcdMonitoringModule loaded for high-availability monitoring data storage in etcd");
                        }
                        else
                        {
                            m_log.Warn("EtcdMonitoringModule was configured ([Etcd] EtcdUrls specified) but could not be loaded. Check that OpenSim.Region.OptionalModules.dll and netcd dependencies are available.");
                        }
                    }
                    else
                    {
                        if(m_log.IsDebugEnabled) m_log.Debug("EtcdMonitoringModule not loaded - no EtcdUrls configured in [Etcd] section");
                    }
                }
                else
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("EtcdMonitoringModule not loaded - no [Etcd] configuration section found");
                }
            }
            else
            {
                if(m_log.IsDebugEnabled) m_log.Debug("EtcdMonitoringModule not loaded - no configuration source provided");
            }

            // Load RegionReadyModule based on configuration
            if (configSource != null)
            {
                var regionReadyConfig = configSource.Configs["RegionReady"];
                if (regionReadyConfig?.GetBoolean("enabled", false) == true)
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("Loading RegionReadyModule for region startup coordination and readiness notifications");
                    var regionReadyModuleInstance = LoadRegionReadyModule();
                    if (regionReadyModuleInstance != null)
                    {
                        yield return regionReadyModuleInstance;
                        if(m_log.IsInfoEnabled) m_log.Info("RegionReadyModule loaded for region startup coordination, script compilation monitoring, and readiness notifications");
                    }
                    else
                    {
                        m_log.Warn("RegionReadyModule was configured ([RegionReady] enabled = true) but could not be loaded. Check that OpenSim.Region.OptionalModules.dll is available.");
                    }
                }
                else
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("RegionReadyModule disabled - set enabled = true in [RegionReady] to enable region readiness coordination");
                }
            }
            else
            {
                if(m_log.IsDebugEnabled) m_log.Debug("RegionReadyModule not loaded - no configuration source provided");
            }

            // Load WorldViewModule based on configuration
            if (configSource != null)
            {
                var modulesConfig = configSource.Configs["Modules"];
                if (modulesConfig?.GetString("WorldViewModule", String.Empty) == "WorldViewModule")
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("Loading WorldViewModule for HTTP-based world view image generation");
                    var worldViewModuleInstance = LoadWorldViewModule();
                    if (worldViewModuleInstance != null)
                    {
                        yield return worldViewModuleInstance;
                        if(m_log.IsInfoEnabled) m_log.Info("WorldViewModule loaded for HTTP world view endpoints and image generation");
                    }
                    else
                    {
                        m_log.Warn("WorldViewModule was configured ([Modules] WorldViewModule = WorldViewModule) but could not be loaded. Check that OpenSim.Region.OptionalModules.dll is available.");
                    }
                }
                else
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("WorldViewModule not loaded - set WorldViewModule = WorldViewModule in [Modules] section to enable world view functionality");
                }
            }

            // Load DynamicFloaterModule based on configuration
            if (configSource != null)
            {
                var dynamicFloaterConfig = configSource.Configs["DynamicFloaterModule"];
                if (dynamicFloaterConfig?.GetBoolean("enabled", false) == true)
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("Loading DynamicFloaterModule for dynamic UI floater and dialog support");
                    var dynamicFloaterModuleInstance = LoadDynamicFloaterModule();
                    if (dynamicFloaterModuleInstance != null)
                    {
                        yield return dynamicFloaterModuleInstance;
                        if(m_log.IsInfoEnabled) m_log.Info("DynamicFloaterModule loaded for dynamic floater dialogs, XML-based UI elements, and viewer integration");
                    }
                    else
                    {
                        m_log.Warn("DynamicFloaterModule was configured ([DynamicFloaterModule] enabled = true) but could not be loaded. Check that OpenSim.Region.OptionalModules.dll is available.");
                    }
                }
                else
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("DynamicFloaterModule not loaded - set enabled = true in [DynamicFloaterModule] section to enable dynamic floater support");
                }
            }

            // Load DynamicMenuModule based on configuration
            if (configSource != null)
            {
                var dynamicMenuConfig = configSource.Configs["DynamicMenuModule"];
                if (dynamicMenuConfig?.GetBoolean("enabled", false) == true)
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("Loading DynamicMenuModule for dynamic viewer menu customization and script integration");
                    var dynamicMenuModuleInstance = LoadDynamicMenuModule();
                    if (dynamicMenuModuleInstance != null)
                    {
                        yield return dynamicMenuModuleInstance;
                        if(m_log.IsInfoEnabled) m_log.Info("DynamicMenuModule loaded for viewer menu customization, permission-based menu items, and script-driven menu actions");
                    }
                    else
                    {
                        m_log.Warn("DynamicMenuModule was configured ([DynamicMenuModule] enabled = true) but could not be loaded. Check that OpenSim.Region.OptionalModules.dll is available.");
                    }
                }
                else
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("DynamicMenuModule not loaded - set enabled = true in [DynamicMenuModule] section to enable dynamic menu support");
                }
            }

            // Load SpecialUIModule based on configuration
            if (configSource != null)
            {
                var specialUIConfig = configSource.Configs["SpecialUIModule"];
                if (specialUIConfig?.GetBoolean("enabled", false) == true)
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("Loading SpecialUIModule for custom viewer UI elements and toolbar customization");
                    var specialUIModuleInstance = LoadSpecialUIModule();
                    if (specialUIModuleInstance != null)
                    {
                        yield return specialUIModuleInstance;
                        if(m_log.IsInfoEnabled) m_log.Info("SpecialUIModule loaded for viewer UI customization, toolbar panels, and user-level-based UI elements");
                    }
                    else
                    {
                        m_log.Warn("SpecialUIModule was configured ([SpecialUIModule] enabled = true) but could not be loaded. Check that OpenSim.Region.OptionalModules.dll is available.");
                    }
                }
                else
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("SpecialUIModule not loaded - set enabled = true in [SpecialUIModule] section to enable special UI support");
                }
            }

            // Load CameraOnlyModeModule based on configuration
            if (configSource != null)
            {
                var cameraOnlyConfig = configSource.Configs["CameraOnlyModeModule"];
                if (cameraOnlyConfig?.GetBoolean("enabled", false) == true)
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("Loading CameraOnlyModeModule for viewer camera-only mode support");
                    var cameraOnlyModuleInstance = LoadCameraOnlyModeModule();
                    if (cameraOnlyModuleInstance != null)
                    {
                        yield return cameraOnlyModuleInstance;
                        if(m_log.IsInfoEnabled) m_log.Info("CameraOnlyModeModule loaded for camera-only mode and viewer feature control");
                    }
                    else
                    {
                        m_log.Warn("CameraOnlyModeModule was configured ([CameraOnlyModeModule] enabled = true) but could not be loaded. Check that OpenSim.Region.OptionalModules.dll is available.");
                    }
                }
                else
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("CameraOnlyModeModule not loaded - set enabled = true in [CameraOnlyModeModule] section to enable camera-only mode");
                }
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
            yield return new CombatModule();
            yield return new InventoryTransferModule();
            yield return new InstantMessageModule();
            yield return new MuteListModule();
            yield return new PresenceModule();

            // Load CallingCardModule based on configuration for calling card functionality
            var callingCardConfig = configSource?.Configs["XCallingCard"];
            if (callingCardConfig?.GetBoolean("Enabled", true) == true)  // Default to true for avatar functionality
            {
                if(m_log.IsDebugEnabled) m_log.Debug("Loading CallingCardModule for calling card creation, offers, and friendship management");
                yield return new CallingCardModule();
                if(m_log.IsInfoEnabled) m_log.Info("CallingCardModule loaded for calling card offers, inventory management, and friendship integration");
            }
            else
            {
                if(m_log.IsDebugEnabled) m_log.Debug("CallingCardModule disabled - set Enabled = true in [XCallingCard] section to enable calling card functionality");
            }

            // Load IPBanModule for IP and hostname based access control (always enabled for security)
            if(m_log.IsDebugEnabled) m_log.Debug("Loading IPBanModule for IP and hostname based access control");
            yield return new IPBanModule();
            if(m_log.IsInfoEnabled) m_log.Info("IPBanModule loaded for IP blocking, hostname filtering, and estate ban enforcement");

            // Load AccessModule for login control and region access management (always enabled)
            if(m_log.IsDebugEnabled) m_log.Debug("Loading AccessModule for login control and region access management");
            yield return new AccessModule();
            if(m_log.IsInfoEnabled) m_log.Info("AccessModule loaded for login enable/disable console commands and region access control");

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

            // Load UrlModule if enabled for LSL HTTP-In functionality and llRequestURL/llRequestSecureURL
            if (modulesConfig?.GetBoolean("UrlModule", true) == true)  // Default to true as it's essential for LSL HTTP-In functionality
            {
                if(m_log.IsDebugEnabled) m_log.Debug("Loading UrlModule for LSL HTTP-In functionality and external URL management");
                yield return new UrlModule();
                if(m_log.IsInfoEnabled) m_log.Info("UrlModule loaded for LSL HTTP-In functionality, llRequestURL, llRequestSecureURL, and external HTTP endpoints");
            }
            else
            {
                if(m_log.IsDebugEnabled) m_log.Debug("UrlModule disabled - set UrlModule = true in [Modules] to enable LSL HTTP-In functionality");
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

            // Load ServiceThrottleModule for grid service request throttling and performance optimization
            if (configSource != null)
            {
                // ServiceThrottleModule is typically enabled by default for performance optimization
                var serviceThrottleConfig = configSource.Configs["ServiceThrottle"];
                bool enableServiceThrottle = serviceThrottleConfig?.GetBoolean("Enabled", true) ?? true; // Default to enabled

                if (enableServiceThrottle)
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("Loading ServiceThrottleModule for grid service request throttling and performance optimization");
                    yield return new ServiceThrottleModule();
                    if(m_log.IsInfoEnabled) m_log.Info("ServiceThrottleModule loaded for grid service throttling, region handle requests, and job queue management");
                }
                else
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("ServiceThrottleModule disabled - set Enabled = true in [ServiceThrottle] to enable service throttling");
                }
            }
            else
            {
                // Default to loading if no config source (essential module for performance)
                if(m_log.IsDebugEnabled) m_log.Debug("Loading ServiceThrottleModule (no config source - using default)");
                yield return new ServiceThrottleModule();
                if(m_log.IsInfoEnabled) m_log.Info("ServiceThrottleModule loaded for grid service throttling and performance optimization");
            }

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
                if(m_log.IsDebugEnabled) m_log.Debug("Loading RegionGridServicesConnector for grid service communication and region discovery");
                yield return new RegionGridServicesConnector();
                if(m_log.IsInfoEnabled) m_log.Info("RegionGridServicesConnector loaded for grid service interface, region lookups, neighbor discovery, and Hypergrid support");
            }
            else if (!string.IsNullOrEmpty(gridServicesModule))
            {
                if(m_log.IsDebugEnabled) m_log.DebugFormat("RegionGridServicesConnector not loaded - GridServices configured as '{0}', expected 'RegionGridServicesConnector'", gridServicesModule);
            }
            else
            {
                if(m_log.IsDebugEnabled) m_log.Debug("RegionGridServicesConnector not loaded - no GridServices configured");
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
                if(m_log.IsDebugEnabled) m_log.Debug("Loading NeighbourServicesOutConnector for cross-region neighbor communication and discovery");
                yield return new NeighbourServicesOutConnector();
                if(m_log.IsInfoEnabled) m_log.Info("NeighbourServicesOutConnector loaded for neighbor hello protocol, region discovery, and cross-region coordination");
            }
            else if (!string.IsNullOrEmpty(neighbourServicesModule))
            {
                if(m_log.IsDebugEnabled) m_log.DebugFormat("NeighbourServicesOutConnector not loaded - NeighbourServices configured as '{0}', expected 'NeighbourServicesOutConnector'", neighbourServicesModule);
            }
            else
            {
                if(m_log.IsDebugEnabled) m_log.Debug("NeighbourServicesOutConnector not loaded - no NeighbourServices configured");
            }

            // Load SimulationServices module based on configuration
            string simulationServicesModule = modulesConfig?.GetString("SimulationServices", "");
            if (simulationServicesModule == "LocalSimulationConnectorModule")
            {
                if(m_log.IsDebugEnabled) m_log.Debug("Loading LocalSimulationConnectorModule for local region-to-region communication");
                yield return new LocalSimulationConnectorModule();
                if(m_log.IsInfoEnabled) m_log.Info("LocalSimulationConnectorModule loaded for local simulation services (agent transfers, object crossing, and region queries)");
            }
            else if (simulationServicesModule == "RemoteSimulationConnectorModule")
            {
                if(m_log.IsDebugEnabled) m_log.Debug("Loading RemoteSimulationConnectorModule for remote grid communication");
                yield return new RemoteSimulationConnectorModule();
                if(m_log.IsInfoEnabled) m_log.Info("RemoteSimulationConnectorModule loaded for remote simulation services (inter-grid agent transfers and object crossing)");
            }
            else if (!string.IsNullOrEmpty(simulationServicesModule))
            {
                if(m_log.IsDebugEnabled) m_log.DebugFormat("SimulationServices module not loaded - configured as '{0}', expected 'LocalSimulationConnectorModule' or 'RemoteSimulationConnectorModule'", simulationServicesModule);
            }
            else
            {
                if(m_log.IsDebugEnabled) m_log.Debug("SimulationServices module not loaded - no SimulationServices configured in [Modules]");
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

            // Load MapImageService module based on configuration
            string mapImageServicesModule = modulesConfig?.GetString("MapImageService", "");
            m_log.InfoFormat("[MODULE FACTORY] Checking for MapImageServiceModule - configured value: '{0}'", mapImageServicesModule);
            if (mapImageServicesModule == "MapImageServiceModule")
            {
                m_log.Info("[MODULE FACTORY] Loading MapImageServiceModule for map tile upload and management");
                yield return new MapImageServiceModule();
                m_log.Info("[MODULE FACTORY] MapImageServiceModule loaded for map tile upload and management services");
            }
            else
            {
                m_log.WarnFormat("[MODULE FACTORY] MapImageServiceModule NOT loaded - configured value: '{0}'", mapImageServicesModule);
            }

            // Load MapSearchModule - provides map search functionality for WorldMap modules
            // MapSearchModule works with any WorldMap implementation to provide region search capabilities
            m_log.InfoFormat("[MODULE FACTORY] Checking for MapSearchModule...");
            var worldMapModuleConfig = configSource?.Configs["Startup"] ?? configSource?.Configs["Map"];
            m_log.InfoFormat("[MODULE FACTORY] worldMapModuleConfig is null: {0}", worldMapModuleConfig == null);
            string worldMapModuleName = worldMapModuleConfig?.GetString("WorldMapModule", "");
            m_log.InfoFormat("[MODULE FACTORY] WorldMapModule name: '{0}'", worldMapModuleName);
            if (!string.IsNullOrEmpty(worldMapModuleName) && (worldMapModuleName == "WorldMap" || worldMapModuleName == "HGWorldMap"))
            {
                m_log.InfoFormat("[MODULE FACTORY] Condition matched! Loading MapSearchModule...");
                if(m_log.IsDebugEnabled) m_log.Debug("Loading MapSearchModule for map search functionality");
                yield return new MapSearchModule();
                m_log.Info("MapSearchModule loaded for region search capabilities");
            }
            else
            {
                m_log.WarnFormat("[MODULE FACTORY] MapSearchModule NOT loaded. WorldMapModule='{0}', isEmpty={1}",
                    worldMapModuleName, string.IsNullOrEmpty(worldMapModuleName));
            }

            // Load BasicSearchModule based on configuration
            string searchModule = modulesConfig?.GetString("SearchModule", "");
            if (searchModule == "BasicSearchModule")
            {
                if(m_log.IsDebugEnabled) m_log.Debug("Loading BasicSearchModule for people and groups search functionality");
                yield return new BasicSearchModule();
                if(m_log.IsInfoEnabled) m_log.Info("BasicSearchModule loaded for people and groups search capabilities");
            }
            else if (!string.IsNullOrEmpty(searchModule))
            {
                if(m_log.IsDebugEnabled) m_log.DebugFormat("BasicSearchModule not loaded - SearchModule configured as '{0}', expected 'BasicSearchModule'", searchModule);
            }
            else
            {
                if(m_log.IsDebugEnabled) m_log.Debug("BasicSearchModule not loaded - no SearchModule configured");
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
                if(m_log.IsDebugEnabled) m_log.Debug("Loading NPCModule for non-player character creation and management");
                var npcModuleInstance = LoadNPCModule();
                if (npcModuleInstance != null)
                {
                    yield return npcModuleInstance;
                    if(m_log.IsInfoEnabled) m_log.Info("NPCModule loaded for NPC creation, appearance management, movement control, and communication capabilities");
                }
                else
                {
                    m_log.Warn("NPCModule was configured ([Modules] NPCModule = true) but could not be loaded. Check that OpenSim.Region.OptionalModules.dll is available.");
                }
            }
            else
            {
                if(m_log.IsDebugEnabled) m_log.Debug("NPCModule not loaded - set NPCModule = true in [Modules] to enable NPC support");
            }

            // Load ConciergeModule based on configuration
            var conciergeConfig = configSource?.Configs["Concierge"];
            if (conciergeConfig?.GetBoolean("enabled", false) == true)
            {
                if(m_log.IsDebugEnabled) m_log.Debug("Loading ConciergeModule for avatar welcome messages and region announcements");
                var conciergeModuleInstance = LoadConciergeModule();
                if (conciergeModuleInstance != null)
                {
                    yield return conciergeModuleInstance;
                    if(m_log.IsInfoEnabled) m_log.Info("ConciergeModule loaded for avatar greetings, announcements, and chat integration");
                }
                else
                {
                    m_log.Warn("ConciergeModule was configured ([Concierge] enabled = true) but could not be loaded. Check that OpenSim.Region.OptionalModules.dll is available.");
                }
            }
            else
            {
                if(m_log.IsDebugEnabled) m_log.Debug("ConciergeModule not loaded - set enabled = true in [Concierge] section to enable concierge features");
            }

            // Note: WorldViewModule is now loaded in CreateNonSharedModules as it implements INonSharedRegionModule

            // Note: CameraOnlyModeModule is now loaded in CreateNonSharedModules as it implements INonSharedRegionModule

            // Note: DynamicFloaterModule is now loaded in CreateNonSharedModules as it implements INonSharedRegionModule

            // Note: DynamicMenuModule is now loaded in CreateNonSharedModules as it implements INonSharedRegionModule

            // Note: SpecialUIModule is now loaded in CreateNonSharedModules as it implements INonSharedRegionModule

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

            // Load SerialiserModule for region and object serialization (XML export/import)
            if (modulesConfig?.GetBoolean("SerialiserModule", true) == true)  // Default to true for backward compatibility
            {
                if(m_log.IsDebugEnabled) m_log.Debug("Loading SerialiserModule for region and object serialization to XML format");
                yield return new SerialiserModule();
                if(m_log.IsInfoEnabled) m_log.Info("SerialiserModule loaded for XML-based region exports, object serialization, and scene data archival");
            }
            else
            {
                if(m_log.IsDebugEnabled) m_log.Debug("SerialiserModule disabled - set SerialiserModule = true in [Modules] to enable XML serialization functionality");
            }

            // Note: EtcdMonitoringModule is now loaded in CreateNonSharedModules as it implements INonSharedRegionModule

            // Load AutoBackupModule based on configuration
            var autoBackupConfig = configSource?.Configs["AutoBackupModule"];
            if (autoBackupConfig?.GetBoolean("AutoBackupModuleEnabled", false) == true)
            {
                var autoBackupModule = LoadAutoBackupModule();
                if (autoBackupModule != null)
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("Loading AutoBackupModule for automated region backup scheduling and OAR generation");
                    yield return autoBackupModule;
                    if(m_log.IsInfoEnabled) m_log.Info("AutoBackupModule loaded for scheduled region backups, file retention management, and post-backup script execution");
                }
                else
                {
                    m_log.Warn("AutoBackupModule was configured but could not be loaded. Check that OpenSim.Region.OptionalModules.dll is available.");
                }
            }
            else
            {
                if(m_log.IsDebugEnabled) m_log.Debug("AutoBackupModule not loaded - set AutoBackupModuleEnabled = true in [AutoBackupModule] section to enable automated backup functionality");
            }

            // Load WebSocketEchoModule for WebSocket testing and development (example module)
            var webSocketEchoConfig = configSource?.Configs["WebSocketEcho"];
            if (webSocketEchoConfig != null)
            {
                var webSocketEchoModule = LoadWebSocketEchoModule();
                if (webSocketEchoModule != null)
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("Loading WebSocketEchoModule for WebSocket echo testing and development");
                    yield return webSocketEchoModule;
                    if(m_log.IsInfoEnabled) m_log.Info("WebSocketEchoModule loaded for WebSocket echo functionality at /echo endpoint");
                }
                else
                {
                    m_log.Warn("WebSocketEchoModule was configured but could not be loaded. Check that OpenSim.Region.OptionalModules.dll is available.");
                }
            }
            else
            {
                if(m_log.IsDebugEnabled) m_log.Debug("WebSocketEchoModule not loaded - add [WebSocketEcho] section to enable WebSocket echo testing functionality");
            }

            // Load EstateModule based on configuration for estate management communication
            var estateConfig = configSource?.Configs["Estates"];
            if (estateConfig?.GetString("EstateCommunicationsHandler", "EstateModule") == "EstateModule" || estateConfig == null)
            {
                if(m_log.IsDebugEnabled) m_log.Debug("Loading EstateModule for estate communications, region info updates, and estate management");
                yield return new EstateModule();
                if(m_log.IsInfoEnabled) m_log.Info("EstateModule loaded for estate communication handlers, teleport management, and cross-region estate coordination");
            }
            else
            {
                if(m_log.IsDebugEnabled) m_log.DebugFormat("EstateModule not loaded - EstateCommunicationsHandler configured as '{0}', expected 'EstateModule'", estateConfig.GetString("EstateCommunicationsHandler", ""));
            }

            // Load LindenUDPInfoModule for UDP stack monitoring and diagnostics via reflection
            // This module provides console commands for inspecting UDP client queues, throttles, and statistics
            var lindenUDPInfoModule = LoadLindenUDPInfoModule();
            if (lindenUDPInfoModule != null)
            {
                if(m_log.IsDebugEnabled) m_log.Debug("Loading LindenUDPInfoModule for UDP stack diagnostics and monitoring commands");
                yield return lindenUDPInfoModule;
                if(m_log.IsInfoEnabled) m_log.Info("LindenUDPInfoModule loaded for UDP queue inspection, throttle monitoring, and client diagnostics");
            }
            else
            {
                if(m_log.IsDebugEnabled) m_log.Debug("LindenUDPInfoModule not available - UDP diagnostics will not be available");
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
                if(m_log.IsDebugEnabled) m_log.Debug("Attempting to load NPCModule via reflection");

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

                // Try to find the NPCModule type in any loaded assembly
                Type npcModuleType = null;
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    npcModuleType = assembly.GetType("OpenSim.Region.OptionalModules.World.NPC.NPCModule");
                    if (npcModuleType != null)
                    {
                        if(m_log.IsDebugEnabled) m_log.DebugFormat("Found NPCModule type in assembly: {0}", assembly.FullName);
                        break;
                    }
                }

                if (npcModuleType != null)
                {
                    var moduleInstance = Activator.CreateInstance(npcModuleType) as ISharedRegionModule;
                    if (moduleInstance != null)
                    {
                        if(m_log.IsDebugEnabled) m_log.Debug("Successfully created NPCModule instance");
                        return moduleInstance;
                    }
                    else
                    {
                        m_log.Warn("NPCModule type found but could not cast to ISharedRegionModule");
                    }
                }
                else
                {
                    m_log.Warn("NPCModule type not found in any loaded assembly");
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
                m_log.ErrorFormat("Failed to load NPCModule: {0}", ex.Message);
                if(m_log.IsDebugEnabled) m_log.DebugFormat("NPCModule loading exception details: {0}", ex.ToString());
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
        /// Loads RegionReadyModule using reflection to avoid hard dependency
        /// </summary>
        private static INonSharedRegionModule LoadRegionReadyModule()
        {
            try
            {
                if(m_log.IsDebugEnabled) m_log.Debug("Attempting to load RegionReadyModule via reflection");

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

                // Try to find the RegionReadyModule type in any loaded assembly
                Type regionReadyModuleType = null;
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    regionReadyModuleType = assembly.GetType("OpenSim.Region.OptionalModules.Scripting.RegionReady.RegionReadyModule");
                    if (regionReadyModuleType != null)
                    {
                        if(m_log.IsDebugEnabled) m_log.DebugFormat("Found RegionReadyModule type in assembly: {0}", assembly.FullName);
                        break;
                    }
                }

                if (regionReadyModuleType != null)
                {
                    var moduleInstance = Activator.CreateInstance(regionReadyModuleType) as INonSharedRegionModule;
                    if (moduleInstance != null)
                    {
                        if(m_log.IsDebugEnabled) m_log.Debug("Successfully created RegionReadyModule instance");
                        return moduleInstance;
                    }
                    else
                    {
                        m_log.Warn("RegionReadyModule type found but could not cast to INonSharedRegionModule");
                    }
                }
                else
                {
                    m_log.Warn("RegionReadyModule type not found in any loaded assembly");
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
                m_log.ErrorFormat("Failed to load RegionReadyModule: {0}", ex.Message);
                if(m_log.IsDebugEnabled) m_log.DebugFormat("RegionReadyModule loading exception details: {0}", ex.ToString());
            }

            return null;
        }

        /// <summary>
        /// Loads EtcdMonitoringModule using reflection to avoid hard dependency
        /// </summary>
        private static INonSharedRegionModule LoadEtcdMonitoringModule()
        {
            try
            {
                if(m_log.IsDebugEnabled) m_log.Debug("Attempting to load EtcdMonitoringModule via reflection");

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

                // Try to find the EtcdMonitoringModule type in any loaded assembly
                Type etcdModuleType = null;
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    etcdModuleType = assembly.GetType("OpenSim.Region.OptionalModules.Framework.Monitoring.EtcdMonitoringModule");
                    if (etcdModuleType != null)
                    {
                        if(m_log.IsDebugEnabled) m_log.DebugFormat("Found EtcdMonitoringModule type in assembly: {0}", assembly.FullName);
                        break;
                    }
                }

                if (etcdModuleType != null)
                {
                    var moduleInstance = Activator.CreateInstance(etcdModuleType) as INonSharedRegionModule;
                    if (moduleInstance != null)
                    {
                        if(m_log.IsDebugEnabled) m_log.Debug("Successfully created EtcdMonitoringModule instance");
                        return moduleInstance;
                    }
                    else
                    {
                        m_log.Warn("EtcdMonitoringModule type found but could not cast to INonSharedRegionModule");
                    }
                }
                else
                {
                    m_log.Warn("EtcdMonitoringModule type not found in any loaded assembly");
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
                m_log.ErrorFormat("Failed to load EtcdMonitoringModule: {0}", ex.Message);
                if(m_log.IsDebugEnabled) m_log.DebugFormat("EtcdMonitoringModule loading exception details: {0}", ex.ToString());
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

        /// <summary>
        /// Loads PrimLimitsModule using reflection to avoid hard dependency
        /// </summary>
        private static INonSharedRegionModule LoadPrimLimitsModule()
        {
            try
            {
                if(m_log.IsDebugEnabled) m_log.Debug("Attempting to load PrimLimitsModule via reflection");

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

                // Try to find the PrimLimitsModule type in any loaded assembly
                Type primLimitsModuleType = null;
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    primLimitsModuleType = assembly.GetType("OpenSim.Region.OptionalModules.PrimLimitsModule");
                    if (primLimitsModuleType != null)
                    {
                        if(m_log.IsDebugEnabled) m_log.DebugFormat("Found PrimLimitsModule type in assembly: {0}", assembly.FullName);
                        break;
                    }
                }

                if (primLimitsModuleType != null)
                {
                    var moduleInstance = Activator.CreateInstance(primLimitsModuleType) as INonSharedRegionModule;
                    if (moduleInstance != null)
                    {
                        if(m_log.IsDebugEnabled) m_log.Debug("Successfully created PrimLimitsModule instance");
                        return moduleInstance;
                    }
                    else
                    {
                        m_log.Warn("PrimLimitsModule type found but could not cast to INonSharedRegionModule");
                    }
                }
                else
                {
                    m_log.Warn("PrimLimitsModule type not found in any loaded assembly");
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
                m_log.ErrorFormat("Failed to load PrimLimitsModule: {0}", ex.Message);
                if(m_log.IsDebugEnabled) m_log.DebugFormat("PrimLimitsModule loading exception details: {0}", ex.ToString());
            }

            return null;
        }

        /// <summary>
        /// Loads ConciergeModule using reflection to avoid hard dependency
        /// </summary>
        private static ISharedRegionModule LoadConciergeModule()
        {
            try
            {
                if(m_log.IsDebugEnabled) m_log.Debug("Attempting to load ConciergeModule via reflection");

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

                // Try to find the ConciergeModule type in any loaded assembly
                Type conciergeModuleType = null;
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    conciergeModuleType = assembly.GetType("OpenSim.Region.OptionalModules.Avatar.Concierge.ConciergeModule");
                    if (conciergeModuleType != null)
                    {
                        if(m_log.IsDebugEnabled) m_log.DebugFormat("Found ConciergeModule type in assembly: {0}", assembly.FullName);
                        break;
                    }
                }

                if (conciergeModuleType != null)
                {
                    var moduleInstance = (ISharedRegionModule)Activator.CreateInstance(conciergeModuleType);
                    if(m_log.IsDebugEnabled) m_log.Debug("Successfully created ConciergeModule instance");
                    return moduleInstance;
                }
                else
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("ConciergeModule type not found in any loaded assembly");
                }
            }
            catch (Exception ex)
            {
                m_log.ErrorFormat("Failed to load ConciergeModule: {0}", ex.Message);
                if(m_log.IsDebugEnabled) m_log.DebugFormat("ConciergeModule loading exception details: {0}", ex.ToString());
            }

            return null;
        }

        /// <summary>
        /// Loads WorldViewModule using reflection to avoid hard dependency
        /// </summary>
        private static INonSharedRegionModule LoadWorldViewModule()
        {
            try
            {
                if(m_log.IsDebugEnabled) m_log.Debug("Attempting to load WorldViewModule via reflection");

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

                // Try to find the WorldViewModule type in any loaded assembly
                Type worldViewModuleType = null;
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    worldViewModuleType = assembly.GetType("OpenSim.Region.OptionalModules.World.WorldView.WorldViewModule");
                    if (worldViewModuleType != null)
                    {
                        if(m_log.IsDebugEnabled) m_log.DebugFormat("Found WorldViewModule type in assembly: {0}", assembly.FullName);
                        break;
                    }
                }

                if (worldViewModuleType != null)
                {
                    var moduleInstance = (INonSharedRegionModule)Activator.CreateInstance(worldViewModuleType);
                    if(m_log.IsDebugEnabled) m_log.Debug("Successfully created WorldViewModule instance");
                    return moduleInstance;
                }
                else
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("WorldViewModule type not found in any loaded assembly");
                }
            }
            catch (Exception ex)
            {
                m_log.ErrorFormat("Failed to load WorldViewModule: {0}", ex.Message);
                if(m_log.IsDebugEnabled) m_log.DebugFormat("WorldViewModule loading exception details: {0}", ex.ToString());
            }

            return null;
        }

        /// <summary>
        /// Loads DynamicFloaterModule using reflection to avoid hard dependency
        /// </summary>
        private static INonSharedRegionModule LoadDynamicFloaterModule()
        {
            try
            {
                if(m_log.IsDebugEnabled) m_log.Debug("Attempting to load DynamicFloaterModule via reflection");

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

                // Try to find the DynamicFloaterModule type in any loaded assembly
                Type dynamicFloaterModuleType = null;
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    dynamicFloaterModuleType = assembly.GetType("OpenSim.Region.OptionalModules.ViewerSupport.DynamicFloaterModule");
                    if (dynamicFloaterModuleType != null)
                    {
                        if(m_log.IsDebugEnabled) m_log.DebugFormat("Found DynamicFloaterModule type in assembly: {0}", assembly.FullName);
                        break;
                    }
                }

                if (dynamicFloaterModuleType != null)
                {
                    var moduleInstance = (INonSharedRegionModule)Activator.CreateInstance(dynamicFloaterModuleType);
                    if(m_log.IsDebugEnabled) m_log.Debug("Successfully created DynamicFloaterModule instance");
                    return moduleInstance;
                }
                else
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("DynamicFloaterModule type not found in any loaded assembly");
                }
            }
            catch (Exception ex)
            {
                m_log.ErrorFormat("Failed to load DynamicFloaterModule: {0}", ex.Message);
                if(m_log.IsDebugEnabled) m_log.DebugFormat("DynamicFloaterModule loading exception details: {0}", ex.ToString());
            }

            return null;
        }

        /// <summary>
        /// Loads DynamicMenuModule using reflection to avoid hard dependency
        /// </summary>
        private static INonSharedRegionModule LoadDynamicMenuModule()
        {
            try
            {
                if(m_log.IsDebugEnabled) m_log.Debug("Attempting to load DynamicMenuModule via reflection");

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

                // Try to find the DynamicMenuModule type in any loaded assembly
                Type dynamicMenuModuleType = null;
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    dynamicMenuModuleType = assembly.GetType("OpenSim.Region.OptionalModules.ViewerSupport.DynamicMenuModule");
                    if (dynamicMenuModuleType != null)
                    {
                        if(m_log.IsDebugEnabled) m_log.DebugFormat("Found DynamicMenuModule type in assembly: {0}", assembly.FullName);
                        break;
                    }
                }

                if (dynamicMenuModuleType != null)
                {
                    var moduleInstance = (INonSharedRegionModule)Activator.CreateInstance(dynamicMenuModuleType);
                    if(m_log.IsDebugEnabled) m_log.Debug("Successfully created DynamicMenuModule instance");
                    return moduleInstance;
                }
                else
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("DynamicMenuModule type not found in any loaded assembly");
                }
            }
            catch (Exception ex)
            {
                m_log.ErrorFormat("Failed to load DynamicMenuModule: {0}", ex.Message);
                if(m_log.IsDebugEnabled) m_log.DebugFormat("DynamicMenuModule loading exception details: {0}", ex.ToString());
            }

            return null;
        }

        /// <summary>
        /// Loads SpecialUIModule using reflection to avoid hard dependency
        /// </summary>
        private static INonSharedRegionModule LoadSpecialUIModule()
        {
            try
            {
                if(m_log.IsDebugEnabled) m_log.Debug("Attempting to load SpecialUIModule via reflection");

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

                // Try to find the SpecialUIModule type in any loaded assembly
                Type specialUIModuleType = null;
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    specialUIModuleType = assembly.GetType("OpenSim.Region.OptionalModules.ViewerSupport.SpecialUIModule");
                    if (specialUIModuleType != null)
                    {
                        if(m_log.IsDebugEnabled) m_log.DebugFormat("Found SpecialUIModule type in assembly: {0}", assembly.FullName);
                        break;
                    }
                }

                if (specialUIModuleType != null)
                {
                    var moduleInstance = (INonSharedRegionModule)Activator.CreateInstance(specialUIModuleType);
                    if(m_log.IsDebugEnabled) m_log.Debug("Successfully created SpecialUIModule instance");
                    return moduleInstance;
                }
                else
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("SpecialUIModule type not found in any loaded assembly");
                }
            }
            catch (Exception ex)
            {
                m_log.ErrorFormat("Failed to load SpecialUIModule: {0}", ex.Message);
                if(m_log.IsDebugEnabled) m_log.DebugFormat("SpecialUIModule loading exception details: {0}", ex.ToString());
            }

            return null;
        }

        /// <summary>
        /// Loads CameraOnlyModeModule using reflection to avoid hard dependency
        /// </summary>
        private static INonSharedRegionModule LoadCameraOnlyModeModule()
        {
            try
            {
                if(m_log.IsDebugEnabled) m_log.Debug("Attempting to load CameraOnlyModeModule via reflection");

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

                // Try to find the CameraOnlyModeModule type in any loaded assembly
                Type cameraOnlyModeModuleType = null;
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    cameraOnlyModeModuleType = assembly.GetType("OpenSim.Region.OptionalModules.ViewerSupport.CameraOnlyModeModule");
                    if (cameraOnlyModeModuleType != null)
                    {
                        if(m_log.IsDebugEnabled) m_log.DebugFormat("Found CameraOnlyModeModule type in assembly: {0}", assembly.FullName);
                        break;
                    }
                }

                if (cameraOnlyModeModuleType != null)
                {
                    var moduleInstance = (INonSharedRegionModule)Activator.CreateInstance(cameraOnlyModeModuleType);
                    if(m_log.IsDebugEnabled) m_log.Debug("Successfully created CameraOnlyModeModule instance");
                    return moduleInstance;
                }
                else
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("CameraOnlyModeModule type not found in any loaded assembly");
                }
            }
            catch (Exception ex)
            {
                m_log.ErrorFormat("Failed to load CameraOnlyModeModule: {0}", ex.Message);
                if(m_log.IsDebugEnabled) m_log.DebugFormat("CameraOnlyModeModule loading exception details: {0}", ex.ToString());
            }

            return null;
        }

        /// <summary>
        /// Loads LindenUDPInfoModule using reflection to avoid hard dependency on OptionalModules
        /// </summary>
        private static ISharedRegionModule LoadLindenUDPInfoModule()
        {
            try
            {
                // Try to find the LindenUDPInfoModule type in any loaded assembly
                Type lindenUDPInfoModuleType = null;
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    lindenUDPInfoModuleType = assembly.GetType("OpenSim.Region.OptionalModules.UDP.Linden.LindenUDPInfoModule");
                    if (lindenUDPInfoModuleType != null)
                    {
                        if(m_log.IsDebugEnabled) m_log.DebugFormat("Found LindenUDPInfoModule in assembly: {0}", assembly.FullName);
                        break;
                    }
                }

                if (lindenUDPInfoModuleType != null)
                {
                    var moduleInstance = Activator.CreateInstance(lindenUDPInfoModuleType) as ISharedRegionModule;
                    if (moduleInstance != null)
                    {
                        if(m_log.IsDebugEnabled) m_log.Debug("Successfully loaded LindenUDPInfoModule");
                        return moduleInstance;
                    }
                    else
                    {
                        m_log.Warn("LindenUDPInfoModule type found but could not cast to ISharedRegionModule");
                    }
                }
                else
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("LindenUDPInfoModule type not found in any loaded assembly");
                }
            }
            catch (Exception ex)
            {
                m_log.WarnFormat("Could not load LindenUDPInfoModule: {0}", ex.Message);
            }

            return null;
        }

        /// <summary>
        /// Loads AutoBackupModule using reflection to avoid hard dependency on OptionalModules
        /// </summary>
        private static ISharedRegionModule LoadAutoBackupModule()
        {
            try
            {
                // Try to find the AutoBackupModule type in any loaded assembly
                Type autoBackupModuleType = null;
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    autoBackupModuleType = assembly.GetType("OpenSim.Region.OptionalModules.World.AutoBackup.AutoBackupModule");
                    if (autoBackupModuleType != null)
                    {
                        if(m_log.IsDebugEnabled) m_log.DebugFormat("Found AutoBackupModule in assembly: {0}", assembly.FullName);
                        break;
                    }
                }

                if (autoBackupModuleType != null)
                {
                    var moduleInstance = Activator.CreateInstance(autoBackupModuleType) as ISharedRegionModule;
                    if (moduleInstance != null)
                    {
                        if(m_log.IsDebugEnabled) m_log.Debug("Successfully loaded AutoBackupModule");
                        return moduleInstance;
                    }
                    else
                    {
                        m_log.Warn("AutoBackupModule type found but could not cast to ISharedRegionModule");
                    }
                }
                else
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("AutoBackupModule type not found in any loaded assembly");
                }
            }
            catch (Exception ex)
            {
                m_log.WarnFormat("Could not load AutoBackupModule: {0}", ex.Message);
            }

            return null;
        }

        /// <summary>
        /// Loads WebSocketEchoModule using reflection to avoid hard dependency on OptionalModules
        /// </summary>
        private static ISharedRegionModule LoadWebSocketEchoModule()
        {
            try
            {
                // Try to find the WebSocketEchoModule type in any loaded assembly
                Type webSocketEchoModuleType = null;
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    webSocketEchoModuleType = assembly.GetType("OpenSim.Region.OptionalModules.Example.WebSocketEchoTest.WebSocketEchoModule");
                    if (webSocketEchoModuleType != null)
                    {
                        if(m_log.IsDebugEnabled) m_log.DebugFormat("Found WebSocketEchoModule in assembly: {0}", assembly.FullName);
                        break;
                    }
                }

                if (webSocketEchoModuleType != null)
                {
                    var moduleInstance = Activator.CreateInstance(webSocketEchoModuleType) as ISharedRegionModule;
                    if (moduleInstance != null)
                    {
                        if(m_log.IsDebugEnabled) m_log.Debug("Successfully loaded WebSocketEchoModule");
                        return moduleInstance;
                    }
                    else
                    {
                        m_log.Warn("WebSocketEchoModule type found but could not cast to ISharedRegionModule");
                    }
                }
                else
                {
                    if(m_log.IsDebugEnabled) m_log.Debug("WebSocketEchoModule type not found in any loaded assembly");
                }
            }
            catch (Exception ex)
            {
                m_log.WarnFormat("Could not load WebSocketEchoModule: {0}", ex.Message);
            }

            return null;
        }
    }
}