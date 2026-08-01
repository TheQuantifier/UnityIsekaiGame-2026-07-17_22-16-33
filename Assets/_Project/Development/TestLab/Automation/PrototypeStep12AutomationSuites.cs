#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.Social.Relationships;

namespace UnityIsekaiGame.Development.Automation
{
    [PrototypeTestLabAutomationProvider(12, "Social", 1200)]
    public static class PrototypeStep12AutomationSuites
    {
        public static void RegisterDefaults(TestLabAutomationRegistry registry)
        {
            registry?.TryRegister(new TestLabAutomationSuite(
                "feature.12.1.relationship-identity-records",
                "Relationship Identity and Records",
                "12.1",
                "Persistent person-to-person relationship records with deterministic identity, roles, lifecycle, and persistence.",
                12010,
                TestLabAutomationCategory.Standard,
                includeInRunAll: true,
                requiredServices: new[] { "RelationshipRuntime", "RelationshipDefinition", "RelationshipPersistenceParticipant" },
                scenarios: new[]
                {
                    Scenario("symmetric-and-directed-records", "Symmetric and directed relationships create deterministic records", 10,
                        Step("step12-relationship-create", "Create and query relationships", SymmetricAndDirectedRelationships)),
                    Scenario("lifecycle-and-duplicates", "Relationship lifecycle and duplicate active rules are enforced", 20,
                        Step("step12-relationship-lifecycle", "End and reject duplicate active records", LifecycleAndDuplicates)),
                    Scenario("persistence-validation", "Relationship persistence validates before restoring", 30,
                        Step("step12-relationship-persistence", "Save, restore, and reject corrupt payloads", PersistenceValidation))
                }), out _);
        }

        private static TestLabAutomationStepResult SymmetricAndDirectedRelationships(TestLabAutomationContext context)
        {
            if (!TryGetRuntime(context, out RelationshipRuntime runtime, out DefinitionRegistry registry, out string failure))
            {
                return TestLabAssertions.Fail("step12-relationship-create", "Create and query relationships", "RelationshipRuntime", "MissingRuntime", failure);
            }

            RelationshipOperationResult friend = runtime.CreateRelationship(new RelationshipCreateRequest
            {
                recordId = Scoped(context, "friend"),
                relationshipDefinitionId = PrototypeRelationshipDefinitionFactory.FriendRelationshipId,
                firstPersonId = "person.prototype.friend",
                firstRoleId = "friend",
                secondPersonId = context.ScenarioContext.Runtimes.PersonId,
                secondRoleId = "friend",
                sourceEventId = "event.relationship.friendship-start",
                startWorldTime = 10d,
                transactionId = Tx(context, "friend")
            });
            RelationshipOperationResult parentChild = runtime.CreateRelationship(new RelationshipCreateRequest
            {
                recordId = Scoped(context, "parent-child"),
                relationshipDefinitionId = PrototypeRelationshipDefinitionFactory.ParentChildRelationshipId,
                firstPersonId = "person.prototype.parent",
                firstRoleId = "parent",
                secondPersonId = "person.prototype.child",
                secondRoleId = "child",
                sourceRecordId = "record.relationship.family-register",
                startWorldTime = 1d,
                transactionId = Tx(context, "parent-child")
            });

            RelationshipSnapshot friendSnapshot = friend.Snapshot;
            bool symmetricCanonical = friendSnapshot != null
                && friendSnapshot.Participants.Count == 2
                && string.CompareOrdinal(friendSnapshot.Participants[0].personId, friendSnapshot.Participants[1].personId) <= 0;
            bool valid = registry.Contains(PrototypeRelationshipDefinitionFactory.FriendRelationshipId)
                && friend.Succeeded
                && parentChild.Succeeded
                && symmetricCanonical
                && runtime.QueryBetween(context.ScenarioContext.Runtimes.PersonId, "person.prototype.friend", activeOnly: true).Count == 1
                && runtime.QueryByRole("parent", activeOnly: true).Count == 1
                && runtime.QueryByCategory(RelationshipCategory.Personal, activeOnly: true).Count == 1
                && runtime.QueryByDefinition(PrototypeRelationshipDefinitionFactory.ParentChildRelationshipId, activeOnly: true).Count == 1;
            return TestLabAssertions.True("step12-relationship-create", "Symmetric and directed relationships create deterministic records", valid, $"Friend={friend.Status} Directed={parentChild.Status} Canonical={symmetricCanonical} Count={runtime.Count}");
        }

