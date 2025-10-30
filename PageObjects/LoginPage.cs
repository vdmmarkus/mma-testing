using System;
using OpenQA.Selenium;
using MMA_tests.Utils;
using MMA_tests.Helpers;
using System.Threading;
using System.Linq;

namespace MMA_tests.PageObjects
{
    public class LoginPage : BasePage
    {
        private readonly By _usernameField = By.Id("Username");
        private readonly By _passwordField = By.Id("Password");
        private readonly By _loginButton = By.CssSelector("button[type='submit']");
        private readonly By _validationSummary = By.CssSelector(".validation-summary-errors");
        
        public static string LoginUrl => WebDriverConfig.BaseUrl + "Account/Login";
        
        public LoginPage(IWebDriver driver) : base(driver) { }

        public void Navigate()
        {
            TestLogger.LogInfo("Navigating to login page");
            Driver.Navigate().GoToUrl(LoginUrl);
            WaitSeconds(1, "waiting for login page to load");
        }

        public bool IsOnLoginPage()
        {
            try
            {
                return Driver.Url.Contains("/Account/Login") || 
                       Driver.Url.Contains("/Identity/Account/Login");
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void EnterCredentials(string username, string password)
        {
            TestLogger.LogInfo($"Entering credentials for user: {username}");
            
            try
            {
                var usernameInput = FindElement(_usernameField);
                var passwordInput = FindElement(_passwordField);

                usernameInput.Clear();
                usernameInput.SendKeys(username);

                passwordInput.Clear();
                passwordInput.SendKeys(password);
            }
            catch (Exception ex)
            {
                TestLogger.LogError($"Failed to enter credentials: {ex.Message}");
                throw;
            }
        }

        public void Login(string username, string password)
        {
            TestLogger.LogInfo($"Attempting login with username: {username}");
            
            try
            {
                EnterCredentials(username, password);

                TakeScreenshot("before-login-click");
                
                // Try to click submit button
                bool clicked = ClickSubmitButton("on login form");
                
                if (!clicked)
                {
                    TestLogger.LogError("Could not find submit button. Login will likely fail.");
                }
                
                // Wait for redirect or error
                WaitSeconds(2, "waiting for login to complete");
                
                // Log the result
                if (IsOnLoginPage())
                {
                    TestLogger.LogError("Still on login page after attempt. Login may have failed.");
                    if (HasLoginErrors())
                    {
                        TestLogger.LogError($"Login errors found: {GetLoginErrors()}");
                    }
                }
                else
                {
                    TestLogger.LogInfo("Successfully navigated away from login page");
                }
            }
            catch (Exception ex)
            {
                TestLogger.LogError($"Login failed: {ex.Message}");
                throw;
            }
        }

        public bool IsLoginErrorDisplayed()
        {
            return HasLoginErrors();
        }

        public bool HasLoginErrors()
        {
            return HasValidationErrors() ||
                   Driver.Url.Contains("?loginError=True") ||
                   Driver.Url.Contains("error=true");
        }

        public bool HasClientSideValidationErrors()
        {
            return IsElementDisplayed(By.CssSelector(".field-validation-error")) ||
                   IsElementDisplayed(By.CssSelector(".validation-summary-errors"));
        }

        public string GetLoginErrors()
        {
            TestLogger.LogInfo("Checking for login error messages");
            
            try
            {
                // Check for validation errors
                bool hasErrorMessages = HasValidationErrors();
                
                // Check URL parameters
                bool hasLoginError = Driver.Url.Contains("?loginError=True");
                bool hasGenericError = Driver.Url.Contains("error=true");
                
                if (!hasErrorMessages && !hasLoginError && !hasGenericError)
                {
                    return string.Empty;
                }
                
                var errors = GetValidationErrors();
                return errors.Any() ? string.Join(Environment.NewLine, errors) : "An unknown error occurred during login.";
            }
            catch (Exception ex)
            {
                TestLogger.LogError($"Error getting login errors: {ex.Message}");
                return "Could not retrieve error messages.";
            }
        }
    }
}
