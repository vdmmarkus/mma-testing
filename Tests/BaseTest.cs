using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;
using MMA_tests.Helpers;
using MMA_tests.Utils;
using Xunit;
using System.IO;
using System.Reflection;

namespace MMA_tests.Tests
{
    public abstract class BaseTest : IDisposable
    {
        protected readonly IWebDriver Driver;
        protected string TestName { get; private set; }
        private bool _isDisposed;

        protected BaseTest()
        {
            TestName = GetType().Name;
            var methodName = TestContext.CurrentContext?.TestMethod;
            var acceptanceCriteriaAttr = GetAcceptanceCriteriaInfo();

            TestRunContext.StartTest(
                methodName ?? TestName,
                acceptanceCriteriaAttr.Item1,
                acceptanceCriteriaAttr.Item2);

            TestLogger.LogInfo($"Initializing test {TestName}...");
            
            try
            {
                Driver = WebDriverFactory.CreateDriver();
                TestLogger.LogInfo("Test initialization successful");
            }
            catch (Exception ex)
            {
                TestLogger.LogError($"Failed to initialize test: {ex.Message}");
                TestRunContext.EndTest(false, ex.Message);
                throw;
            }
        }

        private (string, string) GetAcceptanceCriteriaInfo()
        {
            var testMethod = GetType().GetMethods()
                .FirstOrDefault(m => m.GetCustomAttribute<FactAttribute>() != null);

            if (testMethod != null)
            {
                var factAttr = testMethod.GetCustomAttribute<FactAttribute>();
                if (factAttr != null && !string.IsNullOrEmpty(factAttr.DisplayName))
                {
                    var parts = factAttr.DisplayName.Split(':', 2);
                    if (parts.Length == 2)
                    {
                        return (parts[0].Trim(), parts[1].Trim());
                    }
                }
            }

            return ("Unknown", "No criteria description available");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                TestLogger.LogInfo($"Cleaning up test {TestName}...");
                
                try
                {
                    // Take a screenshot before closing the browser
                    try
                    {
                        if (Driver != null)
                        {
                            var screenshotPath = TakeScreenshot($"{TestName}_Final");
                            if (!string.IsNullOrEmpty(screenshotPath))
                            {
                                TestRunContext.SetScreenshotPath(screenshotPath);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        TestLogger.LogError($"Failed to take final screenshot: {ex.Message}");
                    }

                    // Dispose of the driver properly
                    if (Driver != null)
                    {
                        WebDriverFactory.DisposeSingle(Driver);
                    }
                    
                    // Generate test report
                    var outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestReports");
                    TestReporter.GenerateReport(outputDir);
                    
                    TestLogger.LogInfo("Test cleanup successful");
                    TestRunContext.EndTest(true);
                }
                catch (Exception ex)
                {
                    TestLogger.LogError($"Error during test cleanup: {ex.Message}");
                    TestRunContext.EndTest(false, ex.Message);
                }
            }

            _isDisposed = true;
        }

        /// <summary>
        /// Takes a screenshot for debugging purposes
        /// </summary>
        protected string TakeScreenshot(string screenshotName)
        {
            try
            {
                if (Driver is ITakesScreenshot ts)
                {
                    var screenshot = ts.GetScreenshot();
                    var fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{screenshotName}.png";
                    
                    // Create Screenshots directory in the test project
                    var directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Screenshots");
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    
                    var filePath = Path.Combine(directory, fileName);
                    screenshot.SaveAsFile(filePath);
                    TestLogger.LogInfo($"Screenshot saved to {filePath}");
                    return filePath;
                }
            }
            catch (Exception ex)
            {
                TestLogger.LogError($"Failed to take screenshot: {ex.Message}");
            }
            return null;
        }
        
        protected void DumpPageSource(string identifier)
        {
            try
            {
                var pageSource = Driver.PageSource;
                var fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{identifier}.html";
                
                // Create PageSources directory in the test project
                var directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PageSources");
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                var filePath = Path.Combine(directory, fileName);
                File.WriteAllText(filePath, pageSource);
                
                TestLogger.LogInfo($"Page source saved to {filePath}");
            }
            catch (Exception ex)
            {
                TestLogger.LogError($"Failed to dump page source: {ex.Message}");
            }
        }
        
        protected void WaitSeconds(double seconds, string reason = null)
        {
            if (!string.IsNullOrEmpty(reason))
            {
                TestLogger.LogInfo($"Waiting {seconds} seconds: {reason}");
            }
            
            System.Threading.Thread.Sleep((int)(seconds * 1000));
        }

        protected bool UrlMatches(string expectedUrl)
        {
            string currentUrl = Driver.Url.TrimEnd('/');
            expectedUrl = expectedUrl.TrimEnd('/');
            
            return string.Equals(currentUrl, expectedUrl, StringComparison.OrdinalIgnoreCase);
        }
        
        protected bool UrlIsOneOf(params string[] expectedUrls)
        {
            string currentUrl = Driver.Url.TrimEnd('/');

            // Add common variations of the logout destination URL
            var validUrls = new List<string>(expectedUrls);
            validUrls.AddRange(new[]
            {
                WebDriverConfig.BaseUrl.TrimEnd('/') + "?page=%2FIndex",
                WebDriverConfig.BaseUrl.TrimEnd('/') + "/?page=%2FIndex"
            });
            
            foreach (var expectedUrl in validUrls)
            {
                if (string.Equals(currentUrl, expectedUrl.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        protected bool UrlContainsOneOf(params string[] urlParts)
        {
            foreach (var part in urlParts)
            {
                if (Driver.Url.Contains(part))
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}