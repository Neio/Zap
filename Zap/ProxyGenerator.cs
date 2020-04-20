using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Zap
{
    public class ProxyGenerator
    {
        public static string Generate(String dllFile)
        {

            Assembly asm = Assembly.Load(dllFile);
            return Generate(asm);
        }

        public static string Generate(Assembly asm)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Text;\n\n");

           
            foreach (var type in asm.GetTypes())
            {
                sb.AppendLine("");
                sb.AppendLine("");

                var tps = type.GetCustomAttributes(typeof(RemoteObjectAttribute), false);
                if (tps.Length == 0)
                { 
                    //is not a remote object
                    continue;
                }
                
                sb.Append("\tpublic class ");
                sb.Append(type.Name);
                sb.Append(" : ProxyClass\r\n\t{\r\n");
                sb.Append("\t\tpublic ");
                sb.Append(type.Name);
                sb.AppendLine("(ITunnel Tunnel) : base(Tunnel.Name) { }");

                sb.Append("\t\tpublic ");
                sb.Append(type.Name);
                sb.AppendLine("(String TunnelName) : base(TunnelName) { }");


                foreach (var method in type.GetMethods())
                {
                    var methods = method.GetCustomAttributes(typeof(RemoteMethodAttribute), false);
                    if (methods.Length == 0)
                        continue;

                    //get method name and parameters
                    var methodName = method.Name;
                    var methodReturnType = method.ReturnType;
                    var parameters = method.GetParameters();
                    int count = 0;
                    StringBuilder sbb = new StringBuilder();
                    foreach (var param in parameters)
                    {
                        count++;
                        sbb.AppendFormat("{0} {1}", param.ParameterType.Name, param.Name);
                        if (count != parameters.Length)
                            sbb.Append(",");
                    }
                    if (methodReturnType == typeof(void))
                        sb.AppendFormat("\t\tpublic void {0}({1})\r\n", methodName, sbb.ToString());
                    else
                        sb.AppendFormat("\t\tpublic {0} {1}({2})\r\n", methodReturnType.Name, methodName, sbb.ToString());

                    sb.AppendLine("\t\t{");
                    sb.AppendFormat("\t\t\tvar message = new SendMessage(){{ ObjectName = \"{0}\", Method = \"{1}\"}};\r\n", type.Name, methodName);
                    foreach (var param in parameters)
                    {
                        sb.AppendFormat("\t\t\tmessage.Parameters.Add(\"{0}\", {0}.ToString());\r\n", param.Name);
                    }
                    if (methodReturnType == typeof(void))
                    {
                        sb.AppendFormat("\t\t\tSendVoid(Tunnel, message);\r\n");
                    }
                    else
                    {
                        sb.AppendFormat("\t\t\treturn Send<{0}>(Tunnel, message);\r\n", methodReturnType.Name);
                    }
                    sb.AppendLine("\t\t}");
                }

                foreach (var method in type.GetEvents())
                {
                    var methods = method.GetCustomAttributes(typeof(RemoteEventAttribute), false);
                    if (methods.Length == 0)
                        continue;
                    var methodName = method.Name;
                    var handlerType = method.EventHandlerType;

                    sb.AppendFormat("\t\tpublic event EventHandler {0} {{\r\n", methodName);
                    sb.AppendLine("\t\t\tadd{");
                    sb.AppendFormat("\t\t\t\tAddEvent(\"{1}\",\"{0}\",value);\r\n", methodName, type.Name);
                    sb.AppendLine("\t\t\t}");
                    sb.AppendLine("\t\t\tremove{");
                    sb.AppendFormat("\t\t\t\tRemoveEvent(\"{1}\",\"{0}\",value);\r\n", methodName, type.Name);
                    sb.AppendLine("\t\t\t}");
                    sb.AppendLine("\t\t}");
                }

                sb.AppendLine("\t}");
            }

            

            return sb.ToString();
        }
    }
}
