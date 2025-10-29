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
    public class PatientDashboardPage : BasePage
    {
        // URLs
        public static string DashboardUrl => WebDriverConfig.BaseUrl;
        public static string MyPrescriptionsUrl => WebDriverConfig.BaseUrl + "Prescriptions/MyPrescriptions";
        public static string PrescriptionDetailsUrl => WebDriverConfig.BaseUrl + "Prescriptions/Details/";

        // Patient-specific elements - using only unique selectors
        private readonly By _prescriptionTable = By.Id("prescriptions-table");
        private readonly By _noPrescriptionsMessage = By.Id("no-prescriptions-message");
        private readonly By _backToListLink = By.LinkText("Terug naar overzicht");
        private readonly By _backToListLinkFallback = By.LinkText("Back to List");

        public PatientDashboardPage(IWebDriver driver) : base(driver) { }

        /// <summary>
        /// Navigates to the patient dashboard
        /// </summary>
        public void NavigateToDashboard()
        {
            NavigateTo(DashboardUrl);
        }

        /// <summary>
        /// Override to provide specific URL checks for patient areas
        /// </summary>
        protected override bool IsInProtectedArea()
        {
            return Driver.Url.Contains("/Prescriptions/MyPrescriptions") || 
                   Driver.Url.Contains("/Prescriptions/Details");
        }

        /// <summary>
        /// Checks if the user is on the dashboard
        /// </summary>
        public bool IsOnDashboard()
        {
            return Driver.Url.TrimEnd('/') == DashboardUrl.TrimEnd('/') || 
                   Driver.Url.Contains("/Prescriptions/MyPrescriptions") ||
                   IsElementDisplayed(_prescriptionTable) || 
                   IsElementDisplayed(CommonSelectors.Table);
        }
        
        #region Prescription List Methods
        
        /// <summary>
        /// Navigates to the My Prescriptions page
        /// </summary>
        public void NavigateToMyPrescriptions()
        {
            NavigateTo(MyPrescriptionsUrl);
        }

        /// <summary>
        /// Checks if we are on the My Prescriptions page
        /// </summary>
        public bool IsMyPrescriptionsPage()
        {
            return Driver.Url.Contains("/Prescriptions/MyPrescriptions") || 
                   (Driver.Url.TrimEnd('/') == DashboardUrl.TrimEnd('/') && IsUserLoggedIn()) ||
                   HasPrescriptionsTable();
        }

        /// <summary>
        /// Checks if the page has a prescriptions table
        /// </summary>
        public bool HasPrescriptionsTable()
        {
            return IsElementDisplayed(_prescriptionTable) || 
                   IsElementDisplayed(CommonSelectors.Table);
        }

        /// <summary>
        /// Checks if the page has a "no prescriptions" message
        /// </summary>
        public bool HasNoPrescriptionsMessage()
        {
            return IsElementDisplayed(_noPrescriptionsMessage);
        }

        /// <summary>
        /// Gets the number of prescription rows in the table
        /// </summary>
        public int GetPrescriptionCount()
        {
            try
            {
                return Driver.FindElements(CommonSelectors.TableRows).Count;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets the names of the table headers
        /// </summary>
        public List<string> GetTableHeaderNames()
        {
            var headers = Driver.FindElements(CommonSelectors.TableHeaders);
            return headers.Select(h => h.Text).ToList();
        }
        
        #endregion
        
        #region Prescription Detail Methods
        
        /// <summary>
        /// Navigates back to the prescriptions list
        /// </summary>
        public void NavigateBackToList()
        {
            try
            {
                if (IsElementDisplayed(_backToListLink))
                {
                    FindElement(_backToListLink).Click();
                }
                else if (IsElementDisplayed(_backToListLinkFallback))
                {
                    FindElement(_backToListLinkFallback).Click();
                }
                else
                {
                    TestLogger.LogInfo("Back to list link not found, navigating to prescriptions page");
                    NavigateToMyPrescriptions();
                }
                
                WaitSeconds(1);
            }
            catch (Exception ex)
            {
                TestLogger.LogError($"Error navigating back to list: {ex.Message}");
                NavigateToMyPrescriptions(); // Fallback
            }
        }
        
        #endregion
    }
}