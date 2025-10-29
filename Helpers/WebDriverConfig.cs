using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMA_tests.Helpers
{
    public static class WebDriverConfig
    {
        // Path to Chrome WebDriver - this will find it in the bin directory after package restore
        public static string ChromeDriverPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory);
        
        // Make sure this matches your application URL exactly
        public static string BaseUrl => "https://localhost:7058/";
        
        // Default timeout for WebDriverWait - moderate timeout
        public static TimeSpan DefaultTimeout => TimeSpan.FromSeconds(10);

        // Page load timeout - moderate timeout
        public static TimeSpan PageLoadTimeout => TimeSpan.FromSeconds(30);
        
        // Implicit wait timeout - short timeout for better performance
        public static TimeSpan ImplicitWaitTimeout => TimeSpan.FromSeconds(5);

        // Command timeout - how long to wait for commands to complete
        public static TimeSpan CommandTimeout => TimeSpan.FromSeconds(30);
        
        // Test settings
        public static class TestUsers
        {
            // Ensure these match your application's test users exactly
            public static readonly (string Username, string Password) Patient = ("patient", "patient1");
            public static readonly (string Username, string Password) Doctor = ("doctor", "doctor1");
            public static readonly (string Username, string Password) Admin = ("admin", "admin1");
        }
    }
}