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

using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using Nini.Config;

using OpenSim.Framework;
using OpenSim.Framework.Monitoring;
using OpenSim.Framework.ServiceAuth;
using OpenSim.Services.Interfaces;
using OpenSim.Server.Base;
using OpenMetaverse;
using System.Text;
using System.Threading;

namespace OpenSim.Services.Connectors
{
    public class XInventoryServicesConnector : BaseServiceConnector, IInventoryService
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Number of requests made to the remote inventory service.
        /// </summary>
        public int RequestsMade { get; private set; }

        private string m_InventoryURL = string.Empty;

        /// <summary>
        /// Timeout for remote requests.
        /// </summary>
        /// <remarks>
        /// In this case, -1 is default timeout (100 seconds), not infinite.
        /// </remarks>
        private int m_requestTimeout = -1;
        private readonly string m_configName = "InventoryService";

        private const double CACHE_EXPIRATION_SECONDS = 30.0;
        private static readonly ExpiringCacheOS<UUID, InventoryItemBase> m_ItemCache = new(15000);

        public XInventoryServicesConnector()
        {
        }

        public XInventoryServicesConnector(string serverURI)
        {
            if (serverURI.EndsWith('/'))
                m_InventoryURL = serverURI + "xinventory";
            else
                m_InventoryURL = serverURI + "/xinventory";

        }

        public XInventoryServicesConnector(IConfigSource source, string configName)
            : base(source, configName)
        {
            m_configName = configName;
            Initialise(source);
        }

        public XInventoryServicesConnector(IConfigSource source)
            : base(source, "InventoryService")
        {
            Initialise(source);
        }

        public virtual void Initialise(IConfigSource source)
        {
            IConfig config = source.Configs[m_configName];
            if (config is null)
            {
                m_log.ErrorFormat("[INVENTORY CONNECTOR]: {0} missing from OpenSim.ini", m_configName);
                throw new Exception("Inventory connector init error");
            }

            string serviceURI = config.GetString("InventoryServerURI", string.Empty);
            if (serviceURI.Length == 0)
            {
                m_log.Error("[INVENTORY CONNECTOR]: No Server URI named in section InventoryService");
                throw new Exception("Inventory connector init error");
            }
            if (serviceURI.EndsWith('/'))
                m_InventoryURL = serviceURI + "xinventory";
            else
                m_InventoryURL = serviceURI + "/xinventory";

             m_requestTimeout = 1000 * config.GetInt("RemoteRequestTimeout", -1);

            StatsManager.RegisterStat(
                new Stat(
                "RequestsMade",
                "Requests made",
                "Number of requests made to the remove inventory service",
                "requests",
                "inventory",
                serviceURI,
                StatType.Pull,
                MeasuresOfInterest.AverageChangeOverTime,
                s => s.Value = RequestsMade,
                StatVerbosity.Debug));
        }

        private static bool CheckReturn(Dictionary<string, object> ret)
        {
            if (m_log.IsDebugEnabled) {
                m_log.DebugFormat("CheckReturn ret-keys: {0}", ret.Keys);
            }
            
            if (ret is null || ret.Count == 0)
                return false;

            if (ret.TryGetValue("RESULT", out object retResult))
            {
                if (retResult is string sretResult)
                {
                    if (bool.TryParse(sretResult, out bool result))
                        return result;
                    return false;
                }
            }
            return true;
        }

        public bool CreateUserInventory(UUID principalID)
        {
            if (m_log.IsDebugEnabled) {
                m_log.DebugFormat("CreateUserInventory({0})", principalID);
            }
            
            Dictionary<string,object> ret = MakeRequest(
                $"METHOD=CREATEUSERINVENTORY&PRINCIPAL={principalID}");

            return CheckReturn(ret);
        }

        public List<InventoryFolderBase> GetInventorySkeleton(UUID principalID)
        {
            if (m_log.IsDebugEnabled) {
                m_log.DebugFormat("GetInventorySkeleton({0})", principalID);
            }
            
            Dictionary<string,object> ret = MakeRequest(
                $"METHOD=GETINVENTORYSKELETON&PRINCIPAL={principalID}");

            if (!CheckReturn(ret))
                return null;

            Dictionary<string, object> folders = (Dictionary<string, object>)ret["FOLDERS"];

            List<InventoryFolderBase> fldrs = new();

            try
            {
                foreach (Object o in folders.Values)
                    fldrs.Add(BuildFolder((Dictionary<string, object>)o));
            }
            catch (Exception e)
            {
                m_log.Error("Exception unwrapping folder list: " + e.Message);
            }

            return fldrs;
        }

