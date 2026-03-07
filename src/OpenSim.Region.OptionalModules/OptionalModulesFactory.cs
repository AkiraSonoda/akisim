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

using OpenSim.Region.OptionalModules.Avatar.Friends;
using OpenSim.Region.CoreModules.Avatars.Commands;
using OpenSim.Region.OptionalModules.Avatar.Appearance;
using OpenSim.Region.OptionalModules.Avatar.Animations;
using OpenSim.Region.CoreModules.ServiceConnectorsOut.Land;
using OpenSim.Region.CoreModules.ServiceConnectorsOut.MuteList;
using OpenSim.Region.CoreModules.ServiceConnectorsOut.Presence;
using OpenSim.Region.CoreModules.ServiceConnectorsOut.Simulation;
using OpenSim.Region.CoreModules.ServiceConnectorsOut.UserAccounts;
using OpenSim.Region.CoreModules.ServiceConnectorsOut.Inventory;
using OpenSim.Region.CoreModules.World.Archiver;
using OpenSim.Region.CoreModules.Avatar.Inventory.Archiver;
using OpenSim.Region.CoreModules.Framework.Library;
using OpenSim.Region.CoreModules.Avatar.Inventory.Transfer;
using OpenSim.Region.CoreModules.Asset;
using OpenSim.Region.CoreModules.ServiceConnectorsOut.Asset;
using OpenSim.Region.OptionalModules.Asset;
using OpenSim.Region.OptionalModules.Agent.TextureSender;
using OpenSim.Region.ClientStack.Linden;
using OpenSim.Region.CoreModules.Scripting.DynamicTexture;
using OpenSim.Region.OptionalModules.Materials;
using OpenSim.Region.OptionalModules.Scripting.JsonStore;
using OpenSim.Region.OptionalModules.Scripting.XmlRpcGridRouterModule;
using OpenSim.Region.OptionalModules.World.TreePopulator;
using OpenSim.Region.OptionalModules.World.MoneyModule;
using OpenSim.Region.OptionalModules.ViewerSupport;
using OpenSim.Region.OptionalModules.World.SceneCommands;

