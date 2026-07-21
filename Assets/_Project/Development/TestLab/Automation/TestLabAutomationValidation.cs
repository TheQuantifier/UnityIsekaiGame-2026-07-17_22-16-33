#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityIsekaiGame.Development.Automation
{
    public sealed class TestLabAutomationValidationResult
    {
        private readonly IReadOnlyList<string> errors;
        private readonly IReadOnlyList<string> warnings;

        public TestLabAutomationValidationResult(IEnumerable<string> errors, IEnumerable<string> warnings)
        {
            this.errors = new List<string>(errors ?? Array.Empty<string>()).AsReadOnly();
            this.warnings = new List<string>(warnings ?? Array.Empty<string>()).AsReadOnly();
        }

        public IReadOnlyList<string> Errors => errors;
        public IReadOnlyList<string> Warnings => warnings;
        public bool Succeeded => errors.Count == 0;

        public string ToSummary()
        {
            return $"Automation validation: {errors.Count} error(s), {warnings.Count} warning(s).";
        }
    }

    public static class TestLabAutomationValidation
    {
        public static TestLabAutomationValidationResult Validate(TestLabAutomationRegistry registry)
        {
            List<string> errors = new List<string>();
            List<string> warnings = new List<string>();

            if (registry == null)
            {
                errors.Add("Automation registry is missing.");
                return new TestLabAutomationValidationResult(errors, warnings);
            }

            HashSet<string> suiteIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (ITestLabAutomationSuite suite in registry.Suites)
            {
                ValidateSuite(suite, suiteIds, errors, warnings);
            }

            return new TestLabAutomationValidationResult(errors, warnings);
        }

        private static void ValidateSuite(ITestLabAutomationSuite suite, HashSet<string> suiteIds, List<string> errors, List<string> warnings)
        {
            if (suite == null)
            {
                errors.Add("Null suite registered.");
                return;
            }

            if (string.IsNullOrWhiteSpace(suite.SuiteId))
            {
                errors.Add("Suite has no stable suite ID.");
            }
            else if (!suiteIds.Add(suite.SuiteId))
            {
                errors.Add($"Duplicate suite ID '{suite.SuiteId}'.");
            }

            if (string.IsNullOrWhiteSpace(suite.DisplayName))
            {
                errors.Add($"Suite '{suite.SuiteId}' has no display name.");
            }

            if (suite.Scenarios == null || suite.Scenarios.Count == 0)
            {
                errors.Add($"Suite '{suite.SuiteId}' has no scenarios.");
            }

            if (suite.RequiredServices == null || suite.RequiredServices.Count == 0)
            {
                warnings.Add($"Suite '{suite.SuiteId}' has no required service declarations.");
            }

            HashSet<string> scenarioIds = new HashSet<string>(StringComparer.Ordinal);
            int previousOrder = int.MinValue;
            foreach (ITestLabAutomationScenario scenario in suite.Scenarios ?? Array.Empty<ITestLabAutomationScenario>())
            {
                if (scenario == null)
                {
                    errors.Add($"Suite '{suite.SuiteId}' contains a null scenario.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(scenario.ScenarioId))
                {
                    errors.Add($"Suite '{suite.SuiteId}' contains a scenario with no stable scenario ID.");
                }
                else if (!scenarioIds.Add(scenario.ScenarioId))
                {
                    errors.Add($"Suite '{suite.SuiteId}' has duplicate scenario ID '{scenario.ScenarioId}'.");
                }

                if (string.IsNullOrWhiteSpace(scenario.DisplayName))
                {
                    errors.Add($"Scenario '{suite.SuiteId}/{scenario.ScenarioId}' has no display name.");
                }

                if (scenario.Order < previousOrder)
                {
                    errors.Add($"Scenario '{suite.SuiteId}/{scenario.ScenarioId}' is not in deterministic order.");
                }

                previousOrder = scenario.Order;

                if (scenario.Steps == null || scenario.Steps.Count == 0)
                {
                    errors.Add($"Scenario '{suite.SuiteId}/{scenario.ScenarioId}' has no steps.");
                }
            }
        }
    }
}
#endif
