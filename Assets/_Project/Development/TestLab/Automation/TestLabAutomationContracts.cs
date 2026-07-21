#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;

namespace UnityIsekaiGame.Development.Automation
{
    public interface ITestLabAutomationSuite
    {
        string SuiteId { get; }
        string DisplayName { get; }
        string Feature { get; }
        string Description { get; }
        int Order { get; }
        TestLabAutomationCategory Category { get; }
        bool IncludeInRunAll { get; }
        IReadOnlyList<string> RequiredServices { get; }
        IReadOnlyList<ITestLabAutomationScenario> Scenarios { get; }
        TestLabAutomationStepResult Setup(TestLabAutomationContext context);
        TestLabAutomationStepResult Teardown(TestLabAutomationContext context);
    }

    public interface ITestLabAutomationScenario
    {
        string ScenarioId { get; }
        string DisplayName { get; }
        string Description { get; }
        int Order { get; }
        TestLabAutomationCategory Category { get; }
        bool IncludeInQuickRun { get; }
        IReadOnlyList<ITestLabScenarioStep> Steps { get; }
        TestLabAutomationStepResult Setup(TestLabAutomationContext context);
        TestLabAutomationStepResult Cleanup(TestLabAutomationContext context);
    }

    public interface ITestLabScenarioStep
    {
        string StepId { get; }
        string DisplayName { get; }
        TestLabAutomationStepResult Run(TestLabAutomationContext context);
    }

    public interface ITestLabAutomationResetCoordinator
    {
        TestLabAutomationStepResult Reset(TestLabAutomationContext context, string reason);
    }

    public interface ITestLabAutomationRunner
    {
        TestLabAutomationResult LastResult { get; }
        bool IsCancellationRequested { get; }
        TestLabAutomationResult RunScenario(string suiteId, string scenarioId, TestLabAutomationOptions options);
        TestLabAutomationResult RunSuite(string suiteId, TestLabAutomationOptions options);
        TestLabAutomationResult RunAll(bool quickOnly, TestLabAutomationOptions options);
        TestLabAutomationResult RerunFailed(TestLabAutomationOptions options);
        void Cancel();
    }

    public sealed class TestLabAutomationContext
    {
        public TestLabAutomationContext(
            PrototypeTestLabService service,
            TestLabAutomationRegistry registry,
            ITestLabAutomationResetCoordinator resetCoordinator,
            TestLabAutomationTransactionIds transactionIds,
            TestLabAutomationEventCapture eventCapture,
            string runId)
        {
            Service = service;
            Registry = registry;
            ResetCoordinator = resetCoordinator;
            TransactionIds = transactionIds;
            EventCapture = eventCapture;
            RunId = runId ?? string.Empty;
        }

        public PrototypeTestLabService Service { get; }
        public TestLabAutomationRegistry Registry { get; }
        public ITestLabAutomationResetCoordinator ResetCoordinator { get; }
        public TestLabAutomationTransactionIds TransactionIds { get; }
        public TestLabAutomationEventCapture EventCapture { get; }
        public string RunId { get; }
        public string CurrentSuiteId { get; set; }
        public string CurrentScenarioId { get; set; }
        public int CurrentStepIndex { get; set; }
        public bool CancellationRequested { get; set; }
    }

    public sealed class TestLabAutomationSuite : ITestLabAutomationSuite
    {
        private readonly IReadOnlyList<ITestLabAutomationScenario> scenarios;
        private readonly IReadOnlyList<string> requiredServices;
        private readonly Func<TestLabAutomationContext, TestLabAutomationStepResult> setup;
        private readonly Func<TestLabAutomationContext, TestLabAutomationStepResult> teardown;

