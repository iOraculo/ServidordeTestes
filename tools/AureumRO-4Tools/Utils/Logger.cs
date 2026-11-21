using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace _4RTools.Utils
{
    public static class Logger
    {
        private static readonly object SyncRoot = new object();
        private const long MaxLogSizeBytes = 2 * 1024 * 1024; // 2 MB

        private static string LogFilePath
        {
            get
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "4rtools.log");
            }
        }

        public static void Info(string context, string message)
        {
            Write("INFO", context, message, null);
        }

        public static void Warn(string context, string message, Exception ex = null)
        {
            Write("WARN", context, message, ex);
        }

        public static void Error(string context, string message, Exception ex = null)
        {
            Write("ERROR", context, message, ex);
        }

        private static void Write(string level, string context, string message, Exception ex)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            StringBuilder sb = new StringBuilder();
            sb.Append('[').Append(timestamp).Append("] [").Append(level).Append("] [").Append(context).Append("] ").Append(message);
            if (ex != null)
            {
                sb.Append(" | ").Append(ex.GetType().Name).Append(": ").Append(ex.Message);
            }

            string line = sb.ToString();
            Debug.WriteLine(line);

            try
            {
                lock (SyncRoot)
                {
                    string path = LogFilePath;
                    string dir = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    if (File.Exists(path))
                    {
                        FileInfo info = new FileInfo(path);
                        if (info.Length > MaxLogSizeBytes)
                        {
                            string backupPath = path + ".bak";
                            if (File.Exists(backupPath))
                            {
                                File.Delete(backupPath);
                            }
                            File.Move(path, backupPath);
                        }
                    }

                    File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch
            {
                // Logging must never crash the application.
            }
        }
    }
}
