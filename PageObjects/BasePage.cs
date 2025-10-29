using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        protected IWebElement FindElementWithRetry(By locator, int maxRetries = 3)
        {
            int attempts = 0;
            while (attempts < maxRetries)
            {
                try
                {
                    return Wait.Until(d => d.FindElement(locator));
                }
                catch (WebDriverTimeoutException)
                {
                    attempts++;
                    if (attempts >= maxRetries)
                    {
                        TestLogger.LogError($"Element not found after {maxRetries} attempts: {locator}");
                        throw;
                    }
                    System.Threading.Thread.Sleep(1000); // Wait a bit before retrying
                    TestLogger.LogInfo($"Retrying element find: {locator} (Attempt {attempts})");
                }
            }
            
            // This should never be reached due to the throw above, but added for compiler satisfaction
            throw new WebDriverTimeoutException($"Element not found after {maxRetries} attempts: {locator}");
        }

        /// <summary>
        /// Checks if an element is displayed on the page
        /// </summary>
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

        protected bool IsElementPresent(By locator)
        {
            try
            {
                Driver.FindElement(locator);
                return true;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
            catch (Exception ex)
            {
                TestLogger.LogError($"Error checking if element is present: {locator}. Error: {ex.Message}");
                return false;
            }
        }
        
        protected bool ElementExists(By locator)
        {
            try
            {
                return Driver.FindElements(locator).Count > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        protected bool IsClickable(By locator)
        {
            try
            {
                var element = FindElement(locator);
                return element.Displayed && element.Enabled;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Common Navigation and UI Methods

        /// <summary>
        /// Navigates to the specified URL and waits for the page to load
        /// </summary>
        protected void NavigateTo(string url)
        {
            TestLogger.LogInfo($"Navigating to: {url}");
            Driver.Navigate().GoToUrl(url);
            System.Threading.Thread.Sleep(1000); // Short wait for page to start loading
        }

        /// <summary>
        /// Checks if user is logged in using common indicators
        /// </summary>
        public bool IsUserLoggedIn()
        {
            try
            {
                TestLogger.LogInfo($"Checking if user is logged in. Current URL: {Driver.Url}");
                
                // Check for logout button - primary indicator
                bool hasLogoutButton = IsElementDisplayed(CommonSelectors.LogoutButton) || 
                                      IsElementDisplayed(CommonSelectors.LogoutButtonFallback);
                
                // Check if we're on a login/logout page
                bool onLoginPage = Driver.Url.Contains("/Account/Login") || 
                                  Driver.Url.Contains("/Identity/Account/Login") ||
                                  Driver.Url.Contains("?loginError=True") ||
                                  Driver.Url.Contains("/Account/LogOut");
                
                // If we definitely found a logout button or we're not on a login page
                // and the URL indicates we're in a protected area
                bool isLoggedIn = hasLogoutButton || (!onLoginPage && IsInProtectedArea());
                
                TestLogger.LogInfo($"User is logged in: {isLoggedIn}");
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

        /// <summary>
        /// Gets username of logged in user
        /// </summary>
        public string GetLoggedInUsername()
        {
            try
            {
                return IsElementDisplayed(CommonSelectors.UserGreeting) ? 
                       FindElement(CommonSelectors.UserGreeting).Text : string.Empty;
            }
            catch (Exception ex)
            {
                TestLogger.LogError($"Error getting username: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Performs logout action, with fallbacks
        /// </summary>
        public void Logout()
        {
            TestLogger.LogInfo("Attempting to log out");
            
            try
            {
                // Try primary logout button
                if (IsElementDisplayed(CommonSelectors.LogoutButton))
                {
                    FindElement(CommonSelectors.LogoutButton).Click();
                    TestLogger.LogInfo("Clicked logout button");
                }
                // Try alternative logout button
                else if (IsElementDisplayed(CommonSelectors.LogoutButtonFallback))
                {
                    FindElement(CommonSelectors.LogoutButtonFallback).Click();
                    TestLogger.LogInfo("Clicked fallback logout button");
                }
                // Direct URL navigation as fallback
                else
                {
                    TestLogger.LogInfo("No logout button found, using direct URL navigation");
                    Driver.Navigate().GoToUrl(WebDriverConfig.BaseUrl + "Account/LogOut");
                }
                
                // Wait for logout to process
                System.Threading.Thread.Sleep(2000);
                
                TestLogger.LogInfo($"URL after logout: {Driver.Url}");
            }
            catch (Exception ex)
            {
                TestLogger.LogError($"Error during logout: {ex.Message}");
                
                // Try direct navigation as a final fallback
                try
                {
                    Driver.Navigate().GoToUrl(WebDriverConfig.BaseUrl + "Account/LogOut");
                    System.Threading.Thread.Sleep(1000);
                }
                catch { }
            }
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
        /// Clicks a button that looks like a submit button
        /// </summary>
        protected bool ClickSubmitButton(string context = "")
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
        protected bool ClickSearchButton(string context = "")
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
        protected bool ClickConfirmButton(string context = "")
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

        /// <summary>
        /// Enter text into input field with multiple fallback strategies
        /// </summary>
        protected bool EnterText(string fieldIdentifier, string text)
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

        #endregion

        #region Validation Methods

        /// <summary>
        /// Checks for validation errors on the page
        /// </summary>
        public bool HasValidationErrors()
        {
            // Check for standard validation errors
            bool hasErrorMessages = 
                IsElementDisplayed(CommonSelectors.ValidationSummary) ||
                IsElementDisplayed(CommonSelectors.ValidationErrors) ||
                IsElementDisplayed(CommonSelectors.ErrorMessage);
            
            // Check if form fields have validation state
            bool hasInvalidFields = 
                IsElementDisplayed(CommonSelectors.InputValidationErrors) ||
                IsElementDisplayed(CommonSelectors.InvalidFields);
            
            return hasErrorMessages || hasInvalidFields;
        }

        /// <summary>
        /// Gets validation error messages from the page
        /// </summary>
        public string GetValidationErrors()
        {
            StringBuilder errors = new StringBuilder();
            
            try
            {
                // Check validation summary
                if (IsElementDisplayed(CommonSelectors.ValidationSummary))
                {
                    errors.AppendLine(FindElement(CommonSelectors.ValidationSummary).Text);
                }
                
                // Check individual field errors
                var fieldErrors = Driver.FindElements(CommonSelectors.ValidationErrors);
                foreach (var error in fieldErrors)
                {
                    errors.AppendLine(error.Text);
                }
                
                // Check general error message
                if (IsElementDisplayed(CommonSelectors.ErrorMessage))
                {
                    errors.AppendLine(FindElement(CommonSelectors.ErrorMessage).Text);
                }
            }
            catch (Exception ex)
            {
                TestLogger.LogError($"Error getting validation messages: {ex.Message}");
            }
            
            return errors.ToString().Trim();
        }

        #endregion

        #region Debugging Methods

        /// <summary>
        /// Takes a screenshot for debugging purposes
        /// </summary>
        protected void TakeScreenshot(string screenshotName)
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
        protected void WaitSeconds(double seconds, string reason = null)
        {
            if (!string.IsNullOrEmpty(reason))
            {
                TestLogger.LogInfo($"Waiting {seconds} seconds: {reason}");
            }
            
            System.Threading.Thread.Sleep((int)(seconds * 1000));
        }

        #endregion
    }
}