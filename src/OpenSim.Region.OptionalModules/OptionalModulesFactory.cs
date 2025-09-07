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
            // Future non-shared optional modules would go here
            yield break;
        }
    }
}