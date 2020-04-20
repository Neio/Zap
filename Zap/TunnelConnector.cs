using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace Zap
{
    public class TunnelConnector : TunnelBase, ITunnel
    {

        private String _address;
        private int _port;
        private bool _isConnecting = false;
        public bool IsConnecting { get { return _isConnecting; } }

        public event EventHandler OnConnected;

        public TunnelConnector(String Name, String Address, int Port)
        {
            this.Name = Name;
            _address = Address;
            _port = Port;
            
        }

        public void Connect()
        {
            if (_isConnecting || _isWorking)
            {
                return;
            }
            _isConnecting = true;
            client = new TcpClient();
            client.SendTimeout = 1000;
            client.Connect(_address, _port);

            _isWorking = true;
            _isConnecting = false;

            if (!Init())
                return;

            //first send name
            writer.WriteLine("+{0}+", Name);
            writer.Flush();


            Thread running = new Thread(() =>
            {
                AsyncCallback invokeEvent = (p) =>
                {

                    if (OnConnected != null)
                        OnConnected(this, EventArgs.Empty);
                };
                invokeEvent.BeginInvoke(null, null, null);


                while (_isWorking)
                {
                    Receive();
                }

                Dispose();
            });
            running.Start();




        }

        


    }
}
