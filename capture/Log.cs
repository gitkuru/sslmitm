using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace capture
{
    public class Log
    {
        /// <summary>
        /// ファイル排他オブジェクト
        /// </summary>
        private static Object lockObject = new Object();

        /// <summary>
        /// ログ積算番号
        /// </summary>
        private static int LogNumber = 0;

        /// <summary>
        /// Debugメッセージの記録を有効にするか
        /// </summary>
        public static bool EnableDebug = true;

        /// <summary>
        /// Debugメッセージのコンソール表示を有効にするか
        /// </summary>
        public static bool EnableDebugConsole = true;


        /// <summary>
        /// 通常ログ
        /// </summary>
        /// <param name="log"></param>
        public static void Debug(string log)
        {
            if (EnableDebug)
            {
                lock (lockObject)
                {
                    var msg = logTag("Log") + log + System.Environment.NewLine;

                    if (EnableDebugConsole)
                    {
                        Console.Write(msg);
                    }
                    writefile(msg);
                }
            }
        }

        /// <summary>
        /// 通常ログ
        /// </summary>
        /// <param name="log"></param>
        public static void Info(string log)
        {
            lock (lockObject)
            {
                var msg = logTag("Log") + log + System.Environment.NewLine;

                Console.Write(msg);
                writefile(msg);
            }
        }

        /// <summary>
        /// エラーログ
        /// </summary>
        /// <param name="log"></param>
        public static void Error(string log)
        {
            lock (lockObject)
            {
                var msg = logTag("ERROR") + log + System.Environment.NewLine;

                Console.Write(msg);
                writefile(msg);
            }
        }


        private static string logTag(string tag)
        {

            var ret = "[" + tag + "]";
            ret += "(" + DateTime.Now + ")";
            ret += "[" + LogNumber + "]";

            ret += " ";

            LogNumber++;
            return ret;
        }


        private static void writefile(string msg)
        {
            try
            {
                using (var file = new FileStream("logging.txt", FileMode.Append))
                {
                    using (var writer = new StreamWriter(file))
                    {
                        writer.Write(msg);
                    }
                }
            }
            catch(Exception err)
            {
                Console.Write("logging.txt cannot open. " + err.Message + System.Environment.NewLine);
            }
        }
    }
}
