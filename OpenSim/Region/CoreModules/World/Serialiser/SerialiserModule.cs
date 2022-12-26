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
using System.IO;
using System.Reflection;

using log4net;
using Nini.Config;
using Mono.Addins;

using OpenMetaverse;
using OpenSim.Region.CoreModules.Framework.InterfaceCommander;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Scenes.Serialization;
using ThreadedClasses;

namespace OpenSim.Region.CoreModules.World.Serialiser
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "SerialiserModule")]
    public class SerialiserModule : ISharedRegionModule, IRegionSerialiserModule
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

//        private Commander m_commander = new Commander("export");
        private RwLockedList<Scene> m_regions = new RwLockedList<Scene>(); // AKIDO
        private string m_savedir = "exports";
        private RwLockedList<IFileSerialiser> m_serialisers = new RwLockedList<IFileSerialiser>(); // AKIDO

        #region ISharedRegionModule Members

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void Initialise(IConfigSource source)
        {
            IConfig config = source.Configs["Serialiser"];
            if (config != null)
            {
                m_savedir = config.GetString("save_dir", m_savedir);
            }

            m_log.InfoFormat("Enabled, using save dir \"{0}\"", m_savedir);
        }

        public void PostInitialise()
        {
            // AKIDO
            m_serialisers.Add(new SerialiseTerrain());
            m_serialisers.Add(new SerialiseObjects());
            // AKIDO

//            LoadCommanderCommands();
        }

        public void AddRegion(Scene scene)
        {
//            scene.RegisterModuleCommander(m_commander);
//            scene.EventManager.OnPluginConsole += EventManager_OnPluginConsole;
            scene.RegisterModuleInterface<IRegionSerialiserModule>(this);

            // AKIDO
            m_regions.Add(scene);
            // AKIDO
        }

        public void RegionLoaded(Scene scene)
        {
        }

        public void RemoveRegion(Scene scene)
        {
            // AKIDO
            m_regions.Remove(scene);
            // AKIDO
        }

        public void Close()
        {
            m_regions.Clear();
        }

        public string Name
        {
            get { return "ExportSerialisationModule"; }
        }

        #endregion


        #region IRegionSerialiser Members

        public void LoadPrimsFromXml(Scene scene, string fileName, bool newIDS, Vector3 loadOffset)
        {
            SceneXmlLoader.LoadPrimsFromXml(scene, fileName, newIDS, loadOffset);
        }

        public void SavePrimsToXml(Scene scene, string fileName)
        {
            SceneXmlLoader.SavePrimsToXml(scene, fileName);
        }

        public void LoadPrimsFromXml2(Scene scene, string fileName)
        {
            SceneXmlLoader.LoadPrimsFromXml2(scene, fileName);
        }

        public void LoadPrimsFromXml2(Scene scene, TextReader reader, bool startScripts)
        {
            SceneXmlLoader.LoadPrimsFromXml2(scene, reader, startScripts);
        }

        public void SavePrimsToXml2(Scene scene, string fileName)
        {
            SceneXmlLoader.SavePrimsToXml2(scene, fileName);
        }

        public void SavePrimsToXml2(Scene scene, TextWriter stream, Vector3 min, Vector3 max)
        {
            SceneXmlLoader.SavePrimsToXml2(scene, stream, min, max);
        }

        public void SaveNamedPrimsToXml2(Scene scene, string primName, string fileName)
        {
            SceneXmlLoader.SaveNamedPrimsToXml2(scene, primName, fileName);
        }

        public SceneObjectGroup DeserializeGroupFromXml2(string xmlString)
        {
            return SceneXmlLoader.DeserializeGroupFromXml2(xmlString);
        }

        public string SerializeGroupToXml2(SceneObjectGroup grp, Dictionary<string, object> options)
        {
            return SceneXmlLoader.SaveGroupToXml2(grp, options);
        }

        public void SavePrimListToXml2(EntityBase[] entityList, string fileName)
        {
            SceneXmlLoader.SavePrimListToXml2(entityList, fileName);
        }

        public void SavePrimListToXml2(EntityBase[] entityList, TextWriter stream, Vector3 min, Vector3 max)
        {
            SceneXmlLoader.SavePrimListToXml2(entityList, stream, min, max);
        }

        public List<string> SerialiseRegion(Scene scene, string saveDir)
        {
            List<string> results = new List<string>();

            if (!Directory.Exists(saveDir))
            {
                Directory.CreateDirectory(saveDir);
            }

            // AKIDO
            foreach (IFileSerialiser serialiser in m_serialisers)
            {
                results.Add(serialiser.WriteToFile(scene, saveDir));
            }
            // AKIDO

            TextWriter regionInfoWriter = new StreamWriter(Path.Combine(saveDir, "README.TXT"));
            regionInfoWriter.WriteLine("Region Name: " + scene.RegionInfo.RegionName);
            regionInfoWriter.WriteLine("Region ID: " + scene.RegionInfo.RegionID.ToString());
            regionInfoWriter.WriteLine("Backup Time: UTC " + DateTime.UtcNow.ToString());
            regionInfoWriter.WriteLine("Serialise Version: 0.1");
            regionInfoWriter.Close();

            TextWriter manifestWriter = new StreamWriter(Path.Combine(saveDir, "region.manifest"));
            foreach (string line in results)
            {
                manifestWriter.WriteLine(line);
            }
            manifestWriter.Close();

            return results;
        }

        #endregion


        private void InterfaceSaveRegion(Object[] args)
        {
            foreach (Scene region in m_regions)
            {
                if (region.RegionInfo.RegionName == (string) args[0])
                {
                    // List<string> results = SerialiseRegion(region, m_savedir + region.RegionInfo.RegionID.ToString() + "/");
                    SerialiseRegion(region, Path.Combine(m_savedir, region.RegionInfo.RegionID.ToString()));
                }
            }
        }

        private void InterfaceSaveAllRegions(Object[] args)
        {
            foreach (Scene region in m_regions)
            {
                // List<string> results = SerialiseRegion(region, m_savedir + region.RegionInfo.RegionID.ToString() + "/");
                SerialiseRegion(region, Path.Combine(m_savedir, region.RegionInfo.RegionID.ToString()));
            }
        }
        
    }
}
