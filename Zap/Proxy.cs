/**********************************************************************************************************
 * Copyright (c) 2012 Neio Zhou
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
 * associated documentation files (the "Software"), to deal in the Software without restriction, 
 * including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or substantial
 * portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT 
 * LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 ***********************************************************************************************************/
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Reflection;
using System.Threading;

namespace Zap
{
    public sealed class Proxy
    {
        static Proxy()
        { 
            //load tunnelConfig
            TunnelConfig = new TunnelConfig();

            //ReloadTunnels();

            
            Processors = new Dictionary<String, Listener>();
            Tunnels = new Dictionary<string, ITunnel>();
        }

        private static Dictionary<String, Listener> Processors;

        private static TunnelConfig TunnelConfig;


        private static TunnelListener Listener;

        private static object TunnelLock = new object();
        private static Dictionary<String, ITunnel> Tunnels;

        public static void Register(Listener Obj)
        {
            lock (typeof(Proxy))
            {
                Processors.Add(Obj.GetType().Name, Obj);
            }
        }

        public static TunnelListener SetListener(int Port) {

            Listener = new TunnelListener(Port);
            Listener.OnConnected += new EventHandler<TunnelEventArgs>(Listener_OnConnected);
            Listener.TurnOn();
            return Listener;
        }

        public static void AddTunnel(TunnelConnector connector)
        {
            lock (TunnelLock)
            {
                connector.OnReceived += new EventHandler<ReceivedEventArgs>(OnReceived);
                connector.OnDisconnected += new EventHandler<TunnelBrokenEventArgs>(connector_OnDisconnected);
                Tunnels.Add(connector.Name, connector);
                connector.Connect();
            }
        }

        public static ITunnel GetTunnel(String Name)
        {
            return Tunnels[Name];
        }

        static void connector_OnDisconnected(object sender, TunnelBrokenEventArgs e)
        {
            //tunnel disconnect
            //remote all call backs
            lock (TunnelLock)
            {
                var tunnelName = ((ITunnel)sender).Name;
                Tunnels.Remove(tunnelName);
            }
        }

       
        static void Listener_OnConnected(object sender, TunnelEventArgs e)
        {
            var tunnel = e.Tunnel;
            lock (TunnelLock)
            {
                Tunnels.Add(tunnel.Name, tunnel);
                //CallbackIndex.Add(tunnel.Name, Hashtable.Synchronized(new Hashtable()));
                tunnel.OnDisconnected += (s, ee) =>
                {
                    var name = (s as ITunnel).Name;
                    lock (TunnelLock)
                    {
                        Tunnels.Remove(name);
                        //CallbackIndex.Remove(name);
                    }
                    //clear relative event handlers
                    foreach (var item in Processors)
                    {
                        var listener = item.Value;
                        listener.RemoveTunnelListener(name);
                    }

                };

                tunnel.OnReceived += new EventHandler<ReceivedEventArgs>(OnReceived);
            }
        }

        private static void OnReceived(object sender, ReceivedEventArgs e)
        {
            //Process Received Messages
            var message = e.Message;
            if (message is SendMessage)
            {
                ProcessReceivedMessage(message, e.Tunnel);
            }
            else if (message is EventRegistrationMessage)
            {
                ProcessEventRegistrationMessage(message as EventRegistrationMessage, e.Tunnel);
            }
            //Console.WriteLine("Thread ID: {0}", Thread.CurrentThread.ManagedThreadId);
           
        }

       

        private static void ProcessEventRegistrationMessage(EventRegistrationMessage message, ITunnel tunnel)
        {
            if (!Processors.ContainsKey(message.ObjectName))
            {
                //send exception
                PushMessage(tunnel.Name, new ExceptionMessage(message.Token) { Message = "Remote Object Not Found" });
            }

            //get object info
            var processor = Processors[message.ObjectName];

            EventInfo e = processor.GetType().GetEvent(message.EventName);
            if (e == null)
            {
                //send exception
                PushMessage(tunnel.Name, new ExceptionMessage(message.Token) { Message = "Remote Event Not Found" });
                return;
            }

            if (message.IsRegister)
            {
                //register
                processor.AddListener(tunnel.Name, message);
            }
            else { 
                //deregister
                processor.RemoveListener(message);

            }
            PushMessage(tunnel.Name, new CallbackMessage(message.Token) { ReturnType = typeof(void), ReturnValue = "void" });
        }



        private static void ProcessReceivedMessage(Message message, ITunnel tunnel)
        {
            var msg = message as SendMessage;
            if (!Processors.ContainsKey(msg.ObjectName))
            {
                //send exception
                PushMessage(tunnel.Name, new ExceptionMessage(msg.Token) { Message = "Remote Object Not Found" });
            }

            var processor = Processors[msg.ObjectName];
            var method = processor.GetType().GetMethod(msg.Method);
            object result = null;
            if (msg.Parameters.Count == 0)
            {
                try
                {
                    result = method.Invoke(processor, null); 
                }
                catch (Exception e)
                {
                    //invoke occurs exception
                    PushMessage(tunnel.Name, new ExceptionMessage(msg.Token) { Message = e.InnerException.Message });

                    //do not send return message
                    return;
                }
            }
            else
            {
                //create parameter objects
                var paramss = method.GetParameters();
                var paramObjects = new List<Object>();
                foreach (var param in paramss)
                {
                    var obj = TypeHelper.GetValue(param.ParameterType, msg.Parameters[param.Name]);
                    paramObjects.Add(obj);
                }
                try
                {
                    result = method.Invoke(processor, paramObjects.ToArray());
                }
                catch (Exception e)
                {
                    //invoke occurs exception
                    PushMessage(tunnel.Name, new ExceptionMessage(msg.Token) { Message = e.InnerException.Message });

                    //do not send return message
                    return;
                }
            }

            if (result != null)
            {
                //send callback
                PushMessage(tunnel.Name, new CallbackMessage(msg.Token) { ReturnType = TypeHelper.GetBasicType(method.ReturnType), ReturnValue = result });
                
            }
            else
            {
                //send callback
                PushMessage(tunnel.Name, new CallbackMessage(msg.Token) { ReturnType= typeof(void), ReturnValue = "" });
            }
        }



        public static void PushMessage(String Tunnel, Message Message)
        {
            if (!Tunnels.ContainsKey(Tunnel))
            {
                throw new ArgumentException("\"" + Tunnel + "\" tunnel was closed or does not exist.");
            }
            Tunnels[Tunnel].Send(Message);
        }

    }
}
