using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityIsekaiGame.Development.Automation;

namespace UnityIsekaiGame.Tests
{
    public sealed class TestLabAutomationFrameworkTests
    {
        [Test]
        public void SuiteRegistration_IsDeterministic()
        {
            TestLabAutomationRegistry registry = new TestLabAutomationRegistry();
            Assert.That(registry.TryRegister(Suite("suite.b", 20, Scenario("scenario.b", 20, PassStep("b"))), out _), Is.True);
            Assert.That(registry.TryRegister(Suite("suite.a", 10, Scenario("scenario.a", 10, PassStep("a"))), out _), Is.True);

            Assert.That(registry.Suites.Select(suite => suite.SuiteId), Is.EqualTo(new[] { "suite.a", "suite.b" }));
        }

        [Test]
        public void DuplicateSuiteIds_AreRejected()
        {
            TestLabAutomationRegistry registry = new TestLabAutomationRegistry();
            Assert.That(registry.TryRegister(Suite("suite.duplicate", 10, Scenario("scenario.one", 10, PassStep("one"))), out _), Is.True);

            bool registered = registry.TryRegister(Suite("suite.duplicate", 20, Scenario("scenario.two", 10, PassStep("two"))), out string failure);

            Assert.That(registered, Is.False);
            Assert.That(failure, Does.Contain("Duplicate suite ID"));
        }

        [Test]
        public void DuplicateScenarioIds_AreValidationErrors()
        {
            TestLabAutomationRegistry registry = Registry(Suite("suite", 10,
                Scenario("scenario.same", 10, PassStep("one")),
                Scenario("scenario.same", 20, PassStep("two"))));

            TestLabAutomationValidationResult result = TestLabAutomationValidation.Validate(registry);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors.Any(error => error.Contains("duplicate scenario ID")), Is.True);
        }

        [Test]
        public void SelectedScenario_RunsOnlyOnce()
        {
            int count = 0;
            TestLabAutomationRunner runner = Runner(Registry(Suite("suite", 10,
                Scenario("scenario.one", 10, CountStep("one", () => count++)),
                Scenario("scenario.two", 20, CountStep("two", () => count++)))));

            TestLabAutomationResult result = runner.RunScenario("suite", "scenario.two", TestLabAutomationOptions.Default);

            Assert.That(count, Is.EqualTo(1));
            Assert.That(result.Scenarios.Single().ScenarioId, Is.EqualTo("scenario.two"));
        }

        [Test]
        public void CurrentSuite_RunsScenariosInOrder()
        {
            List<string> order = new List<string>();
            TestLabAutomationRunner runner = Runner(Registry(Suite("suite", 10,
                Scenario("scenario.b", 20, CountStep("b", () => order.Add("b"))),
                Scenario("scenario.a", 10, CountStep("a", () => order.Add("a"))))));

            runner.RunSuite("suite", TestLabAutomationOptions.Default);

            Assert.That(order, Is.EqualTo(new[] { "a", "b" }));
        }

        [Test]
        public void RunAll_RespectsSuiteOrdering()
        {
            List<string> order = new List<string>();
            TestLabAutomationRunner runner = Runner(Registry(
                Suite("suite.b", 20, Scenario("scenario", 10, CountStep("b", () => order.Add("b")))),
                Suite("suite.a", 10, Scenario("scenario", 10, CountStep("a", () => order.Add("a"))))));

            runner.RunAll(quickOnly: false, TestLabAutomationOptions.Default);

            Assert.That(order, Is.EqualTo(new[] { "a", "b" }));
        }

        [Test]
        public void RunAllQuick_RunsOnlyQuickScenarios()
        {
            int quick = 0;
            int standard = 0;
            ITestLabAutomationScenario quickScenario = new TestLabAutomationScenario("quick", "quick", "quick", 10, TestLabAutomationCategory.Quick, true, new[] { CountStep("quick", () => quick++) });
            ITestLabAutomationScenario standardScenario = new TestLabAutomationScenario("standard", "standard", "standard", 20, TestLabAutomationCategory.Standard, false, new[] { CountStep("standard", () => standard++) });
            TestLabAutomationRunner runner = Runner(Registry(Suite("suite", 10, quickScenario, standardScenario)));

            TestLabAutomationResult result = runner.RunAll(quickOnly: true, TestLabAutomationOptions.Default);

            Assert.That(result.TotalScenarios, Is.EqualTo(1));
            Assert.That(quick, Is.EqualTo(1));
            Assert.That(standard, Is.Zero);
        }

