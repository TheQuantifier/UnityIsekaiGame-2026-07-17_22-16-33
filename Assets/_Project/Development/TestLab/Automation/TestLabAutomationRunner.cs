#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityIsekaiGame.Development.Automation
{
    public sealed class TestLabAutomationRunner : ITestLabAutomationRunner
    {
        private readonly PrototypeTestLabService service;
        private readonly TestLabAutomationRegistry registry;
        private readonly ITestLabAutomationResetCoordinator resetCoordinator;
        private readonly TestLabAutomationTransactionIds transactionIds = new TestLabAutomationTransactionIds();
        private readonly List<(string SuiteId, string ScenarioId)> failedSelections = new List<(string SuiteId, string ScenarioId)>();
        private int runCounter;

        public TestLabAutomationRunner(PrototypeTestLabService service, TestLabAutomationRegistry registry, ITestLabAutomationResetCoordinator resetCoordinator)
        {
            this.service = service;
            this.registry = registry;
            this.resetCoordinator = resetCoordinator;
        }

        public TestLabAutomationResult LastResult { get; private set; }
        public bool IsCancellationRequested { get; private set; }

        public TestLabAutomationResult RunScenario(string suiteId, string scenarioId, TestLabAutomationOptions options)
        {
            if (!registry.TryGetScenario(suiteId, scenarioId, out ITestLabAutomationSuite suite, out ITestLabAutomationScenario scenario))
            {
                return CompleteRun(CreateRunId(), TestLabAutomationRunMode.SelectedScenario, DateTime.UtcNow, false, new[]
                {
                    MissingScenarioResult(suiteId, scenarioId)
                });
            }

            return RunSelected(TestLabAutomationRunMode.SelectedScenario, new[] { (suite, scenario) }, options);
        }

        public TestLabAutomationResult RunSuite(string suiteId, TestLabAutomationOptions options)
        {
            if (!registry.TryGetSuite(suiteId, out ITestLabAutomationSuite suite))
            {
                return CompleteRun(CreateRunId(), TestLabAutomationRunMode.CurrentSuite, DateTime.UtcNow, false, new[]
                {
                    MissingScenarioResult(suiteId, string.Empty)
                });
            }

            return RunSelected(TestLabAutomationRunMode.CurrentSuite, suite.Scenarios
                .OrderBy(scenario => scenario.Order)
                .ThenBy(scenario => scenario.ScenarioId, StringComparer.Ordinal)
                .Select(scenario => (suite, scenario)), options);
        }

        public TestLabAutomationResult RunAll(bool quickOnly, TestLabAutomationOptions options)
        {
            IEnumerable<(ITestLabAutomationSuite Suite, ITestLabAutomationScenario Scenario)> selections =
                from suite in registry.Suites
                where suite.IncludeInRunAll
                from scenario in suite.Scenarios.OrderBy(scenario => scenario.Order).ThenBy(scenario => scenario.ScenarioId, StringComparer.Ordinal)
                where !quickOnly || scenario.IncludeInQuickRun || scenario.Category == TestLabAutomationCategory.Quick
                select (suite, scenario);

            return RunSelected(quickOnly ? TestLabAutomationRunMode.AllQuickSuites : TestLabAutomationRunMode.AllSuites, selections, options);
        }

        public TestLabAutomationResult RerunFailed(TestLabAutomationOptions options)
        {
            List<(ITestLabAutomationSuite Suite, ITestLabAutomationScenario Scenario)> selections = new List<(ITestLabAutomationSuite Suite, ITestLabAutomationScenario Scenario)>();
            foreach ((string suiteId, string scenarioId) in failedSelections.ToArray())
            {
                if (registry.TryGetScenario(suiteId, scenarioId, out ITestLabAutomationSuite suite, out ITestLabAutomationScenario scenario))
                {
                    selections.Add((suite, scenario));
                }
            }

            return RunSelected(TestLabAutomationRunMode.RerunFailed, selections, options);
        }

        public void Cancel()
        {
            IsCancellationRequested = true;
        }

        private TestLabAutomationResult RunSelected(TestLabAutomationRunMode mode, IEnumerable<(ITestLabAutomationSuite Suite, ITestLabAutomationScenario Scenario)> selections, TestLabAutomationOptions options)
        {
            options = options ?? TestLabAutomationOptions.Default;
            IsCancellationRequested = false;
            string runId = CreateRunId();
            DateTime started = DateTime.UtcNow;
            List<TestLabScenarioResult> results = new List<TestLabScenarioResult>();
            TestLabAutomationContext context = new TestLabAutomationContext(service, registry, resetCoordinator, transactionIds, new TestLabAutomationEventCapture(), runId);

            foreach ((ITestLabAutomationSuite suite, ITestLabAutomationScenario scenario) in selections.ToArray())
            {
                if (IsCancellationRequested)
                {
                    results.Add(CancelledScenarioResult(suite, scenario));
                    continue;
                }

                TestLabScenarioResult result = RunOne(context, suite, scenario);
                results.Add(result);
                if (options.StopOnFirstFailure && (result.Status == TestLabAutomationStatus.Failed || result.Status == TestLabAutomationStatus.Error))
                {
                    IsCancellationRequested = true;
                }
            }

            context.EventCapture.Dispose();
            failedSelections.Clear();
            failedSelections.AddRange(results
                .Where(result => result.Status == TestLabAutomationStatus.Failed || result.Status == TestLabAutomationStatus.Error)
                .Select(result => (result.SuiteId, result.ScenarioId)));

            return CompleteRun(runId, mode, started, IsCancellationRequested, results);
        }

        private TestLabScenarioResult RunOne(TestLabAutomationContext context, ITestLabAutomationSuite suite, ITestLabAutomationScenario scenario)
        {
            DateTime started = DateTime.UtcNow;
            List<TestLabAutomationStepResult> steps = new List<TestLabAutomationStepResult>();
            context.CurrentSuiteId = suite.SuiteId;
            context.CurrentScenarioId = scenario.ScenarioId;
            context.CurrentStepIndex = 0;
            context.CancellationRequested = false;

            try
            {
                AddIfMeaningful(steps, context.ResetCoordinator.Reset(context, $"Preparing {suite.SuiteId}/{scenario.ScenarioId}."));
                AddIfMeaningful(steps, suite.Setup(context));
                AddIfMeaningful(steps, scenario.Setup(context));

                foreach (ITestLabScenarioStep step in scenario.Steps)
                {
                    if (IsCancellationRequested)
                    {
                        steps.Add(TestLabAssertions.Cancelled(step.StepId, step.DisplayName, "Run cancellation requested."));
                        break;
                    }

                    context.CurrentStepIndex++;
                    TestLabAutomationStepResult stepResult = RunStep(context, step);
                    steps.Add(stepResult);
                    if (!stepResult.Succeeded)
                    {
                        break;
                    }
                }
            }
            catch (Exception exception)
            {
                steps.Add(TestLabAssertions.Error("scenario.exception", scenario.DisplayName, exception));
            }
            finally
            {
                AddIfMeaningful(steps, SafeRun("scenario.cleanup", "Scenario cleanup", () => scenario.Cleanup(context)));
                AddIfMeaningful(steps, SafeRun("suite.teardown", "Suite teardown", () => suite.Teardown(context)));
                AddIfMeaningful(steps, SafeRun("reset.cleanup", "Reset after scenario", () => context.ResetCoordinator.Reset(context, $"Cleaning {suite.SuiteId}/{scenario.ScenarioId}.")));
            }

            TestLabAutomationStatus status = ResolveScenarioStatus(steps);
            return new TestLabScenarioResult(suite.SuiteId, scenario.ScenarioId, scenario.DisplayName, status, started, DateTime.UtcNow, steps);
        }

        private static TestLabAutomationStepResult RunStep(TestLabAutomationContext context, ITestLabScenarioStep step)
        {
            try
            {
                return step.Run(context);
            }
            catch (Exception exception)
            {
                return TestLabAssertions.Error(step.StepId, step.DisplayName, exception);
            }
        }

        private static TestLabAutomationStepResult SafeRun(string stepId, string displayName, Func<TestLabAutomationStepResult> action)
        {
            try
            {
                return action == null ? TestLabAssertions.Pass(stepId, displayName) : action();
            }
            catch (Exception exception)
            {
                return TestLabAssertions.Error(stepId, displayName, exception);
            }
        }

        private static void AddIfMeaningful(List<TestLabAutomationStepResult> steps, TestLabAutomationStepResult step)
        {
            if (step != null)
            {
                steps.Add(step);
            }
        }

        private static TestLabAutomationStatus ResolveScenarioStatus(IReadOnlyList<TestLabAutomationStepResult> steps)
        {
            if (steps.Any(step => step.Status == TestLabAutomationStatus.Error))
            {
                return TestLabAutomationStatus.Error;
            }

            if (steps.Any(step => step.Status == TestLabAutomationStatus.Failed))
            {
                return TestLabAutomationStatus.Failed;
            }

            if (steps.Any(step => step.Status == TestLabAutomationStatus.Cancelled))
            {
                return TestLabAutomationStatus.Cancelled;
            }

            return steps.Count == 0 || steps.All(step => step.Status == TestLabAutomationStatus.Skipped)
                ? TestLabAutomationStatus.Skipped
                : TestLabAutomationStatus.Passed;
        }

        private TestLabAutomationResult CompleteRun(string runId, TestLabAutomationRunMode mode, DateTime started, bool cancelled, IEnumerable<TestLabScenarioResult> scenarios)
        {
            LastResult = new TestLabAutomationResult(runId, mode, started, DateTime.UtcNow, cancelled, scenarios);
            return LastResult;
        }

        private string CreateRunId()
        {
            runCounter++;
            return $"run-{runCounter:0000}";
        }

        private static TestLabScenarioResult MissingScenarioResult(string suiteId, string scenarioId)
        {
            DateTime now = DateTime.UtcNow;
            string id = string.IsNullOrWhiteSpace(scenarioId) ? "missing-suite" : scenarioId;
            return new TestLabScenarioResult(suiteId, id, id, TestLabAutomationStatus.Failed, now, now, new[]
            {
                TestLabAssertions.Fail("select", "Resolve automation selection", "NotNull", "registered suite/scenario", "missing", $"Suite '{suiteId}' scenario '{scenarioId}' was not registered.")
            });
        }

        private static TestLabScenarioResult CancelledScenarioResult(ITestLabAutomationSuite suite, ITestLabAutomationScenario scenario)
        {
            DateTime now = DateTime.UtcNow;
            return new TestLabScenarioResult(suite.SuiteId, scenario.ScenarioId, scenario.DisplayName, TestLabAutomationStatus.Cancelled, now, now, new[]
            {
                TestLabAssertions.Cancelled("cancelled", "Scenario not run", "Run cancellation requested before scenario started.")
            });
        }
    }
}
#endif