namespace OpenSim.Region.OptionalModules
{
    /// <summary>
    /// Factory for creating OptionalModules based on configuration
    /// </summary>
    public static class OptionalModulesFactory
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Creates shared optional modules that are instantiated once per OpenSim instance
        /// Reads configuration to determine which optional modules to load
        /// </summary>
        public static IEnumerable<ISharedRegionModule> CreateOptionalSharedModules(IConfigSource configSource)
        {
            var modulesConfig = configSource?.Configs["Modules"];
            
            if (modulesConfig == null)
            {
                if (m_log.IsDebugEnabled) m_log.Debug("No [Modules] configuration section found, no optional modules will be loaded");
                yield break;
            }

            // Load FriendsCommandsModule if enabled for debugging/administration
            if (modulesConfig.GetBoolean("FriendsCommandsModule", false))
            {
                if (m_log.IsDebugEnabled) m_log.Debug("Loading FriendsCommandsModule for friends debugging commands");
                yield return new FriendsCommandsModule();
            }
            else
            {
                if (m_log.IsDebugEnabled) m_log.Debug("FriendsCommandsModule disabled - set FriendsCommandsModule = true in [Modules] to enable friends debugging commands");
            }

            // Load UserCommandsModule if enabled for user administration commands
            if (modulesConfig.GetBoolean("UserCommandsModule", false))
            {
                if (m_log.IsDebugEnabled) m_log.Debug("Loading UserCommandsModule for user administration commands");
                yield return new UserCommandsModule();
            }
            else
            {
                if (m_log.IsDebugEnabled) m_log.Debug("UserCommandsModule disabled - set UserCommandsModule = true in [Modules] to enable user administration commands");
            }

            // Load AppearanceInfoModule if enabled for appearance debugging commands
            if (modulesConfig.GetBoolean("AppearanceInfoModule", false))
            {
                if (m_log.IsDebugEnabled) m_log.Debug("Loading AppearanceInfoModule for appearance debugging commands");
                yield return new AppearanceInfoModule();
            }
            else
            {
                if (m_log.IsDebugEnabled) m_log.Debug("AppearanceInfoModule disabled - set AppearanceInfoModule = true in [Modules] to enable appearance debugging commands");
            }

            // Load AnimationsCommandModule if enabled for animation debugging commands
            if (modulesConfig.GetBoolean("AnimationsCommandModule", false))
            {
                if (m_log.IsDebugEnabled) m_log.Debug("Loading AnimationsCommandModule for avatar animation debugging and inspection commands");
                yield return new AnimationsCommandModule();
                if (m_log.IsInfoEnabled) m_log.Info("AnimationsCommandModule loaded for avatar animation debugging, animation inspection, and animator state analysis");
            }
            else
            {
                if (m_log.IsDebugEnabled) m_log.Debug("AnimationsCommandModule disabled - set AnimationsCommandModule = true in [Modules] to enable animation debugging commands");
            }

            // Load J2KDecoderCommandModule if enabled for JPEG2000 debugging commands
            if (modulesConfig.GetBoolean("J2KDecoderCommandModule", false))
            {
                if (m_log.IsDebugEnabled) m_log.Debug("Loading J2KDecoderCommandModule for JPEG2000 texture debugging commands");
                yield return new J2KDecoderCommandModule();
                if (m_log.IsInfoEnabled) m_log.Info("J2KDecoderCommandModule loaded for JPEG2000 texture debugging and analysis commands");
            }
            else
            {
                if (m_log.IsDebugEnabled) m_log.Debug("J2KDecoderCommandModule disabled - set J2KDecoderCommandModule = true in [Modules] to enable JPEG2000 debugging commands");
            }

            // Load RemoteLandServicesConnector if enabled for distributed land services
            if (modulesConfig.GetString("LandServices", "") == "RemoteLandServicesConnector")
            {
                if (m_log.IsDebugEnabled) m_log.Debug("Loading RemoteLandServicesConnector for distributed land services");
                yield return new RemoteLandServicesConnector();
            }
            else
            {
                if (m_log.IsDebugEnabled) m_log.Debug("RemoteLandServicesConnector disabled - set LandServices = RemoteLandServicesConnector in [Modules] to enable distributed land services");
            }

            // Load RemoteMuteListServicesConnector if enabled for distributed mute list services
            if (modulesConfig.GetString("MuteListService", "") == "RemoteMuteListServicesConnector")
            {
                if (m_log.IsDebugEnabled) m_log.Debug("Loading RemoteMuteListServicesConnector for distributed mute list services");
                yield return new RemoteMuteListServicesConnector();
            }
            else
            {
                if (m_log.IsDebugEnabled) m_log.Debug("RemoteMuteListServicesConnector disabled - set MuteListService = RemoteMuteListServicesConnector in [Modules] to enable distributed mute list services");
            }

            // Load RemotePresenceServicesConnector if enabled for distributed presence services
            if (modulesConfig.GetString("PresenceServices", "") == "RemotePresenceServicesConnector")
            {
                if (m_log.IsDebugEnabled) m_log.Debug("Loading RemotePresenceServicesConnector for distributed presence services");
                yield return new RemotePresenceServicesConnector();
            }
            else
            {
                if (m_log.IsDebugEnabled) m_log.Debug("RemotePresenceServicesConnector disabled - set PresenceServices = RemotePresenceServicesConnector in [Modules] to enable distributed presence services");
            }

            // Load RemoteSimulationConnectorModule if enabled for distributed simulation services
            if (modulesConfig.GetString("SimulationServices", "") == "RemoteSimulationConnectorModule")
            {
                if (m_log.IsDebugEnabled) m_log.Debug("Loading RemoteSimulationConnectorModule for distributed simulation services");
                yield return new RemoteSimulationConnectorModule();
            }
            else
            {
                if (m_log.IsDebugEnabled) m_log.Debug("RemoteSimulationConnectorModule disabled - set SimulationServices = RemoteSimulationConnectorModule in [Modules] to enable distributed simulation services");
            }

            // Load RemoteUserAccountServicesConnector if enabled for distributed user account services
            if (modulesConfig.GetString("UserAccountServices", "") == "RemoteUserAccountServicesConnector")
            {
                if (m_log.IsDebugEnabled) m_log.Debug("Loading RemoteUserAccountServicesConnector for distributed user account services");
                yield return new RemoteUserAccountServicesConnector();
            }
            else
            {
                if (m_log.IsDebugEnabled) m_log.Debug("RemoteUserAccountServicesConnector disabled - set UserAccountServices = RemoteUserAccountServicesConnector in [Modules] to enable distributed user account services");
            }

            // Load RemoteXInventoryServicesConnector if enabled for distributed inventory services
            if (modulesConfig.GetString("InventoryServices", "") == "RemoteXInventoryServicesConnector")
            {
                if (m_log.IsDebugEnabled) m_log.Debug("Loading RemoteXInventoryServicesConnector for distributed inventory services");
                yield return new RemoteXInventoryServicesConnector();
            }
            else
            {
                if (m_log.IsDebugEnabled) m_log.Debug("RemoteXInventoryServicesConnector disabled - set InventoryServices = RemoteXInventoryServicesConnector in [Modules] to enable distributed inventory services");
            }

            // Load InventoryArchiverModule if enabled for inventory archive operations
            if (modulesConfig.GetBoolean("InventoryArchiverModule", true))  // Default to true for backward compatibility
            {
                if (m_log.IsDebugEnabled) m_log.Debug("Loading InventoryArchiverModule for inventory archive operations");
                yield return new InventoryArchiverModule();
            }
            else
            {
                if (m_log.IsDebugEnabled) m_log.Debug("InventoryArchiverModule disabled - set InventoryArchiverModule = true in [Modules] to enable inventory archive operations");
            }

            // Load LibraryModule if enabled for library services
            if (modulesConfig.GetBoolean("LibraryModule", false))
            {
                if (m_log.IsDebugEnabled) m_log.Debug("Loading LibraryModule for library services and archive loading");
                yield return new LibraryModule();
            }
            else
            {
                if (m_log.IsDebugEnabled) m_log.Debug("LibraryModule disabled - set LibraryModule = true in [Modules] to enable library services");
            }

            // Load InventoryTransferModule if enabled for inventory transfer operations
            if (modulesConfig.GetBoolean("InventoryTransferModule", true))  // Default to true for backward compatibility
            {
                if (m_log.IsDebugEnabled) m_log.Debug("Loading InventoryTransferModule for inventory transfer operations via instant messages");
                yield return new InventoryTransferModule();
            }
            else
            {
                if (m_log.IsDebugEnabled) m_log.Debug("InventoryTransferModule disabled - set InventoryTransferModule = true in [Modules] to enable inventory transfers");
            }

            // Load FlotsamAssetCache if configured as the AssetCaching module
            string assetCaching = modulesConfig.GetString("AssetCaching", "");
            if (assetCaching == "FlotsamAssetCache")
            {
                if (m_log.IsDebugEnabled) m_log.Debug("Loading FlotsamAssetCache for distributed asset caching with memory and file cache support");
                yield return new FlotsamAssetCache();
            }
            else
            {
                if (m_log.IsDebugEnabled) m_log.Debug("FlotsamAssetCache disabled - set AssetCaching = FlotsamAssetCache in [Modules] to enable distributed asset caching");
            }

            // Load RegionAssetConnector if configured as the AssetServices module
            string assetServices = modulesConfig.GetString("AssetServices", "");
            if (assetServices == "RegionAssetConnector")
            {
                if (m_log.IsDebugEnabled) m_log.Debug("Loading RegionAssetConnector for local and hypergrid asset service connectivity with caching support");
                yield return new RegionAssetConnector();
            }
            else
            {
                if (m_log.IsDebugEnabled) m_log.Debug("RegionAssetConnector disabled - set AssetServices = RegionAssetConnector in [Modules] to enable asset service connectivity");
            }

            // Load AssetInfoModule if enabled for asset inspection and debugging commands
            if (modulesConfig.GetBoolean("AssetInfoModule", false))
            {
                if (m_log.IsDebugEnabled) m_log.Debug("Loading AssetInfoModule for asset inspection and debugging commands");
                yield return new AssetInfoModule();
            }
            else
            {
                if (m_log.IsDebugEnabled) m_log.Debug("AssetInfoModule disabled - set AssetInfoModule = true in [Modules] to enable asset inspection commands");
            }

            // Load FetchInventory2Module if enabled for inventory fetching capabilities
            if (modulesConfig.GetBoolean("FetchInventory2Module", false))
            {
                if (m_log.IsDebugEnabled) m_log.Debug("Loading FetchInventory2Module for enhanced inventory fetching and library access capabilities");
                yield return new FetchInventory2Module();
            }
            else
            {
                if (m_log.IsDebugEnabled) m_log.Debug("FetchInventory2Module disabled - set FetchInventory2Module = true in [Modules] to enable inventory fetching capabilities");
            }

            // Load HGInventoryBroker if configured as the InventoryServices module
            string inventoryServices = modulesConfig.GetString("InventoryServices", "");
            if (inventoryServices == "HGInventoryBroker")
            {
                if (m_log.IsDebugEnabled) m_log.Debug("Loading HGInventoryBroker for hypergrid inventory management with local and foreign user support");
                yield return new HGInventoryBroker();
            }
            else
            {
                if (m_log.IsDebugEnabled) m_log.Debug("HGInventoryBroker disabled - set InventoryServices = HGInventoryBroker in [Modules] to enable hypergrid inventory brokering");
            }

            // Load DynamicTextureModule if enabled for dynamic texture generation
            if (modulesConfig.GetBoolean("DynamicTextureModule", false))
            {
                if (m_log.IsDebugEnabled) m_log.Debug("Loading DynamicTextureModule for dynamic texture generation with rendering plugin support");
                yield return new DynamicTextureModule();
            }
            else
            {
                if (m_log.IsDebugEnabled) m_log.Debug("DynamicTextureModule disabled - set DynamicTextureModule = true in [Modules] to enable dynamic texture generation");
            }

            // Load SampleMoneyModule if configured as the EconomyModule for basic economy functionality
            string economyModule = modulesConfig.GetString("EconomyModule", "");
            if (economyModule == "SampleMoneyModule")
            {
                if (m_log.IsDebugEnabled) m_log.Debug("Loading SampleMoneyModule for basic economy and currency functionality with XMLRPC support");
                yield return new SampleMoneyModule();
                if (m_log.IsInfoEnabled) m_log.Info("SampleMoneyModule loaded for basic economy, currency transactions, XMLRPC handlers, and money balance management");
            }
            else
            {
                if (m_log.IsDebugEnabled) m_log.Debug("SampleMoneyModule disabled - set EconomyModule = SampleMoneyModule in [Modules] to enable basic economy functionality");
            }

            // Load GodNamesModule if enabled for viewer god name display functionality
            var godNamesConfig = configSource?.Configs["GodNames"];
            if (godNamesConfig?.GetBoolean("Enabled", false) == true)
            {
                if (m_log.IsDebugEnabled) m_log.Debug("Loading GodNamesModule for viewer god name display and god user identification");
                yield return new GodNamesModule();
                if (m_log.IsInfoEnabled) m_log.Info("GodNamesModule loaded for viewer god name display, god user identification, and simulator features integration");
            }
            else
            {
                if (m_log.IsDebugEnabled) m_log.Debug("GodNamesModule disabled - set Enabled = true in [GodNames] section to enable god name display functionality");
            }

            // Additional optional modules can be added here as needed
            // Example pattern for future modules:
            /*
            if (modulesConfig.GetBoolean("SomeOtherOptionalModule", false))
            {
                if (m_log.IsDebugEnabled) m_log.Debug("Loading SomeOtherOptionalModule");
                yield return new SomeOtherOptionalModule();
            }
            */
        }