        private static TestLabAutomationStepResult LifecycleAndDuplicates(TestLabAutomationContext context)
        {
            if (!TryGetRuntime(context, out RelationshipRuntime runtime, out _, out string failure))
            {
                return TestLabAssertions.Fail("step12-relationship-lifecycle", "End and reject duplicate active records", "RelationshipRuntime", "MissingRuntime", failure);
            }

            RelationshipCreateRequest request = new RelationshipCreateRequest
            {
                recordId = Scoped(context, "rival"),
                relationshipDefinitionId = PrototypeRelationshipDefinitionFactory.RivalRelationshipId,
                firstPersonId = context.ScenarioContext.Runtimes.PersonId,
                firstRoleId = "rival",
                secondPersonId = "person.prototype.rival",
                secondRoleId = "rival",
                startWorldTime = 3d,
                transactionId = Tx(context, "rival")
            };
            RelationshipOperationResult create = runtime.CreateRelationship(request);
            RelationshipOperationResult duplicateSameId = runtime.CreateRelationship(request);
            RelationshipOperationResult duplicateActive = runtime.CreateRelationship(new RelationshipCreateRequest
            {
                recordId = Scoped(context, "rival-second"),
                relationshipDefinitionId = PrototypeRelationshipDefinitionFactory.RivalRelationshipId,
                firstPersonId = "person.prototype.rival",
                firstRoleId = "rival",
                secondPersonId = context.ScenarioContext.Runtimes.PersonId,
                secondRoleId = "rival",
                startWorldTime = 4d,
                transactionId = Tx(context, "rival-duplicate")
            });
            RelationshipOperationResult ended = runtime.EndRelationship(new RelationshipEndRequest
            {
                recordId = request.recordId,
                endWorldTime = 9d,
                sourceEventId = "event.relationship.rivalry-ended",
                transactionId = Tx(context, "rival-end")
            });
            RelationshipOperationResult recreate = runtime.CreateRelationship(new RelationshipCreateRequest
            {
                recordId = Scoped(context, "rival-after-end"),
                relationshipDefinitionId = PrototypeRelationshipDefinitionFactory.RivalRelationshipId,
                firstPersonId = "person.prototype.rival",
                firstRoleId = "rival",
                secondPersonId = context.ScenarioContext.Runtimes.PersonId,
                secondRoleId = "rival",
                startWorldTime = 10d,
                transactionId = Tx(context, "rival-recreate")
            });

            bool valid = create.Succeeded
                && duplicateSameId.Duplicate
                && duplicateActive.Status == RelationshipOperationStatus.DuplicateActiveRelationship
                && ended.Succeeded
                && recreate.Succeeded
                && runtime.QueryByStatus(RelationshipLifecycleStatus.Ended).Count == 1
                && runtime.QueryBetween(context.ScenarioContext.Runtimes.PersonId, "person.prototype.rival", activeOnly: true).Count == 1;
            return TestLabAssertions.True("step12-relationship-lifecycle", "Relationship lifecycle and duplicate active rules are enforced", valid, $"Create={create.Status} SameId={duplicateSameId.Status} DuplicateActive={duplicateActive.Status} End={ended.Status} Recreate={recreate.Status}");
        }