        public InventoryFolderBase GetRootFolder(UUID principalID)
        {
            if (m_log.IsDebugEnabled) {
                m_log.DebugFormat("GetRootFolder({0})", principalID);
            }
            Dictionary<string,object> ret = MakeRequest($"METHOD=GETROOTFOLDER&PRINCIPAL={principalID}");

            if (!CheckReturn(ret))
                return null;

            return BuildFolder((Dictionary<string, object>)ret["folder"]);
        }

        public InventoryFolderBase GetFolderForType(UUID principalID, FolderType type)
        {
            if (m_log.IsDebugEnabled) {
                m_log.DebugFormat("GetFolderForType({0}, {1})", principalID, type);
            }
            Dictionary<string,object> ret = MakeRequest(
                $"METHOD=GETFOLDERFORTYPE&PRINCIPAL={principalID}&TYPE={(int)type}");

            if (!CheckReturn(ret))
                return null;

            return BuildFolder((Dictionary<string, object>)ret["folder"]);
        }

        public InventoryCollection GetFolderContent(UUID principalID, UUID folderID)
        {
            if (m_log.IsDebugEnabled) {
                m_log.DebugFormat("GetFolderContent({0}, {1})", principalID, folderID);
            }
            InventoryCollection inventory = new()
            {
                Folders = new(),
                Items = new(),
                OwnerID = principalID
            };

            try
            {
                Dictionary<string,object> ret = MakeRequest(
                    $"METHOD=GETFOLDERCONTENT&PRINCIPAL={principalID}&FOLDER={folderID}");

                if (!CheckReturn(ret))
                    return null;

                if(ret.TryGetValue("FOLDERS", out object ofolders))
                {
                    var folders = (Dictionary<string, object>)ofolders;
                    foreach (object o in folders.Values) // getting the values directly, we don't care about the keys folder_i
                        inventory.Folders.Add(BuildFolder((Dictionary<string, object>)o));
                }
                if(ret.TryGetValue("ITEMS", out object oitems))
                {
                    var items = (Dictionary<string, object>)oitems;
                    foreach (object o in items.Values) // getting the values directly, we don't care about the keys item_i
                        inventory.Items.Add(BuildItem((Dictionary<string, object>)o));
                }
            }
            catch (Exception e)
            {
                m_log.WarnFormat("Exception in GetFolderContent: {0}", e.Message);
            }

            return inventory;
        }

