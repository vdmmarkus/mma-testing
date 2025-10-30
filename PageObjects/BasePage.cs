using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MMA_tests.Utils;
using MMA_tests.Helpers;

namespace MMA_tests.PageObjects
{
    public class BasePage
    {
        protected IWebDriver Driver { get; private set; }
        protected WebDriverWait Wait { get; private set; }

        public BasePage(IWebDriver driver)
        {
            Driver = driver;
            Wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
        }

        #region Element Interaction Methods

        protected IWebElement FindElement(By locator)
        {
            try
            {
                return Wait.Until(d => d.FindElement(locator));
            }
            catch (WebDriverTimeoutException ex)
            {
                TestLogger.LogError($"Element not found: {locator}. Error: {ex.Message}");
                throw;
            }
        }

        // Method summary: Checks if an element is displayed on the page
        public bool IsElementDisplayed(By locator)
        {
            try
            {
                return Driver.FindElement(locator).Displayed;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
            catch (Exception ex)
            {
                TestLogger.LogError($"Error checking if element is displayed: {locator}. Error: {ex.Message}");
                return false;
            }
        }

        // ... (other element interaction methods like FindElementWithRetry, IsElementPresent, etc. remain unchanged)

        #endregion

        #region Common Navigation and UI Methods

        // Method summary: Navigates to the specified URL
        protected void NavigateTo(string url)
        {
            TestLogger.LogInfo($"Navigating to: {url}");
            Driver.Navigate().GoToUrl(url);
        }

        // Method summary: Checks if user is logged in using common indicators
        public bool IsUserLoggedIn()
        {
            try
            {
                TestLogger.LogInfo($"Checking if user is logged in. Current URL: {Driver.Url}");
                
                // Check if we're on a protected URL
                bool inProtectedArea = IsInProtectedArea();
                TestLogger.LogInfo($"In protected area: {inProtectedArea}");

                // Check for user-specific elements
                bool hasUserElements = IsElementDisplayed(CommonSelectors.UserGreeting);
                TestLogger.LogInfo($"Has user elements: {hasUserElements}");

                // Check for logout options
                bool hasLogoutButton = IsElementDisplayed(CommonSelectors.LogoutButton) || 
                                     IsElementDisplayed(CommonSelectors.LogoutButtonFallback) ||
                                     IsElementDisplayed(By.CssSelector("form[action*='Logout']"));
                TestLogger.LogInfo($"Has logout button: {hasLogoutButton}");

                // Not on login/error pages
                bool onLoginPage = Driver.Url.Contains("/Account/Login") || 
                                 Driver.Url.Contains("/Identity/Account/Login") ||
                                 Driver.Url.Contains("?loginError=True") ||
                                 Driver.Url.Contains("/Account/LogOut");
                TestLogger.LogInfo($"On login page: {onLoginPage}");

                // Consider logged in if in protected area or has user elements, and not on login page
                bool isLoggedIn = (inProtectedArea || hasUserElements || hasLogoutButton) && !onLoginPage;
                TestLogger.LogInfo($"Final login status: {isLoggedIn}");

                return isLoggedIn;
            }
            catch (Exception ex)
            {
                TestLogger.LogError($"Error checking login status: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if current URL indicates we're in a protected area
        /// </summary>
        protected virtual bool IsInProtectedArea()
        {
            // Base implementation checks common protected URLs
            // Page objects can override to provide specific URL checks
            string currentUrl = Driver.Url.ToLower();
            return currentUrl.Contains("/prescriptions/") ||
                   currentUrl.Contains("/patientinfo") ||
                   currentUrl.Contains("/dashboard");
        }

        // ... (other navigation methods like GetLoggedInUsername, Logout remain unchanged)

        #endregion

        #region Form Interaction Helpers

        /// <summary>
        /// Clicks a button that looks like a submit button
        /// </summary>
        public bool ClickSubmitButton(string context = "")
        {
            var preferredLocators = new[] { 
                CommonSelectors.SubmitButton, 
                CommonSelectors.SubmitInput 
            };
            
            return ClickButton(preferredLocators, CommonSelectors.SubmitButtonTexts, context);
        }
        
        /// <summary>
        /// Clicks a button that looks like a search button
        /// </summary>
        public bool ClickSearchButton(string context = "")
        {
            var preferredLocators = new[] { 
                By.Id("searchButton"), 
                By.Name("search"),
                By.CssSelector("button[type='search']")
            };
            
            return ClickButton(preferredLocators, CommonSelectors.SearchButtonTexts, context);
        }

        /// <summary>
        /// Clicks a button or link that looks like a confirm/select button
        /// </summary>
        public bool ClickConfirmButton(string context = "")
        {
            // Try buttons first
            var preferredLocators = new[] { 
                By.Id("confirmButton"), 
                By.Id("selectButton"),
                By.Id("viewButton"),
                By.Id("detailsButton")
            };
            
            if (ClickButton(preferredLocators, CommonSelectors.ConfirmButtonTexts, context))
            {
                return true;
            }
            
            // Try links with confirmation text
            try
            {
                var links = Driver.FindElements(By.TagName("a"));
                foreach (var link in links)
                {
                    try
                    {
                        string linkText = link.Text.ToLower();
                        if (CommonSelectors.ConfirmButtonTexts.Any(text => linkText.Contains(text.ToLower())))
                        {
                            link.Click();
                            TestLogger.LogInfo($"Clicked link with text: '{link.Text}' {context}");
                            return true;
                        }
                        
                        // Try by href if text doesn't match
                        string href = link.GetAttribute("href")?.ToLower() ?? "";
                        if (href.Contains("details") || href.Contains("view") || href.Contains("select"))
                        {
                            link.Click();
                            TestLogger.LogInfo($"Clicked link with href: '{href}' {context}");
                            return true;
                        }
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }
            
            return false;
        }

        /// <summary>
        /// Enter text into input field with multiple fallback strategies
        /// </summary>
        public bool EnterText(string fieldIdentifier, string text)
        {
            try
            {
                var element = FindInputField(fieldIdentifier);
                element.Clear();
                element.SendKeys(text);
                TestLogger.LogInfo($"Entered text '{text}' into field: {fieldIdentifier}");
                return true;
            }
            catch (Exception ex)
            {
                TestLogger.LogError($"Failed to enter text in {fieldIdentifier}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Takes a screenshot for debugging purposes
        /// </summary>
        public void TakeScreenshot(string screenshotName)
        {
            try
            {
                // Create screenshot
                Screenshot screenshot = ((ITakesScreenshot)Driver).GetScreenshot();
                
                // Generate a unique filename with timestamp
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string filename = $"{screenshotName}_{timestamp}.png";
                
                // Ensure directory exists
                var screenshotDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Screenshots");
                if (!System.IO.Directory.Exists(screenshotDir))
                {
                    System.IO.Directory.CreateDirectory(screenshotDir);
                }
                
                // Save the screenshot
                string screenshotPath = System.IO.Path.Combine(screenshotDir, filename);
                screenshot.SaveAsFile(screenshotPath);
                
                TestLogger.LogInfo($"Screenshot saved: {filename}");
            }
            catch (Exception ex)
            {
                TestLogger.LogError($"Error taking screenshot: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Waits for the specified number of seconds
        /// </summary>
        public void WaitSeconds(double seconds, string reason = null)
        {
            if (!string.IsNullOrEmpty(reason))
            {
                TestLogger.LogInfo($"Waiting {seconds} seconds: {reason}");
            }
            
            System.Threading.Thread.Sleep((int)(seconds * 1000));
        }

        #endregion

        #region Validation Methods

        // Method summary: Checks for validation errors on the page
        public bool HasValidationErrors()
        {
            try
            {
                // Look for a common validation error container
                var errorContainer = Driver.FindElement(By.Id("validationSummary"));
                return errorContainer.Displayed && errorContainer.FindElements(By.TagName("li")).Count > 0;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
            catch (Exception ex)
            {
                TestLogger.LogError($"Error checking for validation errors: {ex.Message}");
                return false;
            }
        }

        // Method summary: Retrieves validation error messages from the page
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();
            
            try
            {
                // Common error container
                var errorContainer = Driver.FindElement(By.Id("validationSummary"));
                var errorMessages = errorContainer.FindElements(By.TagName("li"));
                
                foreach (var message in errorMessages)
                {
                    errors.Add(message.Text);
                }
            }
            catch (NoSuchElementException)
            {
                // No validation errors found
            }
            catch (Exception ex)
            {
                TestLogger.LogError($"Error retrieving validation errors: {ex.Message}");
            }
            
            return errors;
        }

        #endregion

        #region Form Interaction Helpers

        /// <summary>
        /// Finds and clicks a button that looks like a submit button
        /// </summary>
        protected bool ClickButton(By[] preferredLocators, string[] buttonTexts, string context = "")
        {
            // Try specific locators first
            foreach (var locator in preferredLocators)
            {
                try
                {
                    if (IsElementDisplayed(locator))
                    {
                        FindElement(locator).Click();
                        TestLogger.LogInfo($"Clicked button using locator {locator} {context}");
                        return true;
                    }
                }
                catch (Exception) { }
            }
            
            // Try to find by text
            try
            {
                var buttons = Driver.FindElements(By.TagName("button"));
                foreach (var button in buttons)
                {
                    try
                    {
                        string buttonText = button.Text.ToLower();
                        if (buttonTexts.Any(text => buttonText.Contains(text.ToLower())))
                        {
                            button.Click();
                            TestLogger.LogInfo($"Clicked button with text: '{button.Text}' {context}");
                            return true;
                        }
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }
            
            // Try input buttons as last resort
            try
            {
                var inputs = Driver.FindElements(By.CssSelector("input[type='submit'], input[type='button']"));
                foreach (var input in inputs)
                {
                    try
                    {
                        string value = (input.GetAttribute("value") ?? "").ToLower();
                        if (buttonTexts.Any(text => value.Contains(text.ToLower())))
                        {
                            input.Click();
                            TestLogger.LogInfo($"Clicked input button with value: '{value}' {context}");
                            return true;
                        }
                    }
                    catch (Exception) { }
                }
                
                // If we didn't find a matching input, just try the first submit input
                var submitInputs = Driver.FindElements(By.CssSelector("input[type='submit']"));
                if (submitInputs.Count > 0)
                {
                    submitInputs[0].Click();
                    TestLogger.LogInfo($"Clicked first submit input {context}");
                    return true;
                }
            }
            catch (Exception) { }
            
            return false;
        }

        /// <summary>
        /// Finds an input field by id, name, or placeholder text
        /// </summary>
        protected IWebElement FindInputField(string fieldIdentifier)
        {
            // Try direct locators first
            try { return Driver.FindElement(By.Id(fieldIdentifier)); } 
            catch { }
            
            try { return Driver.FindElement(By.Name(fieldIdentifier)); } 
            catch { }
            
            // Try common variations
            string[] variations = new[] 
            {
                fieldIdentifier,
                fieldIdentifier.ToLower(),
                fieldIdentifier.ToUpper(),
                char.ToUpper(fieldIdentifier[0]) + fieldIdentifier.Substring(1),
                fieldIdentifier + "Input"
            };
            
            foreach (var variation in variations)
            {
                try { return Driver.FindElement(By.Id(variation)); } 
                catch { }
                
                try { return Driver.FindElement(By.Name(variation)); } 
                catch { }
            }
            
            // Generic search as fallback
            var inputs = Driver.FindElements(By.TagName("input"));
            foreach (var input in inputs)
            {
                try
                {
                    string id = input.GetAttribute("id")?.ToLower() ?? "";
                    string name = input.GetAttribute("name")?.ToLower() ?? "";
                    string placeholder = input.GetAttribute("placeholder")?.ToLower() ?? "";
                    
                    if (id.Contains(fieldIdentifier.ToLower()) || 
                        name.Contains(fieldIdentifier.ToLower()) || 
                        placeholder.Contains(fieldIdentifier.ToLower()))
                    {
                        return input;
                    }
                }
                catch { }
            }
            
            throw new NoSuchElementException($"Could not find input field matching: {fieldIdentifier}");
        }

        #endregion

        #region Authentication Methods

        public virtual void Logout()
        {
            TestLogger.LogInfo("Attempting to log out");
            
            try
            {
                // Take a screenshot before attempting logout
                TakeScreenshot("before-logout");
                TestLogger.LogInfo($"Current URL before logout: {Driver.Url}");

                // Try standard logout button first
                if (IsElementDisplayed(CommonSelectors.LogoutButton))
                {
                    var logoutElement = FindElement(CommonSelectors.LogoutButton);
                    TestLogger.LogInfo($"Found logout element: {logoutElement.TagName} with text: {logoutElement.Text}");
                    logoutElement.Click();
                    TestLogger.LogInfo("Clicked primary logout button");
                }
                // Try alternative logout methods
                else if (IsElementDisplayed(CommonSelectors.LogoutButtonFallback))
                {
                    var fallbackElement = FindElement(CommonSelectors.LogoutButtonFallback);
                    TestLogger.LogInfo($"Found fallback logout element: {fallbackElement.TagName} with text: {fallbackElement.Text}");
                    fallbackElement.Click();
                    TestLogger.LogInfo("Clicked fallback logout button");
                }
                // Try to find and submit a logout form
                else if (IsElementDisplayed(By.CssSelector("form[action*='Logout']")))
                {
                    var logoutForm = Driver.FindElement(By.CssSelector("form[action*='Logout']"));
                    var submitButton = logoutForm.FindElement(By.CssSelector("button[type='submit']"));
                    submitButton.Click();
                    TestLogger.LogInfo("Submitted logout form");
                }
                // Direct URL navigation as last resort
                else
                {
                    TestLogger.LogInfo("No logout button found, using direct URL navigation");
                    Driver.Navigate().GoToUrl(WebDriverConfig.BaseUrl + "Account/LogOut");
                }
                
                // Wait for logout to process
                WaitSeconds(2, "waiting for logout to complete");
                
                // Take a screenshot after logout attempt
                TakeScreenshot("after-logout");
                TestLogger.LogInfo($"URL after logout: {Driver.Url}");

                // Verify logout was successful
                if (IsUserLoggedIn())
                {
                    TestLogger.LogWarning("User appears to still be logged in after logout attempt");
                    throw new Exception("Logout may have failed - user still appears to be logged in");
                }
            }
            catch (Exception ex)
            {
                TestLogger.LogError($"Error during logout: {ex.Message}");
                
                // Try direct navigation as a final fallback
                try
                {
                    Driver.Navigate().GoToUrl(WebDriverConfig.BaseUrl + "Account/LogOut");
                    WaitSeconds(1, "waiting after fallback logout");
                    
                    if (IsUserLoggedIn())
                    {
                        throw new Exception("Failed to log out using all available methods");
                    }
                }
                catch (Exception finalEx)
                {
                    TestLogger.LogError($"Final logout attempt failed: {finalEx.Message}");
                    throw;
                }
            }
        }

        #endregion

        #region Debugging Helpers

        /// <summary>
        /// Dumps the current page HTML source to a file for debugging
        /// </summary>
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

        #endregion
    }
}