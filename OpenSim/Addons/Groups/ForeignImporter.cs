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
using log4net;

namespace OpenSim.Groups
{
    public class ForeignImporter
    {
		private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		IUserManagement m_UserManagement;

		public ForeignImporter(IUserManagement uman)
        {
			if (m_log.IsDebugEnabled) {
				m_log.DebugFormat ("{0} called", System.Reflection.MethodBase.GetCurrentMethod ().Name);
			}

            m_UserManagement = uman;
        }

        public GroupMembersData ConvertGroupMembersData(ExtendedGroupMembersData _m)
        {
			if (m_log.IsDebugEnabled) {
				m_log.DebugFormat ("{0} called", System.Reflection.MethodBase.GetCurrentMethod ().Name);
			}


            GroupMembersData m = new GroupMembersData();
            m.AcceptNotices = _m.AcceptNotices;
            m.AgentPowers = _m.AgentPowers;
            m.Contribution = _m.Contribution;
            m.IsOwner = _m.IsOwner;
            m.ListInProfile = _m.ListInProfile;
            m.OnlineStatus = _m.OnlineStatus;
            m.Title = _m.Title;

            string url = string.Empty, first = string.Empty, last = string.Empty, tmp = string.Empty;
            Util.ParseUniversalUserIdentifier(_m.AgentID, out m.AgentID, out url, out first, out last, out tmp);
            if (url != string.Empty)
                m_UserManagement.AddUser(m.AgentID, first, last, url);

            return m;
        }

        public GroupRoleMembersData ConvertGroupRoleMembersData(ExtendedGroupRoleMembersData _rm)
        {
			if (m_log.IsDebugEnabled) {
				m_log.DebugFormat ("{0} called", System.Reflection.MethodBase.GetCurrentMethod ().Name);
			}

            GroupRoleMembersData rm = new GroupRoleMembersData();
            rm.RoleID = _rm.RoleID;

            string url = string.Empty, first = string.Empty, last = string.Empty, tmp = string.Empty;
            Util.ParseUniversalUserIdentifier(_rm.MemberID, out rm.MemberID, out url, out first, out last, out tmp);
            if (url != string.Empty)
                m_UserManagement.AddUser(rm.MemberID, first, last, url);

            return rm;
        }

    }
}
