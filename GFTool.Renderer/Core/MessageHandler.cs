
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace GFTool.Renderer.Core
{
    public enum MessageType
    {
        LOG,
        WARNING,
        ERROR
    };

    public struct Message
    {
        public MessageType Type;
        public string Content;
        public Message(MessageType type, string content)
        {
            Type = type;
            Content = content;
        }
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return Content.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Content.GetHashCode();
        }
    }
    public class MessageHandler
    {
        private static readonly Lazy<MessageHandler> lazy = new Lazy<MessageHandler>(() => new MessageHandler());
        public static MessageHandler Instance { get { return lazy.Value; } }

        public event EventHandler<Message> MessageCallback;
        public bool DebugLogsEnabled { get; set; }
        private readonly object logLock = new object();
        private string? logPath;

        private MessageHandler()
        {
            //
        }

        public void AddMessage(MessageType type, string content)
        {
            MessageCallback?.Invoke(this, new Message(type, content));
            TryWriteLog(type, content);
        }

        private void TryWriteLog(MessageType type, string content)
        {
            if (!DebugLogsEnabled)
            {
                return;
            }

            try
            {
                var line = $"{DateTime.Now:O} [{type}] {content}{Environment.NewLine}";
                lock (logLock)
                {
                    var path = GetLogPath();
                    File.AppendAllText(path, line);
                }
            }
            catch
            {
                // Ignore log write failures.
            }
        }

        private string GetLogPath()
        {
            if (!string.IsNullOrWhiteSpace(logPath))
            {
                return logPath;
            }

            var dir = EnsureLogsDir();
            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            logPath = Path.Combine(dir, $"debug_{stamp}.log");
            PruneOldLogs(dir, maxLogs: 50);
            return logPath;
        }

        private static string EnsureLogsDir()
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "logs");
            try
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
            catch
            {
                // Fall back; caller already swallows write failures.
                return AppContext.BaseDirectory;
            }

            return dir;
        }

        private static void PruneOldLogs(string logsDir, int maxLogs)
        {
            try
            {
                var files = Directory.EnumerateFiles(logsDir, "debug_*.log", SearchOption.TopDirectoryOnly)
                    .Select(path =>
                    {
                        try
                        {
                            return new FileInfo(path);
                        }
                        catch
                        {
                            return null;
                        }
                    })
                    .Where(fi => fi != null)
                    .Cast<FileInfo>()
                    .OrderByDescending(fi => fi.CreationTimeUtc)
                    .ThenByDescending(fi => fi.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (files.Count <= maxLogs)
                {
                    return;
                }

                foreach (var fi in files.Skip(maxLogs))
                {
                    try
                    {
                        fi.Delete();
                    }
                    catch
                    {
                        // Ignore individual delete failures.
                    }
                }
            }
            catch
            {
                // Ignore prune failures.
            }
        }
    }
}
