#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace UnityIsekaiGame.Development.Automation
{
    public sealed class TestLabAutomationReportExporter
    {
        private const string ReportDirectory = "Temp/TestLabAutomation";

        public string ExportJson(TestLabAutomationResult result)
        {
            EnsureReportDirectory();
            string path = Path.Combine(ReportDirectory, BuildReportFileName(result, "json"));
            File.WriteAllText(path, BuildJson(result), Encoding.UTF8);
            return path;
        }

        public string ExportMarkdown(TestLabAutomationResult result)
        {
            EnsureReportDirectory();
            string path = Path.Combine(ReportDirectory, BuildReportFileName(result, "md"));
            File.WriteAllText(path, BuildMarkdown(result), Encoding.UTF8);
            return path;
        }

        public string BuildJson(TestLabAutomationResult result)
        {
            if (result == null)
            {
                return "{}";
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("{");
            AppendJsonProperty(builder, "projectName", Application.productName, 1, comma: true);
            AppendJsonProperty(builder, "unityVersion", Application.unityVersion, 1, comma: true);
            AppendJsonProperty(builder, "platform", Application.platform.ToString(), 1, comma: true);
            AppendJsonProperty(builder, "runId", result.RunId, 1, comma: true);
            AppendJsonProperty(builder, "runMode", result.RunMode.ToString(), 1, comma: true);
            AppendJsonProperty(builder, "startedAtUtc", result.StartedAtUtc.ToString("O"), 1, comma: true);
            AppendJsonProperty(builder, "endedAtUtc", result.EndedAtUtc.ToString("O"), 1, comma: true);
            AppendJsonProperty(builder, "cancelled", result.Cancelled ? "true" : "false", 1, comma: true, quoteValue: false);
            builder.AppendLine("  \"totals\": {");
            AppendJsonProperty(builder, "scenarios", result.TotalScenarios.ToString(), 2, comma: true, quoteValue: false);
            AppendJsonProperty(builder, "passed", result.PassedScenarios.ToString(), 2, comma: true, quoteValue: false);
            AppendJsonProperty(builder, "failed", result.FailedScenarios.ToString(), 2, comma: true, quoteValue: false);
            AppendJsonProperty(builder, "errors", result.ErrorScenarios.ToString(), 2, comma: true, quoteValue: false);
            AppendJsonProperty(builder, "skipped", result.SkippedScenarios.ToString(), 2, comma: true, quoteValue: false);
            AppendJsonProperty(builder, "cancelled", result.CancelledScenarios.ToString(), 2, comma: true, quoteValue: false);
            AppendJsonProperty(builder, "steps", result.TotalSteps.ToString(), 2, comma: false, quoteValue: false);
            builder.AppendLine("  },");
            builder.AppendLine("  \"scenarios\": [");
            for (int i = 0; i < result.Scenarios.Count; i++)
            {
                TestLabScenarioResult scenario = result.Scenarios[i];
                builder.AppendLine("    {");
                AppendJsonProperty(builder, "suiteId", scenario.SuiteId, 3, comma: true);
                AppendJsonProperty(builder, "scenarioId", scenario.ScenarioId, 3, comma: true);
                AppendJsonProperty(builder, "displayName", scenario.DisplayName, 3, comma: true);
                AppendJsonProperty(builder, "status", scenario.Status.ToString(), 3, comma: true);
                builder.AppendLine("      \"steps\": [");
                for (int stepIndex = 0; stepIndex < scenario.Steps.Count; stepIndex++)
                {
                    TestLabAutomationStepResult step = scenario.Steps[stepIndex];
                    builder.AppendLine("        {");
                    AppendJsonProperty(builder, "stepId", step.StepId, 5, comma: true);
                    AppendJsonProperty(builder, "displayName", step.DisplayName, 5, comma: true);
                    AppendJsonProperty(builder, "status", step.Status.ToString(), 5, comma: true);
                    AppendJsonProperty(builder, "assertionType", step.AssertionType, 5, comma: true);
                    AppendJsonProperty(builder, "expected", step.Expected, 5, comma: true);
                    AppendJsonProperty(builder, "actual", step.Actual, 5, comma: true);
                    AppendJsonProperty(builder, "actorId", step.ActorId, 5, comma: true);
                    AppendJsonProperty(builder, "transactionId", step.TransactionId, 5, comma: true);
                    AppendJsonProperty(builder, "diagnostics", step.Diagnostics, 5, comma: true);
                    AppendJsonProperty(builder, "exceptionType", step.ExceptionType, 5, comma: true);
                    AppendJsonProperty(builder, "exceptionMessage", step.ExceptionMessage, 5, comma: false);
                    builder.Append(stepIndex == scenario.Steps.Count - 1 ? "        }" : "        },");
                    builder.AppendLine();
                }

                builder.AppendLine("      ]");
                builder.Append(i == result.Scenarios.Count - 1 ? "    }" : "    },");
                builder.AppendLine();
            }

            builder.AppendLine("  ]");
            builder.AppendLine("}");
            return builder.ToString();
        }

        public string BuildMarkdown(TestLabAutomationResult result)
        {
            if (result == null)
            {
                return "# Test Lab Automation\n\nNo result available.\n";
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("# Test Lab Automation Report");
            builder.AppendLine();
            builder.AppendLine($"Run: `{result.RunId}`");
            builder.AppendLine($"Mode: `{result.RunMode}`");
            builder.AppendLine($"Started UTC: `{result.StartedAtUtc:O}`");
            builder.AppendLine($"Ended UTC: `{result.EndedAtUtc:O}`");
            builder.AppendLine($"Totals: {result.PassedScenarios} passed, {result.FailedScenarios} failed, {result.ErrorScenarios} error, {result.SkippedScenarios} skipped, {result.CancelledScenarios} cancelled, {result.TotalSteps} steps.");
            builder.AppendLine();

            foreach (TestLabScenarioResult scenario in result.Scenarios)
            {
                builder.AppendLine($"## {scenario.SuiteId} / {scenario.ScenarioId} - {scenario.Status}");
                foreach (TestLabAutomationStepResult step in scenario.Steps)
                {
                    builder.AppendLine($"- `{step.Status}` {step.StepId}: {step.Diagnostics}");
                    if (step.Status == TestLabAutomationStatus.Failed || step.Status == TestLabAutomationStatus.Error)
                    {
                        builder.AppendLine($"  Expected: `{step.Expected}` Actual: `{step.Actual}` Assertion: `{step.AssertionType}`");
                    }
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static void EnsureReportDirectory()
        {
            if (!Directory.Exists(ReportDirectory))
            {
                Directory.CreateDirectory(ReportDirectory);
            }
        }

        private static string BuildReportFileName(TestLabAutomationResult result, string extension)
        {
            string runId = result == null ? "no-run" : result.RunId;
            return $"test-lab-automation-{runId}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.{extension}";
        }

        private static void AppendJsonProperty(StringBuilder builder, string name, string value, int indent, bool comma, bool quoteValue = true)
        {
            builder.Append(' ', indent * 2);
            builder.Append('"').Append(Escape(name)).Append("\": ");
            if (quoteValue)
            {
                builder.Append('"').Append(Escape(value)).Append('"');
            }
            else
            {
                builder.Append(value);
            }

            if (comma)
            {
                builder.Append(',');
            }

            builder.AppendLine();
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }
    }
}
#endif