        [Test]
        public void RerunFailed_RunsOnlyPreviouslyFailedScenarios()
        {
            int passedCount = 0;
            TestLabAutomationRunner runner = Runner(Registry(Suite("suite", 10,
                Scenario("fail", 10, FailStep("fail")),
                Scenario("pass", 20, CountStep("pass", () => passedCount++)))));

            runner.RunSuite("suite", new TestLabAutomationOptions { StopOnFirstFailure = false });
            TestLabAutomationResult rerun = runner.RerunFailed(TestLabAutomationOptions.Default);

            Assert.That(rerun.TotalScenarios, Is.EqualTo(1));
            Assert.That(rerun.Scenarios.Single().ScenarioId, Is.EqualTo("fail"));
            Assert.That(passedCount, Is.EqualTo(1));
        }

        [Test]
        public void StopOnFirstFailure_StopsSubsequentScenarios()
        {
            int count = 0;
            TestLabAutomationRunner runner = Runner(Registry(Suite("suite", 10,
                Scenario("fail", 10, FailStep("fail")),
                Scenario("pass", 20, CountStep("pass", () => count++)))));

            TestLabAutomationResult result = runner.RunSuite("suite", new TestLabAutomationOptions { StopOnFirstFailure = true });

            Assert.That(result.Scenarios[0].Status, Is.EqualTo(TestLabAutomationStatus.Failed));
            Assert.That(result.Scenarios[1].Status, Is.EqualTo(TestLabAutomationStatus.Cancelled));
            Assert.That(count, Is.Zero);
        }

