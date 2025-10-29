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
            
            string expectedUrl = "https://localhost:7058/Prescriptions/MyPrescriptions";
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
            TestLogger.LogAssert("Verifieer dat de gebruiker op de juiste error URL terechtkomt.");
            
            string expectedUrl = "https://localhost:7058/?loginError=True&page=%2FIndex";
            TestLogger.LogInfo($"Huidige URL: {Driver.Url}");
            TestLogger.LogInfo($"Verwachte URL: {expectedUrl}");
            
            Assert.Equal(expectedUrl, Driver.Url, StringComparer.OrdinalIgnoreCase);
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
            Assert.Contains("/Account/Login", Driver.Url);
            
            TestLogger.LogAssert("Verifieer dat er validatiefouten worden weergegeven.");
            
            if (!_loginPage.HasClientSideValidationErrors())
            {
                TestLogger.LogError("DEF-01: Er wordt geen client-side validatiemelding getoond bij lege velden. De test faalt om dit defect te rapporteren.");
                Assert.True(false, "DEF-01: Er wordt geen client-side validatiemelding getoond bij lege velden.");
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
            
            string expectedLoginUrl = "https://localhost:7058/Prescriptions/MyPrescriptions";
            TestLogger.LogInfo($"Huidige URL: {Driver.Url}");
            
            Assert.True(UrlMatches(expectedLoginUrl), 
                       $"Expected login redirect to '{expectedLoginUrl}' but got '{Driver.Url}'");
            
            // Act - Logout
            TestLogger.LogStep("Klik op de logout knop.");
            _patientDashboard.Logout();
            
            WaitSeconds(1, "giving time for logout redirect to complete");
            
            // Assert
            TestLogger.LogAssert("Verifieer dat de gebruiker is uitgelogd en op een logout-pagina is.");
            TestLogger.LogInfo($"Huidige URL na uitloggen: {Driver.Url}");
            
            // Valid logout destinations
            string[] validLogoutUrls = new[] {
                "https://localhost:7058/?page=%2FIndex",
                "https://localhost:7058/Account/LogOut"
            };
            
            bool isAtValidLogoutDestination = 
                UrlIsOneOf(validLogoutUrls) || 
                UrlContainsOneOf("/Account/Login", "/Identity/Account/Login");
            
            Assert.True(isAtValidLogoutDestination, 
                       $"Niet op een geldige uitlogbestemming. Huidige URL: {Driver.Url}");
            
            // Verify user is actually logged out
            Assert.False(_patientDashboard.IsUserLoggedIn(), 
                        "Gebruiker lijkt nog steeds ingelogd te zijn na uitloggen");
        }
    }
}