        public TestLabAutomationSuite(
            string suiteId,
            string displayName,
            string feature,
            string description,
            int order,
            TestLabAutomationCategory category,
            bool includeInRunAll,
            IEnumerable<string> requiredServices,
            IEnumerable<ITestLabAutomationScenario> scenarios,
            Func<TestLabAutomationContext, TestLabAutomationStepResult> setup = null,
            Func<TestLabAutomationContext, TestLabAutomationStepResult> teardown = null)
        {
            SuiteId = suiteId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Feature = feature ?? string.Empty;
            Description = description ?? string.Empty;
            Order = order;
            Category = category;
            IncludeInRunAll = includeInRunAll;
            this.requiredServices = new List<string>(requiredServices ?? Array.Empty<string>()).AsReadOnly();
            this.scenarios = new List<ITestLabAutomationScenario>(scenarios ?? Array.Empty<ITestLabAutomationScenario>()).AsReadOnly();
            this.setup = setup;
            this.teardown = teardown;
        }

        public string SuiteId { get; }
        public string DisplayName { get; }
        public string Feature { get; }
        public string Description { get; }
        public int Order { get; }
        public TestLabAutomationCategory Category { get; }
        public bool IncludeInRunAll { get; }
        public IReadOnlyList<string> RequiredServices => requiredServices;
        public IReadOnlyList<ITestLabAutomationScenario> Scenarios => scenarios;
        public TestLabAutomationStepResult Setup(TestLabAutomationContext context) => setup == null ? TestLabAssertions.Pass("suite.setup", "Suite setup") : setup(context);
        public TestLabAutomationStepResult Teardown(TestLabAutomationContext context) => teardown == null ? TestLabAssertions.Pass("suite.teardown", "Suite teardown") : teardown(context);
    }

    public sealed class TestLabAutomationScenario : ITestLabAutomationScenario
    {
        private readonly IReadOnlyList<ITestLabScenarioStep> steps;
        private readonly Func<TestLabAutomationContext, TestLabAutomationStepResult> setup;
        private readonly Func<TestLabAutomationContext, TestLabAutomationStepResult> cleanup;

        public TestLabAutomationScenario(
            string scenarioId,
            string displayName,
            string description,
            int order,
            TestLabAutomationCategory category,
            bool includeInQuickRun,
            IEnumerable<ITestLabScenarioStep> steps,
            Func<TestLabAutomationContext, TestLabAutomationStepResult> setup = null,
            Func<TestLabAutomationContext, TestLabAutomationStepResult> cleanup = null)
        {
            ScenarioId = scenarioId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Description = description ?? string.Empty;
            Order = order;
            Category = category;
            IncludeInQuickRun = includeInQuickRun;
            this.steps = new List<ITestLabScenarioStep>(steps ?? Array.Empty<ITestLabScenarioStep>()).AsReadOnly();
            this.setup = setup;
            this.cleanup = cleanup;
        }

        public string ScenarioId { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public int Order { get; }
        public TestLabAutomationCategory Category { get; }
        public bool IncludeInQuickRun { get; }
        public IReadOnlyList<ITestLabScenarioStep> Steps => steps;
        public TestLabAutomationStepResult Setup(TestLabAutomationContext context) => setup == null ? TestLabAssertions.Pass("scenario.setup", "Scenario setup") : setup(context);
        public TestLabAutomationStepResult Cleanup(TestLabAutomationContext context) => cleanup == null ? TestLabAssertions.Pass("scenario.cleanup", "Scenario cleanup") : cleanup(context);
    }

    public sealed class TestLabScenarioStep : ITestLabScenarioStep
    {
        private readonly Func<TestLabAutomationContext, TestLabAutomationStepResult> action;

        public TestLabScenarioStep(string stepId, string displayName, Func<TestLabAutomationContext, TestLabAutomationStepResult> action)
        {
            StepId = stepId ?? string.Empty;
            DisplayName = displayName ?? StepId;
            this.action = action;
        }

        public string StepId { get; }
        public string DisplayName { get; }
        public TestLabAutomationStepResult Run(TestLabAutomationContext context) => action == null
            ? TestLabAssertions.Skip(StepId, DisplayName, "No step action is registered.")
            : action(context);
    }
}
#endif
