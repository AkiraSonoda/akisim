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
using System.Threading;
using OpenMetaverse;
using OpenSim.Framework.Monitoring;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.ScriptEngine.Interfaces;
using OpenSim.Region.ScriptEngine.Shared.Api.Plugins;
using ScriptTimer = OpenSim.Region.ScriptEngine.Shared.Api.Plugins.ScriptTimer;
using System.Reflection;
using log4net;
using System.Collections.Concurrent;
using ThreadedClasses;

namespace OpenSim.Region.ScriptEngine.Shared.Api
{
    /// <summary>
    /// Handles LSL commands that takes long time and returns an event, for example timers, HTTP requests, etc.
    /// </summary>
    public class AsyncCommandManager
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static Thread cmdHandlerThread;
        private static int cmdHandlerThreadCycleSleepms;
        private static int numInstances;

        /// <summary>
        /// Lock for reading/writing static components of AsyncCommandManager.
        /// </summary>
        /// <remarks>
        /// This lock exists so that multiple threads from different engines and/or different copies of the same engine
        /// are prevented from running non-thread safe code (e.g. read/write of lists) concurrently.
        /// </remarks>

        private static RwLockedList<IScriptEngine> m_ScriptEngines = // AKIDO
            new RwLockedList<IScriptEngine>();

        public IScriptEngine m_ScriptEngine;

        private static ConcurrentDictionary<IScriptEngine, Dataserver> m_Dataserver = // AKIDO
            new ConcurrentDictionary<IScriptEngine, Dataserver>();

        private static ConcurrentDictionary<IScriptEngine, ScriptTimer> m_ScriptTimer = // AKIDO
            new ConcurrentDictionary<IScriptEngine, ScriptTimer>();

        private static ConcurrentDictionary<IScriptEngine, Listener> m_Listener = // AKIDO
            new ConcurrentDictionary<IScriptEngine, Listener>();

        private static ConcurrentDictionary<IScriptEngine, HttpRequest> m_HttpRequest = // AKIDO
            new ConcurrentDictionary<IScriptEngine, HttpRequest>();

        private static ConcurrentDictionary<IScriptEngine, SensorRepeat> m_SensorRepeat = // AKIDO
            new ConcurrentDictionary<IScriptEngine, SensorRepeat>();

        private static ConcurrentDictionary<IScriptEngine, XmlRequest> m_XmlRequest = // AKIDO
            new ConcurrentDictionary<IScriptEngine, XmlRequest>();

        public Dataserver DataserverPlugin
        {
            get { return m_Dataserver[m_ScriptEngine]; } // AKIDO
        }

        public ScriptTimer TimerPlugin
        {
            get { return m_ScriptTimer[m_ScriptEngine]; } // AKIDO
        }

        public HttpRequest HttpRequestPlugin
        {
            get { return m_HttpRequest[m_ScriptEngine]; } // AKIDO
        }

        public Listener ListenerPlugin
        {
            get { return m_Listener[m_ScriptEngine]; } // AKIDO
        }

        public SensorRepeat SensorRepeatPlugin
        {
            get { return m_SensorRepeat[m_ScriptEngine]; } // AKIDO
        }

        public XmlRequest XmlRequestPlugin
        {
            get { return m_XmlRequest[m_ScriptEngine]; } // AKIDO
        }

        public IScriptEngine[] ScriptEngines
        {
            get { return m_ScriptEngines.ToArray(); } // AKIDO
        }

        public AsyncCommandManager(IScriptEngine _ScriptEngine)
        {
            m_ScriptEngine = _ScriptEngine;

            // If there is more than one scene in the simulator or multiple script engines are used on the same region
            // then more than one thread could arrive at this block of code simultaneously.  However, it cannot be
            // executed concurrently both because concurrent list operations are not thread-safe and because of other
            // race conditions such as the later check of cmdHandlerThread == null.
            if (m_ScriptEngines.Count == 0) // AKIDO
                ReadConfig();

            if (!m_ScriptEngines.Contains(m_ScriptEngine))
                m_ScriptEngines.Add(m_ScriptEngine);

            // Create instances of all plugins
            if (!m_Dataserver.ContainsKey(m_ScriptEngine)) // AKIDO
                m_Dataserver[m_ScriptEngine] = new Dataserver(this);
            if (!m_ScriptTimer.ContainsKey(m_ScriptEngine))
                m_ScriptTimer[m_ScriptEngine] = new ScriptTimer(this);
            if (!m_HttpRequest.ContainsKey(m_ScriptEngine))
                m_HttpRequest[m_ScriptEngine] = new HttpRequest(this);
            if (!m_Listener.ContainsKey(m_ScriptEngine))
                m_Listener[m_ScriptEngine] = new Listener(this);
            if (!m_SensorRepeat.ContainsKey(m_ScriptEngine))
                m_SensorRepeat[m_ScriptEngine] = new SensorRepeat(this);
            if (!m_XmlRequest.ContainsKey(m_ScriptEngine))
                m_XmlRequest[m_ScriptEngine] = new XmlRequest(this);

            numInstances++;
            if (cmdHandlerThread == null)
            {
                cmdHandlerThread = WorkManager.StartThread(
                    CmdHandlerThreadLoop, "AsyncLSLCmdHandlerThread");
            }
        }

