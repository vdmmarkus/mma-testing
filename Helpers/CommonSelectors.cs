using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMA_tests.Helpers
{
    /// <summary>
    /// Centralizes common selectors used across page objects to reduce duplication
    /// </summary>
    public static class CommonSelectors
    {
        // Common navigation elements
        public static readonly By LogoutButton = By.LinkText("Logout");
        public static readonly By LogoutButtonFallback = By.XPath("//a[contains(text(),'Logout') or contains(text(),'Log out')]");
        public static readonly By UserGreeting = By.CssSelector(".navbar-text");
        
        // Common form elements
        public static readonly By SubmitButton = By.CssSelector("button[type='submit']");
        public static readonly By SubmitInput = By.CssSelector("input[type='submit']");
        
        // Common UI elements
        public static readonly By Table = By.TagName("table");
        public static readonly By TableRows = By.CssSelector("table tbody tr");
        public static readonly By TableHeaders = By.CssSelector("table th");
        
        // Common validation elements
        public static readonly By ValidationSummary = By.CssSelector(".validation-summary-errors");
        public static readonly By ValidationErrors = By.CssSelector(".field-validation-error");
        public static readonly By InputValidationErrors = By.CssSelector("input.input-validation-error");
        public static readonly By InvalidFields = By.CssSelector(".is-invalid");
        
        // Common message elements
        public static readonly By SuccessMessage = By.CssSelector(".alert-success");
        public static readonly By ErrorMessage = By.CssSelector(".alert-danger");
        
        // Common search terms for identifying elements by text content
        public static readonly string[] SubmitButtonTexts = new[] { 
            "Submit", "Save", "Add", "Create", "Opslaan", "Toevoegen", 
            "Aanmaken", "Verstuur", "OK", "Voeg toe"
        };
        
        public static readonly string[] SearchButtonTexts = new[] {
            "Search", "Zoek", "Zoeken", "Find", "Vind", "Vinden", "Go"
        };
        
        public static readonly string[] ConfirmButtonTexts = new[] {
            "Confirm", "Select", "View", "Details", "Bekijk", "Bevestig",
            "Choose", "Kies", "Show", "Toon"
        };
    }
}