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
    public class ApothecaryDashboardPage : BasePage
    {
        // URLs
        public static string DashboardUrl => WebDriverConfig.BaseUrl;
        public static string PatientInfoUrl => WebDriverConfig.BaseUrl + "PatientInfo";
        
        // Apothecary-specific elements - using only unique selectors
        private readonly By _searchInput = By.Id("searchTerm");
        private readonly By _searchButton = By.Id("searchButton");
        private readonly By _confirmButton = By.Id("confirmButton");
        private readonly By _patientSearchResults = By.CssSelector(".patient-search-results");
        
        // Patient details elements
        private readonly By _patientDetails = By.CssSelector(".patient-details");
        private readonly By _medicationList = By.CssSelector(".medication-list");

        public ApothecaryDashboardPage(IWebDriver driver) : base(driver) { }

        /// <summary>
        /// Override to provide specific URL checks for apothecary areas
        /// </summary>
        protected override bool IsInProtectedArea()
        {
            return Driver.Url.Contains("/PatientInfo");
        }

        /// <summary>
        /// Navigates to the apothecary dashboard
        /// </summary>
        public void NavigateToDashboard()
        {
            NavigateTo(DashboardUrl);
            TakeScreenshot("ApothecaryDashboard");
        }

        /// <summary>
        /// Navigates to the patient info page
        /// </summary>
        public void NavigateToPatientInfo()
        {
            NavigateTo(PatientInfoUrl);
            TakeScreenshot("PatientInfoPage");
            
            // Check if we got redirected to login
            if (Driver.Url.Contains("Account/Login"))
            {
                TestLogger.LogWarning("Redirected to login page. User might not be authenticated.");
            }
        }
        
        /// <summary>
        /// Searches for a patient using the search form
        /// </summary>
        public void SearchForPatient(string searchTerm)
        {
            TestLogger.LogInfo($"Searching for patient with term: {searchTerm}");
            
            try
            {
                // Take screenshot before search
                TakeScreenshot("BeforePatientSearch");
                
                // Try to find and use the search input field
                if (IsElementDisplayed(_searchInput))
                {
                    var searchInput = FindElement(_searchInput);
                    searchInput.Clear();
                    searchInput.SendKeys(searchTerm);
                    TestLogger.LogInfo($"Entered search term: {searchTerm}");
                    
                    // Take screenshot after entering search term
                    TakeScreenshot("AfterSearchTerm");
                    
                    // Use common search button click method
                    if (!ClickSearchButton("in patient search"))
                    {
                        // If we couldn't find the search button, try pressing Enter key
                        searchInput.SendKeys(Keys.Enter);
                        TestLogger.LogInfo("Submitted search via Enter key");
                    }
                }
                else
                {
                    TestLogger.LogWarning("Search input not found by ID, trying alternative approaches");
                    
                    // Try to find any input field that could be a search field
                    bool found = EnterText("search", searchTerm) || 
                                EnterText("patient", searchTerm) || 
                                EnterText("filter", searchTerm);
                    
                    if (found)
                    {
                        // Try to click a search button
                        ClickSearchButton();
                    }
                    else
                    {
                        TestLogger.LogError("Could not find any suitable search field");
                    }
                }
                
                WaitSeconds(1);
                
                // Take screenshot after search
                TakeScreenshot("AfterPatientSearch");
            }
            catch (Exception ex)
            {
                TestLogger.LogError($"Error during patient search: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Confirms a patient selection to view details
        /// </summary>
        public void ConfirmPatientSelection()
        {
            TestLogger.LogInfo("Confirming patient selection");
            
            try
            {
                // Take screenshot before confirmation
                TakeScreenshot("BeforePatientConfirmation");
                
                // Use the common confirmation button click method
                if (!ClickConfirmButton("for patient selection"))
                {
                    TestLogger.LogWarning("Could not find standard confirm buttons, trying generic selection");
                    
                    // Last resort - try to find any clickable element with patient-related content
                    try
                    {
                        var patientElements = Driver.FindElements(By.XPath("//*[contains(text(), 'patient')]"));
                        foreach (var element in patientElements)
                        {
                            try
                            {
                                if (element.Displayed && element.Enabled)
                                {
                                    element.Click();
                                    TestLogger.LogInfo("Clicked on element containing 'patient' text");
                                    break;
                                }
                            }
                            catch (Exception) { }
                        }
                    }
                    catch (Exception) { }
                }
                
                WaitSeconds(1);
                TakeScreenshot("AfterPatientConfirmation");
            }
            catch (Exception ex)
            {
                TestLogger.LogError($"Error confirming patient selection: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Checks if the page has search results
        /// </summary>
        public bool HasSearchResults()
        {
            TestLogger.LogInfo("Checking for search results");
            
            try
            {
                // Check if there's a designated results container
                bool hasResultsContainer = IsElementDisplayed(_patientSearchResults);
                
                // Check if there's a table that might contain results
                bool hasTable = IsElementDisplayed(CommonSelectors.Table);
                
                // Check for any elements that could indicate results
                bool hasPatientText = Driver.PageSource.ToLower().Contains("patient");
                
                TestLogger.LogInfo($"Has results container: {hasResultsContainer}, " +
                                  $"Has table: {hasTable}, " +
                                  $"Has patient text: {hasPatientText}");
                
                return hasResultsContainer || hasTable || hasPatientText;
            }
            catch (Exception ex)
            {
                TestLogger.LogError($"Error checking for search results: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Checks if the page shows patient details
        /// </summary>
        public bool HasPatientDetails()
        {
            TestLogger.LogInfo("Checking for patient details");
            
            try
            {
                // Check if there's a designated details container
                bool hasDetailsContainer = IsElementDisplayed(_patientDetails);
                
                // Check if there's a table that might contain details
                bool hasTable = IsElementDisplayed(CommonSelectors.Table);
                
                // Check for any elements that could indicate details
                bool hasPatientContent = Driver.PageSource.ToLower().Contains("patient");
                
                // Check if we're not on the search page anymore
                bool notOnSearchPage = !Driver.Url.EndsWith("/PatientInfo");
                
                TestLogger.LogInfo($"Has details container: {hasDetailsContainer}, " +
                                  $"Has table: {hasTable}, " +
                                  $"Has patient content: {hasPatientContent}, " +
                                  $"Not on search page: {notOnSearchPage}");
                
                return hasDetailsContainer || hasTable || (hasPatientContent && notOnSearchPage);
            }
            catch (Exception ex)
            {
                TestLogger.LogError($"Error checking for patient details: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Checks if the page shows a medication list
        /// </summary>
        public bool HasMedicationList()
        {
            TestLogger.LogInfo("Checking for medication list");
            
            try
            {
                // Check if there's a designated medication container
                bool hasMedicationContainer = IsElementDisplayed(_medicationList);
                
                // Check if there's a table that might contain prescriptions
                bool hasTable = IsElementDisplayed(CommonSelectors.Table);
                
                // Check for medication-related text
                string pageSource = Driver.PageSource.ToLower();
                bool hasMedicationContent = pageSource.Contains("medicatie") || 
                                          pageSource.Contains("medication") || 
                                          pageSource.Contains("prescription") || 
                                          pageSource.Contains("voorschrift");
                
                TestLogger.LogInfo($"Has medication container: {hasMedicationContainer}, " +
                                  $"Has table: {hasTable}, " +
                                  $"Has medication content: {hasMedicationContent}");
                
                return hasMedicationContainer || hasTable || hasMedicationContent;
            }
            catch (Exception ex)
            {
                TestLogger.LogError($"Error checking for medication list: {ex.Message}");
                return false;
            }
        }
    }
}