        public virtual InventoryCollection[] GetMultipleFoldersContent(UUID principalID, UUID[] folderIDs)
        {
            if (m_log.IsDebugEnabled) {
                m_log.DebugFormat("GetMultipleFoldersContent({0}, {1})", principalID, string.Join(", ", folderIDs));
            }
            
            InventoryCollection[] inventoryArr = new InventoryCollection[folderIDs.Length];
            try
            {
                Dictionary<string, object> resultSet = MakeRequest(
                    $"METHOD=GETMULTIPLEFOLDERSCONTENT&PRINCIPAL={principalID}&FOLDERS={string.Join(',', folderIDs)}&COUNT={folderIDs.Length}");
        
                if (!CheckReturn(resultSet))
                    return null;
        
                // AKIDO Print resultSet into debug log
                if (m_log.IsDebugEnabled)
                {
                    m_log.Debug("GETMULTIPLEFOLDERSCONTENT ResultSet contents:");
                    foreach (var kvp in resultSet)
                    {
                        m_log.DebugFormat("  {0}: {1}", kvp.Key, kvp.Value);
                    }
                }                
                int i = 0;
                foreach (UUID u in folderIDs.AsSpan())
                {
                    if(resultSet.TryGetValue($"F_{u}", out object oret) && oret is Dictionary<string, object> ret)
                    {
                        UUID inventoryFolderID;
                        if (ret.TryGetValue("FID", out object retFID))
                        {
                            if (!UUID.TryParse((string)retFID, out inventoryFolderID))
                            {
                                m_log.WarnFormat("Could not parse folder id {0}", retFID.ToString());
                                inventoryArr[i] = null;
                                continue;
                            }
                        }
                        else
                        {
                            inventoryArr[i] = null;
                            m_log.WarnFormat("FID key not present in response");
                            continue;
                        }
        
                        if (!ret.TryGetValue("OWNER", out object retOwner) || 
                            !UUID.TryParse((string)retOwner, out UUID inventoryOwnerID))
                        {
                            inventoryArr[i] = null;
                            m_log.Warn($"Could not parse folder {retFID} owner id");
                            continue;
                        }
        
                        InventoryCollection inventory = new()
                        {
                            FolderID = inventoryFolderID,
                            OwnerID = inventoryOwnerID,
                            Folders = new List<InventoryFolderBase>(),
                            Items = new List<InventoryItemBase>()
                        };
        
                        if (!ret.TryGetValue("VERSION", out object retVer) ||
                                !Int32.TryParse((string)retVer, out inventory.Version))
                            inventory.Version = -1;
        
                        if (ret.TryGetValue("FOLDERS", out object ofolders) && ofolders is Dictionary<string, object> folders)
                        {
                            foreach (object o in folders.Values)
                            {
                                inventory.Folders.Add(BuildFolder((Dictionary<string, object>)o));
                            }
                        }
        
                        if (ret.TryGetValue("ITEMS", out object oitems) && oitems is Dictionary<string, object> items)
                        {
                            foreach (object o in items.Values)
                            {
                                inventory.Items.Add(BuildItem((Dictionary<string, object>)o));
                            }
                        }
                        inventoryArr[i] = inventory;
                    }
                    else
                    {
                        inventoryArr[i] = null;
                    }
                    i++;
                }
            }
            catch (Exception e)
            {
                m_log.WarnFormat("Exception in GetMultipleFoldersContent: {0}", e.Message);
            }
        
            return inventoryArr;
        }

        public List<InventoryItemBase> GetFolderItems(UUID principalID, UUID folderID)
        {
            if (m_log.IsDebugEnabled) {
                m_log.DebugFormat("GetFolderItems({0}, {1})", principalID, folderID);
            }
            Dictionary<string,object> ret = MakeRequest(
                $"METHOD=GETFOLDERITEMS&PRINCIPAL={principalID}&FOLDER={folderID}");

            if (!CheckReturn(ret))
                return null;

            Dictionary<string, object> items = (Dictionary<string, object>)ret["ITEMS"];
            List<InventoryItemBase> fitems = new(items.Count);
            foreach (object o in items.Values) // getting the values directly, we don't care about the keys item_i
                fitems.Add(BuildItem((Dictionary<string, object>)o));

            return fitems;
        }

        public bool AddFolder(InventoryFolderBase folder)
        {
            if (m_log.IsDebugEnabled) {
                m_log.DebugFormat("AddFolder({0})", folder);
            }
            Dictionary<string,object> ret = MakeRequest(
                    new Dictionary<string,object> {
                        { "METHOD", "ADDFOLDER"},
                        { "ParentID", folder.ParentID.ToString() },
                        { "Type", folder.Type.ToString() },
                        { "Version", folder.Version.ToString() },
                        { "Name", folder.Name.ToString() },
                        { "Owner", folder.Owner.ToString() },
                        { "ID", folder.ID.ToString() }
                    });

            return CheckReturn(ret);
        }

        public bool UpdateFolder(InventoryFolderBase folder)
        {
            if (m_log.IsDebugEnabled) {
                m_log.DebugFormat("UpdateFolder({0})", folder);
            }
            Dictionary<string,object> ret = MakeRequest(
                $"METHOD=UPDATEFOLDER&ParentID={folder.ParentID}&Type={folder.Type}&Version={folder.Version}&Name={folder.Name}&Owner={folder.Owner}&ID={folder.ID}");

            return CheckReturn(ret);
        }

        public bool MoveFolder(InventoryFolderBase folder)
        {
            if (m_log.IsDebugEnabled) {
                m_log.DebugFormat("MoveFolder({0})", folder);
            }
            Dictionary<string,object> ret = MakeRequest(
                $"METHOD=MOVEFOLDER&ParentID={folder.ParentID}&ID={folder.ID}&PRINCIPAL={folder.Owner}");
            return CheckReturn(ret);
        }

