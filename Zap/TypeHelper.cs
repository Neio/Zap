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

namespace Zap
{
    /// <summary>
    /// Type Convert
    /// </summary>
    public class TypeHelper
    {
        static TypeHelper()
        {
            getTypeDict = new Dictionary<string, Type>();
            getTypeDict.Add("void", typeof(void));

            getTypeDict.Add("bool", typeof(bool));
            getTypeDict.Add("byte", typeof(byte));
            getTypeDict.Add("sbyte", typeof(sbyte));
            getTypeDict.Add("char", typeof(char));
            getTypeDict.Add("int", typeof(int));
            getTypeDict.Add("short", typeof(short));
            getTypeDict.Add("long", typeof(long));
            getTypeDict.Add("float", typeof(float));
            getTypeDict.Add("double", typeof(double));

            getTypeDict.Add("boolean", typeof(bool));
            getTypeDict.Add("int32", typeof(int));
            getTypeDict.Add("int16", typeof(short));
            getTypeDict.Add("int64", typeof(long));

            getTypeDict.Add("string", typeof(string));
            getTypeDict.Add("datetime", typeof(DateTime));
        }

        static Dictionary<String, Type> getTypeDict;

        /// <summary>
        /// String to Object
        /// </summary>
        /// <param name="Type">Target Type</param>
        /// <param name="StringValue">String Value</param>
        /// <returns>Object</returns>
        public static Object GetValue(Type Type, String  StringValue)
        {

            if (Type == typeof(string))
                return StringValue;
            else if (Type == typeof(int))
                return int.Parse(StringValue);
            else if (Type == typeof(long))
                return long.Parse(StringValue);
            else if (Type == typeof(short))
                return short.Parse(StringValue);
            else if (Type == typeof(double))
                return double.Parse(StringValue);
            else if (Type == typeof(float))
                return float.Parse(StringValue);
            else if (Type == typeof(DateTime))
                return DateTime.Parse(StringValue);
            else if (Type == typeof(bool))
                return bool.Parse(StringValue);
            else if (Type == typeof(char))
                return char.Parse(StringValue);
            else if (Type == typeof(byte))
                return byte.Parse(StringValue);
            else if (Type == typeof(sbyte))
                return sbyte.Parse(StringValue);


            throw new ArgumentException("Type Convert doest not support "+ Type.Name);
        }

        public static Type GetType(String typeString)
        {
            return getTypeDict[typeString.ToLower()];
        }

        public static Type GetBasicType(Type ReturnType)
        {
            if (ReturnType == typeof(String))
                return typeof(String);

            if (ReturnType.IsArray)
            { 
                //array
                return ReturnType.GetElementType();
            }

            if (ReturnType.IsEnum)
            {
                return Enum.GetUnderlyingType(ReturnType);
            }  

            var type = GetEnumerableType(ReturnType);
            if (type != null)
                return type;

            type = GetListType(ReturnType);
            if (type != null)
                return type;

            //basic type
            return ReturnType;

        }

        public static Type GetEnumerableType(Type type)
        {
            if (type == null)
                return null;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return type.GetGenericArguments()[0];

            foreach (var item in type.GetInterfaces())
            {
                if (item.IsGenericType && item.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    return item.GetGenericArguments()[0];
            }

            return null;
        }

        public static Type GetListType(Type type)
        {
            if (type == null)
                return null;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IList<>))
                return type.GetGenericArguments()[0];

            foreach (var item in type.GetInterfaces())
            {
                if (item.IsGenericType && item.GetGenericTypeDefinition() == typeof(IList<>))
                    return item.GetGenericArguments()[0];
            }

            return null;
        }
    }
}
