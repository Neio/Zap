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
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Runtime.Remoting.Messaging;

namespace Zap
{
    public class TunnelBase: ITunnel
    {
        static TunnelBase()
        {
            EnableKeepAlive = false;
        }

        protected TunnelBase()
        {
            //_alive = DateTime.Now;
        }

        protected TcpClient client;
        protected NetworkStream networkStream;
        protected StreamReader reader;
        protected StreamWriter writer;
        private object readerLock = new object();
        private object writerLock = new object();

        protected bool _isWorking = false;

        /// <summary>
        /// Enable TCP Keep Alive Signal (disable by default)
        /// </summary>
        public static bool EnableKeepAlive { get; set; }


        protected MessageReader mReader = new MessageReader();


        public event EventHandler<ReceivedEventArgs> OnReceived;
        public event EventHandler<TunnelBrokenEventArgs> OnDisconnected;


        public string Name
        {
            get;
            protected set;
        }

        protected bool Init()
        {
            try
            {
                if (EnableKeepAlive)
                {
                    SetKeepAlive(client.Client, 5, 1);
                }
                networkStream = client.GetStream();
                reader = new StreamReader(networkStream, ASCIIEncoding.UTF8);
                writer = new StreamWriter(networkStream, ASCIIEncoding.UTF8);
                return true;
            }
            catch (Exception e)
            { 
                //error occurs
                _isWorking = false;
                Dispose();
                if (OnDisconnected != null)
                    OnDisconnected(this, new TunnelBrokenEventArgs(e));
                return false;
            }
        }

        protected void Receive()
        {
            try
            {

                //receive

                var line = reader.ReadLine();

                if (line == null)
                {
                    //receive null
                    _isWorking = false;
                    if (OnDisconnected != null)
                        OnDisconnected(this, new TunnelBrokenEventArgs(null));
                    return;
                }

                if (line[0] == '+')
                {
                    if (line == "+*+")
                    {
                        //Keep Alive Message
                        //_remoteAlive = DateTime.Now;
                        lock (writerLock)
                        {
                            writer.WriteLine("+Echo+");
                            writer.Flush();
                        }

                    }


                }
                else
                {
                    //Console.WriteLine("REC:{0}", line);

                    AsyncCallback call = new AsyncCallback(r =>
                    {
                        try
                        {
                            Message message = mReader.Read(line);

                            ReceivedEventArgs args = new ReceivedEventArgs(message, this);
                            if (args != null)
                            {
                                if (OnReceived != null)
                                    OnReceived(this, (ReceivedEventArgs)args);

                            }
                        }
                        catch (Exception e)
                        {
                            //error reading
                            //send invalid Message
                            //var invalidMessage = new ExceptionMessage("0") { Message = "Invalid Message" };
                            //Send(invalidMessage);
                        }

                    });
                    call.BeginInvoke(null, null, null);


                }
            }
            catch (IOException e)
            {
                _isWorking = false;
                if (OnDisconnected != null)
                    OnDisconnected(this, new TunnelBrokenEventArgs(e));
            }
            catch (SocketException e)
            {
                _isWorking = false;
                if (OnDisconnected != null)
                    OnDisconnected(this, new TunnelBrokenEventArgs(e));
            }
        }

        public void Send(Message Message)
        {
            if (!_isWorking)
            {
                //not connected;
                Console.WriteLine("Not Connected.");
            }
  
                    try
                    {
                        var message = Message.ToString();
                        //Console.WriteLine("SEND:{0}", message);
                        lock (writerLock)
                        {
                            writer.WriteLine(message);
                            writer.Flush();
                        }
                    }
                    catch (IOException e)
                    {
                        _isWorking = false;
                        if(networkStream!=null)
                            networkStream.Close();
                        if (OnDisconnected != null)
                            OnDisconnected(this, new TunnelBrokenEventArgs(e));
                    }
                    catch (SocketException e)
                    {
                        _isWorking = false;
                        if (networkStream != null)
                            networkStream.Close();
                        if (OnDisconnected != null)
                            OnDisconnected(this, new TunnelBrokenEventArgs(e));
                    }
               
            
        }

        public bool IsWorking
        {
            get { return _isWorking; }
        }

        public void Close()
        {
            if (_isWorking)
            {
                

                lock (this)
                {
                    writer.WriteLine("+*+");
                    writer.Flush();
                }

                _isWorking = false;

                if (OnDisconnected != null)
                    OnDisconnected(this, new TunnelBrokenEventArgs(null));
            }
        }


        protected void Dispose()
        {
            try
            {
                reader.Dispose();
            }
            catch { }
            finally
            {
                reader = null;
            }
            try
            {
                writer.Dispose();
            }
            catch { }
            finally
            {
                writer = null;
            }

            try
            {
                networkStream.Close();
                networkStream.Dispose();
            }
            catch { }
            finally
            {
                networkStream = null;
            }

            try
            {
                client.Close();
            }
            catch { }
            finally
            {
                client = null;
            }

           
        }

#if __MonoCS__
        protected bool SetKeepAlive(Socket sock, ulong time, ulong interval)
        {
            return true;
        }
#else

        const int bytesperlong = 4; // 32 / 8
        const int bitsperbyte = 8;

        /// <summary>
        /// Set keep alive 
        /// </summary>
        /// <param name="sock"></param>
        /// <param name="time"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        /// <remarks>method from http://social.msdn.microsoft.com/Forums/en-US/netfxnetcom/thread/d5b6ae25-eac8-4e3d-9782-53059de04628/ </remarks>
        protected bool SetKeepAlive(Socket sock, ulong time, ulong interval)
        {
            try
            {
                // resulting structure
                byte[] SIO_KEEPALIVE_VALS = new byte[3 * bytesperlong];

                // array to hold input values
                ulong[] input = new ulong[3];

                // put input arguments in input array
                if (time == 0 || interval == 0) // enable disable keep-alive
                    input[0] = (0UL); // off
                else
                    input[0] = (1UL); // on

                input[1] = (time); // time millis
                input[2] = (interval); // interval millis

                // pack input into byte struct
                for (int i = 0; i < input.Length; i++)
                {
                    SIO_KEEPALIVE_VALS[i * bytesperlong + 3] = (byte)(input[i] >> ((bytesperlong - 1) * bitsperbyte) & 0xff);
                    SIO_KEEPALIVE_VALS[i * bytesperlong + 2] = (byte)(input[i] >> ((bytesperlong - 2) * bitsperbyte) & 0xff);
                    SIO_KEEPALIVE_VALS[i * bytesperlong + 1] = (byte)(input[i] >> ((bytesperlong - 3) * bitsperbyte) & 0xff);
                    SIO_KEEPALIVE_VALS[i * bytesperlong + 0] = (byte)(input[i] >> ((bytesperlong - 4) * bitsperbyte) & 0xff);
                }
                // create bytestruct for result (bytes pending on server socket)
                byte[] result = BitConverter.GetBytes(0);
                // write SIO_VALS to Socket IOControl
                sock.IOControl(IOControlCode.KeepAliveValues, SIO_KEEPALIVE_VALS, result);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

#endif
    }
}