        public bool DeleteFolders(UUID principalID, List<UUID> folderIDs)
        {
            if (m_log.IsDebugEnabled) {
                m_log.DebugFormat("DeleteFolders({0}, {1})", principalID, folderIDs);
            }
            List<string> slist = new();

            foreach (UUID f in folderIDs)
                slist.Add(f.ToString());

            Dictionary<string,object> ret = MakeRequest(
                    new Dictionary<string,object> {
                        { "METHOD", "DELETEFOLDERS"},
                        { "PRINCIPAL", principalID.ToString() },
                        { "FOLDERS", slist }
                    });

            return CheckReturn(ret);
        }

        public bool PurgeFolder(InventoryFolderBase folder)
        {
            if (m_log.IsDebugEnabled) {
                m_log.DebugFormat("PurgeFolder({0})", folder);
            }
            Dictionary<string,object> ret = MakeRequest(
                $"METHOD=PURGEFOLDER&ID={folder.ID}");
            return CheckReturn(ret);
        }

        public bool AddItem(InventoryItemBase item)
        {
            if (m_log.IsDebugEnabled) {
                m_log.DebugFormat("AddItem({0})", item);
            }
            item.Description ??= string.Empty;
            item.CreatorData ??= string.Empty;
            item.CreatorId ??= string.Empty;
            Dictionary<string, object> ret = MakeRequest(
                    new Dictionary<string,object> {
                        { "METHOD", "ADDITEM"},
                        { "AssetID", item.AssetID.ToString() },
                        { "AssetType", item.AssetType.ToString() },
                        { "Name", item.Name.ToString() },
                        { "Owner", item.Owner.ToString() },
                        { "ID", item.ID.ToString() },
                        { "InvType", item.InvType.ToString() },
                        { "Folder", item.Folder.ToString() },
                        { "CreatorId", item.CreatorId.ToString() },
                        { "CreatorData", item.CreatorData.ToString() },
                        { "Description", item.Description.ToString() },
                        { "NextPermissions", item.NextPermissions.ToString() },
                        { "CurrentPermissions", item.CurrentPermissions.ToString() },
                        { "BasePermissions", item.BasePermissions.ToString() },
                        { "EveryOnePermissions", item.EveryOnePermissions.ToString() },
                        { "GroupPermissions", item.GroupPermissions.ToString() },
                        { "GroupID", item.GroupID.ToString() },
                        { "GroupOwned", item.GroupOwned.ToString() },
                        { "SalePrice", item.SalePrice.ToString() },
                        { "SaleType", item.SaleType.ToString() },
                        { "Flags", item.Flags.ToString() },
                        { "CreationDate", item.CreationDate.ToString() }
                    });

            return CheckReturn(ret);
        }

        public bool UpdateItem(InventoryItemBase item)
        {
            if (m_log.IsDebugEnabled) {
                m_log.DebugFormat("UpdateItem({0})", item);
            }
            item.CreatorData ??= string.Empty;
            Dictionary<string,object> ret = MakeRequest(
                    new Dictionary<string,object> {
                        { "METHOD", "UPDATEITEM"},
                        { "AssetID", item.AssetID.ToString() },
                        { "AssetType", item.AssetType.ToString() },
                        { "Name", item.Name },
                        { "Owner", item.Owner.ToString() },
                        { "ID", item.ID.ToString() },
                        { "InvType", item.InvType.ToString() },
                        { "Folder", item.Folder.ToString() },
                        { "CreatorId", item.CreatorId },
                        { "CreatorData", item.CreatorData },
                        { "Description", item.Description },
                        { "NextPermissions", item.NextPermissions.ToString() },
                        { "CurrentPermissions", item.CurrentPermissions.ToString() },
                        { "BasePermissions", item.BasePermissions.ToString() },
                        { "EveryOnePermissions", item.EveryOnePermissions.ToString() },
                        { "GroupPermissions", item.GroupPermissions.ToString() },
                        { "GroupID", item.GroupID.ToString() },
                        { "GroupOwned", item.GroupOwned.ToString() },
                        { "SalePrice", item.SalePrice.ToString() },
                        { "SaleType", item.SaleType.ToString() },
                        { "Flags", item.Flags.ToString() },
                        { "CreationDate", item.CreationDate.ToString() }
                    });

            bool result = CheckReturn(ret);
            if (result)
            {
                m_ItemCache.AddOrUpdate(item.ID, item, CACHE_EXPIRATION_SECONDS);
            }

            return result;
        }

