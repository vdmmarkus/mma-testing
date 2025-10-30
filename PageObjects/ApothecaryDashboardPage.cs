using System;
using OpenQA.Selenium;
using MMA_tests.Helpers;
using MMA_tests.Utils;

namespace MMA_tests.PageObjects
{
    public class ApothecaryDashboardPage : BasePage
    {
        // URLs
        public static string DashboardUrl => WebDriverConfig.BaseUrl;
        public static string PatientInfoUrl => WebDriverConfig.BaseUrl + "PatientInfo";

        // Essential selectors that match the actual HTML
        private readonly By _searchButton = By.CssSelector("button#search-btn[name='action'][value='search']");
        private readonly By _confirmButton = By.CssSelector("button[name='action'][value='confirm']");
        private readonly By _activePrescriptionText = By.XPath("//*[contains(text(),'Active Prescription')]");

        public ApothecaryDashboardPage(IWebDriver driver) : base(driver) { }

        public void NavigateToPatientInfo()
        {
            NavigateTo(PatientInfoUrl);
        }

        public void SearchForPatient(string searchTerm)
        {
            TestLogger.LogInfo($"Searching for patient with term: {searchTerm}");

            try
            {
                // Enter search term
                if (EnterText("searchTerm", searchTerm))
                {
                    // Click the search button
                    if (IsElementDisplayed(_searchButton))
                    {
                        FindElement(_searchButton).Click();
                        TestLogger.LogInfo("Clicked search button");
                    }
                    else
                    {
                        TestLogger.LogWarning("Search button not found, trying fallback");
                        ClickSearchButton("in patient search");
                    }
                }
                else
                {
                    TestLogger.LogError("Could not enter search term");
                }
            }
            catch (Exception ex)
            {
                TestLogger.LogError($"Error during patient search: {ex.Message}");
            }
        }

        public void ConfirmPatientSelection()
        {
            TestLogger.LogInfo("Confirming patient selection");

            try
            {
                // Click the confirm button
                if (IsElementDisplayed(_confirmButton))
                {
                    FindElement(_confirmButton).Click();
                    TestLogger.LogInfo("Clicked confirm button");
                }
                else
                {
                    TestLogger.LogWarning("Standard confirm button not found, trying fallback");
                    ClickConfirmButton("for patient selection");
                }

                // Wait for the page to load after selection
                WaitSeconds(2, "waiting for patient details to load");
            }
            catch (Exception ex)
            {
                TestLogger.LogError($"Error confirming patient selection: {ex.Message}");
            }
        }

        public bool HasPatientDetails()
        {
            return HasMedicationList();  // If we see the medication list, we have patient details
        }

        public bool HasMedicationList()
        {
            try
            {
                return IsElementDisplayed(_activePrescriptionText);
            }
            catch (Exception ex)
            {
                TestLogger.LogError($"Error checking for medication list: {ex.Message}");
                return false;
            }
        }
    }
}