        /// <summary>
        /// Creates non-shared optional modules that are instantiated per region
        /// Currently empty but can be extended as needed
        /// </summary>
        public static IEnumerable<IRegionModuleBase> CreateOptionalRegionModules(IConfigSource configSource)
        {
            var modulesConfig = configSource?.Configs["Modules"];

            if (modulesConfig == null)
            {
                if (m_log.IsDebugEnabled) m_log.Debug("No [Modules] configuration section found, no optional region modules will be loaded");
                yield break;
            }

            // Load ArchiverModule if enabled for region archive operations
            if (modulesConfig.GetBoolean("ArchiverModule", true))  // Default to true for backward compatibility
            {
                if (m_log.IsDebugEnabled) m_log.Debug("Loading ArchiverModule for region archive operations");
                yield return new ArchiverModule();
            }
            else
            {
                if (m_log.IsDebugEnabled) m_log.Debug("ArchiverModule disabled - set ArchiverModule = true in [Modules] to enable region archive operations");
            }

            // Load MaterialsModule if enabled for PBR materials support
            if (modulesConfig.GetBoolean("MaterialsModule", true))  // Default to true for backward compatibility
            {
                if (m_log.IsDebugEnabled) m_log.Debug("Loading MaterialsModule for PBR materials and glTF rendering support");
                yield return new MaterialsModule();
            }
            else
            {
                if (m_log.IsDebugEnabled) m_log.Debug("MaterialsModule disabled - set MaterialsModule = true in [Modules] to enable PBR materials support");
            }

            // Load YEngine script engine if enabled
            var startupConfig = configSource?.Configs["Startup"];
            string defaultScriptEngine = startupConfig?.GetString("DefaultScriptEngine", "");
            var yengineConfig = configSource?.Configs["YEngine"];
            bool yengineEnabled = yengineConfig?.GetBoolean("Enabled", false) ?? false;

            if (defaultScriptEngine == "YEngine" || yengineEnabled)
            {
                if (m_log.IsDebugEnabled) m_log.Debug("Loading YEngine script engine");
                var yengineModule = LoadYEngine();
                if (yengineModule != null)
                {
                    yield return yengineModule;
                    if (m_log.IsInfoEnabled) m_log.Info("YEngine script engine loaded successfully");
                }
                else
                {
                    m_log.Warn("YEngine was configured but could not be loaded. Check that OpenSim.Region.ScriptEngine.YEngine.dll is available.");
                }
            }
            else if (!string.IsNullOrEmpty(defaultScriptEngine))
            {
                if (m_log.IsDebugEnabled) m_log.DebugFormat("YEngine not loaded - DefaultScriptEngine configured as '{0}'", defaultScriptEngine);
            }
            else
            {
                if (m_log.IsDebugEnabled) m_log.Debug("YEngine not loaded - no DefaultScriptEngine configured and YEngine not explicitly enabled");
            }

            // Load JsonStoreModule if enabled for JSON data storage functionality
            if (modulesConfig.GetBoolean("JsonStoreModule", false))  // Default to false - must be explicitly enabled
            {
                if (m_log.IsDebugEnabled) m_log.Debug("Loading JsonStoreModule for JSON data storage and script integration");
                yield return new JsonStoreModule();
                if (m_log.IsInfoEnabled) m_log.Info("JsonStoreModule loaded for JSON data storage, JsonStore LSL functions, and script data persistence");
            }
            else
            {
                if (m_log.IsDebugEnabled) m_log.Debug("JsonStoreModule disabled - set JsonStoreModule = true in [Modules] to enable JSON data storage functionality");
            }

            // Load JsonStoreCommandsModule if JsonStore is enabled for console command support
            if (modulesConfig.GetBoolean("JsonStoreModule", false))  // Depends on JsonStore being enabled
            {
                if (m_log.IsDebugEnabled) m_log.Debug("Loading JsonStoreCommandsModule for JsonStore console commands and administration");
                yield return new JsonStoreCommandsModule();
                if (m_log.IsInfoEnabled) m_log.Info("JsonStoreCommandsModule loaded for JsonStore console commands and store statistics");
            }
            else
            {
                if (m_log.IsDebugEnabled) m_log.Debug("JsonStoreCommandsModule disabled - requires JsonStoreModule = true in [Modules] to enable JsonStore console commands");
            }

            // Load JsonStoreScriptModule if JsonStore is enabled for LSL function support
            if (modulesConfig.GetBoolean("JsonStoreModule", false))  // Depends on JsonStore being enabled
            {
                if (m_log.IsDebugEnabled) m_log.Debug("Loading JsonStoreScriptModule for JsonStore LSL functions and script integration");
                yield return new JsonStoreScriptModule();
                if (m_log.IsInfoEnabled) m_log.Info("JsonStoreScriptModule loaded for JsonStore LSL functions, script data operations, and notecard integration");
            }
            else
            {
                if (m_log.IsDebugEnabled) m_log.Debug("JsonStoreScriptModule disabled - requires JsonStoreModule = true in [Modules] to enable JsonStore LSL functions");
            }

            // Load XmlRpcGridRouter if enabled for grid-wide XMLRPC routing functionality
            if (modulesConfig.GetBoolean("XmlRpcGridRouterModule", false))  // Default to false - must be explicitly enabled
            {
                if (m_log.IsDebugEnabled) m_log.Debug("Loading XmlRpcGridRouter for grid-wide XMLRPC routing and hub integration");
                yield return new XmlRpcGridRouter();
                if (m_log.IsInfoEnabled) m_log.Info("XmlRpcGridRouter loaded for grid-wide XMLRPC routing, hub integration, and cross-region communication");
            }
            else
            {
                if (m_log.IsDebugEnabled) m_log.Debug("XmlRpcGridRouter disabled - set XmlRpcGridRouterModule = true in [Modules] to enable grid-wide XMLRPC routing");
            }

            // Load TreePopulatorModule if enabled for automated tree growth and management functionality
            if (modulesConfig.GetBoolean("TreePopulatorModule", false))  // Default to false - must be explicitly enabled
            {
                if (m_log.IsDebugEnabled) m_log.Debug("Loading TreePopulatorModule for automated tree growth, seeding, and lifecycle management");
                yield return new TreePopulatorModule();
                if (m_log.IsInfoEnabled) m_log.Info("TreePopulatorModule loaded for automated tree growth, copse management, and vegetation simulation");
            }
            else
            {
                if (m_log.IsDebugEnabled) m_log.Debug("TreePopulatorModule disabled - set TreePopulatorModule = true in [Modules] to enable automated tree population features");
            }

            // Load SceneCommandsModule if enabled for scene debugging and management commands
            if (modulesConfig.GetBoolean("SceneCommandsModule", false))  // Default to false - must be explicitly enabled
            {
                if (m_log.IsDebugEnabled) m_log.Debug("Loading SceneCommandsModule for scene debugging and management console commands");
                yield return new SceneCommandsModule();
                if (m_log.IsInfoEnabled) m_log.Info("SceneCommandsModule loaded for scene debugging, runtime configuration, and diagnostic commands");
            }
            else
            {
                if (m_log.IsDebugEnabled) m_log.Debug("SceneCommandsModule disabled - set SceneCommandsModule = true in [Modules] to enable scene debugging commands");
            }

            // Future non-shared optional modules would go here
        }

