using System;
using System.IO;

namespace BixPlugins
{
    public class Log
    {
        private static Log _outputSingleton;
        private readonly string LogDirPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

        public Log()
        {
            EnsureLogDirectoryExists();
            InstantiateStreamWriter();
        }

        private static Log OutputSingleton
        {
            get
            {
                if (_outputSingleton == null)
                {
                    _outputSingleton = new Log();
                }
                return _outputSingleton;
            }
        }

        public StreamWriter SW { get; set; }
        public static event MessageEventHandler OnMessageReceived;

        ~Log()
        {
            if (SW != null)
            {
                try
                {
                    SW.Dispose();
                }
                catch (ObjectDisposedException)
                {
                } // object already disposed - ignore exception
            }
        }

        private static void WriteColor(string level, string msg, ConsoleColor consoleColor = ConsoleColor.Green)
        {
            switch (level)
            {
                case "ERROR":
                    consoleColor = ConsoleColor.Red;
                    break;
                case "DEBUG":
                    consoleColor = ConsoleColor.Magenta;
                    break;
                case "BULB":
                    consoleColor = ConsoleColor.DarkYellow;
                    break;
            }

            Console.ForegroundColor = consoleColor;
            Console.Write(level + ": ");
            Console.ResetColor();
            Console.WriteLine($"{DateTime.Now} {msg}");
            OutputSingleton.OnOnMessageReceived($"{DateTime.Now} {msg}");
        }

        private static void Write(string type, string msg)
        {
            WriteColor(type, msg);
            OutputSingleton.SW.WriteLine($"{type}: {DateTime.Now} {msg}");
        }

        public static void Info(string msg)
        {
            Write("INFO", msg);
        }

        public static void Error(string msg)
        {
            Write("ERROR", msg);
        }

        public static void Debug(string msg)
        {
            Write("DEBUG", msg);
        }

        public static void Bulb(string msg)
        {
            Write("BULB", msg);
        }

        public static void Write(string msg)
        {
            Console.WriteLine(msg);
            OutputSingleton.SW.Write(msg);
        }


        private void InstantiateStreamWriter()
        {
            var filePath = Path.Combine(LogDirPath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")) + ".txt";
            try
            {
                SW = new StreamWriter(filePath);
                SW.AutoFlush = true;
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new ApplicationException(
                    string.Format("Access denied. Could not instantiate StreamWriter using path: {0}.", filePath), ex);
            }
        }

        private void EnsureLogDirectoryExists()
        {
            if (!Directory.Exists(LogDirPath))
            {
                try
                {
                    Directory.CreateDirectory(LogDirPath);
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw new ApplicationException(
                        string.Format("Access denied. Could not create log directory using path: {0}.", LogDirPath), ex);
                }
            }
        }

        protected void OnOnMessageReceived(string message)
        {
            var e = new MessageEvent {Message = message};
            OnMessageReceived?.Invoke(this, e);
        }
    }
}