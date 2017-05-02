using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;

namespace BixPlugins
{
    public class Log
    {
        private static Log _outputSingleton;
        private readonly string _logDirPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

        public const int MaxMessagesDef = 100;
        public static int MaxMessages = MaxMessagesDef;
        public LinkedList<string> Messages { get; set; }
        public Log()
        {
            MaxMessages = GetAppSetting("MaxMessages", MaxMessagesDef);
            Messages =new LinkedList<string>();
            EnsureLogDirectoryExists();
            InstantiateStreamWriter();
        }


        private static int GetAppSetting(string key, int defaultValue = 0)
        {
            var appSettings = ConfigurationManager.AppSettings;
            var result = appSettings[key] ?? defaultValue.ToString();

            int ret;
            if (string.IsNullOrEmpty(result) || !int.TryParse(result, out ret))
                return defaultValue;

            return ret;
        }

        public static string GetMessages()
        {
            var builder = new StringBuilder();
            var messages = OutputSingleton.Messages.ToList();

            builder.AppendLine($"<font color=\"{GetColor("INFO")}\"> <strong>INFO: </strong></font> Max Messages is set to: {Log.MaxMessages}<br>");

            foreach (var message in messages)
            {
                if (! message.Contains(":"))
                    continue;

                var level = message.Substring(0, message.IndexOf(":"));
                if (!ValidMessages.Contains(level))
                    continue;

                var message1 = message.Remove(0, message.IndexOf(":"));
                var ccolor = GetColor(level);

                builder.AppendLine($"<font color=\"{ccolor}\"> <strong>{level}</strong></font> {message1}<br>");
            }

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
            lock (OutputSingleton.Messages)
            {
                MaxMessages =GetAppSetting("MaxMessages", MaxMessagesDef);
                if (Messages.Count >= MaxMessages)
                {
                    OutputSingleton.Messages.RemoveFirst();
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

        private static List<string> ValidMessages= new List<string> {"ERROR","DEBUG","BULB","INFO"};

        private static ConsoleColor GetCColor(string level)
        {
            if (level == "ERROR")
            {
                return ConsoleColor.Red;
            }
            else if (level == "DEBUG")
            {
                return ConsoleColor.Magenta;
            }
            else if (level == "BULB")
            {
                return ConsoleColor.DarkYellow;
            }
            return ConsoleColor.Green;
            
        }

        private static string GetColor(string level)
        {
            if (level == "ERROR")
            {
                return BIXColors.Colors["firebrick"].Hex;
            }
            else if (level == "DEBUG")
            {
                return BIXColors.Colors["deeppink"].Hex;
            }
            else if (level == "BULB")
            {
                return BIXColors.Colors["darkgoldenrod"].Hex;
            }

            return BIXColors.Colors["darkgreen"].Hex;

        }

        private static void WriteColor(string level, string msg, ConsoleColor consoleColor = ConsoleColor.Green)
        {
            //if (level == "ERROR")
            //{
            //    consoleColor = ConsoleColor.Red;
            //}
            //else if (level == "DEBUG")
            //{
            //    consoleColor = ConsoleColor.Magenta;
            //}
            //else if (level == "BULB")
            //{
            //    consoleColor = ConsoleColor.DarkYellow;
            //}
            consoleColor = GetCColor(level);

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