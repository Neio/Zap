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
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;

namespace Zap
{
    public class TunnelListener
    {
        private int _port;
        private bool _isWorking;
        private TcpListener _listener;

        public event EventHandler<TunnelEventArgs> OnConnected;

        public TunnelListener(int Port)
        {
            _port = Port;
            _isWorking = false;
        }

        public int ServerName
        {
            get;
            set;
        }

        public bool IsWorking {
            get {
                return _isWorking;
            }
        }

        public void TurnOn()
        {
            Thread thread = new Thread(() => {
                SyncTurnOn();
            });
            thread.IsBackground = true;
            thread.Start();
        }

        public void SyncTurnOn()
        {
            if (_isWorking)
                return;

            _isWorking = true;
            
            _listener = new TcpListener(IPAddress.Any ,_port);
            _listener.Start();
            //_listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true); 
            while (_isWorking)
            {
                //blocks until a client has connected to the server

                var result = _listener.BeginAcceptTcpClient(
                    new AsyncCallback(ReceiveCallback)
                , _listener);
                result.AsyncWaitHandle.WaitOne();
            }

            _listener.Stop();
        }

        public void TurnOff()
        {
            _isWorking = false;
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            //process connection
            TcpListener listener = (TcpListener)ar.AsyncState;
            var client = listener.EndAcceptTcpClient(ar);


            var tunnel = new TunnelListenerUnit(client, this);
            if (tunnel.IsValid)
            {
                //valid
                AsyncCallback call = new AsyncCallback(result =>
                {
                    if (OnConnected != null)
                        OnConnected(this, new TunnelEventArgs(tunnel));
                });
                call.BeginInvoke(null, null, null);
                tunnel.Run();
            }
            else
            {
                throw new InvalidOperationException("Tunnel Connection Failed");
            }

        }
    }
}
