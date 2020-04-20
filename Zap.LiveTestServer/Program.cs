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

using Zap;

namespace Zap.LiveTestServer
{
    class Program
    {
        static void Main(string[] args)
        {
            ExampleObject sample = new ExampleObject();
            Proxy.Register(sample);

            int Port = 1023;

            var target = Proxy.SetListener(Port);
            
            target.OnConnected += (s, e) =>
            {
                ITunnel tunnel = e.Tunnel;
                Console.WriteLine("Tunnel Connected: {0}", tunnel.Name);
                tunnel.OnDisconnected += (ss, ee) =>
                {
                    Console.WriteLine("Tunnel Disconnected: {0}", ((ITunnel)ss).Name);
                    tunnel = null;
                };
            };

            Console.WriteLine("Start Listening");
            Console.ReadKey();

            Console.WriteLine("Closing");
            target.TurnOff();
        }
    }

    [RemoteObject()]
    public class ExampleObject : Listener
    {
        [RemoteMethod]
        public int Add(int A, int B)
        {
            Console.WriteLine("{0} + {1} + C ={2}", A, B, A + B + C);
            return A + B + C;

        }

        [RemoteMethod]
        public void Increase()
        {

            C++;
            Console.WriteLine("C = {0}", C);
            if (OnIncreased != null)
                OnIncreased(this, EventArgs.Empty);
        }

        [RemoteMethod]
        public int Divide(int A, int B)
        {
            Console.WriteLine("A / B");
            return A / B;
        }


        [RemoteMethod]
        public int GetC()
        {
            return C;
        }

        [RemoteMethod]
        public int[] GetList()
        {
            return new[] { 1, 2, 3 };
        }

        int C = 0;

        [RemoteEvent]
        public event EventHandler OnIncreased;
    }
}
