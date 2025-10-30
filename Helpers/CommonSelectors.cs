using OpenQA.Selenium;

namespace MMA_tests.Helpers
{
    /// <summary>
    /// Centralizes common selectors used across page objects
    /// </summary>
    public static class CommonSelectors
    {
        // Authentication buttons
        public static readonly By LoginButton = By.CssSelector("button[name='btn-login']");
        public static readonly By LogoutButton = By.CssSelector("button[name='btn_logout']");

        // Navigation links
        public static readonly By PatientInfoLink = By.CssSelector("a[name='menu_patient_info']");
        public static readonly By PrescriptionsLink = By.CssSelector("a[name='menu_prescriptions']");
        public static readonly By NewPrescriptionLink = By.CssSelector("a[href='/Prescriptions/new']");

        // Search and confirmation buttons
        public static readonly By SearchButton = By.CssSelector("button#search-btn[name='action'][value='search']");
        public static readonly By ConfirmButton = By.CssSelector("button[name='action'][value='confirm']");

        // Prescription form elements
        public static readonly By PatientSelect = By.CssSelector("select#patientSelect");
        public static readonly By AddMedicineButton = By.CssSelector("button[formaction='/Prescriptions/new?handler=AddMedicine']");
        public static readonly By CreatePrescriptionButton = By.CssSelector("button.btn-primary[type='submit']:not([name])");
        public static readonly By AddButton = By.CssSelector("button[name='action'][value='add']");

        // Validation elements
        public static readonly By ValidationSummary = By.CssSelector(".validation-summary-errors");
        public static readonly By ValidationErrors = By.CssSelector(".field-validation-error");
        public static readonly By InputValidationErrors = By.CssSelector("input.input-validation-error");

        // Form state indicators
        public static readonly By SuccessMessage = By.CssSelector(".alert-success");
        public static readonly By ErrorMessage = By.CssSelector(".alert-danger");

        // Table elements
        public static readonly By Table = By.TagName("table");
        public static readonly By TableRows = By.CssSelector("table tbody tr");
        public static readonly By TableHeaders = By.CssSelector("table th");

        // Navigation indicators
        public static readonly By UserGreeting = By.CssSelector(".navbar-text, .user-info");
        
        // Common button text patterns (for text-based searches)
        public static readonly string[] SubmitButtonTexts = new[] { "Submit", "Save", "Create" };
        public static readonly string[] SearchButtonTexts = new[] { "Search", "Find" };
        public static readonly string[] ConfirmButtonTexts = new[] { "Confirm", "Select", "View" };
        
        // Submit button fallbacks
        public static readonly By SubmitButton = By.CssSelector("button[type='submit']");
        public static readonly By SubmitInput = By.CssSelector("input[type='submit']");
        
        // Logout button fallback
        public static readonly By LogoutButtonFallback = By.CssSelector("form[action*='Logout'] button, button[form*='logoutForm']");
    }
}