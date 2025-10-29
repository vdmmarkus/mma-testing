using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using MMA_tests.Utils;

namespace MMA_tests.Helpers
{
    public static class WebDriverFactory
    {
        private static readonly object _lock = new object();
        private static readonly Dictionary<string, IWebDriver> _activeDrivers = new Dictionary<string, IWebDriver>();
        
        // Keep track of the port we last used
        private static int _lastUsedPort = 9515;

        static WebDriverFactory()
        {
            // Kill any existing chromedriver processes on startup
            KillExistingDriverProcesses();
        }

        private static void KillExistingDriverProcesses()
        {
            try
            {
                foreach (var process in Process.GetProcessesByName("chromedriver"))
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill(true);
                            process.WaitForExit(3000);
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                TestLogger.LogError($"Error killing existing chromedriver processes: {ex.Message}");
            }
        }

        public static IWebDriver CreateDriver(string browserType = "Chrome")
        {
            TestLogger.LogInfo($"Creating WebDriver instance for browser: {browserType}");
            
            string sessionId = Guid.NewGuid().ToString();
            IWebDriver driver = null;
            int retryCount = 0;
            const int maxRetries = 3;

            while (driver == null && retryCount < maxRetries)
            {
                try
                {
                    lock (_lock)
                    {
                        driver = browserType.ToLower() switch
                        {
                            "chrome" => CreateChromeDriver(),
                            "firefox" => CreateFirefoxDriver(),
                            "edge" => CreateEdgeDriver(),
                            _ => CreateChromeDriver()
                        };

                        if (driver != null)
                        {
                            _activeDrivers[sessionId] = driver;
                        }
                    }
                }
                catch (Exception ex)
                {
                    TestLogger.LogError($"Attempt {retryCount + 1} failed: {ex.Message}");
                    if (retryCount < maxRetries - 1)
                    {
                        KillExistingDriverProcesses();
                        System.Threading.Thread.Sleep(1000 * (retryCount + 1)); // Exponential backoff
                    }
                    retryCount++;
                }
            }

            if (driver == null)
            {
                throw new WebDriverException("Failed to create WebDriver instance after multiple attempts");
            }

            try
            {
                driver.Manage().Timeouts().PageLoad = WebDriverConfig.PageLoadTimeout;
                driver.Manage().Timeouts().ImplicitWait = WebDriverConfig.ImplicitWaitTimeout;
                driver.Manage().Window.Maximize();
            }
            catch (Exception ex)
            {
                TestLogger.LogError($"Error configuring WebDriver: {ex.Message}");
                throw;
            }

            TestLogger.LogInfo("WebDriver successfully created");
            return driver;
        }

        private static IWebDriver CreateChromeDriver()
        {
            var chromeOptions = new ChromeOptions();
            
            // Essential options only
            chromeOptions.AddArgument("--no-sandbox");
            chromeOptions.AddArgument("--disable-dev-shm-usage");
            chromeOptions.AddArgument("--disable-gpu");
            chromeOptions.AddArgument("--disable-extensions");
            
            // Accept insecure certificates
            chromeOptions.AcceptInsecureCertificates = true;

            // Create and configure ChromeDriverService with a unique port
            var service = ChromeDriverService.CreateDefaultService();
            service.Port = GetNextAvailablePort();
            service.HostName = "127.0.0.1";
            service.SuppressInitialDiagnosticInformation = true;

            // Verify ChromeDriver exists
            string chromeDriverPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "chromedriver.exe");
            if (!File.Exists(chromeDriverPath))
            {
                TestLogger.LogError($"ChromeDriver not found at: {chromeDriverPath}");
                throw new WebDriverException($"ChromeDriver not found at: {chromeDriverPath}");
            }

            try
            {
                return new ChromeDriver(service, chromeOptions, WebDriverConfig.CommandTimeout);
            }
            catch (Exception ex)
            {
                TestLogger.LogError($"Failed to create ChromeDriver: {ex.Message}");
                throw;
            }
        }

        private static int GetNextAvailablePort()
        {
            lock (_lock)
            {
                _lastUsedPort++;
                if (_lastUsedPort > 9999)
                {
                    _lastUsedPort = 9515;
                }
                return _lastUsedPort;
            }
        }

        private static IWebDriver CreateFirefoxDriver()
        {
            var firefoxOptions = new FirefoxOptions();
            firefoxOptions.AcceptInsecureCertificates = true;
            
            var service = FirefoxDriverService.CreateDefaultService();
            service.HostName = "127.0.0.1";
            
            return new FirefoxDriver(service, firefoxOptions, WebDriverConfig.CommandTimeout);
        }

        private static IWebDriver CreateEdgeDriver()
        {
            var edgeOptions = new EdgeOptions();
            edgeOptions.AcceptInsecureCertificates = true;
            
            var service = EdgeDriverService.CreateDefaultService();
            service.HostName = "127.0.0.1";
            
            return new EdgeDriver(service, edgeOptions, WebDriverConfig.CommandTimeout);
        }
        
        public static void CleanupDrivers()
        {
            lock (_lock)
            {
                foreach (var kvp in _activeDrivers.ToList())
                {
                    try
                    {
                        var driver = kvp.Value;
                        if (driver != null)
                        {
                            try { driver.Close(); } catch { }
                            try { driver.Quit(); } catch { }
                            try { driver.Dispose(); } catch { }
                        }
                    }
                    catch (Exception ex)
                    {
                        TestLogger.LogError($"Error cleaning up WebDriver: {ex.Message}");
                    }
                    finally
                    {
                        _activeDrivers.Remove(kvp.Key);
                    }
                }
            }

            KillExistingDriverProcesses();
        }

        public static void DisposeSingle(IWebDriver driver)
        {
            if (driver == null) return;

            lock (_lock)
            {
                var entry = _activeDrivers.FirstOrDefault(x => x.Value == driver);
                if (!string.IsNullOrEmpty(entry.Key))
                {
                    try
                    {
                        driver.Close();
                        driver.Quit();
                        driver.Dispose();
                    }
                    catch (Exception ex)
                    {
                        TestLogger.LogError($"Error disposing WebDriver: {ex.Message}");
                    }
                    finally
                    {
                        _activeDrivers.Remove(entry.Key);
                    }
                }
            }
        }
    }
}