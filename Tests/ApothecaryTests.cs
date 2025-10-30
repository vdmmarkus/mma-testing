using System;
using MMA_tests.PageObjects;
using MMA_tests.Utils;
using Xunit;

namespace MMA_tests.Tests
{
    public class ApothecaryTests : BaseTest
    {
        private readonly LoginPage _loginPage;
        private readonly ApothecaryDashboardPage _apothecaryDashboard;

        // Constants
        private const string ApothecaryUsername = "apothecary";
        private const string ApothecaryPassword = "apothecary1";
        private const string PatientSearchTerm = "patient";

        public ApothecaryTests() : base()
        {
            _loginPage = new LoginPage(Driver);
            _apothecaryDashboard = new ApothecaryDashboardPage(Driver);
        }

        [Fact(DisplayName = "AC-12.1: Een apotheker kan de voorgeschreven medicatie van een patiënt inzien.")]
        public void TC_12_1_1_ApothecaryCanViewPatientMedication()
        {
            // ARRANGE - Log in as an apothecary
            TestLogger.LogStep("Navigeer naar de loginpagina en log in als apotheker.");
            _loginPage.Navigate();
            _loginPage.Login(ApothecaryUsername, ApothecaryPassword);

            // Wait for the page to process login and redirect
            WaitSeconds(2, "wachten op login en doorverwijzing");
            TakeScreenshot("AfterApothecaryLogin");

            // ASSERT - Verify login was successful
            Assert.True(_apothecaryDashboard.IsUserLoggedIn(), "De apotheker moet succesvol kunnen inloggen.");
            Assert.False(UrlContainsOneOf("Account/Login"), "Na een succesvolle login mag de gebruiker niet op de loginpagina zijn.");

            // ACT - Navigate to patient info and search for a patient
            TestLogger.LogStep("Navigeer naar de patiëntinformatiepagina.");
            _apothecaryDashboard.NavigateToPatientInfo();

            // ASSERT - Verify navigation was successful
            Assert.True(UrlContainsOneOf("PatientInfo"), "De apotheker moet naar de patiëntinformatiepagina kunnen navigeren.");

            // ACT - Search for and select a patient
            TestLogger.LogStep($"Zoek naar patiënt met zoekterm '{PatientSearchTerm}'.");
            _apothecaryDashboard.SearchForPatient(PatientSearchTerm);

            // ASSERT - Verify we're still on the PatientInfo page after search
            Assert.True(UrlContainsOneOf("PatientInfo"), "Na het zoeken moet de gebruiker op de patiëntinformatiepagina blijven.");

            TestLogger.LogStep("Selecteer de patiënt om de medicatiegegevens te bekijken.");
            _apothecaryDashboard.ConfirmPatientSelection();
            WaitSeconds(1, "wachten tot patiëntdetails zijn geladen");
            TakeScreenshot("PatientMedicationView");

            // ASSERT - Verify that patient medication details are visible
            TestLogger.LogAssert("Controleer of de medicatiegegevens van de patiënt zichtbaar zijn.");

            // A successful outcome is viewing the patient's medication list
            bool hasMedicationList = _apothecaryDashboard.HasMedicationList();
            TestLogger.LogInfo($"Heeft medicatielijst: {hasMedicationList}");

            // Main assertion - we should see the medication list after confirming
            Assert.True(hasMedicationList, "De medicatielijst moet worden weergegeven na het selecteren van een patiënt.");
            
            // URL should still be PatientInfo
            Assert.True(UrlContainsOneOf("PatientInfo"), "De gebruiker moet op de patiëntinformatiepagina blijven na het selecteren van een patiënt.");
        }
    }
}