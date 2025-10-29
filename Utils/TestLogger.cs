using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MMA_tests.Utils
{
    public static class TestLogger
    {
        private static readonly object _lockObject = new object();
        private static string LogDirectory => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        private static string CurrentLogFile => Path.Combine(LogDirectory, $"TestLog_{DateTime.Now:yyyyMMdd}.log");

        static TestLogger()
        {
            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }
        }

        private static void WriteToLog(string level, string message)
        {
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
            Console.WriteLine(logMessage);
            
            lock (_lockObject)
            {
                try
                {
                    // Use FileShare.ReadWrite to allow concurrent read access
                    using (var writer = new StreamWriter(CurrentLogFile, true, Encoding.UTF8))
                    {
                        writer.WriteLine(logMessage);
                    }
                }
                catch (IOException ex)
                {
                    // If we still get an IOException, add a small delay and retry once
                    try
                    {
                        System.Threading.Thread.Sleep(100);
                        using (var writer = new StreamWriter(CurrentLogFile, true, Encoding.UTF8))
                        {
                            writer.WriteLine(logMessage);
                        }
                    }
                    catch (Exception retryEx)
                    {
                        // If retry fails, write to console but don't throw
                        Console.WriteLine($"Failed to write to log file: {retryEx.Message}");
                    }
                }
            }
        }

        public static void LogStep(string stepDescription)
        {
            WriteToLog("STEP", stepDescription);
            TestRunContext.AddTestStep(stepDescription);
        }

        public static void LogAssert(string assertDescription)
        {
            WriteToLog("ASSERT", assertDescription);
            TestRunContext.AddTestStep($"Assert: {assertDescription}");
        }

        public static void LogInfo(string info)
        {
            WriteToLog("INFO", info);
        }

        public static void LogWarning(string warning)
        {
            WriteToLog("WARNING", warning);
        }

        public static void LogError(string error)
        {
            WriteToLog("ERROR", error);
        }

        public static List<string> GetCurrentTestLogs()
        {
            lock (_lockObject)
            {
                if (File.Exists(CurrentLogFile))
                {
                    try
                    {
                        // Use FileShare.ReadWrite when reading the file as well
                        using (var reader = new StreamReader(CurrentLogFile, Encoding.UTF8))
                        {
                            return reader.ReadToEnd().Split(Environment.NewLine).ToList();
                        }
                    }
                    catch (IOException)
                    {
                        // If we can't read the file, return empty list rather than failing
                        return new List<string>();
                    }
                }
                return new List<string>();
            }
        }
    }
}