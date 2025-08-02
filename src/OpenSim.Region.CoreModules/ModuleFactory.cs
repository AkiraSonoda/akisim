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
using OpenSim.Region.Framework.Interfaces;

using OpenSim.Region.CoreModules.Agent.AssetTransaction;
using OpenSim.Region.CoreModules.Agent.IPBan;
using OpenSim.Region.CoreModules.Agent.TextureSender;
using OpenSim.Region.CoreModules.Agent.UserManagement;
using OpenSim.Region.CoreModules.Agent.Xfer;
using OpenSim.Region.CoreModules.Avatar.Attachments;
using OpenSim.Region.CoreModules.Avatar.AvatarFactory;
using OpenSim.Region.CoreModules.Avatar.Chat;
using OpenSim.Region.CoreModules.Avatar.Dialog;
using OpenSim.Region.CoreModules.Avatar.Friends;
using OpenSim.Region.CoreModules.Avatar.Gods;
using OpenSim.Region.CoreModules.Avatar.Groups;
using OpenSim.Region.CoreModules.Avatar.InstantMessage;
using OpenSim.Region.CoreModules.Avatar.Inventory.Archiver;
using OpenSim.Region.CoreModules.Avatar.Inventory.Transfer;
using OpenSim.Region.CoreModules.Avatar.Lure;
using OpenSim.Region.CoreModules.Avatar.Profiles;
using OpenSim.Region.CoreModules.Framework.InventoryAccess;
using OpenSim.Region.CoreModules.Framework.Statistics.Logging;
using OpenSim.Region.CoreModules.Framework.UserManagement;
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
using OpenSim.Region.CoreModules.ServiceConnectorsOut.Simulation;
using OpenSim.Region.CoreModules.ServiceConnectorsOut.UserAccounts;
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

namespace OpenSim.Region.CoreModules
{
    /// <summary>
    /// Factory for creating core region modules without relying on Mono.Addins
    /// This provides a .NET 8 compatible alternative to the plugin system
    /// </summary>
    public static class CoreModuleFactory
    {
        /// <summary>
        /// Creates all essential core modules for basic OpenSim functionality
        /// </summary>
        public static IEnumerable<INonSharedRegionModule> CreateNonSharedModules()
        {
            // Agent modules
            yield return new AssetTransactionModule();
            yield return new IPBanModule();
            yield return new TextureSender();
            yield return new XferModule();

            // Avatar modules  
            yield return new AttachmentsModule();
            yield return new AvatarFactoryModule();
            yield return new ChatModule();
            yield return new DialogModule();
            yield return new FriendsModule();
            yield return new GodsModule();
            yield return new GroupsModule();
            yield return new MessageTransferModule();
            yield return new InventoryArchiverModule();
            yield return new InventoryTransferModule();
            yield return new LureModule();
            yield return new BasicProfileModule();

            // Framework modules
            yield return new BasicInventoryAccessModule();
            yield return new BinaryLoggingModule();
            yield return new UserManagementModule();

            // Service connector modules
            yield return new LocalAssetServicesConnector();
            yield return new LocalAuthenticationServicesConnector();
            yield return new LocalAuthorizationServicesConnector();
            yield return new LocalAvatarServicesConnector();
            yield return new LocalGridServicesConnector();
            yield return new LocalGridUserServicesConnector();
            yield return new LocalInventoryServicesConnector();
            yield return new LandServicesConnector();
            yield return new NeighbourServicesConnector();
            yield return new LocalPresenceServicesConnector();
            yield return new LocalSimulationConnectorModule();
            yield return new LocalUserAccountServicesConnector();

            // World modules
            yield return new ArchiverModule();
            yield return new EstateModule();
            yield return new DefaultDwellModule();
            yield return new LandManagementModule();
            yield return new PrimCountModule();
            yield return new BuySellModule();
            yield return new ObjectCommandsModule();
            yield return new DefaultPermissionsModule();
            yield return new RestartModule();
            yield return new SerialiserModule();
            yield return new SoundModule();
            yield return new TerrainModule();
            yield return new VegetationModule();
            yield return new WindModule();
        }

        /// <summary>
        /// Creates shared modules that are instantiated once per OpenSim instance
        /// </summary>
        public static IEnumerable<ISharedRegionModule> CreateSharedModules()
        {
            // Add shared modules here when needed
            // Most core modules are non-shared
            yield break;
        }
    }
}