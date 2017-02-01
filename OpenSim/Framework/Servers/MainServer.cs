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
using System.Net;
using System.Text;
using log4net;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using OpenSim.Framework.Servers.HttpServer;

namespace OpenSim.Framework.Servers
{
    public class MainServer
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static BaseHttpServer instance = null;
        private static Dictionary<uint, BaseHttpServer> m_Servers = new Dictionary<uint, BaseHttpServer>();

        /// <summary>
        /// Set the main HTTP server instance.
        /// </summary>
        /// <remarks>
        /// This will be used to register all handlers that listen to the default port.
        /// </remarks>
        /// <exception cref='Exception'>
        /// Thrown if the HTTP server has not already been registered via AddHttpServer()
        /// </exception>
        public static BaseHttpServer Instance
        {
            get { return instance; }

            set
            {
				if (m_log.IsDebugEnabled) {
					m_log.DebugFormat ("{0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);
				}

                lock (m_Servers)
                    if (!m_Servers.ContainsValue(value))
                        throw new Exception("HTTP server must already have been registered to be set as the main instance");

                instance = value;
            }
        }

        /// <summary>
        /// Get all the registered servers.
        /// </summary>
        /// <remarks>
        /// Returns a copy of the dictionary so this can be iterated through without locking.
        /// </remarks>
        /// <value></value>
        public static Dictionary<uint, BaseHttpServer> Servers
        {
            get { return new Dictionary<uint, BaseHttpServer>(m_Servers); }
        }

        public static void RegisterHttpConsoleCommands(ICommandConsole console)
        {
            console.Commands.AddCommand(
                "Comms", false, "show http-handlers",
                "show http-handlers",
                "Show all registered http handlers", HandleShowHttpHandlersCommand);

        }


        private static void HandleShowHttpHandlersCommand(string module, string[] args)
        {
			if (m_log.IsDebugEnabled) {
				m_log.DebugFormat ("{0} called", System.Reflection.MethodBase.GetCurrentMethod ().Name);
			}


            if (args.Length != 2)
            {
                MainConsole.Instance.Output("Usage: show http-handlers");
                return;
            }

            StringBuilder handlers = new StringBuilder();

            lock (m_Servers)
            {
                foreach (BaseHttpServer httpServer in m_Servers.Values)
                {
                    handlers.AppendFormat(
                        "Registered HTTP Handlers for server at {0}:{1}\n", httpServer.ListenIPAddress, httpServer.Port);

                    handlers.AppendFormat("* XMLRPC:\n");
                    foreach (String s in httpServer.GetXmlRpcHandlerKeys())
                        handlers.AppendFormat("\t{0}\n", s);

                    handlers.AppendFormat("* HTTP:\n");
                    foreach (String s in httpServer.GetHTTPHandlerKeys())
                        handlers.AppendFormat("\t{0}\n", s);

                    handlers.AppendFormat("* HTTP (poll):\n");
                    foreach (String s in httpServer.GetPollServiceHandlerKeys())
                        handlers.AppendFormat("\t{0}\n", s);

                    handlers.AppendFormat("* JSONRPC:\n");
                    foreach (String s in httpServer.GetJsonRpcHandlerKeys())
                        handlers.AppendFormat("\t{0}\n", s);

//                    handlers.AppendFormat("* Agent:\n");
//                    foreach (String s in httpServer.GetAgentHandlerKeys())
//                        handlers.AppendFormat("\t{0}\n", s);

                    handlers.AppendFormat("* LLSD:\n");
                    foreach (String s in httpServer.GetLLSDHandlerKeys())
                        handlers.AppendFormat("\t{0}\n", s);

                    handlers.AppendFormat("* StreamHandlers ({0}):\n", httpServer.GetStreamHandlerKeys().Count);
                    foreach (String s in httpServer.GetStreamHandlerKeys())
                        handlers.AppendFormat("\t{0}\n", s);

                    handlers.Append("\n");
                }
            }

            MainConsole.Instance.Output(handlers.ToString());
        }

        /// <summary>
        /// Register an already started HTTP server to the collection of known servers.
        /// </summary>
        /// <param name='server'></param>
        public static void AddHttpServer(BaseHttpServer server)
        {
			if (m_log.IsDebugEnabled) {
				m_log.DebugFormat ("{0} called", System.Reflection.MethodBase.GetCurrentMethod ().Name);
			}


            lock (m_Servers)
            {
                if (m_Servers.ContainsKey(server.Port))
                    throw new Exception(string.Format("HTTP server for port {0} already exists.", server.Port));

                m_Servers.Add(server.Port, server);
            }
        }

        /// <summary>
        /// Removes the http server listening on the given port.
        /// </summary>
        /// <remarks>
        /// It is the responsibility of the caller to do clean up.
        /// </remarks>
        /// <param name='port'></param>
        /// <returns></returns>
        public static bool RemoveHttpServer(uint port)
        {
			if (m_log.IsDebugEnabled) {
				m_log.DebugFormat ("{0} called", System.Reflection.MethodBase.GetCurrentMethod ().Name);
			}


            lock (m_Servers)
            {
                if (instance != null && instance.Port == port)
                    instance = null;

                return m_Servers.Remove(port);
            }
        }

        /// <summary>
        /// Does this collection of servers contain one with the given port?
        /// </summary>
        /// <remarks>
        /// Unlike GetHttpServer, this will not instantiate a server if one does not exist on that port.
        /// </remarks>
        /// <param name='port'></param>
        /// <returns>true if a server with the given port is registered, false otherwise.</returns>
        public static bool ContainsHttpServer(uint port)
        {
            lock (m_Servers)
                return m_Servers.ContainsKey(port);
        }

        /// <summary>
        /// Get the default http server or an http server for a specific port.
        /// </summary>
        /// <remarks>
        /// If the requested HTTP server doesn't already exist then a new one is instantiated and started.
        /// </remarks>
        /// <returns></returns>
        /// <param name='port'>If 0 then the default HTTP server is returned.</param>
        public static IHttpServer GetHttpServer(uint port)
        {
            return GetHttpServer(port, null);
        }

        /// <summary>
        /// Get the default http server, an http server for a specific port
        /// and/or an http server bound to a specific address
        /// </summary>
        /// <remarks>
        /// If the requested HTTP server doesn't already exist then a new one is instantiated and started.
        /// </remarks>
        /// <returns></returns>
        /// <param name='port'>If 0 then the default HTTP server is returned.</param>
        /// <param name='ipaddr'>A specific IP address to bind to.  If null then the default IP address is used.</param>
        public static IHttpServer GetHttpServer(uint port, IPAddress ipaddr)
        {
			if (m_log.IsDebugEnabled) {
				m_log.DebugFormat ("{0} called", System.Reflection.MethodBase.GetCurrentMethod ().Name);
			}


            if (port == 0)
                return Instance;

            if (instance != null && port == Instance.Port)
                return Instance;

            lock (m_Servers)
            {
                if (m_Servers.ContainsKey(port))
                    return m_Servers[port];

                m_Servers[port] = new BaseHttpServer(port);

                if (ipaddr != null)
                    m_Servers[port].ListenIPAddress = ipaddr;

                m_Servers[port].Start();

                return m_Servers[port];
            }
        }
    }
}