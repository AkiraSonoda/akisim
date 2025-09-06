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
using OpenSim.Region.CoreModules.Avatar.Friends;
using OpenSim.Region.CoreModules.Avatar.Gods;
using OpenSim.Region.CoreModules.Avatar.Groups;
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
using OpenSim.Region.CoreModules.World.Terrain;
using OpenSim.Region.CoreModules.World.Vegetation;
using OpenSim.Region.CoreModules.World.Wind;
using OpenSim.Region.CoreModules.World.WorldMap;
using OpenSim.Region.CoreModules.Hypergrid;
using OpenSim.Region.CoreModules.Framework.Library;
using OpenSim.Region.CoreModules.Framework.EntityTransfer;
using OpenSim.Region.CoreModules.ServiceConnectorsIn.Simulation;
using OpenSim.Region.CoreModules.ServiceConnectorsIn.Land;
using OpenSim.Region.CoreModules.ServiceConnectorsIn.Neighbour;
using OpenSim.Region.CoreModules.ServiceConnectorsIn.Grid;
using OpenSim.Region.CoreModules.ServiceConnectorsIn.MapImage;
using OpenSim.Region.CoreModules.ServiceConnectorsIn.Asset;
using OpenSim.Region.CoreModules.ServiceConnectorsIn.Authentication;
using OpenSim.Region.CoreModules.ServiceConnectorsIn.Inventory;
using OpenSim.Region.CoreModules.ServiceConnectorsIn.Hypergrid;