        [Test]
        public void ContinueOnFailure_RunsRemainingScenarios()
        {
            int count = 0;
            TestLabAutomationRunner runner = Runner(Registry(Suite("suite", 10,
                Scenario("fail", 10, FailStep("fail")),
                Scenario("pass", 20, CountStep("pass", () => count++)))));

            TestLabAutomationResult result = runner.RunSuite("suite", new TestLabAutomationOptions { StopOnFirstFailure = false });

            Assert.That(result.Scenarios[0].Status, Is.EqualTo(TestLabAutomationStatus.Failed));
            Assert.That(result.Scenarios[1].Status, Is.EqualTo(TestLabAutomationStatus.Passed));
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void FailedAssertion_ReportsExpectedAndActualValues()
        {
            TestLabAutomationStepResult result = TestLabAssertions.Equal("equal", "Equal", 1, 2, "numbers");

            Assert.That(result.Status, Is.EqualTo(TestLabAutomationStatus.Failed));
            Assert.That(result.Expected, Is.EqualTo("1"));
            Assert.That(result.Actual, Is.EqualTo("2"));
            Assert.That(result.Diagnostics, Is.EqualTo("numbers"));
        }

        [Test]
        public void ValidationAssertions_ReportSucceededAndFailedStates()
        {
            TestLabAutomationValidationResult success = new TestLabAutomationValidationResult(Array.Empty<string>(), Array.Empty<string>());
            TestLabAutomationValidationResult failure = new TestLabAutomationValidationResult(new[] { "bad" }, Array.Empty<string>());

            Assert.That(TestLabAssertions.ValidationSucceeded("valid", "Valid", success).Status, Is.EqualTo(TestLabAutomationStatus.Passed));
            Assert.That(TestLabAssertions.ValidationFailed("invalid", "Invalid", failure).Status, Is.EqualTo(TestLabAutomationStatus.Passed));
        }

        [Test]
        public void CountAndSequenceAssertions_ReportExpectedResults()
        {
            Assert.That(TestLabAssertions.Count("count", "Count", 2, new[] { "a", "b" }).Status, Is.EqualTo(TestLabAutomationStatus.Passed));
            Assert.That(TestLabAssertions.SequenceEqual("sequence", "Sequence", new[] { 1, 2 }, new[] { 1, 2 }).Status, Is.EqualTo(TestLabAutomationStatus.Passed));
        }

        [Test]
        public void UnexpectedException_ProducesErrorStatus()
        {
            TestLabAutomationRunner runner = Runner(Registry(Suite("suite", 10,
                Scenario("throws", 10, new TestLabScenarioStep("throw", "Throw", _ => throw new InvalidOperationException("boom"))))));

            TestLabAutomationResult result = runner.RunSuite("suite", TestLabAutomationOptions.Default);

            Assert.That(result.Scenarios.Single().Status, Is.EqualTo(TestLabAutomationStatus.Error));
            Assert.That(result.Scenarios.Single().Steps.Any(step => step.ExceptionMessage == "boom"), Is.True);
        }

        [Test]
        public void Cleanup_RunsAfterPassFailureAndException()
        {
            int cleanupCount = 0;
            TestLabAutomationRunner runner = Runner(Registry(Suite("suite", 10,
                Scenario("pass", 10, PassStep("pass"), cleanup: _ => { cleanupCount++; return TestLabAssertions.Pass("cleanup", "Cleanup"); }),
                Scenario("fail", 20, FailStep("fail"), cleanup: _ => { cleanupCount++; return TestLabAssertions.Pass("cleanup", "Cleanup"); }),
                Scenario("error", 30, new TestLabScenarioStep("error", "Error", _ => throw new InvalidOperationException()), cleanup: _ => { cleanupCount++; return TestLabAssertions.Pass("cleanup", "Cleanup"); }))));

            runner.RunSuite("suite", new TestLabAutomationOptions { StopOnFirstFailure = false });

            Assert.That(cleanupCount, Is.EqualTo(3));
        }

        [Test]
        public void Cancellation_MarksRemainingScenarios()
        {
            TestLabAutomationRunner runner = null;
            runner = Runner(Registry(Suite("suite", 10,
                Scenario("cancel", 10, new TestLabScenarioStep("cancel", "Cancel", _ => { runner.Cancel(); return TestLabAssertions.Pass("cancel", "Cancel"); })),
                Scenario("remaining", 20, PassStep("remaining")))));

            TestLabAutomationResult result = runner.RunSuite("suite", TestLabAutomationOptions.Default);

            Assert.That(result.Scenarios[0].Status, Is.EqualTo(TestLabAutomationStatus.Passed));
            Assert.That(result.Scenarios[1].Status, Is.EqualTo(TestLabAutomationStatus.Cancelled));
        }

        [Test]
        public void TransactionIds_AreDeterministic()
        {
            TestLabAutomationTransactionIds ids = new TestLabAutomationTransactionIds();

            string first = ids.Create("suite.id", "scenario.id", "run-0001", 3, "execute");
            string second = ids.Create("suite.id", "scenario.id", "run-0001", 3, "execute");

            Assert.That(second, Is.EqualTo(first));
            Assert.That(first, Does.Contain("suite.id"));
            Assert.That(first, Does.Contain("scenario.id"));
            Assert.That(first, Does.Contain("step-003"));
        }

        [Test]
        public void EventCapture_PreservesOrderAndRemovesSubscriptions()
        {
            int unsubscribeCount = 0;
            using (TestLabAutomationEventCapture capture = new TestLabAutomationEventCapture())
            {
                capture.AddSubscription(() => unsubscribeCount++);
                capture.Record("first");
                capture.Record("second");

                Assert.That(capture.OccurredBefore("first", "second"), Is.True);
                Assert.That(capture.HasEvent("first"), Is.True);
                Assert.That(capture.HasNoEvent("missing"), Is.True);
            }

            Assert.That(unsubscribeCount, Is.EqualTo(1));
        }

        [Test]
        public void BaselineReset_IsolatesTrackedRuntimeBuckets()
        {
            FakeResetCoordinator reset = new FakeResetCoordinator();
            TestLabAutomationRunner runner = Runner(Registry(Suite("suite", 10, Scenario("scenario", 10, PassStep("pass")))), reset);

            runner.RunSuite("suite", TestLabAutomationOptions.Default);

            Assert.That(reset.ResourceResets, Is.EqualTo(2));
            Assert.That(reset.LifecycleResets, Is.EqualTo(2));
            Assert.That(reset.CombatStateResets, Is.EqualTo(2));
            Assert.That(reset.DefenseResets, Is.EqualTo(2));
            Assert.That(reset.ExecutionResets, Is.EqualTo(2));
        }

        [Test]
        public void PreviewAssertions_DetectMutation()
        {
            int before = 1;
            int after = 2;

            TestLabAutomationStepResult result = TestLabAssertions.RevisionUnchanged("preview", "Preview", before, after);

            Assert.That(result.Status, Is.EqualTo(TestLabAutomationStatus.Failed));
            Assert.That(result.AssertionType, Is.EqualTo("RevisionUnchanged"));
        }

        [Test]
        public void ReportExport_ProducesJsonOutsideAssets()
        {
            TestLabAutomationResult result = new TestLabAutomationResult("run-test", TestLabAutomationRunMode.SelectedScenario, DateTime.UtcNow, DateTime.UtcNow, false, new[]
            {
                new TestLabScenarioResult("suite", "scenario", "Scenario", TestLabAutomationStatus.Passed, DateTime.UtcNow, DateTime.UtcNow, new[] { TestLabAssertions.Pass("step", "Step") })
            });
            TestLabAutomationReportExporter exporter = new TestLabAutomationReportExporter();

            string path = exporter.ExportJson(result);

            Assert.That(path.Replace('\\', '/'), Does.StartWith("Temp/TestLabAutomation/"));
            Assert.That(path.Replace('\\', '/'), Does.Not.StartWith("Assets/"));
            Assert.That(File.Exists(path), Is.True);
            Assert.That(File.ReadAllText(path), Does.Contain("\"runId\": \"run-test\""));
        }

        [Test]
        public void ReportExport_ProducesMarkdownOutsideAssets()
        {
            TestLabAutomationResult result = new TestLabAutomationResult("run-md", TestLabAutomationRunMode.AllSuites, DateTime.UtcNow, DateTime.UtcNow, false, new[]
            {
                new TestLabScenarioResult("suite", "scenario", "Scenario", TestLabAutomationStatus.Passed, DateTime.UtcNow, DateTime.UtcNow, new[] { TestLabAssertions.Pass("step", "Step") })
            });
            TestLabAutomationReportExporter exporter = new TestLabAutomationReportExporter();

            string path = exporter.ExportMarkdown(result);

            Assert.That(path.Replace('\\', '/'), Does.StartWith("Temp/TestLabAutomation/"));
            Assert.That(path.Replace('\\', '/'), Does.Not.StartWith("Assets/"));
            Assert.That(File.ReadAllText(path), Does.Contain("run-md"));
        }

        [Test]
        public void ImmutableResults_ExposeNoMutableCollections()
        {
            List<TestLabAutomationStepResult> steps = new List<TestLabAutomationStepResult> { TestLabAssertions.Pass("one", "One") };
            TestLabScenarioResult scenario = new TestLabScenarioResult("suite", "scenario", "Scenario", TestLabAutomationStatus.Passed, DateTime.UtcNow, DateTime.UtcNow, steps);
            steps.Add(TestLabAssertions.Pass("two", "Two"));

            Assert.That(scenario.Steps.Count, Is.EqualTo(1));
            Assert.That(scenario.Steps, Is.Not.InstanceOf<List<TestLabAutomationStepResult>>());
        }

        [Test]
        public void ResultTotals_AreAccurate()
        {
            DateTime now = DateTime.UtcNow;
            TestLabAutomationResult result = new TestLabAutomationResult("run", TestLabAutomationRunMode.AllSuites, now, now, false, new[]
            {
                new TestLabScenarioResult("suite", "pass", "Pass", TestLabAutomationStatus.Passed, now, now, new[] { TestLabAssertions.Pass("step", "Step") }),
                new TestLabScenarioResult("suite", "fail", "Fail", TestLabAutomationStatus.Failed, now, now, new[] { TestLabAssertions.Fail("step", "Step", "Equal", 1, 2) }),
                new TestLabScenarioResult("suite", "skip", "Skip", TestLabAutomationStatus.Skipped, now, now, Array.Empty<TestLabAutomationStepResult>())
            });

            Assert.That(result.TotalScenarios, Is.EqualTo(3));
            Assert.That(result.PassedScenarios, Is.EqualTo(1));
            Assert.That(result.FailedScenarios, Is.EqualTo(1));
            Assert.That(result.SkippedScenarios, Is.EqualTo(1));
            Assert.That(result.TotalSteps, Is.EqualTo(2));
        }

        [Test]
        public void MissingOptionalSuite_IsHandledSafely()
        {
            TestLabAutomationRunner runner = Runner(new TestLabAutomationRegistry());

            TestLabAutomationResult result = runner.RunSuite("missing", TestLabAutomationOptions.Default);

            Assert.That(result.Scenarios.Single().Status, Is.EqualTo(TestLabAutomationStatus.Failed));
            Assert.That(result.Scenarios.Single().Steps.Single().Diagnostics, Does.Contain("not registered").IgnoreCase);
        }

        [Test]
        public void MissingScenario_IsHandledSafely()
        {
            TestLabAutomationRunner runner = Runner(Registry(Suite("suite", 10, Scenario("scenario", 10, PassStep("pass")))));

            TestLabAutomationResult result = runner.RunScenario("suite", "missing", TestLabAutomationOptions.Default);

            Assert.That(result.Scenarios.Single().Status, Is.EqualTo(TestLabAutomationStatus.Failed));
            Assert.That(result.Scenarios.Single().Steps.Single().Diagnostics, Does.Contain("not registered").IgnoreCase);
        }

        [Test]
        public void Validation_CatchesMissingDisplayNameNoScenariosAndMissingServices()
        {
            TestLabAutomationRegistry registry = Registry(new TestLabAutomationSuite("suite", "", "test", "test", 10, TestLabAutomationCategory.Quick, true, Array.Empty<string>(), Array.Empty<ITestLabAutomationScenario>()));

            TestLabAutomationValidationResult result = TestLabAutomationValidation.Validate(registry);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors.Any(error => error.Contains("display name")), Is.True);
            Assert.That(result.Errors.Any(error => error.Contains("no scenarios")), Is.True);
            Assert.That(result.Warnings.Any(warning => warning.Contains("no required service")), Is.True);
        }

