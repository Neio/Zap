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

namespace Zap
{
    public class SendMessage : Message
    {
        public SendMessage(String Token)
        {
            this.Token = Token;
            Parameters = new Dictionary<string, string>();
        }

        public SendMessage()
        {

            Parameters = new Dictionary<string, string>();
        }

        public String ObjectName
        {
            get;
            set;
        }

        public String Method
        {
            get;
            set;
        }

        public Dictionary<String, String> Parameters
        {
            get;
            set;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Token);
            sb.Append(spliter);
            sb.Append("Call");
            sb.Append(spliter);
            sb.Append(ObjectName);
            sb.Append(spliter);
            sb.Append(Method);
            sb.Append(spliter);
            if (Parameters != null)
            {
                var count = 0;
                var all = Parameters.Count;
                foreach (var item in Parameters)
                {
                    sb.Append(item.Key);
                    sb.Append(',');
                    sb.Append(JsonEscape.Escape(item.Value));
                    if (++count != all)
                        sb.Append(spliter);
                }
            }
            return sb.ToString();
        }
    }
}
