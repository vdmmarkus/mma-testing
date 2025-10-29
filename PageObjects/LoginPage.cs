using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;
using MMA_tests.Helpers;
using MMA_tests.Utils;

namespace MMA_tests.PageObjects
{
    public class LoginPage : BasePage
    {
        public static string LoginUrl => WebDriverConfig.BaseUrl + "Account/Login";

        // Locators for login form elements
        private readonly By _usernameInput = By.Id("Input_Email");
        private readonly By _usernameInputAlt = By.Id("Username");
        private readonly By _usernameInputByName = By.Name("Username");
        
        private readonly By _passwordInput = By.Id("Input_Password");
        private readonly By _passwordInputAlt = By.Id("Password");
        private readonly By _passwordInputByName = By.Name("Password");
        
        // Login button is already covered by SubmitButton in CommonSelectors

        public LoginPage(IWebDriver driver) : base(driver) { }

        /// <summary>
        /// Navigates to the login page
        /// </summary>
        public void Navigate() 
        {
            NavigateTo(LoginUrl);
            WaitSeconds(1, "waiting for login page to load");
            TestLogger.LogInfo($"Current URL after navigation: {Driver.Url}");
        }

        /// <summary>
        /// Finds username input field with multiple fallback strategies
        /// </summary>
        private IWebElement GetUsernameElement()
        {
            // Try all possible username fields
            By[] usernameCandidates = new[] { 
                _usernameInput, _usernameInputAlt, _usernameInputByName,
                By.Id("Email"), By.Name("Email")
            };
            
            foreach (var locator in usernameCandidates)
            {
                try
                {
                    if (IsElementDisplayed(locator))
                    {
                        return FindElement(locator);
                    }
                }
                catch { }
            }
            
            throw new NoSuchElementException("Could not find username field");
        }
        
        /// <summary>
        /// Finds password input field with multiple fallback strategies
        /// </summary>
        private IWebElement GetPasswordElement()
        {
            // Try all possible password fields
            By[] passwordCandidates = new[] { 
                _passwordInput, _passwordInputAlt, _passwordInputByName
            };
            
            foreach (var locator in passwordCandidates)
            {
                try
                {
                    if (IsElementDisplayed(locator))
                    {
                        return FindElement(locator);
                    }
                }
                catch { }
            }
            
            throw new NoSuchElementException("Could not find password field");
        }

        /// <summary>
        /// Logs in with the given username and password
        /// </summary>
        public void Login(string username, string password)
        {
            TestLogger.LogInfo($"Attempting to login with username: {username}");
            
            try
            {
                // Get elements using our more robust methods
                var usernameElement = GetUsernameElement();
                var passwordElement = GetPasswordElement();
                
                // Clear existing text first
                usernameElement.Clear();
                usernameElement.SendKeys(username);
                TestLogger.LogInfo("Username entered");
                
                passwordElement.Clear();
                passwordElement.SendKeys(password);
                TestLogger.LogInfo("Password entered");
                
                // Take a screenshot before clicking login (useful for debugging)
                TakeScreenshot("before-login-click");
                
                // Use common submit button click method
                bool clicked = ClickSubmitButton("on login form");
                
                if (!clicked)
                {
                    TestLogger.LogWarning("Could not find standard submit button, trying to submit form directly");
                    
                    // Try to submit the form directly
                    try
                    {
                        var form = Driver.FindElement(By.TagName("form"));
                        form.Submit();
                        TestLogger.LogInfo("Submitted login form directly");
                    }
                    catch (Exception ex)
                    {
                        TestLogger.LogError($"Failed to submit form: {ex.Message}");
                        throw;
                    }
                }
                
                // Allow a moment for the login to process
                WaitSeconds(2, "waiting for login to complete");
                
                TestLogger.LogInfo($"Current URL after login attempt: {Driver.Url}");
            }
            catch (Exception ex)
            {
                TestLogger.LogError($"Exception during login: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Checks if there are client-side validation errors on the login form
        /// </summary>
        public bool HasClientSideValidationErrors()
        {
            return HasValidationErrors() || 
                   Driver.PageSource.ToLower().Contains("veld is verplicht") ||
                   Driver.PageSource.ToLower().Contains("field is required");
        }

        /// <summary>
        /// Gets error messages from the login form
        /// </summary>
        public string GetErrorMessages()
        {
            return GetValidationErrors();
        }
    }
}