        public bool MoveItems(UUID principalID, List<InventoryItemBase> items)
        {
            if (m_log.IsDebugEnabled) {
                m_log.DebugFormat("MoveItems({0}, {1})", principalID, items);
            }
            List<string> idlist = new();
            List<string> destlist = new();

            foreach (InventoryItemBase item in items)
            {
                idlist.Add(item.ID.ToString());
                m_ItemCache.Remove(item.ID);
                destlist.Add(item.Folder.ToString());
            }

            Dictionary<string,object> ret = MakeRequest(
                    new Dictionary<string,object> {
                        { "METHOD", "MOVEITEMS"},
                        { "PRINCIPAL", principalID.ToString() },
                        { "IDLIST", idlist },
                        { "DESTLIST", destlist }
                    });

            return CheckReturn(ret);
        }

        public bool DeleteItems(UUID principalID, List<UUID> itemIDs)
        {
            if (m_log.IsDebugEnabled) {
                m_log.DebugFormat("DeleteItems({0}, {1})", principalID, itemIDs);
            }
            List<string> slist = new();

            foreach (UUID f in itemIDs)
            {
                slist.Add(f.ToString());
                m_ItemCache.Remove(f);
            }

            Dictionary<string,object> ret = MakeRequest(
                new Dictionary<string,object> {
                    { "METHOD", "DELETEITEMS"},
                    { "PRINCIPAL", principalID.ToString() },
                    { "ITEMS", slist }
                });

            return CheckReturn(ret);
        }

        public InventoryItemBase GetItem(UUID principalID, UUID itemID)
        {
            if (m_log.IsDebugEnabled) {
                m_log.DebugFormat("GetItem({0}, {1})", principalID, itemID);
            }
            if (m_ItemCache.TryGetValue(itemID, out InventoryItemBase retrieved))
                return retrieved;

            try
            {
                Dictionary<string, object> ret = MakeRequest($"METHOD=GETITEM&ID={itemID}&PRINCIPAL={principalID}");
                if (!CheckReturn(ret))
                    return null;

                retrieved = BuildItem((Dictionary<string, object>)ret["item"]);
            }
            catch (Exception e)
            {
                m_log.Error("Exception in GetItem: " + e.Message);
            }

            m_ItemCache.AddOrUpdate(itemID, retrieved, CACHE_EXPIRATION_SECONDS);

            return retrieved;
        }

        public virtual InventoryItemBase[] GetMultipleItems(UUID principalID, UUID[] itemIDs)
        {
            if (m_log.IsDebugEnabled) {
                m_log.DebugFormat("GetMultipleItems({0}, {1})", principalID, String.Join(",", itemIDs));
            }

            InventoryItemBase[] itemArr = new InventoryItemBase[itemIDs.Length];

            // Try to get them from the cache
            InventoryItemBase item;
            int i = 0;
            int pending = 0;

            StringBuilder sb = new(4096);
            sb.Append($"METHOD=GETMULTIPLEITEMS&PRINCIPAL={principalID}&ITEMS=");
            foreach (UUID id in itemIDs.AsSpan())
            {
                if (m_ItemCache.TryGetValue(id, out item))
                    itemArr[i++] = item;
                else
                {
                    sb.Append(id.ToString());
                    sb.Append(',');
                    pending++;
                }
            }
            if(pending == 0)
            {
                return itemArr;
            }

            sb.Remove(sb.Length - 1, 1);
            sb.Append($"&COUNT={pending}");

            try
            {
                Dictionary<string, object> resultSet = MakeRequest(sb.ToString());

                if (!CheckReturn(resultSet))
                {
                    return i == 0 ? null : itemArr;
                }

                // carry over index i where we left above
                foreach (KeyValuePair<string, object> kvp in resultSet)
                {
                    if (kvp.Key.StartsWith("item_"))
                    {
                        if (kvp.Value is Dictionary<string, object> dic)
                        {
                            item = BuildItem(dic);
                            m_ItemCache.AddOrUpdate(item.ID, item, CACHE_EXPIRATION_SECONDS);
                            itemArr[i++] = item;
                        }
                        else
                            itemArr[i++] = null;
                    }
                }
            }
            catch (Exception e)
            {
                m_log.WarnFormat("Exception in GetMultipleItems: {0}", e.Message);
            }

            return itemArr;
        }

