using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatheServer
{
    public class Logger
    {
        private static readonly Logger _instance = new Logger();

        // 通过单例控制来使得 Logger 类不会被创建多次，进而保证日志写入锁只有一个，保证日志输出不会错位
        public static Logger Instance { get => _instance; }

        private Logger()
        {

        }

        private static void WriteLine(params object[] args) => Console.WriteLine(string.Join(" ", args));

        private static void Write(params object[] args) => Console.Write(string.Join(" ", args));

        public void LogDebug(params object[] args)
        {
            lock (this)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Write($"[{DateTime.Now.TimeOfDay}]", "[DEBUG] ");
                WriteLine(args);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }

        public void LogInfo(params object[] args)
        {
            lock (this)
            {
                Write($"[{DateTime.Now.TimeOfDay}]", "[INFO] ");
                WriteLine(args);
            }
        }

        public void LogWarn(params object[] args)
        {
            lock (this)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Write($"[{DateTime.Now.TimeOfDay}]", "[WARN] ");
                WriteLine(args);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }

        public void LogError(params object[] args)
        {
            lock (this)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Write($"[{DateTime.Now.TimeOfDay}]", "[ERROR] ");
                WriteLine(args);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }

        public void LogSystem(params object[] args)
        {
            lock (this)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Write($"[{DateTime.Now.TimeOfDay}]", "[SYSTEM] ");
                WriteLine(args);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
    }
}