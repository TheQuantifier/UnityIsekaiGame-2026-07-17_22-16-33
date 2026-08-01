#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.Social.Attitudes;
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

            registry?.TryRegister(new TestLabAutomationSuite(
                "feature.12.2.interpersonal-attitudes-relationship-values",
                "Interpersonal Attitudes and Relationship Values",
                "12.2",
                "Directional person-to-person attitude values with definition-backed dimensions, source contributions, thresholds, and persistence.",
                12020,
                TestLabAutomationCategory.Standard,
                includeInRunAll: true,
                requiredServices: new[] { "InterpersonalAttitudeRuntime", "AttitudeDimensionDefinition", "InterpersonalAttitudePersistenceParticipant" },
                scenarios: new[]
                {
                    AttitudeScenario("definitions-and-neutral-values", "Canonical attitude dimensions resolve with neutral defaults", 10,
                        Step("step12-attitudes-definitions", "Resolve attitude definitions and neutral values", AttitudeDefinitionsAndNeutralValues)),
                    AttitudeScenario("directional-values", "Directional attitudes do not mirror between people", 20,
                        Step("step12-attitudes-directional", "Mutate directed trust and hostility independently", DirectionalAttitudeValues)),
                    AttitudeScenario("contributions-and-idempotence", "Source contributions clamp and duplicate transactions are idempotent", 30,
                        Step("step12-attitudes-contributions", "Preview, execute, duplicate, and clamp source-owned contributions", ContributionsAndIdempotence)),
                    AttitudeScenario("relationship-independence", "Relationship records can inform attitudes without owning them", 40,
                        Step("step12-attitudes-relationship-independent", "End a relationship without deleting attitude values", RelationshipIndependence)),
                    AttitudeScenario("persistence-validation", "Attitudes persist and reject corrupt restores without mutation", 50,
                        Step("step12-attitudes-persistence", "Save, restore, and reject invalid attitude payloads", AttitudePersistenceValidation))
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

        private static TestLabAutomationStepResult AttitudeDefinitionsAndNeutralValues(TestLabAutomationContext context)
        {
            if (!TryGetAttitudeRuntime(context, out InterpersonalAttitudeRuntime runtime, out DefinitionRegistry registry, out string failure))
            {
                return TestLabAssertions.Fail("step12-attitudes-definitions", "Resolve attitude definitions and neutral values", "InterpersonalAttitudeRuntime", "MissingRuntime", failure);
            }

            string[] required =
            {
                PrototypeAttitudeDefinitionFactory.TrustId,
                PrototypeAttitudeDefinitionFactory.AffectionId,
                PrototypeAttitudeDefinitionFactory.RespectId,
                PrototypeAttitudeDefinitionFactory.FearId,
                PrototypeAttitudeDefinitionFactory.LoyaltyId,
                PrototypeAttitudeDefinitionFactory.HostilityId
            };
            bool allDefinitions = required.All(id => registry.TryGet(id, out AttitudeDimensionDefinition _));
            AttitudeEffectiveValueSnapshot trust = runtime.ResolveValue(context.ScenarioContext.Runtimes.PersonId, "person.prototype.friend", PrototypeAttitudeDefinitionFactory.TrustId);
            AttitudeEffectiveValueSnapshot fear = runtime.ResolveValue(context.ScenarioContext.Runtimes.PersonId, "person.prototype.rival", PrototypeAttitudeDefinitionFactory.FearId);
            bool valid = allDefinitions
                && runtime.Count == 0
                && trust.EffectiveValue == 0
                && fear.EffectiveValue == 0
                && trust.IsNeutralDefault
                && fear.IsNeutralDefault;
            return TestLabAssertions.True("step12-attitudes-definitions", "Canonical attitude dimensions resolve with neutral defaults", valid, $"Definitions={allDefinitions} Count={runtime.Count} Trust={trust.EffectiveValue} Fear={fear.EffectiveValue}");
        }

        private static TestLabAutomationStepResult DirectionalAttitudeValues(TestLabAutomationContext context)
        {
            if (!TryGetAttitudeRuntime(context, out InterpersonalAttitudeRuntime runtime, out _, out string failure))
            {
                return TestLabAssertions.Fail("step12-attitudes-directional", "Mutate directed trust and hostility independently", "InterpersonalAttitudeRuntime", "MissingRuntime", failure);
            }

            string player = context.ScenarioContext.Runtimes.PersonId;
            AttitudeMutationResult trust = runtime.Mutate(new AttitudeMutationRequest
            {
                transactionId = Tx(context, "player-trusts-friend"),
                observerPersonId = player,
                subjectPersonId = "person.prototype.friend",
                dimensionId = PrototypeAttitudeDefinitionFactory.TrustId,
                mutationKind = AttitudeMutationKind.SetBaseline,
                value = 35,
                worldTime = 15d
            });
            AttitudeMutationResult hostility = runtime.Mutate(new AttitudeMutationRequest
            {
                transactionId = Tx(context, "friend-hostile-player"),
                observerPersonId = "person.prototype.friend",
                subjectPersonId = player,
                dimensionId = PrototypeAttitudeDefinitionFactory.HostilityId,
                mutationKind = AttitudeMutationKind.SetBaseline,
                value = 20,
                worldTime = 16d
            });

            int forwardTrust = runtime.ResolveValue(player, "person.prototype.friend", PrototypeAttitudeDefinitionFactory.TrustId).EffectiveValue;
            int reverseTrust = runtime.ResolveValue("person.prototype.friend", player, PrototypeAttitudeDefinitionFactory.TrustId).EffectiveValue;
            int reverseHostility = runtime.ResolveValue("person.prototype.friend", player, PrototypeAttitudeDefinitionFactory.HostilityId).EffectiveValue;
            bool valid = trust.Succeeded
                && hostility.Succeeded
                && forwardTrust == 35
                && reverseTrust == 0
                && reverseHostility == 20
                && runtime.QueryByObserver(player).Count == 1
                && runtime.QueryBySubject(player).Count == 1;
            return TestLabAssertions.True("step12-attitudes-directional", "Directional attitudes do not mirror between people", valid, $"Trust={trust.Status}:{forwardTrust}/{reverseTrust} Hostility={hostility.Status}:{reverseHostility} Count={runtime.Count}");
        }

        private static TestLabAutomationStepResult ContributionsAndIdempotence(TestLabAutomationContext context)
        {
            if (!TryGetAttitudeRuntime(context, out InterpersonalAttitudeRuntime runtime, out _, out string failure))
            {
                return TestLabAssertions.Fail("step12-attitudes-contributions", "Preview, execute, duplicate, and clamp source-owned contributions", "InterpersonalAttitudeRuntime", "MissingRuntime", failure);
            }

            string player = context.ScenarioContext.Runtimes.PersonId;
            AttitudeMutationRequest request = new AttitudeMutationRequest
            {
                transactionId = Tx(context, "hostility-source"),
                observerPersonId = player,
                subjectPersonId = "person.prototype.rival",
                dimensionId = PrototypeAttitudeDefinitionFactory.HostilityId,
                mutationKind = AttitudeMutationKind.AddOrReplaceContribution,
                sourceId = Scoped(context, "ambush-source"),
                sourceCategory = AttitudeContributionSourceCategory.TestLab,
                value = 150,
                worldTime = 20d,
                historicalEventId = "history.relationship.ambush"
            };
            request.preview = true;
            AttitudeMutationResult preview = runtime.Mutate(request);
            int afterPreviewCount = runtime.Count;
            request.preview = false;
            AttitudeMutationResult execute = runtime.Mutate(request);
            AttitudeMutationResult duplicate = runtime.Mutate(request);
            AttitudeEffectiveValueSnapshot effective = runtime.ResolveValue(player, "person.prototype.rival", PrototypeAttitudeDefinitionFactory.HostilityId);

            bool valid = preview.Preview
                && afterPreviewCount == 0
                && execute.Succeeded
                && duplicate.Status == AttitudeOperationStatus.Duplicate
                && runtime.Count == 1
                && effective.EffectiveValue == 100
                && effective.Clamped
                && effective.Contributions.Count == 1
                && runtime.QueryByHistoricalEvent("history.relationship.ambush").Count == 1;
            return TestLabAssertions.True("step12-attitudes-contributions", "Source contributions clamp and duplicate transactions are idempotent", valid, $"Preview={preview.Status} Execute={execute.Status} Duplicate={duplicate.Status} Count={runtime.Count} Effective={effective.EffectiveValue} Clamped={effective.Clamped}");
        }

        private static TestLabAutomationStepResult RelationshipIndependence(TestLabAutomationContext context)
        {
            bool hasRelationships = TryGetRuntime(context, out RelationshipRuntime relationships, out _, out string relationshipFailure);
            bool hasAttitudes = TryGetAttitudeRuntime(context, out InterpersonalAttitudeRuntime attitudes, out _, out string attitudeFailure);
            if (!hasRelationships || !hasAttitudes)
            {
                return TestLabAssertions.Fail("step12-attitudes-relationship-independent", "End a relationship without deleting attitude values", "SocialRuntime", "MissingRuntime", $"{relationshipFailure} {attitudeFailure}".Trim());
            }

            string player = context.ScenarioContext.Runtimes.PersonId;
            RelationshipOperationResult friendship = relationships.CreateRelationship(new RelationshipCreateRequest
            {
                recordId = Scoped(context, "attitude-friendship"),
                relationshipDefinitionId = PrototypeRelationshipDefinitionFactory.FriendRelationshipId,
                firstPersonId = player,
                firstRoleId = "friend",
                secondPersonId = "person.prototype.friend",
                secondRoleId = "friend",
                startWorldTime = 1d,
                transactionId = Tx(context, "friendship")
            });
            AttitudeMutationResult loyalty = attitudes.Mutate(new AttitudeMutationRequest
            {
                transactionId = Tx(context, "friendship-loyalty"),
                observerPersonId = player,
                subjectPersonId = "person.prototype.friend",
                dimensionId = PrototypeAttitudeDefinitionFactory.LoyaltyId,
                mutationKind = AttitudeMutationKind.AddOrReplaceContribution,
                sourceId = Scoped(context, "friendship-loyalty-source"),
                sourceCategory = AttitudeContributionSourceCategory.Relationship,
                relationshipRecordId = friendship.Snapshot?.RecordId,
                value = 40,
                worldTime = 2d
            });
            RelationshipOperationResult ended = relationships.EndRelationship(new RelationshipEndRequest
            {
                recordId = friendship.Snapshot?.RecordId,
                endWorldTime = 3d,
                transactionId = Tx(context, "friendship-ended")
            });

            AttitudeEffectiveValueSnapshot value = attitudes.ResolveValue(player, "person.prototype.friend", PrototypeAttitudeDefinitionFactory.LoyaltyId);
            bool relationshipEnded = relationships.QueryBetween(player, "person.prototype.friend", activeOnly: true).Count == 0;
            bool valid = friendship.Succeeded
                && loyalty.Succeeded
                && ended.Succeeded
                && relationshipEnded
                && value.EffectiveValue == 40
                && attitudes.QueryByThreshold(PrototypeAttitudeDefinitionFactory.LoyaltyId, AttitudeThresholdComparison.GreaterThanOrEqual, 40).Count == 1;
            return TestLabAssertions.True("step12-attitudes-relationship-independent", "Relationship records can inform attitudes without owning them", valid, $"Friendship={friendship.Status} Loyalty={loyalty.Status} Ended={ended.Status} RelationshipEnded={relationshipEnded} LoyaltyValue={value.EffectiveValue}");
        }

        private static TestLabAutomationStepResult AttitudePersistenceValidation(TestLabAutomationContext context)
        {
            if (!TryGetAttitudeRuntime(context, out InterpersonalAttitudeRuntime runtime, out DefinitionRegistry registry, out string failure))
            {
                return TestLabAssertions.Fail("step12-attitudes-persistence", "Save, restore, and reject invalid attitude payloads", "InterpersonalAttitudeRuntime", "MissingRuntime", failure);
            }

            string player = context.ScenarioContext.Runtimes.PersonId;
            AttitudeMutationResult respect = runtime.Mutate(new AttitudeMutationRequest
            {
                transactionId = Tx(context, "respect-baseline"),
                observerPersonId = player,
                subjectPersonId = "person.prototype.mentor",
                dimensionId = PrototypeAttitudeDefinitionFactory.RespectId,
                mutationKind = AttitudeMutationKind.SetBaseline,
                value = 55,
                worldTime = 25d
            });
            InterpersonalAttitudeRuntimeSaveData save = runtime.CreateSaveData();
            InterpersonalAttitudeRuntime restored = new InterpersonalAttitudeRuntime();
            AttitudeMutationResult restore = restored.RestoreFromSaveData(save, registry, context.ScenarioContext.Runtimes.KnownPersonIds, restoringState: true);
            InterpersonalAttitudeRuntimeSaveData corrupt = save.Clone();
            corrupt.records[0].dimensions[0].baselineValue = 999;
            bool rejected = !InterpersonalAttitudeRuntime.ValidateSaveData(corrupt, registry, context.ScenarioContext.Runtimes.KnownPersonIds, out string validationFailure);
            int liveValue = runtime.ResolveValue(player, "person.prototype.mentor", PrototypeAttitudeDefinitionFactory.RespectId).EffectiveValue;
            int restoredValue = restored.ResolveValue(player, "person.prototype.mentor", PrototypeAttitudeDefinitionFactory.RespectId).EffectiveValue;

            bool valid = respect.Succeeded
                && restore.Succeeded
                && rejected
                && liveValue == 55
                && restoredValue == 55
                && runtime.Count == 1
                && restored.Count == 1;
            return TestLabAssertions.True("step12-attitudes-persistence", "Attitudes persist and reject corrupt restores without mutation", valid, $"Respect={respect.Status} Restore={restore.Status} Rejected={rejected} Failure='{validationFailure}' Values={liveValue}/{restoredValue}");
        }

        private static bool TryGetAttitudeRuntime(TestLabAutomationContext context, out InterpersonalAttitudeRuntime runtime, out DefinitionRegistry registry, out string failure)
        {
            runtime = context?.ScenarioContext?.Runtimes?.Attitudes;
            registry = context?.ScenarioContext?.Runtimes?.DefinitionRegistry;
            if (runtime == null || registry == null)
            {
                failure = runtime == null ? "Interpersonal attitude runtime is missing from the Test Lab runtime bundle." : "Definition registry is missing from the Test Lab runtime bundle.";
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

        private static ITestLabAutomationScenario AttitudeScenario(string scenarioId, string displayName, int order, params ITestLabScenarioStep[] steps)
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
                    PrototypeAttitudeDefinitionFactory.TrustId,
                    PrototypeAttitudeDefinitionFactory.AffectionId,
                    PrototypeAttitudeDefinitionFactory.RespectId,
                    PrototypeAttitudeDefinitionFactory.FearId,
                    PrototypeAttitudeDefinitionFactory.LoyaltyId,
                    PrototypeAttitudeDefinitionFactory.HostilityId,
                    PrototypeRelationshipDefinitionFactory.FriendRelationshipId
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