        private void ReadConfig()
        {
            cmdHandlerThreadCycleSleepms = m_ScriptEngine.Config.GetInt("AsyncLLCommandLoopms", 100);
            cmdHandlerThreadCycleSleepms = Utils.Clamp(cmdHandlerThreadCycleSleepms, 25, 250);
        }

/*
        ~AsyncCommandManager()
        {
            // Shut down thread

            try
            {
                lock (staticLock)
                {
                    numInstances--;
                    if(numInstances > 0)
                        return;
                    if (cmdHandlerThread != null)
                    {
                        if (cmdHandlerThread.IsAlive == true)
                        {
                            cmdHandlerThread.Abort();
                            //cmdHandlerThread.Join();
                            cmdHandlerThread = null;
                        }
                    }
                }
            }
            catch
            {
            }
        }
*/
        /// <summary>
        /// Main loop for the manager thread
        /// </summary>
        private static void CmdHandlerThreadLoop()
        {
            bool running = true;
            while (running)
            {
                try
                {
                    Thread.Sleep(cmdHandlerThreadCycleSleepms);
                    Watchdog.UpdateThread();
                    DoOneCmdHandlerPass();
                    Watchdog.UpdateThread();
                }
                catch (System.Threading.ThreadAbortException)
                {
                    //Thread.ResetAbort();
                    running = false;
                }
                catch (Exception e)
                {
                    m_log.ErrorFormat("CmdHandlerThreadLoop Exception: {0}", e);
                }
            }
        }

        private static void DoOneCmdHandlerPass()
        {
            // Check XMLRPCRequests
            try
            {
                m_XmlRequest[m_ScriptEngines[0]].CheckXMLRPCRequests();
            }
            catch (Exception ex)
            {
                m_log.DebugFormat("XmlRequest CheckXMLRPCRequests - exception: {0}", ex.Message );
            }


            foreach (IScriptEngine s in m_ScriptEngines)
            {
                // Check HttpRequests
                try
                {
                    m_HttpRequest[s].CheckHttpRequests();
                }
                catch (Exception ex) // AKIDO
                {
                    m_log.DebugFormat("HttpRequest CheckHttpRequests - exception: {0}", ex.Message );
                }
                // Check ScriptTimers
                try
                {
                    m_ScriptTimer[s].CheckTimerEvents();
                }
                catch (Exception ex) // AKIDO
                {
                    m_log.DebugFormat("ScriptTimer CheckTimerEvents - exception: {0}", ex.Message );
                }

                // Check Sensors
                try
                {
                    m_SensorRepeat[s].CheckSenseRepeaterEvents();
                }
                catch (Exception ex) // AKIDO
                {
                    m_log.DebugFormat("SensorRepeat CheckSenseRepeaterEvents - exception: {0}", ex.Message );
                }

                // Check dataserver
                try
                {
                    m_Dataserver[s].ExpireRequests();
                }
                catch (Exception ex) // AKIDO
                {
                    m_log.DebugFormat("Dataserver ExpireRequests - exception: {0}", ex.Message );
                }
            }
        }

        /// <summary>
        /// Remove a specific script (and all its pending commands)
        /// </summary>
        /// <param name="localID"></param>
        /// <param name="itemID"></param>
        public static void RemoveScript(IScriptEngine engine, uint localID, UUID itemID)
        {
            // Remove a specific script
            m_log.DebugFormat("RemoveScript - engine: {0} itemid: {1}", engine, itemID);

            // Remove dataserver events
            m_Dataserver[engine].RemoveEvents(localID, itemID); // AKIDO

            // Remove from: ScriptTimers
            m_ScriptTimer[engine].UnSetTimerEvents(localID, itemID);

            if (engine.World != null)
            {
                // Remove from: HttpRequest
                IHttpRequestModule iHttpReq = engine.World.RequestModuleInterface<IHttpRequestModule>();
                if (iHttpReq != null)
                    iHttpReq.StopHttpRequest(localID, itemID);

                IWorldComm comms = engine.World.RequestModuleInterface<IWorldComm>();
                if (comms != null)
                    comms.DeleteListener(itemID);

                IXMLRPC xmlrpc = engine.World.RequestModuleInterface<IXMLRPC>();
                if (xmlrpc != null)
                {
                    xmlrpc.DeleteChannels(itemID);
                    xmlrpc.CancelSRDRequests(itemID);
                }
            }

                // Remove Sensors
                m_SensorRepeat[engine].UnSetSenseRepeaterEvents(localID, itemID);
        }

