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
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace Zap
{
    internal class TunnelListenerUnit : TunnelBase, ITunnel
    {
        internal TunnelListenerUnit(TcpClient client, TunnelListener Manager)
        {
            this.client = client;
            client.SendTimeout = 1000;
            _listener = Manager;

            if (!Init())
            {
                IsValid = false;
                return;
            }

            //read first line as tunnel name 
            //message format "+tunnel+"
            while (true)
            {
                //trying
                if (networkStream.DataAvailable)
                {
                    var line = reader.ReadLine();
                    if (line.StartsWith("+") && line.EndsWith("+"))
                    {
                        //correct message
                        Name = line.Substring(1, line.Length - 2);
                        IsValid = true;
                        
                        break;
                    }
                    else
                    {
                        //not valid
                        //throw new Exception("Invalid Tunnel");
                        IsValid = false;
                        break;
                    }
                }
            }
        }


        internal TunnelListener _listener;
        internal bool IsValid;
       



        internal void Run()
        {
            _isWorking = true;

            while (_isWorking && _listener.IsWorking)
            {
                Receive();
            }
            Dispose();
        }

        

        


        
    }
}
