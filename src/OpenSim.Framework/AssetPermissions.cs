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
using Nini.Config;
using log4net;
using OpenMetaverse;

namespace OpenSim.Framework
{
    public class AssetPermissions
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(
            MethodBase.GetCurrentMethod().DeclaringType);

        private bool[] m_DisallowExport, m_DisallowImport;
        private string[] m_AssetTypeNames;

        public AssetPermissions(IConfig config)
        {
            Type enumType = typeof(AssetType);
            m_AssetTypeNames = Enum.GetNames(enumType);
            for (int i = 0; i < m_AssetTypeNames.Length; i++)
                m_AssetTypeNames[i] = m_AssetTypeNames[i].ToLower();
            int n = Enum.GetValues(enumType).Length;
            m_DisallowExport = new bool[n];
            m_DisallowImport = new bool[n];

            LoadPermsFromConfig(config, "DisallowExport", m_DisallowExport);
            LoadPermsFromConfig(config, "DisallowImport", m_DisallowImport);

        }

        private void LoadPermsFromConfig(IConfig assetConfig, string variable, bool[] bitArray)
        {
            if (assetConfig == null)
                return;

            string perms = assetConfig.GetString(variable, String.Empty);
            string[] parts = perms.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string s in parts)
            {
                int index = Array.IndexOf(m_AssetTypeNames, s.Trim().ToLower());
                if (index >= 0)
                    bitArray[index] = true;
                else
                    m_log.Warn($"[Asset Permissions]: Invalid AssetType {s}");
            }

        }

        public bool AllowedExport(sbyte type)
        {
            string assetTypeName = ((AssetType)type).ToString();

            int index = Array.IndexOf(m_AssetTypeNames, assetTypeName.ToLower());
            if (index >= 0 && m_DisallowExport[index])
            {
                m_log.Debug($"[Asset Permissions]: Export denied: configuration does not allow export of AssetType {assetTypeName}");
                return false;
            }

            return true;
        }

        public bool AllowedImport(sbyte type)
        {
            string assetTypeName = ((AssetType)type).ToString();

            int index = Array.IndexOf(m_AssetTypeNames, assetTypeName.ToLower());
            if (index >= 0 && m_DisallowImport[index])
            {
                m_log.Debug($"[Asset Permissions]: Import denied: configuration does not allow import of AssetType {assetTypeName}");
                return false;
            }

            return true;
        }


    }
}
