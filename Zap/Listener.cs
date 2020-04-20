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
using System.Reflection;

namespace Zap
{
    public class Listener
    {
        private Dictionary<String, EventHandler> Handlers = new Dictionary<string, EventHandler>();
        private List<KeyValuePair<String, EventRegistrationMessage>> TunnelEvents = new List<KeyValuePair<string, EventRegistrationMessage>>();

        internal void AddListener(String TunnelName, EventRegistrationMessage Msg)
        {
            EventInfo e = this.GetType().GetEvent(Msg.EventName);
            var handler = new EventHandler((ssender, args) =>
            {
                var message = new EventMessage(Msg.Token);
                message.EventName = Msg.EventName;
                var properties = args.GetType().GetProperties();
                foreach (var item in properties)
                {
                    message.Parameters.Add(item.Name, item.GetValue(this, null).ToString());
                }
                Proxy.PushMessage(TunnelName, message);
                //callback.EndSend();
            });
            lock (this)
            {
                e.AddEventHandler(this, handler);
                Handlers.Add(Msg.Token, handler);
                TunnelEvents.Add(new KeyValuePair<string, EventRegistrationMessage>(TunnelName, Msg));
            }
        }

        internal void RemoveListener(EventRegistrationMessage Msg)
        {
            lock (this)
            {
                EventInfo e = this.GetType().GetEvent(Msg.EventName);
                var handler = Handlers[Msg.Token];
                e.RemoveEventHandler(this, handler);
            }
        }

        internal void RemoveTunnelListener(String Tunnel)
        {
            lock (this)
            {
                foreach (var item in TunnelEvents)
                {
                    if (item.Key == Tunnel)
                    {
                        var Msg = item.Value;
                        EventInfo e = this.GetType().GetEvent(Msg.EventName);
                        var handler = Handlers[Msg.Token];
                        e.RemoveEventHandler(this, handler);
                    }
                }

                TunnelEvents.RemoveAll(p => p.Key == Tunnel);
            }
        }
    }

  
}
