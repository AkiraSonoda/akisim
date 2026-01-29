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
using log4net;
using Nini.Config;
using OpenSim;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.CoreModules;
using OpenSim.Region.OptionalModules;

namespace OpenSim.ApplicationPlugins.RegionModulesController
{
    public class RegionModulesControllerPlugin : IRegionModulesController,
            IApplicationPlugin
    {
        // Logger
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Controls whether we load modules from Mono.Addins.
        /// </summary>
        /// <remarks>For debug purposes.  Defaults to true.</remarks>
        public bool LoadModulesFromAddins { get; set; }

        // Config access
        private OpenSimBase m_openSim;

        // Internal lists to collect information about modules present
        // Note: Mono.Addins support removed - these lists are no longer used
        // private List<TypeExtensionNode> m_nonSharedModules = new List<TypeExtensionNode>();
        // private List<TypeExtensionNode> m_sharedModules = new List<TypeExtensionNode>();

        // List of shared module instances, for adding to Scenes
        private List<ISharedRegionModule> m_sharedInstances = new List<ISharedRegionModule>();
        
        // For factory pattern: store types instead of TypeExtensionNodes
        private List<Type> m_factoryNonSharedModuleTypes = new List<Type>();

        public RegionModulesControllerPlugin()
        {
            LoadModulesFromAddins = false; // Use factory pattern instead of Mono.Addins
        }

#region IApplicationPlugin implementation

        public void Initialise (OpenSimBase openSim)
        {
            m_openSim = openSim;
            m_openSim.ApplicationRegistry.RegisterInterface<IRegionModulesController>(this);
            m_log.Debug("Initializing...");

            // Mono.Addins support removed - always use factory pattern
            LoadCoreModulesFromFactory();
        }

        public void PostInitialise ()
        {
            m_log.Debug("PostInitializing...");

            // Immediately run PostInitialise on shared modules
            foreach (ISharedRegionModule module in m_sharedInstances)
            {
                module.PostInitialise();
            }
        }

#endregion

#region IPlugin implementation

        // NOTE: AddNode method removed - Mono.Addins support removed

        // We don't do that here
        //
        public void Initialise ()
        {
            throw new System.NotImplementedException();
        }

#endregion

#region IDisposable implementation

        // Cleanup
        //
        public void Dispose ()
        {
            // We expect that all regions have been removed already
            while (m_sharedInstances.Count > 0)
            {
                m_sharedInstances[0].Close();
                m_sharedInstances.RemoveAt(0);
            }
        }

#endregion

        public string Version
        {
            get
            {
                return "1.0.0";
            }
        }

        public string Name
        {
            get
            {
                return "RegionModulesController";
            }
        }

#region Region Module interfacesController implementation

        // NOTE: CheckModuleEnabled method removed - Mono.Addins support removed

        // The root of all evil.
        // This is where we handle adding the modules to scenes when they
        // load. This means that here we deal with replaceable interfaces,
        // nonshared modules, etc.
        //
        public void AddRegionToModules (Scene scene)
        {
            Dictionary<Type, ISharedRegionModule> deferredSharedModules = new Dictionary<Type, ISharedRegionModule>();
            Dictionary<Type, INonSharedRegionModule> deferredNonSharedModules = new Dictionary<Type, INonSharedRegionModule>();

            // We need this to see if a module has already been loaded and
            // has defined a replaceable interface. It's a generic call,
            // so this can't be used directly. It will be used later
            Type s = scene.GetType();
            MethodInfo mi = s.GetMethod("RequestModuleInterface");

            // This will hold the shared modules we actually load
            List<ISharedRegionModule> sharedlist = new List<ISharedRegionModule>();

            // Iterate over the shared modules that have been loaded
            // Add them to the new Scene
            foreach (ISharedRegionModule module in m_sharedInstances)
            {
                // Here is where we check if a replaceable interface
                // is defined. If it is, the module is checked against
                // the interfaces already defined. If the interface is
                // defined, we simply skip the module. Else, if the module
                // defines a replaceable interface, we add it to the deferred
                // list.
                Type replaceableInterface = module.ReplaceableInterface;
                if (replaceableInterface != null)
                {
                    MethodInfo mii = mi.MakeGenericMethod(replaceableInterface);

                    if (mii.Invoke(scene, new object[0]) != null)
                    {
                        if(m_log.IsDebugEnabled) m_log.DebugFormat("Not loading {0} because another module has registered {1}",
                            module.Name, replaceableInterface.ToString());
                        continue;
                    }

                    deferredSharedModules[replaceableInterface] = module;
                    if(m_log.IsDebugEnabled) m_log.DebugFormat("Deferred load of {0}", module.Name);
                    continue;
                }

                if(m_log.IsDebugEnabled) m_log.DebugFormat("Adding scene {0} to shared module {1}",
                    scene.RegionInfo.RegionName, module.Name);

                module.AddRegion(scene);
                scene.AddRegionModule(module.Name, module);

                sharedlist.Add(module);
            }

            IConfig modulesConfig = m_openSim.ConfigSource.Source.Configs["Modules"];

            // Load non-shared modules from factory pattern
            List<INonSharedRegionModule> list = new List<INonSharedRegionModule>();
            foreach (Type moduleType in m_factoryNonSharedModuleTypes)
                {
                    try
                    {
                        INonSharedRegionModule module = (INonSharedRegionModule)Activator.CreateInstance(moduleType);
                        
                        // Check for replaceable interfaces
                        Type replaceableInterface = module.ReplaceableInterface;
                        if (replaceableInterface != null)
                        {
                            MethodInfo mii = mi.MakeGenericMethod(replaceableInterface);
                            
                            if (mii.Invoke(scene, new object[0]) != null)
                            {
                                if(m_log.IsDebugEnabled) m_log.DebugFormat("Not loading {0} because another module has registered {1}", 
                                    module.Name, replaceableInterface.ToString());
                                continue;
                            }
                            
                            deferredNonSharedModules[replaceableInterface] = module;
                            if(m_log.IsDebugEnabled) m_log.DebugFormat("Deferred load of {0}", module.Name);
                            continue;
                        }
                        
                        if(m_log.IsDebugEnabled) m_log.DebugFormat("Adding scene {0} to factory non-shared module {1}",
                            scene.RegionInfo.RegionName, module.Name);
                        
                        // Initialise the module
                        module.Initialise(m_openSim.ConfigSource.Source);
                        
                        list.Add(module);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat("Failed to instantiate factory module {0}: {1}", moduleType.Name, e.Message);
                        if(m_log.IsDebugEnabled) m_log.Debug("Exception details:", e);
                    }
                }

            // Now add the modules that we found to the scene. If a module
            // wishes to override a replaceable interface, it needs to
            // register it in Initialise, so that the deferred module
            // won't load.
            foreach (INonSharedRegionModule module in list)
            {
                module.AddRegion(scene);
                scene.AddRegionModule(module.Name, module);
            }

            // Now all modules without a replaceable base interface are loaded
            // Replaceable modules have either been skipped, or omitted.
            // Now scan the deferred modules here
            foreach (ISharedRegionModule module in deferredSharedModules.Values)
            {
                // Determine if the interface has been replaced
                Type replaceableInterface = module.ReplaceableInterface;
                MethodInfo mii = mi.MakeGenericMethod(replaceableInterface);

                if (mii.Invoke(scene, new object[0]) != null)
                {
                    if(m_log.IsDebugEnabled) m_log.DebugFormat("Not loading {0} because another module has registered {1}", 
                        module.Name, replaceableInterface.ToString());
                    continue;
                }

                if(m_log.IsDebugEnabled) m_log.DebugFormat("Adding scene {0} to shared module {1} (deferred)",
                    scene.RegionInfo.RegionName, module.Name);

                // Not replaced, load the module
                module.AddRegion(scene);
                scene.AddRegionModule(module.Name, module);

                sharedlist.Add(module);
            }

            // Same thing for nonshared modules, load them unless overridden
            List<INonSharedRegionModule> deferredlist = new List<INonSharedRegionModule>();

            foreach (INonSharedRegionModule module in deferredNonSharedModules.Values)
            {
                // Check interface override
                Type replaceableInterface = module.ReplaceableInterface;
                if (replaceableInterface != null)
                {
                    MethodInfo mii = mi.MakeGenericMethod(replaceableInterface);

                    if (mii.Invoke(scene, new object[0]) != null)
                    {
                        if(m_log.IsDebugEnabled) m_log.DebugFormat("Not loading {0} because another module has registered {1}", 
                            module.Name, replaceableInterface.ToString());
                        continue;
                    }
                }

                if(m_log.IsDebugEnabled) m_log.DebugFormat("Adding scene {0} to non-shared module {1} (deferred)",
                    scene.RegionInfo.RegionName, module.Name);

                module.Initialise(m_openSim.ConfigSource.Source);

                list.Add(module);
                deferredlist.Add(module);
            }

            // Finally, load valid deferred modules
            foreach (INonSharedRegionModule module in deferredlist)
            {
                module.AddRegion(scene);
                scene.AddRegionModule(module.Name, module);
            }

            // This is needed for all module types. Modules will register
            // Interfaces with scene in AddScene, and will also need a means
            // to access interfaces registered by other modules. Without
            // this extra method, a module attempting to use another modules's
            // interface would be successful only depending on load order,
            // which can't be depended upon, or modules would need to resort
            // to ugly kludges to attempt to request interfaces when needed
            // and unneccessary caching logic repeated in all modules.
            // The extra function stub is just that much cleaner
            //
            foreach (ISharedRegionModule module in sharedlist)
            {
                module.RegionLoaded(scene);
            }

            foreach (INonSharedRegionModule module in list)
            {
                module.RegionLoaded(scene);
            }

            scene.AllModulesLoaded();
        }

        public void RemoveRegionFromModules (Scene scene)
        {
            foreach (IRegionModuleBase module in scene.RegionModules.Values)
            {
                if(m_log.IsDebugEnabled) m_log.DebugFormat("Removing scene {0} from module {1}",
                    scene.RegionInfo.RegionName, module.Name);
                
                module.RemoveRegion(scene);
                if (module is INonSharedRegionModule)
                {
                    // as we were the only user, this instance has to die
                    module.Close();
                }
            }
            scene.RegionModules.Clear();
        }

        /// <summary>
        /// Load core modules using factory pattern instead of Mono.Addins
        /// </summary>
        private void LoadCoreModulesFromFactory()
        {
            m_log.Info("Loading core modules from factory (bypassing Mono.Addins)");
            
            try
            {
                int nonSharedCount = 0;
                
                // Store non-shared module types for instantiation per region
                foreach (var module in CoreModuleFactory.CreateNonSharedModules(m_openSim.ConfigSource.Source))
                {
                    Type moduleType = module.GetType();
                    m_factoryNonSharedModuleTypes.Add(moduleType);
                    m_log.DebugFormat("Registered non-shared module type: {0}", moduleType.Name);
                    nonSharedCount++;
                    
                    // Dispose the instance since we only needed it for type registration
                    if (module is IDisposable disposable)
                        disposable.Dispose();
                }
                
                // Load shared modules and initialize them
                int sharedCoreCount = 0;
                foreach (var module in CoreModuleFactory.CreateSharedModules(m_openSim.ConfigSource.Source))
                {
                    m_log.DebugFormat("Initializing shared core module: {0}", module.GetType().Name);
                    module.Initialise(m_openSim.ConfigSource.Source);
                    m_sharedInstances.Add(module);
                    sharedCoreCount++;
                }
                
                // Load optional shared modules and initialize them
                int sharedOptionalCount = 0;
                foreach (var module in OptionalModulesFactory.CreateOptionalSharedModules(m_openSim.ConfigSource.Source))
                {
                    m_log.DebugFormat("Initializing shared optional module: {0}", module.GetType().Name);
                    module.Initialise(m_openSim.ConfigSource.Source);
                    m_sharedInstances.Add(module);
                    sharedOptionalCount++;
                }
                
                // Load optional non-shared module types
                int optionalNonSharedCount = 0;
                foreach (var module in OptionalModulesFactory.CreateOptionalRegionModules(m_openSim.ConfigSource.Source))
                {
                    Type moduleType = module.GetType();
                    m_factoryNonSharedModuleTypes.Add(moduleType);
                    m_log.DebugFormat("Registered non-shared optional module type: {0}", moduleType.Name);
                    optionalNonSharedCount++;
                    
                    // Dispose the instance since we only needed it for type registration
                    if (module is IDisposable disposable)
                        disposable.Dispose();
                }
                
                m_log.InfoFormat("Loaded {0} core non-shared + {1} optional non-shared module types, {2} core shared + {3} optional shared module instances from factory", 
                    nonSharedCount, optionalNonSharedCount, sharedCoreCount, sharedOptionalCount);
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("Failed to load core modules from factory: {0}", e.Message);
                m_log.Debug("Exception details:", e);
            }
        }

#endregion

    }
}
