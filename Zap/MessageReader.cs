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
    public class MessageReader
    {
        static MessageReader()
        {
            ParseMethod = new Dictionary<string, Parse>();
            ParseMethod.Add("Call", new Parse(ParseCall));
            ParseMethod.Add("Callback", new Parse(ParseCallback));
            ParseMethod.Add("Exception", new Parse(ParseException));
            ParseMethod.Add("Event", new Parse(ParseEvent));
            ParseMethod.Add("EventRegistration", new Parse(ParseRegistration));
        }

        public MessageReader()
        { 
        
        }

        private static Dictionary<String, Parse> ParseMethod;
        private delegate Message Parse(string[] message);
        internal delegate Message ReadDelegate(String Stream);
        public void BeginRead(String Stream, AsyncCallback Callback)
        {
            ReadDelegate del = new ReadDelegate(stream =>
            {
                var message = stream.Split(Message.spliter);
                //JObject message = JObject.Parse(Stream);
                String type = message[1]; //(String)message["Type"];


                return ParseMethod[type](message);
            });
            del.BeginInvoke(Stream, Callback, null);
        }

        public Message Read(String Stream)
        {
            var message = Stream.Split(Message.spliter);
            String type = message[1]; 
            return ParseMethod[type](message);
        }

        private static Message ParseCall(string[] message)
        {
            var token = message[0];
            var msg = new SendMessage(token);
            msg.ObjectName = message[2];
            msg.Method = message[3];
            msg.Parameters = new Dictionary<string, string>();
            for (int i = 4; i < message.Length; i++)
            {
                if (message[i] == "")
                    break;
                var pair = message[i].Split(',');
                var name = pair[0];
                var value = JsonEscape.Unescape(pair[1]);
                msg.Parameters.Add(name, value);
            }
            //foreach (JObject item in list)
            //{
            //    var name = (String)item["Name"];
            //    var value = (string)item["Value"];
            //    msg.Parameters.Add(name, value);
            //}
            return msg;
        }

        private static Message ParseCallback(string[] message)
        {
            var token = message[0];
            var msg = new CallbackMessage(token);
            var typeString = message[2];
            if (typeString.ToLower() == "void")
            {
                msg.ReturnType = typeof(void);
                msg.ReturnValue = null;
            }
            else
            {
                Type type = TypeHelper.GetType(typeString);
                msg.ReturnType = type;
                if (message.Length > 4)
                {
                    var values = Array.CreateInstance(type, message.Length - 3);
                    for (int i = 3; i < message.Length; i++)
                    {
                        values.SetValue(
                            TypeHelper.GetValue(type, (String)message[i]), i-3);
                    }

                    msg.ReturnValue = values;
                }
                else
                {
                    msg.ReturnValue = TypeHelper.GetValue(type, message[3]);
                }
            }
            return msg;
        }

        private static Message ParseException(string[] message)
        {
            var token = message[0];
            var msg = new ExceptionMessage(token);
            msg.Message = message[2];
            return msg;
        }

        private static Message ParseRegistration(string[] message)
        {
            var token = message[0];
            var msg = new EventRegistrationMessage(token);
            msg.ObjectName = message[2];
            msg.EventName = message[3];
            msg.IsRegister = Boolean.Parse(message[4]);
            return msg;
        }

        private static Message ParseEvent(string[] message)
        {
            var token = message[0];
            var eventToken = message[2];
            var msg = new EventMessage(token, eventToken);
            msg.EventName = message[3];
            msg.Parameters = new Dictionary<string, string>();
            for (int i = 4; i < message.Length; i++)
            {
                if (message[i] == "")
                    break;
                var pair = message[i].Split(',');
                var name = pair[0];
                var value = JsonEscape.Unescape(pair[1]);
                msg.Parameters.Add(name, value);
            }
            //var list = (JArray)message["Param"];
            //foreach (JObject item in list)
            //{
            //    var name = (String)item["Name"];
            //    var value = JsonEscape.Unescape((string)item["Value"]);
            //    msg.Parameters.Add(name, value);
            //}
            return msg;
        }
    }
}
