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
using System.Text;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using netcd;
using netcd.Serialization;
using netcd.Advanced;
using netcd.Advanced.Requests;

namespace OpenSim.Region.OptionalModules.Framework.Monitoring
{
    /// <summary>
    /// Allows to store monitoring data in etcd, a high availability
    /// name-value store.
    /// </summary>
    public class EtcdMonitoringModule : INonSharedRegionModule, IEtcdModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected Scene m_scene;
        protected IEtcdClient m_client;
        protected bool m_enabled = false;
        protected string m_etcdBasePath = String.Empty;
        protected bool m_appendRegionID = true;

        public string Name
        {
            get { return "EtcdMonitoringModule"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void Initialise(IConfigSource source)
        {
            if(m_log.IsDebugEnabled) m_log.Debug("Initializing EtcdMonitoringModule for high-availability monitoring data storage");

            if (source.Configs["Etcd"] == null)
            {
                if(m_log.IsDebugEnabled) m_log.Debug("EtcdMonitoringModule disabled - no [Etcd] configuration section found");
                return;
            }

            IConfig etcdConfig = source.Configs["Etcd"];

            string etcdUrls = etcdConfig.GetString("EtcdUrls", String.Empty);
            if (etcdUrls.Length == 0)
            {
                if(m_log.IsDebugEnabled) m_log.Debug("EtcdMonitoringModule disabled - no EtcdUrls configured in [Etcd] section");
                return;
            }

            m_etcdBasePath = etcdConfig.GetString("BasePath", m_etcdBasePath);
            m_appendRegionID = etcdConfig.GetBoolean("AppendRegionID", m_appendRegionID);

            if (!m_etcdBasePath.EndsWith("/"))
                m_etcdBasePath += "/";

            if(m_log.IsDebugEnabled) m_log.DebugFormat("EtcdMonitoringModule configuration: URLs={0}, BasePath={1}, AppendRegionID={2}", etcdUrls, m_etcdBasePath, m_appendRegionID);

            try
            {
                string[] endpoints = etcdUrls.Split(new char[] {','});
                List<Uri> uris = new List<Uri>();
                foreach (string endpoint in endpoints)
                    uris.Add(new Uri(endpoint.Trim()));

                m_client = new EtcdClient(uris.ToArray(), new DefaultSerializer(), new DefaultSerializer());
                if(m_log.IsDebugEnabled) m_log.DebugFormat("EtcdMonitoringModule successfully connected to etcd cluster with {0} endpoints", endpoints.Length);
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("Error initializing connection to etcd cluster: {0}", e.ToString());
                return;
            }

            m_log.InfoFormat("EtcdMonitoringModule initialized successfully - connected to etcd cluster for monitoring data storage");
            m_enabled = true;
        }

        public void Close()
        {
            if (m_enabled && m_log.IsDebugEnabled)
                m_log.Debug("EtcdMonitoringModule closing - cleaning up etcd connections");

            //m_client = null;
            m_scene = null;
        }

        public void AddRegion(Scene scene)
        {
            m_scene = scene;

            if (m_enabled)
            {
                if(m_log.IsDebugEnabled) m_log.DebugFormat("Adding EtcdMonitoringModule to region {0} - setting up etcd monitoring data storage", scene.RegionInfo.RegionName);

                if (m_appendRegionID)
                    m_etcdBasePath += m_scene.RegionInfo.RegionID.ToString() + "/";

                if(m_log.IsDebugEnabled) m_log.DebugFormat("Using base path {0} for all keys in region {1}", m_etcdBasePath, scene.RegionInfo.RegionName);

                try
                {
                    m_client.Advanced.CreateDirectory(new CreateDirectoryRequest() {Key = m_etcdBasePath});
                    if(m_log.IsDebugEnabled) m_log.DebugFormat("Successfully created base directory {0} in etcd", m_etcdBasePath);
                }
                catch (Exception e)
                {
                    m_log.ErrorFormat("Exception trying to create base path {0} in etcd: {1}", m_etcdBasePath, e.ToString());
                }

                scene.RegisterModuleInterface<IEtcdModule>(this);

                if(m_log.IsInfoEnabled) m_log.InfoFormat("EtcdMonitoringModule added to region {0} - etcd monitoring data storage available at base path {1}", scene.RegionInfo.RegionName, m_etcdBasePath);
            }
        }

        public void RemoveRegion(Scene scene)
        {
            if (m_enabled && m_log.IsDebugEnabled)
                m_log.DebugFormat("Removing EtcdMonitoringModule from region {0} - etcd monitoring data storage no longer available", scene.RegionInfo.RegionName);
        }

        public void RegionLoaded(Scene scene)
        {
        }

        public bool Store(string k, string v)
        {
            return Store(k, v, 0);
        }

        public bool Store(string k, string v, int ttl)
        {
            string fullKey = m_etcdBasePath + k;
            if(m_log.IsDebugEnabled) m_log.DebugFormat("Storing key {0} with value {1} (TTL: {2})", fullKey, v, ttl > 0 ? ttl.ToString() : "none");

            Response resp = m_client.Advanced.SetKey(new SetKeyRequest() { Key = fullKey, Value = v, TimeToLive = ttl });

            if (resp == null)
            {
                m_log.WarnFormat("Null response when storing key {0}", fullKey);
                return false;
            }

            if (resp.ErrorCode.HasValue)
            {
                m_log.WarnFormat("Error {0} ({1}) storing {2} => {3}", resp.Cause, (int)resp.ErrorCode, fullKey, v);
                return false;
            }

            if(m_log.IsDebugEnabled) m_log.DebugFormat("Successfully stored key {0}", fullKey);
            return true;
        }

        public string Get(string k)
        {
            string fullKey = m_etcdBasePath + k;
            if(m_log.IsDebugEnabled) m_log.DebugFormat("Getting key {0}", fullKey);

            Response resp = m_client.Advanced.GetKey(new GetKeyRequest() { Key = fullKey });

            if (resp == null)
            {
                m_log.WarnFormat("Null response when getting key {0}", fullKey);
                return String.Empty;
            }

            if (resp.ErrorCode.HasValue)
            {
                m_log.WarnFormat("Error {0} ({1}) getting {2}", resp.Cause, (int)resp.ErrorCode, fullKey);
                return String.Empty;
            }

            if(m_log.IsDebugEnabled) m_log.DebugFormat("Successfully retrieved key {0} with value {1}", fullKey, resp.Node.Value);
            return resp.Node.Value;
        }

        public void Delete(string k)
        {
            string fullKey = m_etcdBasePath + k;
            if(m_log.IsDebugEnabled) m_log.DebugFormat("Deleting key {0}", fullKey);

            try
            {
                m_client.Advanced.DeleteKey(new DeleteKeyRequest() { Key = fullKey });
                if(m_log.IsDebugEnabled) m_log.DebugFormat("Successfully deleted key {0}", fullKey);
            }
            catch (Exception e)
            {
                m_log.WarnFormat("Error deleting key {0}: {1}", fullKey, e.Message);
            }
        }

        public void Watch(string k, Action<string> callback)
        {
            string fullKey = m_etcdBasePath + k;
            if(m_log.IsDebugEnabled) m_log.DebugFormat("Setting up watch on key {0}", fullKey);

            try
            {
                m_client.Advanced.WatchKey(new WatchKeyRequest() { Key = fullKey, Callback = (x) => {
                    if(m_log.IsDebugEnabled) m_log.DebugFormat("Watch triggered for key {0} with value {1}", fullKey, x.Node.Value);
                    callback(x.Node.Value);
                } });
                if(m_log.IsDebugEnabled) m_log.DebugFormat("Watch successfully established on key {0}", fullKey);
            }
            catch (Exception e)
            {
                m_log.WarnFormat("Error setting up watch on key {0}: {1}", fullKey, e.Message);
            }
        }
    }
}
