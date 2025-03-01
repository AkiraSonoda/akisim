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

using System.Collections.Generic;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Services.Interfaces;
// AKIDO: clean

namespace OpenSim.Services.HypergridService
{
    public class UserAccountCache : IUserAccountService
    {
        private const double CACHE_EXPIRATION_SECONDS = 120000000; // 33 hours!

//        private static readonly ILog m_log =
//                LogManager.GetLogger(
//                MethodBase.GetCurrentMethod().DeclaringType);

        private ExpiringCacheOS<UUID, UserAccount> m_UUIDCache;

        private IUserAccountService m_UserAccountService;

        private static UserAccountCache m_Singleton;

        public static UserAccountCache CreateUserAccountCache(IUserAccountService u)
        {
            if (m_Singleton == null)
                m_Singleton = new UserAccountCache(u);

            return m_Singleton;
        }

        private UserAccountCache(IUserAccountService u)
        {
            m_UUIDCache = new ExpiringCacheOS<UUID, UserAccount>(120000000);
            m_UserAccountService = u;
        }

        public void Cache(UUID userID, UserAccount account)
        {
            // Cache even null accounts
            m_UUIDCache.AddOrUpdate(userID, account, CACHE_EXPIRATION_SECONDS);

            //m_log.DebugFormat("[USER CACHE]: cached user {0}", userID);
        }

        public UserAccount Get(UUID userID, out bool inCache)
        {
            UserAccount account = null;
            inCache = false;
            if (m_UUIDCache.TryGetValue(userID, out account))
            {
                //m_log.DebugFormat("[USER CACHE]: Account {0} {1} found in cache", account.FirstName, account.LastName);
                inCache = true;
                return account;
            }

            return null;
        }

        public UserAccount GetUser(string id)
        {
            UUID uuid = UUID.Zero;
            UUID.TryParse(id, out uuid);
            bool inCache = false;
            UserAccount account = Get(uuid, out inCache);
            if (!inCache)
            {
                account = m_UserAccountService.GetUserAccount(UUID.Zero, uuid);
                Cache(uuid, account);
            }

            return account;
        }

        #region IUserAccountService
        public UserAccount GetUserAccount(UUID scopeID, UUID userID)
        {
            return GetUser(userID.ToString());
        }

        public UserAccount GetUserAccount(UUID scopeID, string FirstName, string LastName)
        {
            return null;
        }

        public UserAccount GetUserAccount(UUID scopeID, string Email)
        {
            return null;
        }

        public List<UserAccount> GetUserAccountsWhere(UUID scopeID, string query)
        {
            return null;
        }

        public List<UserAccount> GetUserAccounts(UUID scopeID, string query)
        {
            return null;
        }

        public List<UserAccount> GetUserAccounts(UUID scopeID, List<string> IDs)
        {
            return null;
        }

        public void InvalidateCache(UUID userID)
        {
            m_UUIDCache.Remove(userID);
        }

        public bool StoreUserAccount(UserAccount data)
        {
            return false;
        }
        #endregion

    }

}