// Physics modules
using OpenSim.Region.PhysicsModule.BulletS;
using OpenSim.Region.PhysicsModule.POS;
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
            yield return new BasicInventoryAccessModule();
            yield return new HGInventoryAccessModule();
            yield return new LandManagementModule();
            yield return new PrimCountModule();
            yield return new DefaultPermissionsModule();
            yield return new SoundModule();
            
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
            
            // TerrainModule temporarily disabled due to System.Drawing.Common version issues
            // yield return new TerrainModule();
            
            // Essential capabilities module for viewer functionality
            yield return new CapabilitiesModule();
            
            // Essential UDP server for client communication
            yield return new LLUDPServerShim();

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
                    case "pos":
                        yield return new POSScene();
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
            
            // Load AgentPreferencesModule directly for reliable loading
            yield return new AgentPreferencesModule();
            
            var groupsConfig = configSource.Configs["Groups"];

            // Load Friends module based on configuration
            if (modulesConfig?.GetString("FriendsModule", "") == "HGFriendsModule")
                yield return new HGFriendsModule();
            else
                yield return new FriendsModule();

            // Load MessageTransfer module based on configuration  
            if (messagingConfig?.GetString("MessageTransferModule", "") == "HGMessageTransferModule")
                yield return new HGMessageTransferModule();
            else
                yield return new MessageTransferModule();

            // Load Lure module based on configuration
            if (messagingConfig?.GetString("LureModule", "") == "HGLureModule")
                yield return new HGLureModule();
            else
                yield return new LureModule();

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
                yield return new HGUserManagementModule();
            else
                yield return new UserManagementModule();

            // Load Groups module based on configuration
            string groupsModule = modulesConfig?.GetString("Module", "");
            ISharedRegionModule groupsModuleInstance = null;
            
            if (groupsModule == "Groups Module V2")
            {
                // Try to load Groups Module V2 from OpenSim.Addons.Groups assembly
                try
                {
                    var assembly = System.Reflection.Assembly.LoadFrom("OpenSim.Addons.Groups.dll");
                    var groupsV2Type = assembly.GetType("OpenSim.Region.OptionalModules.Avatar.Groups.GroupsModule");
                    if (groupsV2Type != null)
                    {
                        groupsModuleInstance = (ISharedRegionModule)Activator.CreateInstance(groupsV2Type);
                    }
                }
                catch
                {
                    // Will fallback to regular GroupsModule below
                }
            }
            
            // Use regular GroupsModule if Groups V2 failed to load or not configured
            if (groupsModuleInstance == null)
            {
                groupsModuleInstance = new GroupsModule();
            }
            
            yield return groupsModuleInstance;

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
            else if (userAccountServicesModule == "LocalUserAccountServicesConnector")
            {
                m_log.Error("LocalUserAccountServicesConnector is no longer supported. Use RemoteUserAccountServicesConnector instead.");
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
            else if (simulationServicesModule == "LocalSimulationConnectorModule")
            {
                m_log.Error("LocalSimulationConnectorModule is no longer supported. Use RemoteSimulationConnectorModule instead.");
            }

            // Load SimulationServiceInConnector if enabled
            if (modulesConfig?.GetBoolean("SimulationServiceInConnector", false) == true)
            {
                yield return new SimulationServiceInConnectorModule();
            }

            // Load LandServiceInConnector if enabled
            if (modulesConfig?.GetBoolean("LandServiceInConnector", false) == true)
            {
                yield return new LandServiceInConnectorModule();
            }

            // Load NeighbourServiceInConnector if enabled
            if (modulesConfig?.GetBoolean("NeighbourServiceInConnector", false) == true)
            {
                yield return new NeighbourServiceInConnectorModule();
            }

            // Load GridInfoServiceInConnector if enabled
            if (modulesConfig?.GetBoolean("GridInfoServiceInConnector", false) == true)
            {
                yield return new GridInfoServiceInConnectorModule();
            }

            // Load MapImageServiceInConnector if enabled
            if (modulesConfig?.GetBoolean("MapImageServiceInConnector", false) == true)
            {
                yield return new MapImageServiceInConnectorModule();
            }

            // Load AssetServiceInConnector if enabled
            if (modulesConfig?.GetBoolean("AssetServiceInConnector", false) == true)
            {
                yield return new AssetServiceInConnectorModule();
            }

            // Load AuthenticationServiceInConnector if enabled
            if (modulesConfig?.GetBoolean("AuthenticationServiceInConnector", false) == true)
            {
                yield return new AuthenticationServiceInConnectorModule();
            }

            // Load InventoryServiceInConnector if enabled
            if (modulesConfig?.GetBoolean("InventoryServiceInConnector", false) == true)
            {
                yield return new InventoryServiceInConnectorModule();
            }

            // Load HypergridServiceInConnector if enabled
            if (modulesConfig?.GetBoolean("HypergridServiceInConnector", false) == true)
            {
                yield return new HypergridServiceInConnectorModule();
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
            else if (inventoryServicesModule == "LocalInventoryServicesConnector")
            {
                m_log.Error("LocalInventoryServicesConnector is no longer supported. Use RemoteXInventoryServicesConnector instead.");
            }

            // Load PresenceServices module based on configuration
            string presenceServicesModule = modulesConfig?.GetString("PresenceServices", "");
            if (presenceServicesModule == "RemotePresenceServicesConnector")
            {
                yield return new RemotePresenceServicesConnector();
            }
            else if (presenceServicesModule == "LocalPresenceServicesConnector")
            {
                m_log.Error("LocalPresenceServicesConnector is no longer supported. Use RemotePresenceServicesConnector instead.");
            }

            // Load AuthenticationServices module based on configuration
            string authenticationServicesModule = modulesConfig?.GetString("AuthenticationServices", "");
            if (authenticationServicesModule == "RemoteAuthenticationServicesConnector")
            {
                yield return new RemoteAuthenticationServicesConnector();
            }
            else if (authenticationServicesModule == "LocalAuthenticationServicesConnector")
            {
                m_log.Error("LocalAuthenticationServicesConnector is no longer supported. Use RemoteAuthenticationServicesConnector instead.");
            }

            // Load AuthorizationServices module based on configuration
            string authorizationServicesModule = modulesConfig?.GetString("AuthorizationServices", "");
            if (authorizationServicesModule == "RemoteAuthorizationServicesConnector")
            {
                yield return new RemoteAuthorizationServicesConnector();
            }
            else if (authorizationServicesModule == "LocalAuthorizationServicesConnector")
            {
                m_log.Error("LocalAuthorizationServicesConnector is no longer supported. Use RemoteAuthorizationServicesConnector instead.");
            }

            // Load AvatarServices module based on configuration
            string avatarServicesModule = modulesConfig?.GetString("AvatarServices", "");
            if (avatarServicesModule == "RemoteAvatarServicesConnector")
            {
                yield return new RemoteAvatarServicesConnector();
            }
            else if (avatarServicesModule == "LocalAvatarServicesConnector")
            {
                m_log.Error("LocalAvatarServicesConnector is no longer supported. Use RemoteAvatarServicesConnector instead.");
            }

            // Load GridUserServices module based on configuration
            string gridUserServicesModule = modulesConfig?.GetString("GridUserServices", "");
            if (gridUserServicesModule == "RemoteGridUserServicesConnector")
            {
                yield return new RemoteGridUserServicesConnector();
            }
            else if (gridUserServicesModule == "LocalGridUserServicesConnector")
            {
                m_log.Error("LocalGridUserServicesConnector is no longer supported. Use RemoteGridUserServicesConnector instead.");
            }

            // Load LandServices module based on configuration
            string landServicesModule = modulesConfig?.GetString("LandServices", "");
            if (landServicesModule == "RemoteLandServicesConnector")
            {
                yield return new RemoteLandServicesConnector();
            }
            else if (landServicesModule == "LocalLandServicesConnector")
            {
                m_log.Error("LocalLandServicesConnector is no longer supported. Use RemoteLandServicesConnector instead.");
            }

            // Load MuteListServices module based on configuration
            string muteListServicesModule = modulesConfig?.GetString("MuteListService", "");
            if (muteListServicesModule == "RemoteMuteListServicesConnector")
            {
                yield return new RemoteMuteListServicesConnector();
            }
            else if (muteListServicesModule == "LocalMuteListServicesConnector")
            {
                m_log.Error("LocalMuteListServicesConnector is no longer supported. Use RemoteMuteListServicesConnector instead.");
            }

            // Load AgentPreferencesServices module based on configuration
            string agentPreferencesServicesModule = modulesConfig?.GetString("AgentPreferencesServices", "");
            if (agentPreferencesServicesModule == "RemoteAgentPreferencesServicesConnector")
            {
                yield return new RemoteAgentPreferencesServicesConnector();
            }
            else if (agentPreferencesServicesModule == "LocalAgentPreferencesServicesConnector")
            {
                m_log.Error("LocalAgentPreferencesServicesConnector is no longer supported. Use RemoteAgentPreferencesServicesConnector instead.");
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
            }
            else if (groupsV2Config?.GetBoolean("Enabled", false) == true)
            {
                m_log.Info("Groups Module V2 disabled by configuration ([Groups] Module != \"Groups Module V2\")");
            }
            else
            {
                m_log.Info("Groups Module V2 disabled by configuration ([Groups] Enabled = false)");
            }

            // Estate management module - always load as it's essential for estate management

            // Essential shared caps modules - loaded at runtime to avoid circular dependencies
            foreach (var capsModule in CreateCapsModules())
            {
                if (capsModule != null)
                    yield return capsModule;
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