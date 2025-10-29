using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MMA_tests.Utils
{
    public static class TestReporter
    {
        public static void GenerateReport(string outputDirectory)
        {
            var results = TestRunContext.GetResults();
            var reportPath = Path.Combine(outputDirectory, $"TestReport_{DateTime.Now:yyyyMMdd_HHmmss}.html");
            
            Directory.CreateDirectory(outputDirectory);

            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang='en'>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset='UTF-8'>");
            html.AppendLine("<title>Test Report</title>");
            html.AppendLine("<style>");
            html.AppendLine(@"
                body { font-family: Arial, sans-serif; margin: 20px; }
                .summary { margin-bottom: 20px; }
                .criteria-group { margin-bottom: 30px; border: 1px solid #ddd; padding: 15px; }
                .test-case { margin: 10px 0; padding: 10px; border-left: 4px solid #ddd; }
                .passed { border-left-color: #4CAF50; }
                .failed { border-left-color: #f44336; }
                .steps { margin-left: 20px; font-size: 0.9em; color: #666; }
                .timestamp { color: #999; font-size: 0.8em; }
                table { border-collapse: collapse; width: 100%; }
                th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
                th { background-color: #f5f5f5; }
            ");
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            // Report Header
            html.AppendLine("<h1>Test Execution Report</h1>");
            html.AppendLine($"<p>Generated: {DateTime.Now}</p>");
            html.AppendLine($"<p>Software Version: {TestRunContext.SoftwareVersion}</p>");

            // Summary
            var totalTests = results.Count;
            var passedTests = results.Count(r => r.Passed);
            var failedTests = totalTests - passedTests;
            
            html.AppendLine("<div class='summary'>");
            html.AppendLine("<h2>Summary</h2>");
            html.AppendLine("<table>");
            html.AppendLine("<tr><th>Total Tests</th><th>Passed</th><th>Failed</th><th>Pass Rate</th></tr>");
            html.AppendLine($"<tr><td>{totalTests}</td><td>{passedTests}</td><td>{failedTests}</td><td>{(totalTests > 0 ? (passedTests * 100.0 / totalTests).ToString("F1") : "0")}%</td></tr>");
            html.AppendLine("</table>");
            html.AppendLine("</div>");

            // Group by Acceptance Criteria
            var groupedResults = results.GroupBy(r => r.AcceptanceCriteriaId);
            foreach (var group in groupedResults)
            {
                html.AppendLine("<div class='criteria-group'>");
                var firstTest = group.First();
                html.AppendLine($"<h3>Acceptance Criteria {firstTest.AcceptanceCriteriaId}</h3>");
                html.AppendLine($"<p>{firstTest.AcceptanceCriteriaDescription}</p>");

                foreach (var result in group)
                {
                    string statusClass = result.Passed ? "passed" : "failed";
                    html.AppendLine($"<div class='test-case {statusClass}'>");
                    html.AppendLine($"<h4>{result.TestName}</h4>");
                    html.AppendLine($"<p>Status: {(result.Passed ? "Passed" : "Failed")}</p>");
                    html.AppendLine($"<p>Duration: {result.Duration.TotalSeconds:F2} seconds</p>");
                    
                    if (!string.IsNullOrEmpty(result.BrowserInfo))
                    {
                        html.AppendLine($"<p>Browser: {result.BrowserInfo}</p>");
                    }

                    if (!string.IsNullOrEmpty(result.ErrorMessage))
                    {
                        html.AppendLine($"<p style='color: red;'>Error: {result.ErrorMessage}</p>");
                    }

                    if (result.TestSteps.Any())
                    {
                        html.AppendLine("<div class='steps'>");
                        html.AppendLine("<h5>Test Steps:</h5>");
                        html.AppendLine("<ol>");
                        foreach (var step in result.TestSteps)
                        {
                            html.AppendLine($"<li>{step}</li>");
                        }
                        html.AppendLine("</ol>");
                        html.AppendLine("</div>");
                    }

                    if (!string.IsNullOrEmpty(result.ScreenshotPath))
                    {
                        var relativeScreenshotPath = Path.GetFileName(result.ScreenshotPath);
                        html.AppendLine($"<p><a href='Screenshots/{relativeScreenshotPath}' target='_blank'>View Screenshot</a></p>");
                    }

                    html.AppendLine("</div>");
                }

                html.AppendLine("</div>");
            }

            html.AppendLine("</body>");
            html.AppendLine("</html>");

            File.WriteAllText(reportPath, html.ToString());
            TestLogger.LogInfo($"Test report generated: {reportPath}");
        }
    }
}