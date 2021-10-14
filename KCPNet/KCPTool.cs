using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;

namespace KCPNet
{
    public class KCPTool
    {
        public static Action<string> LogAction;
        public static Action<KCPLogColor,string> ColorLogAction;
        public static Action<string> WarnLogAction;
        public static Action<string> ErrorLogAction;

        public static void Log(string msg, params object[] args)
        {
            msg = string.Format(msg, args);
            if (LogAction != null)
                LogAction(msg);
            else
                ConsoleLog(msg, KCPLogColor.None);
        }
        public static void ColorLog(KCPLogColor kCPLogColor, string msg, params object[] args)
        {
            msg = string.Format(msg, args);
            if (ColorLogAction != null)
                ColorLogAction(kCPLogColor,msg);
            else
                ConsoleLog(msg, kCPLogColor);
        }
        public static void WarnLog(string msg, params object[] args)
        {
            msg = string.Format(msg, args);
            if (WarnLogAction != null)
                WarnLogAction(msg);
            else
                ConsoleLog(msg, KCPLogColor.Yellow);
        }
        public static void ErrorLog(string msg, params object[] args)
        {
            msg = string.Format(msg, args);
            if (ErrorLogAction != null)
                ErrorLogAction(msg);
            else
                ConsoleLog(msg, KCPLogColor.Red);
        }
        static void ConsoleLog(string msg, KCPLogColor color)
        {
            int threadID = Thread.CurrentThread.ManagedThreadId;
            msg = string.Format("ThreadID:{0}  {1}", threadID, msg);
            switch (color)
            {
                case KCPLogColor.Red:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine(msg);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case KCPLogColor.Green:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(msg);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case KCPLogColor.Blue:
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine(msg);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case KCPLogColor.Cyan:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(msg);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case KCPLogColor.Yellow:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(msg);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
            }
        }

        public static byte[] Serialize<T>(T msg)where T : KCPMsg
        {
            using(MemoryStream ms=new MemoryStream())
            {
                try
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(ms, msg);
                    ms.Seek(0, SeekOrigin.Begin);
                    return ms.ToArray();
                }
                catch(SerializationException e)
                {
                    ErrorLog("Failed to serialize.Reason:{0}", e.ToString());
                    throw;
                }
            }
        }

        public static T DeSerialize<T>(byte[] bytes) where T : KCPMsg
        {
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                try
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    T msg = (T)bf.Deserialize(ms);
                    return msg;
                }
                catch (SerializationException e)
                {
                    ErrorLog("Failed to DeSerialize.Reason:{0}", e.ToString());
                    throw;
                }
            }
        }
        /// <summary>
        /// 数据压缩
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static byte[] Compress(byte[] input)
        {
            using (MemoryStream outMS=new MemoryStream())
            {
                using (GZipStream gzs=new GZipStream(outMS, CompressionMode.Compress, true))
                {
                    gzs.Write(input, 0, input.Length);
                    gzs.Close();
                    return outMS.ToArray();
                }
            }
        }
        /// <summary>
        /// 数据解压
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static byte[] DeCompress(byte[] input)
        {
            using (MemoryStream inputMS = new MemoryStream(input))
            {
                using (MemoryStream outMs = new MemoryStream())
                {
                    using (GZipStream gzs = new GZipStream(inputMS, CompressionMode.Decompress))
                    {
                        byte[] bytes = new byte[1024];
                        int len = 0;
                        while ((len=gzs.Read(bytes,0,bytes.Length))>0)
                        {
                            outMs.Write(bytes, 0, len);
                        }
                        gzs.Close();
                        return outMs.ToArray();
                    }
                }
            }
        }
    }
    public enum KCPLogColor
    {
        None,
        Red,
        Green,
        Blue,
        Cyan,
        Yellow
    }
}
