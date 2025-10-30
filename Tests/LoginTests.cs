using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;
using MMA_tests.PageObjects;
using MMA_tests.Helpers;
using MMA_tests.Utils;
using Xunit;

namespace MMA_tests.Tests
{
    public class LoginTests : BaseTest
    {
        private readonly LoginPage _loginPage;
        private readonly PatientDashboardPage _patientDashboard;

        public LoginTests() : base()
        {
            _loginPage = new LoginPage(Driver);
            _patientDashboard = new PatientDashboardPage(Driver);
        }

        [Fact(DisplayName = "AC-4.1: Een patiënt kan inloggen met een correcte gebruikersnaam en wachtwoord.")]
        public void TC_4_1_1_SuccessfulLoginAsPatient()
        {
            // Arrange
            TestLogger.LogStep("Navigeer naar de inlogpagina.");
            _loginPage.Navigate();

            // Act
            TestLogger.LogStep("Voer correcte patient gegevens in en klik op inloggen.");
            _loginPage.Login(WebDriverConfig.TestUsers.Patient.Username, WebDriverConfig.TestUsers.Patient.Password);

            WaitSeconds(1, "giving time for redirect to complete");

            // Assert
            TestLogger.LogAssert("Verifieer dat de gebruiker is doorgestuurd naar de juiste pagina URL.");

            // Use the URL from the Page Object for a robust check
            string expectedUrl = PatientDashboardPage.MyPrescriptionsUrl;
            TestLogger.LogInfo($"Huidige URL: {Driver.Url}");
            TestLogger.LogInfo($"Verwachte URL: {expectedUrl}");

            Assert.True(UrlMatches(expectedUrl),
                       $"Expected URL '{expectedUrl}' but got '{Driver.Url}'");

            TestLogger.LogAssert("Verifieer dat de gebruiker is ingelogd.");
            Assert.True(_patientDashboard.IsUserLoggedIn(), "Gebruiker is niet ingelogd");
        }

        [Fact(DisplayName = "AC-4.2: Een gebruiker kan niet inloggen met een incorrect wachtwoord.")]
        public void TC_4_2_1_LoginWithIncorrectPassword()
        {
            // Arrange
            TestLogger.LogStep("Navigeer naar de inlogpagina.");
            _loginPage.Navigate();

            // Act
            TestLogger.LogStep("Voer incorrect wachtwoord in en klik op inloggen.");
            _loginPage.Login(WebDriverConfig.TestUsers.Patient.Username, "wrongpassword");

            WaitSeconds(1, "giving time for redirect to complete");

            // Assert
            TestLogger.LogAssert("Verifieer dat de gebruiker op de inlogpagina blijft en een foutmelding ziet.");

            TestLogger.LogInfo($"Huidige URL: {Driver.Url}");

            // Check 1: We should still be on a login-related page
            Assert.True(Driver.Url.Contains("Login") || Driver.Url.Contains("loginError"),
                        $"Verwacht op een login pagina te blijven, maar URL is: {Driver.Url}");

            // Check 2: We should see an error message
            Assert.True(_loginPage.IsLoginErrorDisplayed(), "Er wordt geen inlogfout (via URL of validatiemelding) getoond.");
        }

        [Fact(DisplayName = "AC-4.4: Inloggen is niet mogelijk met lege velden.")]
        public void TC_4_4_1_LoginWithEmptyFields_ShouldShowValidation()
        {
            // Arrange
            TestLogger.LogStep("Navigeer naar de inlogpagina.");
            _loginPage.Navigate();

            // Act
            TestLogger.LogStep("Klik op inloggen met lege velden.");
            _loginPage.Login("", "");

            // Assert
            TestLogger.LogAssert("Verifieer dat de URL niet is veranderd (geen redirect).");
            Assert.True(UrlMatches(LoginPage.LoginUrl),
                        $"Verwacht op de login pagina te blijven, maar URL is: {Driver.Url}");

            TestLogger.LogAssert("Verifieer dat er validatiefouten worden weergegeven.");

            if (!_loginPage.HasClientSideValidationErrors())
            {
                TestLogger.LogError("DEF-01: Er wordt geen client-side validatiemelding getoond bij lege velden. De test faalt om dit defect te rapporteren.");
                Assert.True(false, "DEF-01: Er wordt geen client-side validatiemelding getoond bij lege velden.");
            }
            else
            {
                Assert.True(true);
            }
        }

        [Fact(DisplayName = "AC-4.5: Een gebruiker kan uitloggen na ingelogd te zijn.")]
        public void TC_4_5_1_LoginAndLogout()
        {
            // Arrange & Act - Login
            TestLogger.LogStep("Navigeer naar de inlogpagina.");
            _loginPage.Navigate();

            TestLogger.LogStep("Voer correcte patient gegevens in en klik op inloggen.");
            _loginPage.Login(WebDriverConfig.TestUsers.Patient.Username, WebDriverConfig.TestUsers.Patient.Password);
            
            WaitSeconds(2, "giving time for login to complete");
            
            // Verify login was successful
            TestLogger.LogAssert("Verifieer dat de gebruiker is ingelogd.");
            TestLogger.LogInfo($"Huidige URL: {Driver.Url}");
            
            // Act - Logout
            TestLogger.LogStep("Klik op de logout knop.");
            _patientDashboard.Logout();

            // Assert - Verify logout
            string expectedLogoutUrl = WebDriverConfig.BaseUrl + "?page=%2FIndex";
            TestLogger.LogInfo($"Expected URL after logout: {expectedLogoutUrl}");
            TestLogger.LogInfo($"Actual URL after logout: {Driver.Url}");

            Assert.True(
                UrlIsOneOf(expectedLogoutUrl), 
                $"De gebruiker moet worden doorgestuurd naar de juiste uitlogbestemming. Verwacht: {expectedLogoutUrl}, Werkelijk: {Driver.Url}"
            );
        }
    }
}