        public InventoryFolderBase GetFolder(UUID principalID, UUID folderID)
        {
            if (m_log.IsDebugEnabled) {
                m_log.DebugFormat("GetFolder({0}, {1})", principalID, folderID);
            }
            
            try
            {
                Dictionary<string, object> ret = MakeRequest(
                    $"METHOD=GETFOLDER&ID={folderID}&PRINCIPAL={principalID}");

                if (!CheckReturn(ret))
                    return null;

                return BuildFolder((Dictionary<string, object>)ret["folder"]);
            }
            catch (Exception e)
            {
                m_log.Error("Exception in GetFolder: " + e.Message);
            }

            return null;
        }

        public List<InventoryItemBase> GetActiveGestures(UUID principalID)
        {
            if (m_log.IsDebugEnabled) {
                m_log.DebugFormat("GetActiveGestures({0})", principalID);
            }
            Dictionary<string,object> ret = MakeRequest(
                $"METHOD=GETACTIVEGESTURES&PRINCIPAL={principalID}");

            if (!CheckReturn(ret))
                return null;

            if (ret["ITEMS"] is not Dictionary<string,object> itemsDict)
                    return null;

            List<InventoryItemBase> items = new(itemsDict.Count);

            foreach (object o in itemsDict.Values)
                items.Add(BuildItem((Dictionary<string, object>)o));

            return items;
        }

        public int GetAssetPermissions(UUID principalID, UUID assetID)
        {
            if (m_log.IsDebugEnabled) {
                m_log.DebugFormat("GetAssetPermissions({0}, {1})", principalID, assetID);
            }
            Dictionary<string,object> ret = MakeRequest(
                $"METHOD=GETASSETPERMISSIONS&PRINCIPAL={principalID}&ASSET={assetID}");

            // We cannot use CheckReturn() here because valid values for RESULT are "false" (in the case of request failure) or an int
            if (ret is null)
                return 0;

            if (ret.TryGetValue("RESULT", out object retRes))
            {
                if (retRes is string res)
                {
                    if (int.TryParse (res, out int intResult))
                        return intResult;
                }
            }

            return 0;
        }

        public bool HasInventoryForUser(UUID principalID)
        {
            return false;
        }

        // Helpers
        //
        private Dictionary<string, object> MakeRequest(Dictionary<string, object> sendData)
        {
            if (m_log.IsDebugEnabled)
            {
                m_log.DebugFormat("MakeRequest sendData: {0}", sendData);
            }
            RequestsMade++;
            Dictionary<string, object> replyData = MakePostDicRequest(ServerUtils.BuildQueryString(sendData));

            return replyData;
        }

        private Dictionary<string, object> MakeRequest(string query)
        {
            if (m_log.IsDebugEnabled)
            {
                m_log.DebugFormat("MakeRequest query: {0}", query);
            }
            RequestsMade++;
            Dictionary<string, object> replyData = MakePostDicRequest(query);

            return replyData;
        }

        private static InventoryFolderBase BuildFolder(Dictionary<string,object> data)
        {
            if (m_log.IsDebugEnabled)
                m_log.DebugFormat("BuildFolder data: {0}", data.Keys);

            try
            {
                InventoryFolderBase folder = new()
                {
                    ParentID = new UUID((string)data["ParentID"]),
                    Type = short.Parse((string)data["Type"]),
                    Version = ushort.Parse((string)data["Version"]),
                    Name = (string)data["Name"],
                    Owner = new UUID((string)data["Owner"]),
                    ID = new UUID((string)data["ID"])
                };
                return folder;
            }
            catch (Exception e)
            {
                m_log.Error($"Exception building folder: {e.Message}");
            }

            return new InventoryFolderBase();
        }

