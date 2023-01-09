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
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using ThreadedClasses;
using Mono.Addins;
// AKIDO: clean

namespace OpenSim.Region.CoreModules.Avatar.Groups
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "GroupsModule")]
    public class GroupsModule : ISharedRegionModule
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private RwLockedDictionary<UUID, GroupMembershipData> m_GroupMap = // AKIDO
                new RwLockedDictionary<UUID, GroupMembershipData>();

        private RwLockedDictionary<UUID, IClientAPI> m_ClientMap = // AKIDO
                new RwLockedDictionary<UUID, IClientAPI>();

        private UUID opensimulatorGroupID =
                new UUID("00000000-68f9-1111-024e-222222111123");

        private RwLockedList<Scene> m_SceneList = new RwLockedList<Scene>(); // AKIDO

        private static GroupMembershipData osGroup =
                new GroupMembershipData();

        private bool m_Enabled = false;

        #region ISharedRegionModule Members

        public void Initialise(IConfigSource config)
        {
            IConfig groupsConfig = config.Configs["Groups"];

            if (groupsConfig == null)
            {
                m_log.Info("No configuration found. Using defaults");
            }
            else
            {
                m_Enabled = groupsConfig.GetBoolean("Enabled", false);
                if (!m_Enabled)
                {
                    m_log.Info("Groups disabled in configuration");
                    return;
                }

                if (groupsConfig.GetString("Module", "Default") != "Default")
                {
                    m_Enabled = false;
                    return;
                }
            }

        }

        public void AddRegion(Scene scene)
        {
            if (!m_Enabled)
                return;

            // AKIDO
            if (!m_SceneList.Contains(scene))
            {
                if (m_SceneList.Count == 0)
                {
                    osGroup.GroupID = opensimulatorGroupID;
                    osGroup.GroupName = "OpenSimulator Testing";
                    osGroup.GroupPowers =
                        (uint)(GroupPowers.AllowLandmark |
                               GroupPowers.AllowSetHome);
                    m_GroupMap[opensimulatorGroupID] = osGroup;
                }

                m_SceneList.Add(scene);
            }
            // AKIDO

            scene.EventManager.OnNewClient += OnNewClient;
            scene.EventManager.OnClientClosed += OnClientClosed;
            //            scene.EventManager.OnIncomingInstantMessage += OnGridInstantMessage;
        }

        public void RemoveRegion(Scene scene)
        {
            if (!m_Enabled)
                return;

            // AKIDO
            if (m_SceneList.Contains(scene))
                m_SceneList.Remove(scene);
            // AKIDO

            scene.EventManager.OnNewClient -= OnNewClient;
            scene.EventManager.OnClientClosed -= OnClientClosed;
        }

        public void RegionLoaded(Scene scene)
        {
        }

        public void PostInitialise()
        {
        }

        public void Close()
        {
            if (!m_Enabled)
                return;

            m_log.Debug("Shutting down group module.");

            // AKIDO
            m_ClientMap.Clear();
            // AKIDO

            // AKIDO
            m_GroupMap.Clear();
            // AKIDO
        }

        public string Name
        {
            get { return "GroupsModule"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        #endregion

        private void OnNewClient(IClientAPI client)
        {
            // Subscribe to instant messages
//            client.OnInstantMessage += OnInstantMessage;
            client.OnAgentDataUpdateRequest += OnAgentDataUpdateRequest;
            client.OnUUIDGroupNameRequest += HandleUUIDGroupNameRequest;
            // AKIDO
                if (!m_ClientMap.ContainsKey(client.AgentId))
                {
                    m_ClientMap.Add(client.AgentId, client);
                }
            // AKIDO
        }

        private void OnAgentDataUpdateRequest(IClientAPI remoteClient,
                                              UUID AgentID, UUID SessionID)
        {
            UUID ActiveGroupID;
            string ActiveGroupName;
            ulong ActiveGroupPowers;

            string firstname = remoteClient.FirstName;
            string lastname = remoteClient.LastName;

            string ActiveGroupTitle = "I IZ N0T";

            ActiveGroupID = osGroup.GroupID;
            ActiveGroupName = osGroup.GroupName;
            ActiveGroupPowers = osGroup.GroupPowers;

            remoteClient.SendAgentDataUpdate(AgentID, ActiveGroupID, firstname,
                                             lastname, ActiveGroupPowers, ActiveGroupName,
                                             ActiveGroupTitle);
        }
        
        private void HandleUUIDGroupNameRequest(UUID id,IClientAPI remote_client)
        {
            string groupnamereply = "Unknown";
            UUID groupUUID = UUID.Zero;

            // AKIDO
            if (m_GroupMap.ContainsKey(id))
            {
                GroupMembershipData grp = m_GroupMap[id];
                groupnamereply = grp.GroupName;
                groupUUID = grp.GroupID;
            }
            // AKIDO
            remote_client.SendGroupNameReply(groupUUID, groupnamereply);
        }

        public GroupMembershipData[] GetMembershipData(UUID agentID)
        {
            GroupMembershipData[] updateGroups = new GroupMembershipData[1];
            updateGroups[0] = osGroup;
            return updateGroups;
        }

        public GroupMembershipData GetActiveMembershipData(UUID agentID)
        {
            return osGroup;
        }

        private void OnClientClosed(UUID agentID, Scene scene)
        {
            // AKIDO
            if (m_ClientMap.ContainsKey(agentID))
            {
                m_ClientMap.Remove(agentID);
            }
            // AKIDO
        }
    }
}