        [Test]
        public void Validation_CatchesInvalidScenarioOrderingAndNoSteps()
        {
            TestLabAutomationRegistry registry = Registry(Suite("suite", 10,
                Scenario("late", 20, PassStep("late")),
                new TestLabAutomationScenario("early", "early", "early", 10, TestLabAutomationCategory.Quick, true, Array.Empty<ITestLabScenarioStep>())));

            TestLabAutomationValidationResult result = TestLabAutomationValidation.Validate(registry);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors.Any(error => error.Contains("deterministic order")), Is.True);
            Assert.That(result.Errors.Any(error => error.Contains("no steps")), Is.True);
        }

        [Test]
        public void DefaultPrototypeSuites_RegisterStep3ThroughStep7()
        {
            TestLabAutomationRegistry registry = new TestLabAutomationRegistry();

            PrototypeStep3AutomationSuites.RegisterDefaults(registry);
            PrototypeStep4AutomationSuites.RegisterDefaults(registry);
            PrototypeStep5AutomationSuites.RegisterDefaults(registry);
            PrototypeStep6AutomationSuites.RegisterDefaults(registry);
            PrototypeStep7AutomationSuites.RegisterDefaults(registry);

            TestLabAutomationValidationResult validation = TestLabAutomationValidation.Validate(registry);

            Assert.That(validation.Succeeded, Is.True, string.Join(Environment.NewLine, validation.Errors));
            Assert.That(registry.Suites.Select(suite => suite.SuiteId), Is.EqualTo(new[]
            {
                "feature.3.runtime-taxonomy",
                "feature.4.1.save-file-foundation",
                "feature.4.2.inventory-equipment-persistence",
                "feature.4.3.vitals-status-persistence",
                "feature.4.4.quest-contract-persistence",
                "feature.4.5.location-persistence",
                "feature.4.6.world-entity-identity",
                "feature.4.7.save-slots-autosave-load-ui",
                "feature.4.8.persistence-recovery-hardening",
                "feature.5.1.identity-origin-progression",
                "feature.5.2-5.4a.attributes-calculated-stats",
                "feature.5.3.skills-progression",
                "feature.5.4b.current-resources",
                "feature.5.5.traits-requirements",
                "feature.5.6.character-integration",
                "feature.6.1.damage-healing",
                "feature.6.2.attack-resolution",
                "feature.6.3.lifecycle",
                "feature.6.4.ongoing-effects",
                "feature.6.5.combat-state",
                "feature.6.6.defensive-actions",
                "feature.6.7.combat-execution",
                "feature.6.8.combat-reactions",
                "feature.6.9.combat-contribution",
                "feature.6.10.combat-integration",
                "feature.7.1.body-species"
            }));
        }

        [Test]
        public void RuntimeGameplayAssembly_DoesNotReferenceAutomationTypes()
        {
            string gameplayAsmdef = File.ReadAllText("Assets/_Project/Runtime/UnityIsekaiGame.Gameplay.asmdef");

            Assert.That(gameplayAsmdef, Does.Not.Contain("Development"));
            Assert.That(gameplayAsmdef, Does.Not.Contain("Automation"));
        }

        [Test]
        public void DevelopmentAutomation_IsExcludedFromRuntimeAssembly()
        {
            string developmentAsmdef = File.ReadAllText("Assets/_Project/Development/UnityIsekaiGame.Development.asmdef");

            Assert.That(developmentAsmdef, Does.Contain("UnityIsekaiGame.Gameplay"));
            Assert.That(File.Exists("Assets/_Project/Development/TestLab/Automation/TestLabAutomationRunner.cs"), Is.True);
        }

        private static TestLabAutomationRegistry Registry(params ITestLabAutomationSuite[] suites)
        {
            TestLabAutomationRegistry registry = new TestLabAutomationRegistry();
            foreach (ITestLabAutomationSuite suite in suites)
            {
                registry.TryRegister(suite, out _);
            }

            return registry;
        }

        private static TestLabAutomationRunner Runner(TestLabAutomationRegistry registry, ITestLabAutomationResetCoordinator reset = null)
        {
            return new TestLabAutomationRunner(null, registry, reset ?? new FakeResetCoordinator());
        }

        private static ITestLabAutomationSuite Suite(string suiteId, int order, params ITestLabAutomationScenario[] scenarios)
        {
            return new TestLabAutomationSuite(suiteId, suiteId, "test", "test", order, TestLabAutomationCategory.Quick, true, new[] { "fake" }, scenarios);
        }

        private static ITestLabAutomationScenario Scenario(string scenarioId, int order, ITestLabScenarioStep step, Func<TestLabAutomationContext, TestLabAutomationStepResult> cleanup = null)
        {
            return new TestLabAutomationScenario(scenarioId, scenarioId, scenarioId, order, TestLabAutomationCategory.Quick, true, new[] { step }, cleanup: cleanup);
        }

        private static ITestLabScenarioStep PassStep(string stepId)
        {
            return new TestLabScenarioStep(stepId, stepId, _ => TestLabAssertions.Pass(stepId, stepId));
        }

        private static ITestLabScenarioStep FailStep(string stepId)
        {
            return new TestLabScenarioStep(stepId, stepId, _ => TestLabAssertions.Fail(stepId, stepId, "Equal", 1, 2));
        }

        private static ITestLabScenarioStep CountStep(string stepId, Action action)
        {
            return new TestLabScenarioStep(stepId, stepId, _ =>
            {
                action();
                return TestLabAssertions.Pass(stepId, stepId);
            });
        }

        private sealed class FakeResetCoordinator : ITestLabAutomationResetCoordinator
        {
            public int ResourceResets { get; private set; }
            public int LifecycleResets { get; private set; }
            public int CombatStateResets { get; private set; }
            public int DefenseResets { get; private set; }
            public int ExecutionResets { get; private set; }

            public TestLabAutomationStepResult Reset(TestLabAutomationContext context, string reason)
            {
                ResourceResets++;
                LifecycleResets++;
                CombatStateResets++;
                DefenseResets++;
                ExecutionResets++;
                return TestLabAssertions.Pass("reset", "Reset", reason);
            }
        }
    }
}
