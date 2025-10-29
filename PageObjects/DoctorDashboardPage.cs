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
    public class DoctorDashboardPage : BasePage
    {
        // URLs
        public static string DashboardUrl => WebDriverConfig.BaseUrl;
        public static string PatientsUrl => WebDriverConfig.BaseUrl + "Prescriptions"; // List of prescriptions
        public static string PrescriptionCreateUrl => WebDriverConfig.BaseUrl + "Prescriptions/new"; // New prescription page
        public static string AddMedicineUrl => WebDriverConfig.BaseUrl + "Prescriptions/AddMedicineToPrescription"; // Add medicine to prescription

        // Doctor-specific elements - using only unique selectors
        private readonly By _patientsTable = By.Id("patients-table");
        private readonly By _prescribeButton = By.LinkText("Voorschrijven");
        private readonly By _prescribeButtonFallback = By.XPath("//a[contains(text(),'Voorschrijven') or contains(text(),'Prescribe')]");
        
        // Form field locators - consolidated to reduce redundancy
        private readonly By[] _medicineNameLocators = new[] { 
            By.Id("MedicineName"), 
            By.Name("MedicineName"),
            By.Id("MedicationName"),
            By.Name("MedicationName")
        };
        
        private readonly By[] _dosageLocators = new[] { 
            By.Id("Dosage"), 
            By.Name("Dosage")
        };
        
        private readonly By[] _frequencyLocators = new[] { 
            By.Id("Frequency"), 
            By.Name("Frequency")
        };
        
        private readonly By[] _instructionsLocators = new[] { 
            By.Id("Instructions"), 
            By.Name("Instructions")
        };
        
        private readonly By[] _startDateLocators = new[] { 
            By.Id("StartDate"), 
            By.Name("StartDate")
        };
        
        private readonly By[] _endDateLocators = new[] { 
            By.Id("EndDate"), 
            By.Name("EndDate")
        };
        
        private readonly By _patientIdInput = By.Id("PatientId");

        public DoctorDashboardPage(IWebDriver driver) : base(driver) { }

        /// <summary>
        /// Override to provide specific URL checks for doctor areas
        /// </summary>
        protected override bool IsInProtectedArea()
        {
            return Driver.Url.Contains("/Prescriptions/new") || 
                   Driver.Url.Contains("/Prescriptions/AddMedicineToPrescription");
        }

        /// <summary>
        /// Navigates to the doctor dashboard
        /// </summary>
        public void NavigateToDashboard()
        {
            NavigateTo(DashboardUrl);
            WaitSeconds(1);
        }

        /// <summary>
        /// Navigates to the patients/prescriptions list
        /// </summary>
        public void NavigateToPatients()
        {
            NavigateTo(PatientsUrl);
            WaitSeconds(1);
        }

        /// <summary>
        /// Navigates to the new prescription form
        /// </summary>
        public void NavigateToNewPrescriptionForm()
        {
            NavigateTo(PrescriptionCreateUrl);
            WaitSeconds(1);
            
            // Take a screenshot for debugging purposes
            TakeScreenshot("NavigateToNewPrescriptionForm");
        }
        
        /// <summary>
        /// Navigates to the add medicine form
        /// </summary>
        public void NavigateToAddMedicineForm()
        {
            NavigateTo(AddMedicineUrl);
            WaitSeconds(1);
            
            // Take a screenshot for debugging purposes
            TakeScreenshot("NavigateToAddMedicineForm");
        }

        /// <summary>
        /// Logs details about the form elements for debugging
        /// </summary>
        public void LogFormElementDetails()
        {
            TestLogger.LogInfo("--- Form Element Diagnostic Information ---");
            TestLogger.LogInfo($"Current URL: {Driver.Url}");
            TestLogger.LogInfo($"Page Title: {Driver.Title}");
            
            try
            {
                // Try to identify form elements with basic attributes
                var formElements = Driver.FindElements(By.TagName("form"));
                TestLogger.LogInfo($"Number of forms found: {formElements.Count}");
                
                if (formElements.Count > 0)
                {
                    var form = formElements[0];
                    
                    TestLogger.LogInfo($"Form ID: {form.GetAttribute("id")}");
                    TestLogger.LogInfo($"Form Action: {form.GetAttribute("action")}");
                    TestLogger.LogInfo($"Form Method: {form.GetAttribute("method")}");
                    
                    // Log all input elements in the form
                    LogElements(form.FindElements(By.TagName("input")), "Input");
                    LogElements(form.FindElements(By.TagName("textarea")), "Textarea");
                    LogElements(form.FindElements(By.TagName("select")), "Select");
                    LogElements(form.FindElements(By.TagName("button")), "Button");
                }
            }
            catch (Exception ex)
            {
                TestLogger.LogError($"Error during form diagnostic logging: {ex.Message}");
            }
            
            TestLogger.LogInfo("--- End of Form Element Diagnostic Information ---");
        }

        /// <summary>
        /// Helper method to log element details
        /// </summary>
        private void LogElements(IReadOnlyCollection<IWebElement> elements, string elementType)
        {
            TestLogger.LogInfo($"Number of {elementType} elements: {elements.Count}");
            
            foreach (var element in elements)
            {
                try
                {
                    string id = element.GetAttribute("id") ?? "no-id";
                    string name = element.GetAttribute("name") ?? "no-name";
                    string type = element.GetAttribute("type") ?? "no-type";
                    string text = elementType == "Button" ? element.Text : "";
                    
                    if (string.IsNullOrEmpty(text))
                    {
                        TestLogger.LogInfo($"{elementType}: ID={id}, Name={name}, Type={type}");
                    }
                    else
                    {
                        TestLogger.LogInfo($"{elementType}: ID={id}, Name={name}, Type={type}, Text={text}");
                    }
                }
                catch (Exception) { }
            }
        }

        /// <summary>
        /// Checks if the page has a patients table
        /// </summary>
        public bool HasPatientsTable()
        {
            return IsElementDisplayed(_patientsTable) || 
                   Driver.FindElements(By.TagName("table")).Count > 0;
        }

        /// <summary>
        /// Gets the number of patient rows in the table
            /// </summary>
            public int GetPatientCount()
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
        
        /// <summary>
        /// Clicks the prescribe button for a patient
        /// </summary>
        public void ClickPrescribeForPatient(int rowIndex = 0)
        {
            try
            {
                var rows = Driver.FindElements(CommonSelectors.TableRows);
                if (rows.Count > rowIndex)
                {
                    // Try to find prescribe button directly within the row
                    try
                    {
                        var prescribeLink = rows[rowIndex].FindElement(By.LinkText("Voorschrijven"));
                        TestLogger.LogInfo("Found 'Voorschrijven' link in row");
                        prescribeLink.Click();
                        return;
                    }
                    catch (Exception)
                    {
                        // Try English version
                        try
                        {
                            var prescribeLink = rows[rowIndex].FindElement(By.LinkText("Prescribe"));
                            TestLogger.LogInfo("Found 'Prescribe' link in row");
                            prescribeLink.Click();
                            return;
                        }
                        catch (Exception) { }
                    }
                }
                
                // Try direct locators
                if (IsElementDisplayed(_prescribeButton))
                {
                    FindElement(_prescribeButton).Click();
                    TestLogger.LogInfo("Clicked 'Voorschrijven' button directly");
                    return;
                }
                
                if (IsElementDisplayed(_prescribeButtonFallback))
                {
                    FindElement(_prescribeButtonFallback).Click();
                    TestLogger.LogInfo("Clicked prescribe button via XPath");
                    return;
                }
                
                // Last resort - try all links
                var allLinks = Driver.FindElements(By.TagName("a"));
                foreach (var link in allLinks)
                {
                    try
                    {
                        string text = link.Text.ToLower();
                        if (text.Contains("voorschrijven") || 
                            text.Contains("prescribe") ||
                            text.Contains("add medicine") ||
                            text.Contains("medicijn toevoegen"))
                        {
                            TestLogger.LogInfo($"Found prescribe link with text: {link.Text}");
                            link.Click();
                            return;
                        }
                    }
                    catch (Exception) { }
                }
                
                TestLogger.LogError("Could not find prescribe button for patient");
                throw new NoSuchElementException("Prescribe button not found");
            }
            catch (Exception ex)
            {
                TestLogger.LogError($"Error clicking prescribe button: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Finds an input element using multiple locators
        /// </summary>
        private IWebElement FindInputWithLocators(By[] locators, string fieldName)
        {
            foreach (var locator in locators)
            {
                try
                {
                    if (IsElementDisplayed(locator))
                    {
                        return FindElement(locator);
                    }
                }
                catch (Exception) { }
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
                    string type = input.GetAttribute("type") ?? "";
                    
                    if (type != "submit" && type != "button" &&
                        (id.Contains(fieldName) || name.Contains(fieldName) || placeholder.Contains(fieldName)))
                    {
                        return input;
                    }
                }
                catch (Exception) { }
            }
            
            // Check textareas too for instructions
            if (fieldName.Contains("instruction") || fieldName.Contains("notes") || fieldName.Contains("description"))
            {
                var textareas = Driver.FindElements(By.TagName("textarea"));
                foreach (var textarea in textareas)
                {
                    try
                    {
                        string id = textarea.GetAttribute("id")?.ToLower() ?? "";
                        string name = textarea.GetAttribute("name")?.ToLower() ?? "";
                        
                        if (id.Contains(fieldName) || name.Contains(fieldName))
                        {
                            return textarea;
                        }
                    }
                    catch (Exception) { }
                }
                
                // If we still haven't found a match but we're looking for instructions,
                // just take the first textarea
                if (textareas.Count > 0)
                {
                    return textareas[0];
                }
            }
            
            throw new NoSuchElementException($"Could not find input field for: {fieldName}");
        }
        
        /// <summary>
        /// Fills the prescription form with the provided values
        /// </summary>
        public void FillPrescriptionForm(string medicationName, string dosage, string frequency, 
                                         string instructions, string startDate = null, string endDate = null)
        {
            try
            {
                TestLogger.LogInfo("Filling prescription form...");
                TakeScreenshot("BeforeFillingForm");
                
                // Fill medication name
                try
                {
                    var element = FindInputWithLocators(_medicineNameLocators, "medicine");
                    element.Clear();
                    element.SendKeys(medicationName);
                    TestLogger.LogInfo($"Entered medicine name: {medicationName}");
                }
                catch (Exception ex)
                {
                    TestLogger.LogError($"Failed to enter medicine name: {ex.Message}");
                }
                
                // Fill dosage
                try
                {
                    var element = FindInputWithLocators(_dosageLocators, "dosage");
                    element.Clear();
                    element.SendKeys(dosage);
                    TestLogger.LogInfo($"Entered dosage: {dosage}");
                }
                catch (Exception ex)
                {
                    TestLogger.LogError($"Failed to enter dosage: {ex.Message}");
                }
                
                // Fill frequency
                try
                {
                    var element = FindInputWithLocators(_frequencyLocators, "frequency");
                    element.Clear();
                    element.SendKeys(frequency);
                    TestLogger.LogInfo($"Entered frequency: {frequency}");
                }
                catch (Exception ex)
                {
                    TestLogger.LogError($"Failed to enter frequency: {ex.Message}");
                }
                
                // Fill instructions
                try
                {
                    var element = FindInputWithLocators(_instructionsLocators, "instruction");
                    element.Clear();
                    element.SendKeys(instructions);
                    TestLogger.LogInfo($"Entered instructions: {instructions}");
                }
                catch (Exception ex)
                {
                    TestLogger.LogError($"Failed to enter instructions: {ex.Message}");
                }
                
                // Optional fields
                if (!string.IsNullOrEmpty(startDate))
                {
                    try
                    {
                        var element = FindInputWithLocators(_startDateLocators, "start");
                        element.Clear();
                        element.SendKeys(startDate);
                        TestLogger.LogInfo($"Entered start date: {startDate}");
                    }
                    catch (Exception ex)
                    {
                        TestLogger.LogInfo($"Could not set start date: {ex.Message}");
                    }
                }
                
                if (!string.IsNullOrEmpty(endDate))
                {
                    try
                    {
                        var element = FindInputWithLocators(_endDateLocators, "end");
                        element.Clear();
                        element.SendKeys(endDate);
                        TestLogger.LogInfo($"Entered end date: {endDate}");
                    }
                    catch (Exception ex)
                    {
                        TestLogger.LogInfo($"Could not set end date: {ex.Message}");
                    }
                }
                
                TestLogger.LogInfo("Completed filling prescription form");
                TakeScreenshot("AfterFillingForm");
            }
            catch (Exception ex)
            {
                TestLogger.LogError($"Error filling prescription form: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Submits the prescription form
        /// </summary>
        public void SubmitPrescriptionForm()
        {
            try
            {
                TestLogger.LogInfo("Submitting prescription form...");
                TakeScreenshot("BeforeSubmittingForm");
                
                // Use the common method from BasePage
                bool clicked = ClickSubmitButton("on prescription form");
                
                if (!clicked)
                {
                    TestLogger.LogWarning("Could not find standard submit button, trying to submit form directly");
                    
                    try
                    {
                        var forms = Driver.FindElements(By.TagName("form"));
                        if (forms.Count > 0)
                        {
                            // Try to submit the form directly
                            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].submit();", forms[0]);
                            TestLogger.LogInfo("Submitted form using JavaScript");
                        }
                    }
                    catch (Exception ex)
                    {
                        TestLogger.LogError($"Failed to submit form: {ex.Message}");
                    }
                }
                
                WaitSeconds(2, "waiting for form submission to complete");
                TakeScreenshot("AfterSubmittingForm");
            }
            catch (Exception ex)
            {
                TestLogger.LogError($"Error submitting form: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Checks if the prescription form is displayed
        /// </summary>
        public bool IsPrescriptionFormDisplayed()
        {
            TestLogger.LogInfo("Checking if prescription form is displayed...");
            TakeScreenshot("CheckingForPrescriptionForm");
            
            // Check if we're on one of the prescription pages
            bool isOnPrescriptionUrl = 
                Driver.Url.Contains("/Prescriptions/new") || 
                Driver.Url.Contains("/Prescriptions/AddMedicineToPrescription");
                
            TestLogger.LogInfo($"URL check: {isOnPrescriptionUrl}, Current URL: {Driver.Url}");
            
            // Look for form element
            bool hasFormElement = Driver.FindElements(By.TagName("form")).Count > 0;
            TestLogger.LogInfo($"Form element found: {hasFormElement}");
            
            // Try to find at least one medicine input field
            bool hasMedicineInput = _medicineNameLocators.Any(locator => IsElementDisplayed(locator));
            TestLogger.LogInfo($"Medicine input field found: {hasMedicineInput}");
            
            return (isOnPrescriptionUrl && (hasFormElement || Driver.FindElements(By.TagName("input")).Count > 0)) || 
                   hasMedicineInput;
        }
        
        /// <summary>
        /// Checks if a success message is displayed
        /// </summary>
        public bool IsSuccessMessageDisplayed()
        {
            // Check for standard success message
            bool hasSuccessMessage = IsElementDisplayed(CommonSelectors.SuccessMessage);
            
            // Check for any text that might indicate success
            if (!hasSuccessMessage)
            {
                try
                {
                    string pageSource = Driver.PageSource.ToLower();
                    hasSuccessMessage = pageSource.Contains("success") || 
                                       pageSource.Contains("successfully") || 
                                       pageSource.Contains("saved") || 
                                       pageSource.Contains("created") ||
                                       pageSource.Contains("toegevoegd") ||
                                       pageSource.Contains("aangemaakt") ||
                                       pageSource.Contains("opgeslagen");
                }
                catch (Exception) { }
            }
            
            return hasSuccessMessage;
        }
        
        /// <summary>
        /// Gets the success message text
        /// </summary>
        public string GetSuccessMessage()
        {
            if (IsElementDisplayed(CommonSelectors.SuccessMessage))
            {
                return FindElement(CommonSelectors.SuccessMessage).Text;
            }
            
            return string.Empty;
        }
    }
}