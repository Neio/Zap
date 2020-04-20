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
using Zap;
using System.Threading;
using System.Collections;

namespace Commnunication.LiveTest
{
    class Program
    {
        static void Main(string[] args)
        {
            //192.168.137.185
            var ip = "127.0.0.1";
            var tunnelName = "Tunnel1";
            if (args.Length == 2 && args[0].Contains("."))
            {
                ip = args[0];
                tunnelName = args[1];
            }
            else if (args.Length > 0)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("  Zap.LiveText.exe <IP> <TunnelName>");
                Console.WriteLine("Example:");
                Console.WriteLine("  Zap.LiveText.exe 192.168.137.185 ExampleTunnel");
                return;
            }
            TunnelConnector connector = new TunnelConnector(tunnelName, ip, 1023);
            Proxy.AddTunnel(connector);
            connector.OnConnected += (sender, e) => {
                for (int i = 0; i <2; i++)
                {
                    Thread s1 = new Thread(() =>
                    {
                        Process(connector);
                    });
                    s1.Start();
                }              

            };

            connector.OnDisconnected += (sender, e) => {
                Console.WriteLine("Disconnected.");
            };
            Console.WriteLine("Start Connecting");

            Console.ReadKey();
            connector.Close();
            Console.WriteLine("Stopping");

        }

        private static void Process(ITunnel tunnel)
        {

            DateTime timer = DateTime.Now;

            try
            {
                ExampleObject call = new ExampleObject(tunnel);

                //Add Event handler example
                call.OnIncreased += new EventHandler(call_OnCAdded);

                for (int i = 0; i < 100; i++)
                {

                    //call without return value
                    call.Increase();

                    //call with return value
                    var dd = call.Add(1, 2);

                    Console.WriteLine("Get result: {0}", dd);

                }

                //Remote event listener
                call.OnIncreased -= new EventHandler(call_OnCAdded);

                for (int i = 0; i < 100; i++)
                {

                    //call without return value
                    call.Increase();

                    //call with return value
                    var dd = call.GetC();

                    Console.WriteLine("C = {0}", dd);

                }

                //Total 1 + 300(event+increase+method) + 1 + 200(increase+method) = 502 calls

                Console.WriteLine("Execute Time: {0}", (DateTime.Now - timer).Duration());

                

                //get list
                var list = call.GetList();
                if (list != null)
                {
                    Console.WriteLine("Got list:");
                    foreach (var item in list)
                    {
                        Console.WriteLine(item);
                    }
                }
                //exception experiment
                var result = call.Divide(10, 0);
                Console.WriteLine("Result = " + result);

                
            }
            catch (Exception e)
            {
                Console.WriteLine("Remote exception catched: " + e.Message);
            }
        }

        static void call_OnCAdded(object sender, EventArgs e)
        {
            Console.WriteLine("C Added");
        }
    }




    public class ExampleObject : ProxyClass
    {
        public ExampleObject(ITunnel Tunnel) : base(Tunnel.Name) { }
        public ExampleObject(String TunnelName) : base(TunnelName) { }
        public Int32 Add(Int32 A, Int32 B)
        {
            var message = new SendMessage() { ObjectName = "ExampleObject", Method = "Add" };
            message.Parameters.Add("A", A.ToString());
            message.Parameters.Add("B", B.ToString());
            return Send<Int32>(Tunnel, message);
        }
        public void Increase()
        {
            var message = new SendMessage() { ObjectName = "ExampleObject", Method = "Increase" };
            SendVoid(Tunnel, message);
        }
        public int Divide(int A, int B)
        {
            var message = new SendMessage() { ObjectName = "ExampleObject", Method = "Divide" };
            message.Parameters.Add("A", A.ToString());
            message.Parameters.Add("B", B.ToString());
            return Send<int>(Tunnel, message);
        }
        public Int32 GetC()
        {
            var message = new SendMessage() { ObjectName = "ExampleObject", Method = "GetC" };
            return Send<Int32>(Tunnel, message);
        }
        public int[] GetList()
        {
            var message = new SendMessage() { ObjectName = "ExampleObject", Method = "GetList" };
            return Send<int[]>(Tunnel, message);
        }
        public event EventHandler OnIncreased
        {
            add
            {
                AddEvent("ExampleObject", "OnIncreased", value);
            }
            remove
            {
                RemoveEvent("ExampleObject", "OnIncreased", value);
            }
        }
    }
}