        public static void StateChange(IScriptEngine engine, uint localID, UUID itemID)
        {
            // Remove a specific script

            // Remove dataserver events
            m_Dataserver[engine].RemoveEvents(localID, itemID);

            IWorldComm comms = engine.World.RequestModuleInterface<IWorldComm>();
            if (comms != null)
                comms.DeleteListener(itemID);

            IXMLRPC xmlrpc = engine.World.RequestModuleInterface<IXMLRPC>();
            if (xmlrpc != null)
            {
                xmlrpc.DeleteChannels(itemID);
                xmlrpc.CancelSRDRequests(itemID);
            }

            // Remove Sensors
            m_SensorRepeat[engine].UnSetSenseRepeaterEvents(localID, itemID);
        }

        /// <summary>
        /// Get the sensor repeat plugin for this script engine.
        /// </summary>
        /// <param name="engine"></param>
        /// <returns></returns>
        public static SensorRepeat GetSensorRepeatPlugin(IScriptEngine engine)
        {
            if (m_SensorRepeat.ContainsKey(engine)) // AKIDO
                return m_SensorRepeat[engine];
            return null;
        }

        /// <summary>
        /// Get the dataserver plugin for this script engine.
        /// </summary>
        /// <param name="engine"></param>
        /// <returns></returns>
        public static Dataserver GetDataserverPlugin(IScriptEngine engine)
        {
            if (m_Dataserver.ContainsKey(engine)) // AKIDO
                return m_Dataserver[engine];
            return null;
        }

        /// <summary>
        /// Get the ScriptTimer plugin for this script engine.
        /// </summary>
        /// <param name="engine"></param>
        /// <returns></returns>
        public static ScriptTimer GetTimerPlugin(IScriptEngine engine)
        {
            if (m_ScriptTimer.ContainsKey(engine)) // AKIDO
                return m_ScriptTimer[engine];
            return null;
        }

        /// <summary>
        /// Get the listener plugin for this script engine.
        /// </summary>
        /// <param name="engine"></param>
        /// <returns></returns>
        public static Listener GetListenerPlugin(IScriptEngine engine)
        {
            if (m_Listener.ContainsKey(engine)) // AKIDO
                return m_Listener[engine];
            return null;
        }

        public static Object[] GetSerializationData(IScriptEngine engine, UUID itemID)
        {
            List<Object> data = new List<Object>();

            Object[] listeners = m_Listener[engine].GetSerializationData(itemID);
            if (listeners.Length > 0)
            {
                data.Add("listener");
                data.Add(listeners.Length);
                data.AddRange(listeners);
            }

            Object[] ScriptTimers = m_ScriptTimer[engine].GetSerializationData(itemID);
            if (ScriptTimers.Length > 0)
            {
                data.Add("timer");
                data.Add(ScriptTimers.Length);
                data.AddRange(ScriptTimers);
            }

            Object[] sensors = m_SensorRepeat[engine].GetSerializationData(itemID);
            if (sensors.Length > 0)
            {
                data.Add("sensor");
                data.Add(sensors.Length);
                data.AddRange(sensors);
            }

            return data.ToArray();
        }

        public static void CreateFromData(IScriptEngine engine, uint localID,
            UUID itemID, UUID hostID, Object[] data)
        {
            int idx = 0;
            int len;

            while (idx < data.Length)
            {
                string type = data[idx].ToString();
                len = (int)data[idx + 1];
                idx += 2;

                if (len > 0)
                {
                    Object[] item = new Object[len];
                    Array.Copy(data, idx, item, 0, len);

                    idx += len;

                    switch (type)
                    {
                        case "listener":
                            m_Listener[engine].CreateFromData(itemID, hostID, item);
                            break;
                        case "timer":
                            m_ScriptTimer[engine].CreateFromData(localID, itemID, hostID, item);
                            break;
                        case "sensor":
                            m_SensorRepeat[engine].CreateFromData(localID, itemID, hostID, item);
                            break;
                    }
                }
            }
        }
    }
}