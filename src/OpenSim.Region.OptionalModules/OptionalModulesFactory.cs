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
using OpenSim.Region.OptionalModules.Avatar.Commands;
using OpenSim.Region.OptionalModules.Avatar.Appearance;
using OpenSim.Region.CoreModules.ServiceConnectorsOut.Land;
using OpenSim.Region.CoreModules.ServiceConnectorsOut.MuteList;
using OpenSim.Region.CoreModules.ServiceConnectorsOut.Presence;
using OpenSim.Region.CoreModules.ServiceConnectorsOut.Simulation;
using OpenSim.Region.CoreModules.ServiceConnectorsOut.UserAccounts;
using OpenSim.Region.CoreModules.ServiceConnectorsOut.Inventory;
using OpenSim.Region.CoreModules.World.Archiver;
using OpenSim.Region.CoreModules.Avatar.Inventory.Archiver;
using OpenSim.Region.CoreModules.Framework.Library;

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

            // Future non-shared optional modules would go here
        }
    }
}