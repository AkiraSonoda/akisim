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
using System.Collections;
using System.Collections.Generic;
using Nini.Config;
using log4net;
using System.Reflection;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using OpenSim.Services.Connectors;
using OpenSim.Framework;

using OpenMetaverse;

namespace OpenSim.Region.CoreModules.ServiceConnectorsOut.UserAccounts
{
    public class RemoteUserAccountServicesConnector : UserAccountServicesConnector,
            ISharedRegionModule, IUserAccountService
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private bool m_Enabled = false;
        private UserAccountCache m_Cache;

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public string Name
        {
            get { return "RemoteUserAccountServicesConnector"; }
        }

        public override void Initialise(IConfigSource source)
        {
            IConfig moduleConfig = source.Configs["Modules"];
            if (moduleConfig != null)
            {
                string name = moduleConfig.GetString("UserAccountServices", "");
                if (name == Name)
                {
                    IConfig userConfig = source.Configs["UserAccountService"];
                    if (userConfig == null)
                    {
                        m_log.Error("UserAccountService missing from OpenSim.ini");
                        return;
                    }

                    m_Enabled = true;

                    base.Initialise(source);
                    m_Cache = new UserAccountCache();

                    m_log.Info("Remote user account connector enabled for distributed user account services");
                    m_log.Debug("Using UserAccountServicesConnector for remote service communication");
                    m_log.Debug("UserAccountCache configured for improved performance");
                }
                else
                {
                    if (m_log.IsDebugEnabled)
                        m_log.Debug($"Module disabled. UserAccountServices = '{name}', expected '{Name}'");
                }
            }
            else
            {
                if (m_log.IsDebugEnabled)
                    m_log.Debug("No [Modules] configuration section found, connector disabled");
            }
        }

        public void PostInitialise()
        {
            if (!m_Enabled)
                return;
        }

        public void Close()
        {
            if (!m_Enabled)
                return;
        }

        public void AddRegion(Scene scene)
        {
            if (!m_Enabled)
            {
                if (m_log.IsDebugEnabled)
                    m_log.Debug($"Not adding to region {scene.Name} - connector disabled");
                return;
            }

            scene.RegisterModuleInterface<IUserAccountService>(this);
            scene.RegisterModuleInterface<IUserAccountCacheModule>(m_Cache);

            scene.EventManager.OnNewClient += OnNewClient;
            
            if (m_log.IsDebugEnabled)
                m_log.Debug($"Added to region {scene.Name} and registered IUserAccountService interface");
        }

        public void RemoveRegion(Scene scene)
        {
            if (!m_Enabled)
                return;
                
            if (m_log.IsDebugEnabled)
                m_log.Debug($"Removed from region {scene.Name}");
        }

        public void RegionLoaded(Scene scene)
        {
            if (!m_Enabled)
                return;
                
            if (m_log.IsDebugEnabled)
                m_log.Debug($"Region {scene.Name} loaded successfully");
        }

        // When a user actually enters the sim, clear them from
        // cache so the sim will have the current values for
        // flags, title, etc. And country, don't forget country!
        private void OnNewClient(IClientAPI client)
        {
            m_Cache.Remove(client.Name);
            if (m_log.IsDebugEnabled)
                m_log.Debug($"Cleared cache for user {client.Name} on new client connection");
        }

        #region Overwritten methods from IUserAccountService

        public override UserAccount GetUserAccount(UUID scopeID, UUID userID)
        {
            if (m_log.IsDebugEnabled)
                m_log.Debug($"GetUserAccount by UUID for user {userID}");
                
            bool inCache = false;
            UserAccount account;
            account = m_Cache.Get(userID, out inCache);
            if (inCache)
            {
                if (m_log.IsDebugEnabled)
                    m_log.Debug($"GetUserAccount cache hit for user {userID}");
                return account;
            }

            account = base.GetUserAccount(scopeID, userID);
            m_Cache.Cache(userID, account);
            
            if (m_log.IsDebugEnabled)
            {
                if (account != null)
                    m_log.Debug($"GetUserAccount successful for user {userID} ({account.FirstName} {account.LastName})");
                else
                    m_log.Debug($"GetUserAccount returned null for user {userID}");
            }

            return account;
        }

        public override UserAccount GetUserAccount(UUID scopeID, string firstName, string lastName)
        {
            if (m_log.IsDebugEnabled)
                m_log.Debug($"GetUserAccount by name for user '{firstName} {lastName}'");
                
            bool inCache = false;
            UserAccount account;
            account = m_Cache.Get(firstName + " " + lastName, out inCache);
            if (inCache)
            {
                if (m_log.IsDebugEnabled)
                    m_log.Debug($"GetUserAccount cache hit for user '{firstName} {lastName}'");
                return account;
            }

            account = base.GetUserAccount(scopeID, firstName, lastName);
            if (account != null)
                m_Cache.Cache(account.PrincipalID, account);
                
            if (m_log.IsDebugEnabled)
            {
                if (account != null)
                    m_log.Debug($"GetUserAccount successful for user '{firstName} {lastName}' (UUID: {account.PrincipalID})");
                else
                    m_log.Debug($"GetUserAccount returned null for user '{firstName} {lastName}'");
            }

            return account;
        }

        public override List<UserAccount> GetUserAccounts(UUID scopeID, List<string> IDs)
        {
            if (m_log.IsDebugEnabled)
                m_log.Debug($"GetUserAccounts for {IDs.Count} users");
                
            List<UserAccount> accs = new List<UserAccount>();
            List<string> missing = new List<string>();

            UUID uuid = UUID.Zero;
            UserAccount account;
            bool inCache = false;

            foreach(string id in IDs)
            {
                if(UUID.TryParse(id, out uuid))
                {
                    account = m_Cache.Get(uuid, out inCache);
                    if (inCache)
                        accs.Add(account);
                    else
                        missing.Add(id);
                }
            }

            if (m_log.IsDebugEnabled)
                m_log.Debug($"GetUserAccounts found {accs.Count} cached accounts, {missing.Count} need remote lookup");

            if(missing.Count > 0)
            {
                List<UserAccount> ext = base.GetUserAccounts(scopeID, missing);
                if(ext != null && ext.Count >0 )
                {
                    foreach(UserAccount acc in ext)
                    {
                        if(acc != null)
                        {
                            accs.Add(acc);
                            m_Cache.Cache(acc.PrincipalID, acc);
                        }
                    }
                }
            }
            
            if (m_log.IsDebugEnabled)
                m_log.Debug($"GetUserAccounts returning {accs.Count} total accounts");
                
            return accs;
        }

        public override bool StoreUserAccount(UserAccount data)
        {
            // This remote connector refuses to serve this method
            if (m_log.IsDebugEnabled)
                m_log.Debug($"StoreUserAccount called for {data?.FirstName} {data?.LastName} but operation not supported by remote connector");
            return false;
        }

        #endregion
    }
}
