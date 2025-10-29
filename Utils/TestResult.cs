using System;
using System.Collections.Generic;

namespace MMA_tests.Utils
{
    public class TestResult
    {
        public string TestName { get; set; }
        public string AcceptanceCriteriaId { get; set; }
        public string AcceptanceCriteriaDescription { get; set; }
        public bool Passed { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<string> TestSteps { get; set; } = new List<string>();
        public string SoftwareVersion { get; set; }
        public string BrowserInfo { get; set; }
        public string ScreenshotPath { get; set; }

        public TimeSpan Duration => EndTime - StartTime;
    }

    public static class TestRunContext
    {
        private static readonly List<TestResult> _results = new List<TestResult>();
        private static TestResult _currentTest;
        public static string SoftwareVersion { get; set; } = "v0.95"; // Based on repository name

        public static void StartTest(string testName, string acceptanceCriteriaId, string acceptanceCriteriaDescription)
        {
            _currentTest = new TestResult
            {
                TestName = testName,
                AcceptanceCriteriaId = acceptanceCriteriaId,
                AcceptanceCriteriaDescription = acceptanceCriteriaDescription,
                StartTime = DateTime.Now,
                SoftwareVersion = SoftwareVersion
            };
        }

        public static void AddTestStep(string step)
        {
            _currentTest?.TestSteps.Add(step);
        }

        public static void SetBrowserInfo(string browserInfo)
        {
            if (_currentTest != null)
            {
                _currentTest.BrowserInfo = browserInfo;
            }
        }

        public static void SetScreenshotPath(string path)
        {
            if (_currentTest != null)
            {
                _currentTest.ScreenshotPath = path;
            }
        }

        public static void EndTest(bool passed, string errorMessage = null)
        {
            if (_currentTest != null)
            {
                _currentTest.EndTime = DateTime.Now;
                _currentTest.Passed = passed;
                _currentTest.ErrorMessage = errorMessage;
                _results.Add(_currentTest);
                _currentTest = null;
            }
        }

        public static List<TestResult> GetResults()
        {
            return new List<TestResult>(_results);
        }

        public static void ClearResults()
        {
            _results.Clear();
            _currentTest = null;
        }
    }
}