        private static InventoryItemBase BuildItem(Dictionary<string,object> data)
        {
            if (m_log.IsDebugEnabled)
                m_log.DebugFormat("BuildItem data: {0}", data);
            
            try
            {
                InventoryItemBase item = new()
                {
                    AssetID = new UUID((string)data["AssetID"]),
                    AssetType = int.Parse((string)data["AssetType"]),
                    Name = (string)data["Name"],
                    Owner = new UUID((string)data["Owner"]),
                    ID = new UUID((string)data["ID"]),
                    InvType = int.Parse((string)data["InvType"]),
                    Folder = new UUID((string)data["Folder"]),
                    CreatorId = (string)data["CreatorId"],
                    NextPermissions = uint.Parse((string)data["NextPermissions"]),
                    CurrentPermissions = uint.Parse((string)data["CurrentPermissions"]),
                    BasePermissions = uint.Parse((string)data["BasePermissions"]),
                    EveryOnePermissions = uint.Parse((string)data["EveryOnePermissions"]),
                    GroupPermissions = uint.Parse((string)data["GroupPermissions"]),
                    GroupID = new UUID((string)data["GroupID"]),
                    GroupOwned = bool.Parse((string)data["GroupOwned"]),
                    SalePrice = int.Parse((string)data["SalePrice"]),
                    SaleType = byte.Parse((string)data["SaleType"]),
                    Flags = uint.Parse((string)data["Flags"]),
                    CreationDate = int.Parse((string)data["CreationDate"]),
                    Description = (string)data["Description"]
                };
                if (data.TryGetValue("CreatorData", out object oCreatorData))
                    item.CreatorData = (string)oCreatorData;
                return item;
            }
            catch (Exception e)
            {
                m_log.Error($"Exception building item: {e.Message}");
            }
            return new InventoryItemBase();
        }
        public Dictionary<string, object> MakePostDicRequest(string obj)
        {
            if (m_log.IsDebugEnabled)
                m_log.DebugFormat("MakePostDicRequest obj: {0}", obj);
            
            if (string.IsNullOrEmpty(obj))
            {
                m_log.Warn($"empty post data");
                return new Dictionary<string, object>();
            }

            Dictionary<string, object> respDic = null;
            int ticks = Util.EnvironmentTickCount();
            int sendlen = 0;
            int rcvlen = 0;
            HttpResponseMessage responseMessage = null;
            HttpRequestMessage request = null;
            HttpClient client = null;
            try
            {
                client = WebUtil.GetNewGlobalHttpClient(m_requestTimeout);

                request = new(HttpMethod.Post, m_InventoryURL);

                m_Auth?.AddAuthorization(request.Headers);

                //if (keepalive)
                {
                    request.Headers.TryAddWithoutValidation("Keep-Alive", "timeout=30, max=10");
                    request.Headers.TryAddWithoutValidation("Connection", "Keep-Alive");
                    request.Headers.ConnectionClose = false;
                }
                //else
                //    request.Headers.TryAddWithoutValidation("Connection", "close");

                request.Headers.ExpectContinue = false;
                request.Headers.TransferEncodingChunked = false;

                byte[] data = Util.UTF8NBGetbytes(obj);
                sendlen = data.Length;

                request.Content = new ByteArrayContent(data);
                request.Content.Headers.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded");
                request.Content.Headers.TryAddWithoutValidation("Content-Length", sendlen.ToString());

                responseMessage = client.Send(request, HttpCompletionOption.ResponseHeadersRead);
                responseMessage.EnsureSuccessStatusCode();

                // AKIDO - Check Content-Length because of PHPGridServer
                // Sometimes the Content-length will be delivered sometimes when streamed there will be no Content-Length
                if (WebUtil.hasContentLength(responseMessage))
                {
                    m_log.Info($"Found Content-Length header in response from {request.RequestUri}"); 
                    if ((responseMessage.Content.Headers.ContentLength is long contentLength) && contentLength != 0)
                    {
                        rcvlen = (int)contentLength;
                        respDic = ServerUtils.ParseXmlResponse(responseMessage.Content.ReadAsStream());
                    }
                    else {
                        m_log.Info($"Content-Length is 0 in response from {request.RequestUri}"); 
                    }
                }
                else
                {
                    m_log.Info($"No Content-Length found in response from {request.RequestUri} - trying to read anyway..."); 
                    respDic = ServerUtils.ParseXmlResponse(responseMessage.Content.ReadAsStream());
                }
                // AKIDO - End of Content-Length check

            }
            catch (Exception e)
            {
                m_log.Info($"Error receiving response from {m_InventoryURL}: {e.Message}");
                throw;
            }
            finally
            {
                request?.Dispose();
                responseMessage?.Dispose();
                client?.Dispose();
            }

            ticks = Util.EnvironmentTickCountSubtract(ticks);
            if (ticks > WebUtil.LongCallTime)
            {
                m_log.Info($"POST {m_InventoryURL} took {ticks}ms {sendlen}/{rcvlen}bytes");
            }

            return respDic ?? new Dictionary<string, object>();
        }
    }
}