        /// <summary>
        /// Loads YEngine script engine using reflection to avoid circular dependency
        /// </summary>
        private static IRegionModuleBase LoadYEngine()
        {
            try
            {
                if (m_log.IsDebugEnabled) m_log.Debug("Attempting to load YEngine script engine via reflection");

                // First, try to explicitly load the YEngine assembly if not already loaded
                try
                {
                    var assemblyPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "OpenSim.Region.ScriptEngine.YEngine.dll");
                    if (m_log.IsDebugEnabled) m_log.DebugFormat("Looking for YEngine assembly at: {0}", assemblyPath);

                    if (System.IO.File.Exists(assemblyPath))
                    {
                        var assembly = System.Reflection.Assembly.LoadFrom(assemblyPath);
                        if (m_log.IsDebugEnabled) m_log.DebugFormat("Successfully loaded YEngine assembly: {0}", assembly.FullName);
                    }
                    else
                    {
                        if (m_log.IsDebugEnabled) m_log.Debug("OpenSim.Region.ScriptEngine.YEngine.dll not found in base directory");
                    }
                }
                catch (Exception loadEx)
                {
                    if (m_log.IsDebugEnabled) m_log.DebugFormat("Could not load OpenSim.Region.ScriptEngine.YEngine.dll: {0}", loadEx.Message);
                }

                // Try to find the YEngine type in any loaded assembly
                Type yengineType = null;
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    yengineType = assembly.GetType("OpenSim.Region.ScriptEngine.Yengine.Yengine");
                    if (yengineType != null)
                    {
                        if (m_log.IsDebugEnabled) m_log.DebugFormat("Found YEngine type in assembly: {0}", assembly.FullName);
                        break;
                    }
                }

                if (yengineType != null)
                {
                    var moduleInstance = Activator.CreateInstance(yengineType) as IRegionModuleBase;
                    if (moduleInstance != null)
                    {
                        if (m_log.IsDebugEnabled) m_log.Debug("Successfully created YEngine instance");
                        return moduleInstance;
                    }
                    else
                    {
                        m_log.Warn("YEngine type found but could not cast to IRegionModuleBase");
                    }
                }
                else
                {
                    m_log.Warn("YEngine type not found in any loaded assembly");
                    if (m_log.IsDebugEnabled)
                    {
                        m_log.Debug("Available assemblies:");
                        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                        {
                            if (assembly.FullName.Contains("YEngine") || assembly.FullName.Contains("ScriptEngine"))
                                m_log.DebugFormat("  - {0}", assembly.FullName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                m_log.ErrorFormat("Failed to load YEngine script engine: {0}", ex.Message);
                if (m_log.IsDebugEnabled) m_log.DebugFormat("YEngine loading exception details: {0}", ex.ToString());
            }

            return null;
        }
    }
}