        private static TestLabAutomationStepResult PersistenceValidation(TestLabAutomationContext context)
        {
            if (!TryGetRuntime(context, out RelationshipRuntime runtime, out DefinitionRegistry registry, out string failure))
            {
                return TestLabAssertions.Fail("step12-relationship-persistence", "Save, restore, and reject corrupt payloads", "RelationshipRuntime", "MissingRuntime", failure);
            }

            RelationshipOperationResult create = runtime.CreateRelationship(new RelationshipCreateRequest
            {
                recordId = Scoped(context, "mentor"),
                relationshipDefinitionId = PrototypeRelationshipDefinitionFactory.MentorStudentRelationshipId,
                firstPersonId = "person.prototype.mentor",
                firstRoleId = "mentor",
                secondPersonId = "person.prototype.student",
                secondRoleId = "student",
                startWorldTime = 12d,
                sourceEventId = "event.relationship.apprenticeship",
                transactionId = Tx(context, "mentor")
            });
            RelationshipRuntimeSaveData save = runtime.CreateSaveData();
            RelationshipRuntime restored = new RelationshipRuntime();
            RelationshipOperationResult restore = restored.RestoreFromSaveData(save, registry, context.ScenarioContext.Runtimes.KnownPersonIds, restoring: true);
            RelationshipRuntimeSaveData corrupt = save.Clone();
            corrupt.records[0].relationshipDefinitionId = "relationship.prototype.missing";
            bool rejected = !RelationshipRuntime.ValidateSaveData(corrupt, registry, context.ScenarioContext.Runtimes.KnownPersonIds, out string validationFailure);
            int countAfterRejectedValidation = runtime.Count;

            bool valid = create.Succeeded
                && restore.Succeeded
                && restored.Count == runtime.Count
                && restored.TryGetSnapshot(create.Snapshot.RecordId, out RelationshipSnapshot snapshot)
                && snapshot.SourceEventId == "event.relationship.apprenticeship"
                && rejected
                && countAfterRejectedValidation == runtime.Count;
            return TestLabAssertions.True("step12-relationship-persistence", "Relationship persistence validates before restoring", valid, $"Create={create.Status} Restore={restore.Status} Rejected={rejected} Failure='{validationFailure}' Count={runtime.Count}/{restored.Count}");
        }

        private static bool TryGetRuntime(TestLabAutomationContext context, out RelationshipRuntime runtime, out DefinitionRegistry registry, out string failure)
        {
            runtime = context?.ScenarioContext?.Runtimes?.Relationships;
            registry = context?.ScenarioContext?.Runtimes?.DefinitionRegistry;
            if (runtime == null || registry == null)
            {
                failure = runtime == null ? "Relationship runtime is missing from the Test Lab runtime bundle." : "Definition registry is missing from the Test Lab runtime bundle.";
                return false;
            }

            runtime.Configure(registry, context.ScenarioContext.Runtimes.KnownPersonIds);
            failure = string.Empty;
            return true;
        }

        private static ITestLabAutomationScenario Scenario(string scenarioId, string displayName, int order, params ITestLabScenarioStep[] steps)
        {
            return new TestLabAutomationScenario(
                scenarioId,
                displayName,
                displayName,
                order,
                TestLabAutomationCategory.Standard,
                includeInQuickRun: true,
                steps: steps,
                isolationMode: TestLabScenarioIsolationMode.FreshRuntime,
                requiredRuntimeAreas: TestLabRuntimeArea.Social | TestLabRuntimeArea.KnowledgeHistory,
                requiredDefinitionIds: new[]
                {
                    PrototypeRelationshipDefinitionFactory.FriendRelationshipId,
                    PrototypeRelationshipDefinitionFactory.ParentChildRelationshipId,
                    PrototypeRelationshipDefinitionFactory.MentorStudentRelationshipId,
                    PrototypeRelationshipDefinitionFactory.RivalRelationshipId
                });
        }

        private static ITestLabScenarioStep Step(string stepId, string displayName, Func<TestLabAutomationContext, TestLabAutomationStepResult> run)
        {
            return new TestLabScenarioStep(stepId, displayName, run);
        }

        private static string Scoped(TestLabAutomationContext context, string suffix)
        {
            return $"relationship.automation.{context.RunId}.{context.CurrentScenarioId}.{suffix}";
        }

        private static string Tx(TestLabAutomationContext context, string suffix)
        {
            return context.TransactionIds.Create(context.CurrentSuiteId, context.CurrentScenarioId, context.RunId, context.CurrentStepIndex, suffix);
        }
    }
}
#endif
