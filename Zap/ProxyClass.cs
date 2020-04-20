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
using System.Linq;
using System.Text;
using System.Collections;

namespace Zap
{
    public class ProxyClass
    {
        public ProxyClass(String Tunnel)
        {
            this.Tunnel = Tunnel;
            Events = new Dictionary<string, IEvent>();
            var tunnel = Proxy.GetTunnel(Tunnel);
            tunnel.OnReceived += new EventHandler<ReceivedEventArgs>(ProxyClass_OnReceived);
            tunnel.OnDisconnected += new EventHandler<TunnelBrokenEventArgs>(tunnel_OnDisconnected);
        }

        void tunnel_OnDisconnected(object sender, TunnelBrokenEventArgs e)
        {
            var tunnelName = ((ITunnel)sender).Name;
            foreach (CallbackListener item in Callbacks.Values)
            {
                if (item.Tunnel == tunnelName)
                {
                    item.InvokeException(new ExceptionMessage(item.BindToken) { Message = "Connection broken." });
                }
            }
        }

        void ProxyClass_OnReceived(object sender, ReceivedEventArgs e)
        {
            var message = e.Message;
            if (message is EventMessage)
            {
                ProcessEvent(e.Tunnel, message as EventMessage);
            }
            else if (message is ExceptionMessage)
            {
                var tunnelName = e.Tunnel.Name;

                if (Callbacks != null && Callbacks.ContainsKey(message.Token))
                {
                    //callback exception
                    var callback = Callbacks[message.Token] as CallbackListener;
                    callback.InvokeException(message as ExceptionMessage);
                    Callbacks.Remove(message.Token);
                }
            }
            else if (message is CallbackMessage)
            {
                var tunnelName = e.Tunnel.Name;
                if (Callbacks != null && Callbacks.ContainsKey(message.Token))
                {
                    var callback = Callbacks[message.Token] as CallbackListener;
                    callback.InvokeEndSend(message as CallbackMessage);

                    Callbacks.Remove(message.Token);
                }
                else
                {
                    //not contain token
                    //possible an event callback
                }
            }
            else
            {
                //invalid
            }

        }


        private static Dictionary<String, IEvent> Events;

        protected String Tunnel;

        protected Hashtable eventHandles = new Hashtable();

        Hashtable Callbacks = Hashtable.Synchronized(new Hashtable());

        protected void AddEvent(String ObjectName, String EventName, EventHandler Handler) 
        {
            EventRegistrationMessage e = new EventRegistrationMessage();
            e.EventName = EventName;
            e.IsRegister = true;
            e.ObjectName = ObjectName;
            var ev = AddEvent(Tunnel, e);
            ev.OnMessage += (sender, ee) => {
                Handler(this, ee);
            };
            eventHandles[Handler] = ev;
        }


        protected void RemoveEvent(String ObjectName, String EventName, EventHandler Handler)
        {
            EventRegistrationMessage e = new EventRegistrationMessage();
            e.EventName = EventName;
            e.IsRegister = false;
            e.ObjectName = ObjectName;
            var ev = (IEvent)eventHandles[Handler];
            RemoveEvent(ev);
            eventHandles.Remove(Handler);
        }


        public T Send<T>(String Tunnel, Message Message)
        {
            var callback = BeginSend(Tunnel, Message);

            return (T)callback.EndSend();

        }

        public  void SendVoid(String Tunnel, Message Message)
        {
            var callback = BeginSend(Tunnel, Message);
            var rnt = callback.EndSend();
        }


        public  IEvent AddEvent(String Tunnel, EventRegistrationMessage Message)
        {
            IEvent callback = new EventCallback(Tunnel, Message);
            var cb = BeginSend(Tunnel, Message);
            //Tunnels[Tunnel].Send(Message);
            lock (this)
            {
                Events.Add(Message.Token, callback);
            }
            cb.EndSend();
            return callback;
        }

        public  void RemoveEvent(IEvent EventCallback)
        {
            //send deregister message
            var deregister = new EventRegistrationMessage(EventCallback.BindToken);
            deregister.ObjectName = EventCallback.ObjectName;
            deregister.EventName = EventCallback.EventName;
            deregister.IsRegister = false;
            var cb = BeginSend(EventCallback.BindTunnel, deregister);

            //PushMessage(EventCallback.BindTunnel,deregister);

            //remove event
            lock (this)
            {
                Events.Remove(EventCallback.BindToken);
            }
            cb.EndSend();
        }

        public  ICallback BeginSend(String Tunnel, Message Message)
        {
            ICallback callback = new CallbackListener(Tunnel, Message);

            if (Callbacks != null)
            {
                Callbacks.Add(Message.Token, callback);

                //get tunnel
                Proxy.PushMessage(Tunnel, Message);
            }

            return callback;
        }


        private void ProcessEvent(ITunnel tunnel, EventMessage eventMessage)
        {
            EventCallback eventProxy;
            if (Events.ContainsKey(eventMessage.EventToken))
            {

                lock (this)
                {
                    eventProxy = Events[eventMessage.EventToken] as EventCallback;
                }
                eventProxy.InvokeEvent(eventMessage.Parameters);
                //send processed message 
                //Proxy.PushMessage(tunnel.Name, new CallbackMessage(eventMessage.Token) { ReturnType = typeof(void), ReturnValue = "void" });
            }
        }
    }
}
