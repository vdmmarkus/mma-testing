using System;
using System.Collections.Generic;
using System.Threading;

namespace MMA_tests.Utils
{
    public static class TestContext
    {
        private static readonly AsyncLocal<TestContextInfo> _currentContext = new AsyncLocal<TestContextInfo>();

        public static TestContextInfo CurrentContext
        {
            get => _currentContext.Value;
            set => _currentContext.Value = value;
        }

        public static void SetCurrentTestMethod(string methodName)
        {
            if (_currentContext.Value == null)
            {
                _currentContext.Value = new TestContextInfo();
            }
            _currentContext.Value.TestMethod = methodName;
        }
    }

    public class TestContextInfo
    {
        public string TestMethod { get; set; }
    }
}