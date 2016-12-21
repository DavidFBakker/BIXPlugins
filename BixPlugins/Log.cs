using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BixPlugins
{
    public class Log
    {
        private static Log _outputSingleton;
        private readonly string _logDirPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

        public const int MaxMessages = 100;
        public LinkedList<string> Messages { get; set; }
        public Log()
        {
            Messages=new LinkedList<string>();
            EnsureLogDirectoryExists();
            InstantiateStreamWriter();
        }

        public static string GetMessages()
        {
            StringBuilder builder = new StringBuilder();
            foreach (var message in OutputSingleton.Messages)
                builder.AppendLine(message);

            return builder.ToString();
        }

        private static bool WriteToConsole
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Console.Title))
                {
                    return true;
                }
                return true;
            }
        }

        private  void AddMessage(string message)
        {
            lock (Messages)
            {
                if (Messages.Count >= MaxMessages)
                {
                    Messages.RemoveFirst();
                }

                OutputSingleton.Messages.AddLast(message);
            }
         
        }

        private static Log OutputSingleton => _outputSingleton ?? (_outputSingleton = new Log());

        public StreamWriter Sw { get; set; }
        public static event MessageEventHandler OnMessageReceived;

        ~Log()
        {
            if (Sw != null)
            {
                try
                {
                    Sw.Dispose();
                }
                catch (ObjectDisposedException)
                {
                } // object already disposed - ignore exception
            }
        }

        private static void WriteColor(string level, string msg, ConsoleColor consoleColor = ConsoleColor.Green)
        {
            if (level == "ERROR")
            {
                consoleColor = ConsoleColor.Red;
            }
            else if (level == "DEBUG")
            {
                consoleColor = ConsoleColor.Magenta;
            }
            else if (level == "BULB")
            {
                consoleColor = ConsoleColor.DarkYellow;
            }

            if (WriteToConsole)
            {
                Console.ForegroundColor = consoleColor;
                Console.Write(level + ": ");
                Console.ResetColor();
                Console.WriteLine($"{DateTime.Now} {msg}");
            }
            OutputSingleton.AddMessage($"{DateTime.Now} {msg}");

            OutputSingleton.OnOnMessageReceived($"{DateTime.Now} {msg}");
        }

        private static void Write(string type, string msg)
        {
            WriteColor(type, msg);
            OutputSingleton.AddMessage($"{type}: {DateTime.Now} {msg}");
            OutputSingleton.Sw.WriteLine($"{type}: {DateTime.Now} {msg}");
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
            if (WriteToConsole)
            {
                Console.WriteLine(msg);
            }
            OutputSingleton.AddMessage(msg);
            OutputSingleton.Sw.Write(msg);
        }


        private void InstantiateStreamWriter()
        {
            var filePath = Path.Combine(_logDirPath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")) + ".txt";
            try
            {
                Sw = new StreamWriter(filePath) {AutoFlush = true};
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new ApplicationException(
                    $"Access denied. Could not instantiate StreamWriter using path: {filePath}.", ex);
            }
        }

        private void EnsureLogDirectoryExists()
        {
            if (!Directory.Exists(_logDirPath))
            {
                try
                {
                    Directory.CreateDirectory(_logDirPath);
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw new ApplicationException(
                        $"Access denied. Could not create log directory using path: {_logDirPath}.", ex);
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