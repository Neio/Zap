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
using System.Threading;

namespace Zap
{
    internal class CallbackListener : ICallback
    {
        public CallbackListener(String Tunnel, Message OrigionalMessage)
        {
            this.Tunnel = Tunnel;
            BindToken = OrigionalMessage.Token;
            waitcontrol.Reset();

        }

        public String Tunnel { get; set; }

        public object EndSend()
        {
            waitcontrol.WaitOne();
            //wait here
            if (_errorMessage != null)
            {
                throw new InvalidOperationException(_errorMessage);
            }
            return _result;
        }

        object _result = null;
        String _errorMessage = null;
        ManualResetEvent waitcontrol = new ManualResetEvent(false);

        public void InvokeEndSend(CallbackMessage Message)
        { 
            //release
            _result = Message.ReturnValue;
            //set value
            
            waitcontrol.Set();
        }

        public void InvokeException(ExceptionMessage Message)
        {
            //release
            _errorMessage = Message.Message;
            waitcontrol.Set();
        }


        public string BindToken
        {
            get;
            private set;
        }
    }
}
