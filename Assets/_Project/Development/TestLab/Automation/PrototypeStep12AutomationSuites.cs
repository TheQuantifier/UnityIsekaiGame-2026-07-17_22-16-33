#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.GameData.Persistence;
using UnityIsekaiGame.Knowledge;
using UnityIsekaiGame.Knowledge.History;
using UnityIsekaiGame.Persistence;
using UnityIsekaiGame.Social.Attitudes;
using UnityIsekaiGame.Social.Interactions;
using UnityIsekaiGame.Social.Norms;
using UnityIsekaiGame.Social.Reputation;
using UnityIsekaiGame.Social.Relationships;
using UnityIsekaiGame.Social.Rumors;

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

            registry?.TryRegister(new TestLabAutomationSuite(
                "feature.12.3.reputation-audiences-social-standing",
                "Reputation and Social Standing",
                "12.3",
                "Audience-scoped person reputation records with canonical dimensions, source-owned contributions, hierarchy-aware reads, requirements, and persistence.",
                12030,
                TestLabAutomationCategory.Standard,
                includeInRunAll: true,
                requiredServices: new[] { "ReputationRuntime", "ReputationAudienceDefinition", "ReputationDimensionDefinition", "ReputationPersistenceParticipant" },
                scenarios: new[]
                {
                    ReputationScenario("runtime-readiness", "Reputation definitions and runtime are ready", 10,
                        Step("step12-reputation-readiness", "Resolve reputation audiences and dimensions", ReputationRuntimeReadiness)),
                    ReputationScenario("record-identity-dimensions", "Records and dimensions remain stable and independent", 20,
                        Step("step12-reputation-records", "Create records and mutate independent dimensions", ReputationRecordIdentityAndDimensions)),
                    ReputationScenario("audience-independence-hierarchy", "Audience independence and hierarchy are deterministic", 30,
                        Step("step12-reputation-audiences", "Verify direct, inherited, and isolated audience values", ReputationAudienceIndependenceAndHierarchy)),
                    ReputationScenario("contributions-disputes-idempotence", "Source contributions preserve dispute metadata and idempotence", 40,
                        Step("step12-reputation-contributions", "Preview, execute, duplicate, replace, remove, and classify sources", ReputationContributionsAndDisputes)),
                    ReputationScenario("requirements-and-separation", "Requirement checks do not mutate relationships or attitudes", 50,
                        Step("step12-reputation-requirements", "Evaluate thresholds and verify feature separation", ReputationRequirementsAndSeparation)),
                    ReputationScenario("persistence-validation", "Reputation persists and rejects corrupt restores", 60,
                        Step("step12-reputation-persistence", "Save, restore, and reject invalid reputation payloads", ReputationPersistenceValidation))
                }), out _);

            registry?.TryRegister(new TestLabAutomationSuite(
                "feature.12.4.rumors-gossip-social-knowledge-propagation",
                "Rumors, Gossip, and Social Knowledge Propagation",
                "12.4",
                "Definition-backed rumor records, transmission lineage, bounded propagation, listener knowledge and memory effects, and persistence.",
                12040,
                TestLabAutomationCategory.Standard,
                includeInRunAll: true,
                requiredServices: new[] { "RumorRuntime", "RumorDefinition", "RumorCommunicationChannelDefinition", "PersonKnowledgeRuntime", "PersonMemoryRuntime" },
                scenarios: new[]
                {
                    RumorScenario("readiness-and-root-identity", "Rumor definitions resolve and root records are stable", 10,
                        Step("step12-rumor-root", "Create root rumor and query identity", RumorReadinessAndRootIdentity)),
                    RumorScenario("transmission-creates-knowledge-memory", "Transmission records listener evidence and memory", 20,
                        Step("step12-rumor-transmission", "Transmit rumor into listener knowledge and memory", RumorTransmissionCreatesKnowledgeAndMemory)),
                    RumorScenario("distortion-lineage", "Distortion creates a derived version with root lineage", 30,
                        Step("step12-rumor-distortion", "Transmit with deterministic distortion", RumorDistortionLineage)),
                    RumorScenario("bounded-propagation", "Propagation is bounded and deterministic", 40,
                        Step("step12-rumor-propagation", "Propagate rumor to ordered listeners", RumorBoundedPropagation)),
                    RumorScenario("social-boundary", "Rumors do not mutate relationships, attitudes, or reputation directly", 50,
                        Step("step12-rumor-social-boundary", "Verify rumor separation from other social runtimes", RumorSocialBoundary)),
                    RumorScenario("persistence-validation", "Rumors persist and reject corrupt restores", 60,
                        Step("step12-rumor-persistence", "Save, restore, and reject invalid rumor payloads", RumorPersistenceValidation))
                }), out _);

            registry?.TryRegister(new TestLabAutomationSuite(
                "feature.12.5.social-interactions-relationship-evolution",
                "Social Interactions and Relationship Evolution",
                "12.5",
                "Definition-backed social interaction execution with deterministic consequences, pending responses, promises, persistence, and Step 12 runtime delegation.",
                12050,
                TestLabAutomationCategory.Standard,
                includeInRunAll: true,
                requiredServices: new[] { "SocialInteractionRuntime", "SocialInteractionDefinition", "SocialInteractionPersistenceParticipant" },
                scenarios: new[]
                {
                    InteractionScenario("readiness-and-preview", "Interaction definitions resolve and previews are non-mutating", 10,
                        Step("step12-interaction-preview", "Preview interaction without mutation", InteractionReadinessAndPreview)),
                    InteractionScenario("attitude-consequences", "Compliments and insults evolve directed attitudes", 20,
                        Step("step12-interaction-attitudes", "Execute attitude-producing interactions", InteractionAttitudeConsequences)),
                    InteractionScenario("pending-response-promise", "Pending responses and accepted promises are explicit", 30,
                        Step("step12-interaction-pending", "Create pending interaction and accept promise", InteractionPendingResponsePromise)),
                    InteractionScenario("public-reputation", "Witnessed and public interactions affect reputation", 40,
                        Step("step12-interaction-reputation", "Execute public reputation consequences", InteractionPublicReputation)),
                    InteractionScenario("rumor-delegation", "Information sharing delegates through Rumor runtime", 50,
                        Step("step12-interaction-rumor", "Share existing rumor through interaction", InteractionRumorDelegation)),
                    InteractionScenario("persistence-validation", "Interactions persist and reject corrupt restores", 60,
                        Step("step12-interaction-persistence", "Save, restore, duplicate, and reject invalid payloads", InteractionPersistenceValidation))
                }), out _);

            registry?.TryRegister(new TestLabAutomationSuite(
                "feature.12.6.social-norms-etiquette-contextual-expectations",
                "Social Norms, Etiquette, and Contextual Expectations",
                "12.6",
                "Definition-backed social norm assessment with contextual applicability, actor knowledge, observer interpretation, conflict resolution, consequences, idempotence, and persistence.",
                12060,
                TestLabAutomationCategory.Standard,
                includeInRunAll: true,
                requiredServices: new[] { "SocialNormRuntime", "SocialNormDefinition", "SocialNormPersistenceParticipant" },
                scenarios: new[]
                {
                    NormScenario("readiness-preview", "Norm definitions resolve and previews do not mutate", 10,
                        Step("step12-norm-preview", "Preview host greeting norm", NormReadinessAndPreview)),
                    NormScenario("visibility-consequences", "Public and private etiquette produce deterministic consequence plans", 20,
                        Step("step12-norm-visibility", "Assess private and public insult norms", NormVisibilityConsequences)),
                    NormScenario("knowledge-exception-observers", "Actor knowledge, exceptions, and observer interpretation remain explicit", 30,
                        Step("step12-norm-knowledge-exception", "Evaluate ignorance and witness context", NormKnowledgeExceptionObservers)),
                    NormScenario("conflict-and-promise", "Norm conflicts and promise expectations resolve deterministically", 40,
                        Step("step12-norm-conflict-promise", "Assess conflict and promise breach", NormConflictAndPromise)),
                    NormScenario("persistence-idempotence", "Norm assessments persist and duplicate transactions are idempotent", 50,
                        Step("step12-norm-persistence", "Save, restore, duplicate, and reject invalid norm payloads", NormPersistenceIdempotence))
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

        private static TestLabAutomationStepResult ReputationRuntimeReadiness(TestLabAutomationContext context)
        {
            if (!TryGetReputationRuntime(context, out ReputationRuntime runtime, out DefinitionRegistry registry, out string failure))
            {
                return TestLabAssertions.Fail("step12-reputation-readiness", "Resolve reputation audiences and dimensions", "ReputationRuntime", "MissingRuntime", failure);
            }

            bool graphValid = ReputationRuntime.ValidateAudienceGraph(registry, out string graphFailure);
            bool valid = runtime.IsReady
                && runtime.Count == 0
                && registry.TryGet(PrototypeReputationDefinitionFactory.GlobalPublicAudienceId, out ReputationAudienceDefinition _)
                && registry.TryGet(PrototypeReputationDefinitionFactory.PrototypeTownAudienceId, out ReputationAudienceDefinition _)
                && registry.TryGet(PrototypeReputationDefinitionFactory.RenownId, out ReputationDimensionDefinition _)
                && registry.TryGet(PrototypeReputationDefinitionFactory.EsteemId, out ReputationDimensionDefinition _)
                && graphValid;
            return TestLabAssertions.True("step12-reputation-readiness", "Reputation definitions and runtime are ready", valid, $"Ready={runtime.IsReady} Count={runtime.Count} GraphFailure='{graphFailure}'");
        }

        private static TestLabAutomationStepResult ReputationRecordIdentityAndDimensions(TestLabAutomationContext context)
        {
            if (!TryGetReputationRuntime(context, out ReputationRuntime runtime, out _, out string failure))
            {
                return TestLabAssertions.Fail("step12-reputation-records", "Create records and mutate independent dimensions", "ReputationRuntime", "MissingRuntime", failure);
            }

            string subject = context.ScenarioContext.Runtimes.PersonId;
            ReputationMutationResult renown = runtime.Mutate(new ReputationMutationRequest
            {
                transactionId = Tx(context, "renown"),
                subjectPersonId = subject,
                audienceId = PrototypeReputationDefinitionFactory.GlobalPublicAudienceId,
                dimensionId = PrototypeReputationDefinitionFactory.RenownId,
                mutationKind = ReputationMutationKind.SetBaseline,
                value = 80,
                worldTime = 10d
            });
            ReputationMutationResult esteem = runtime.Mutate(new ReputationMutationRequest
            {
                transactionId = Tx(context, "esteem"),
                subjectPersonId = subject,
                audienceId = PrototypeReputationDefinitionFactory.GlobalPublicAudienceId,
                dimensionId = PrototypeReputationDefinitionFactory.EsteemId,
                mutationKind = ReputationMutationKind.SetBaseline,
                value = -35,
                worldTime = 11d
            });
            bool resolvedById = runtime.TryGetSnapshot(renown.RecordId, out ReputationSnapshot byId);
            bool resolvedByPair = runtime.TryGetSnapshotBySubjectAudience(subject, PrototypeReputationDefinitionFactory.GlobalPublicAudienceId, out ReputationSnapshot byPair);
            ReputationMutationResult duplicatePair = runtime.Mutate(new ReputationMutationRequest
            {
                transactionId = Tx(context, "duplicate-pair"),
                recordId = RepScoped(context, "duplicate"),
                subjectPersonId = subject,
                audienceId = PrototypeReputationDefinitionFactory.GlobalPublicAudienceId,
                dimensionId = PrototypeReputationDefinitionFactory.HonorId,
                mutationKind = ReputationMutationKind.SetBaseline,
                value = 10,
                worldTime = 12d
            });

            bool valid = renown.Succeeded
                && esteem.Succeeded
                && resolvedById
                && resolvedByPair
                && byId.RecordId == byPair.RecordId
                && duplicatePair.Status == ReputationOperationStatus.DuplicateSubjectAudience
                && runtime.ResolveValue(subject, PrototypeReputationDefinitionFactory.GlobalPublicAudienceId, PrototypeReputationDefinitionFactory.RenownId).EffectiveValue == 80
                && runtime.ResolveValue(subject, PrototypeReputationDefinitionFactory.GlobalPublicAudienceId, PrototypeReputationDefinitionFactory.EsteemId).EffectiveValue == -35;
            return TestLabAssertions.True("step12-reputation-records", "Records and dimensions remain stable and independent", valid, $"Renown={renown.Status} Esteem={esteem.Status} Duplicate={duplicatePair.Status} Records={runtime.Count}");
        }

        private static TestLabAutomationStepResult ReputationAudienceIndependenceAndHierarchy(TestLabAutomationContext context)
        {
            if (!TryGetReputationRuntime(context, out ReputationRuntime runtime, out _, out string failure))
            {
                return TestLabAssertions.Fail("step12-reputation-audiences", "Verify direct, inherited, and isolated audience values", "ReputationRuntime", "MissingRuntime", failure);
            }

            string subject = context.ScenarioContext.Runtimes.PersonId;
            ReputationMutationResult global = runtime.Mutate(new ReputationMutationRequest
            {
                transactionId = Tx(context, "global-honor"),
                subjectPersonId = subject,
                audienceId = PrototypeReputationDefinitionFactory.GlobalPublicAudienceId,
                dimensionId = PrototypeReputationDefinitionFactory.HonorId,
                mutationKind = ReputationMutationKind.SetBaseline,
                value = 20,
                worldTime = 13d
            });
            ReputationMutationResult guild = runtime.Mutate(new ReputationMutationRequest
            {
                transactionId = Tx(context, "guild-honor"),
                subjectPersonId = subject,
                audienceId = PrototypeReputationDefinitionFactory.AdventurersGuildAudienceId,
                dimensionId = PrototypeReputationDefinitionFactory.HonorId,
                mutationKind = ReputationMutationKind.SetBaseline,
                value = 70,
                worldTime = 14d
            });
            ReputationEffectiveValueSnapshot inherited = runtime.ResolveValue(subject, PrototypeReputationDefinitionFactory.AdventurersGuildVeteransAudienceId, PrototypeReputationDefinitionFactory.HonorId, allowInherited: true);
            ReputationEffectiveValueSnapshot direct = runtime.ResolveValue(subject, PrototypeReputationDefinitionFactory.AdventurersGuildVeteransAudienceId, PrototypeReputationDefinitionFactory.HonorId, allowInherited: false);
            ReputationMutationResult town = runtime.Mutate(new ReputationMutationRequest
            {
                transactionId = Tx(context, "town-honor"),
                subjectPersonId = subject,
                audienceId = PrototypeReputationDefinitionFactory.PrototypeTownAudienceId,
                dimensionId = PrototypeReputationDefinitionFactory.HonorId,
                mutationKind = ReputationMutationKind.SetBaseline,
                value = -15,
                worldTime = 15d
            });

            bool valid = global.Succeeded
                && guild.Succeeded
                && town.Succeeded
                && inherited.EffectiveValue == 70
                && inherited.Inherited
                && inherited.SourceAudienceId == PrototypeReputationDefinitionFactory.AdventurersGuildAudienceId
                && direct.EffectiveValue == 0
                && runtime.ResolveValue(subject, PrototypeReputationDefinitionFactory.PrototypeTownAudienceId, PrototypeReputationDefinitionFactory.HonorId).EffectiveValue == -15
                && runtime.ResolveValue(subject, PrototypeReputationDefinitionFactory.GlobalPublicAudienceId, PrototypeReputationDefinitionFactory.HonorId).EffectiveValue == 20;
            return TestLabAssertions.True("step12-reputation-audiences", "Audience independence and hierarchy are deterministic", valid, $"Inherited={inherited.EffectiveValue}/{inherited.Inherited}/{inherited.SourceAudienceId} Direct={direct.EffectiveValue} Count={runtime.Count}");
        }

        private static TestLabAutomationStepResult ReputationContributionsAndDisputes(TestLabAutomationContext context)
        {
            if (!TryGetReputationRuntime(context, out ReputationRuntime runtime, out _, out string failure))
            {
                return TestLabAssertions.Fail("step12-reputation-contributions", "Preview, execute, duplicate, replace, remove, and classify sources", "ReputationRuntime", "MissingRuntime", failure);
            }

            string subject = context.ScenarioContext.Runtimes.PersonId;
            string accusationEventId = RepScoped(context, "accusation-event");
            ReputationMutationRequest disputed = new ReputationMutationRequest
            {
                transactionId = Tx(context, "disputed"),
                subjectPersonId = subject,
                audienceId = PrototypeReputationDefinitionFactory.RoyalJurisdictionAudienceId,
                dimensionId = PrototypeReputationDefinitionFactory.NotorietyId,
                mutationKind = ReputationMutationKind.AddOrReplaceContribution,
                sourceId = RepScoped(context, "accusation"),
                sourceCategory = ReputationContributionSourceCategory.Accusation,
                authenticity = ReputationAuthenticity.Disputed,
                historicalEventId = accusationEventId,
                value = 90,
                worldTime = 16d,
                preview = true
            };
            ReputationMutationResult preview = runtime.Mutate(disputed);
            int afterPreviewCount = runtime.Count;
            disputed.preview = false;
            ReputationMutationResult execute = runtime.Mutate(disputed);
            ReputationMutationResult duplicate = runtime.Mutate(disputed);
            ReputationMutationResult verified = runtime.Mutate(new ReputationMutationRequest
            {
                transactionId = Tx(context, "verified"),
                subjectPersonId = subject,
                audienceId = PrototypeReputationDefinitionFactory.RoyalJurisdictionAudienceId,
                dimensionId = PrototypeReputationDefinitionFactory.NotorietyId,
                mutationKind = ReputationMutationKind.AddOrReplaceContribution,
                sourceId = RepScoped(context, "conviction"),
                sourceCategory = ReputationContributionSourceCategory.Conviction,
                authenticity = ReputationAuthenticity.Verified,
                value = 20,
                worldTime = 17d
            });
            ReputationMutationResult remove = runtime.Mutate(new ReputationMutationRequest
            {
                transactionId = Tx(context, "remove-disputed"),
                subjectPersonId = subject,
                audienceId = PrototypeReputationDefinitionFactory.RoyalJurisdictionAudienceId,
                dimensionId = PrototypeReputationDefinitionFactory.NotorietyId,
                mutationKind = ReputationMutationKind.RemoveContribution,
                sourceId = RepScoped(context, "accusation"),
                worldTime = 18d
            });
            ReputationEffectiveValueSnapshot value = runtime.ResolveValue(subject, PrototypeReputationDefinitionFactory.RoyalJurisdictionAudienceId, PrototypeReputationDefinitionFactory.NotorietyId);

            bool valid = preview.Preview
                && afterPreviewCount == 0
                && execute.Succeeded
                && duplicate.Status == ReputationOperationStatus.Duplicate
                && verified.Succeeded
                && remove.Succeeded
                && value.EffectiveValue == 20
                && value.Contributions.Count == 1
                && value.Contributions[0].Authenticity == ReputationAuthenticity.Verified
                && runtime.QueryByHistoricalEvent(accusationEventId).Count == 0;
            return TestLabAssertions.True("step12-reputation-contributions", "Source contributions preserve dispute metadata and idempotence", valid, $"Preview={preview.Status} Execute={execute.Status} Duplicate={duplicate.Status} Verified={verified.Status} Remove={remove.Status} Value={value.EffectiveValue}");
        }

        private static TestLabAutomationStepResult ReputationRequirementsAndSeparation(TestLabAutomationContext context)
        {
            bool hasReputation = TryGetReputationRuntime(context, out ReputationRuntime reputationRuntime, out _, out string reputationFailure);
            bool hasRelationships = TryGetRuntime(context, out RelationshipRuntime relationshipRuntime, out _, out string relationshipFailure);
            bool hasAttitudes = TryGetAttitudeRuntime(context, out InterpersonalAttitudeRuntime attitudeRuntime, out _, out string attitudeFailure);
            if (!hasReputation || !hasRelationships || !hasAttitudes)
            {
                return TestLabAssertions.Fail("step12-reputation-requirements", "Evaluate thresholds and verify feature separation", "SocialRuntimes", "MissingRuntime", $"{reputationFailure} {relationshipFailure} {attitudeFailure}".Trim());
            }

            string subject = context.ScenarioContext.Runtimes.PersonId;
            RelationshipOperationResult relationship = relationshipRuntime.CreateRelationship(new RelationshipCreateRequest
            {
                recordId = RepScoped(context, "friendship"),
                relationshipDefinitionId = PrototypeRelationshipDefinitionFactory.FriendRelationshipId,
                firstPersonId = subject,
                firstRoleId = "friend",
                secondPersonId = "person.prototype.friend",
                secondRoleId = "friend",
                startWorldTime = 19d,
                transactionId = Tx(context, "friendship")
            });
            AttitudeMutationResult attitude = attitudeRuntime.Mutate(new AttitudeMutationRequest
            {
                transactionId = Tx(context, "trust"),
                observerPersonId = subject,
                subjectPersonId = "person.prototype.friend",
                dimensionId = PrototypeAttitudeDefinitionFactory.TrustId,
                mutationKind = AttitudeMutationKind.SetBaseline,
                value = 30,
                worldTime = 20d
            });
            ReputationMutationResult reputation = reputationRuntime.Mutate(new ReputationMutationRequest
            {
                transactionId = Tx(context, "credibility"),
                subjectPersonId = subject,
                audienceId = PrototypeReputationDefinitionFactory.PrototypeTownAudienceId,
                dimensionId = PrototypeReputationDefinitionFactory.CredibilityId,
                mutationKind = ReputationMutationKind.SetBaseline,
                value = 45,
                worldTime = 21d
            });
            ReputationThresholdResult passing = reputationRuntime.EvaluateThreshold(new ReputationThresholdRequest
            {
                subjectPersonId = subject,
                audienceId = PrototypeReputationDefinitionFactory.PrototypeTownAudienceId,
                dimensionId = PrototypeReputationDefinitionFactory.CredibilityId,
                comparison = ReputationThresholdComparison.GreaterThanOrEqual,
                value = 40
            });
            ReputationThresholdResult missing = reputationRuntime.EvaluateThreshold(new ReputationThresholdRequest
            {
                subjectPersonId = "person.prototype.unknown",
                audienceId = PrototypeReputationDefinitionFactory.PrototypeTownAudienceId,
                dimensionId = PrototypeReputationDefinitionFactory.CredibilityId,
                comparison = ReputationThresholdComparison.GreaterThanOrEqual,
                value = 40
            });

            bool valid = relationship.Succeeded
                && attitude.Succeeded
                && reputation.Succeeded
                && passing.Passed
                && missing.Status == ReputationOperationStatus.UnknownSubject
                && relationshipRuntime.Count == 1
                && attitudeRuntime.Count == 1
                && reputationRuntime.Count == 1;
            return TestLabAssertions.True("step12-reputation-requirements", "Requirement checks do not mutate relationships or attitudes", valid, $"Relationship={relationship.Status} Attitude={attitude.Status} Reputation={reputation.Status} Passing={passing.Passed} Missing={missing.Status}");
        }

        private static TestLabAutomationStepResult ReputationPersistenceValidation(TestLabAutomationContext context)
        {
            if (!TryGetReputationRuntime(context, out ReputationRuntime runtime, out DefinitionRegistry registry, out string failure))
            {
                return TestLabAssertions.Fail("step12-reputation-persistence", "Save, restore, and reject invalid reputation payloads", "ReputationRuntime", "MissingRuntime", failure);
            }

            string subject = context.ScenarioContext.Runtimes.PersonId;
            ReputationMutationResult baseline = runtime.Mutate(new ReputationMutationRequest
            {
                transactionId = Tx(context, "persist-baseline"),
                subjectPersonId = subject,
                audienceId = PrototypeReputationDefinitionFactory.HiddenInvestigatorsAudienceId,
                dimensionId = PrototypeReputationDefinitionFactory.PerceivedDangerId,
                mutationKind = ReputationMutationKind.SetBaseline,
                value = 65,
                worldTime = 22d
            });
            ReputationMutationResult source = runtime.Mutate(new ReputationMutationRequest
            {
                transactionId = Tx(context, "persist-source"),
                subjectPersonId = subject,
                audienceId = PrototypeReputationDefinitionFactory.HiddenInvestigatorsAudienceId,
                dimensionId = PrototypeReputationDefinitionFactory.PerceivedDangerId,
                mutationKind = ReputationMutationKind.AddOrReplaceContribution,
                sourceId = RepScoped(context, "hidden-report"),
                sourceCategory = ReputationContributionSourceCategory.Propaganda,
                authenticity = ReputationAuthenticity.Propaganda,
                value = 10,
                supportingReferenceId = RepScoped(context, "hidden-supporting-record"),
                worldTime = 23d
            });
            ReputationRuntimeSaveData save = runtime.CreateSaveData();
            ReputationRuntime restored = new ReputationRuntime();
            ReputationMutationResult restore = restored.RestoreFromSaveData(save, registry, context.ScenarioContext.Runtimes.KnownPersonIds, restoringState: true);
            ReputationRuntimeSaveData corrupt = save.Clone();
            corrupt.records[0].dimensions[0].baselineValue = 999;
            bool rejected = !ReputationRuntime.ValidateSaveData(corrupt, registry, context.ScenarioContext.Runtimes.KnownPersonIds, out string validationFailure);
            ReputationEffectiveValueSnapshot live = runtime.ResolveValue(subject, PrototypeReputationDefinitionFactory.HiddenInvestigatorsAudienceId, PrototypeReputationDefinitionFactory.PerceivedDangerId);
            ReputationEffectiveValueSnapshot restoredValue = restored.ResolveValue(subject, PrototypeReputationDefinitionFactory.HiddenInvestigatorsAudienceId, PrototypeReputationDefinitionFactory.PerceivedDangerId);

            bool valid = baseline.Succeeded
                && source.Succeeded
                && restore.Succeeded
                && rejected
                && live.EffectiveValue == 75
                && restoredValue.EffectiveValue == 75
                && restoredValue.Contributions.Count == 1
                && restoredValue.Contributions[0].Authenticity == ReputationAuthenticity.Propaganda;
            return TestLabAssertions.True("step12-reputation-persistence", "Reputation persists and rejects corrupt restores", valid, $"Baseline={baseline.Status} Source={source.Status} Restore={restore.Status} Rejected={rejected} Failure='{validationFailure}' Values={live.EffectiveValue}/{restoredValue.EffectiveValue}");
        }

        private static bool TryGetReputationRuntime(TestLabAutomationContext context, out ReputationRuntime runtime, out DefinitionRegistry registry, out string failure)
        {
            runtime = context?.ScenarioContext?.Runtimes?.Reputation;
            registry = context?.ScenarioContext?.Runtimes?.DefinitionRegistry;
            if (runtime == null || registry == null)
            {
                failure = runtime == null ? "Reputation runtime is missing from the Test Lab runtime bundle." : "Definition registry is missing from the Test Lab runtime bundle.";
                return false;
            }

            runtime.Configure(registry, context.ScenarioContext.Runtimes.KnownPersonIds);
            failure = string.Empty;
            return true;
        }

        private static TestLabAutomationStepResult RumorReadinessAndRootIdentity(TestLabAutomationContext context)
        {
            if (!TryGetRumorRuntime(context, out RumorRuntime runtime, out DefinitionRegistry registry, out string failure))
            {
                return TestLabAssertions.Fail("step12-rumor-root", "Create root rumor and query identity", "RumorRuntime", "MissingRuntime", failure);
            }

            RumorOperationResult created = CreateRootRumor(context, runtime, "root", PrototypeRumorDefinitionFactory.PublicNewsRumorId, context.ScenarioContext.Runtimes.PersonId, RumorAuthenticity.Verified);
            bool valid = created.Succeeded
                && registry.TryGet(PrototypeRumorDefinitionFactory.PublicNewsRumorId, out RumorDefinition _)
                && registry.TryGet(PrototypeRumorDefinitionFactory.ConversationChannelId, out RumorCommunicationChannelDefinition _)
                && created.Rumor != null
                && created.Rumor.RumorId == created.Rumor.RootRumorId
                && runtime.QueryByRoot(created.Rumor.RootRumorId).Count == 1
                && runtime.QueryByClaim(created.Rumor.ClaimIdentity).Count == 1
                && runtime.IsAware(context.ScenarioContext.Runtimes.PersonId, created.Rumor.RumorId);
            return TestLabAssertions.True("step12-rumor-root", "Rumor definitions resolve and root records are stable", valid, $"Create={created.Status} Count={runtime.RumorCount} Root={created.Rumor?.RootRumorId}");
        }

        private static TestLabAutomationStepResult RumorTransmissionCreatesKnowledgeAndMemory(TestLabAutomationContext context)
        {
            if (!TryGetRumorRuntime(context, out RumorRuntime runtime, out _, out string failure))
            {
                return TestLabAssertions.Fail("step12-rumor-transmission", "Transmit rumor into listener knowledge and memory", "RumorRuntime", "MissingRuntime", failure);
            }

            string listener = context.ScenarioContext.Runtimes.PersonId;
            RumorOperationResult created = CreateRootRumor(context, runtime, "testimony-root", PrototypeRumorDefinitionFactory.PersonalConductRumorId, "person.prototype.friend", RumorAuthenticity.Unverified);
            RumorOperationResult transmitted = runtime.Transmit(new RumorTransmissionRequest
            {
                TransactionId = Tx(context, "transmit-to-player"),
                TransmissionId = RumorScoped(context, "transmission-player"),
                RumorVersionId = created.Rumor?.RumorId,
                SpeakerPersonId = "person.prototype.friend",
                ListenerPersonId = listener,
                ChannelId = PrototypeRumorDefinitionFactory.ConversationChannelId,
                RequestedOutcome = RumorTransmissionOutcome.Believed,
                SpeakerConfidence = 760,
                WorldTime = 22d
            });

            KnowledgeSnapshot knowledge = context.ScenarioContext.Runtimes.Knowledge.CreateSnapshot();
            PersonMemorySnapshot memory = context.ScenarioContext.Runtimes.Memory.CreateSnapshot();
            bool valid = created.Succeeded
                && transmitted.Succeeded
                && transmitted.KnowledgeResult?.Succeeded == true
                && transmitted.MemoryResult?.Succeeded == true
                && !string.IsNullOrWhiteSpace(transmitted.Transmission?.EvidenceId)
                && !string.IsNullOrWhiteSpace(transmitted.Transmission?.MemoryId)
                && knowledge.Evidence.Count == 1
                && memory.Memories.Count == 1
                && runtime.IsAware(listener, transmitted.Rumor.RumorId);
            return TestLabAssertions.True("step12-rumor-transmission", "Transmission records listener evidence and memory", valid, $"Create={created.Status} Transmit={transmitted.Status} Evidence={knowledge.Evidence.Count} Memories={memory.Memories.Count} Outcome={transmitted.Transmission?.Outcome}");
        }

        private static TestLabAutomationStepResult RumorDistortionLineage(TestLabAutomationContext context)
        {
            if (!TryGetRumorRuntime(context, out RumorRuntime runtime, out _, out string failure))
            {
                return TestLabAssertions.Fail("step12-rumor-distortion", "Transmit with deterministic distortion", "RumorRuntime", "MissingRuntime", failure);
            }

            RumorOperationResult created = CreateRootRumor(context, runtime, "distortion-root", PrototypeRumorDefinitionFactory.SecretLeakRumorId, "person.prototype.friend", RumorAuthenticity.PartiallyAccurate, RumorDisclosure.Shareable);
            RumorOperationResult transmitted = runtime.Transmit(new RumorTransmissionRequest
            {
                TransactionId = Tx(context, "distort"),
                TransmissionId = RumorScoped(context, "transmission-distorted"),
                RumorVersionId = created.Rumor?.RumorId,
                SpeakerPersonId = "person.prototype.friend",
                ListenerPersonId = context.ScenarioContext.Runtimes.PersonId,
                ChannelId = PrototypeRumorDefinitionFactory.TavernGossipChannelId,
                RequestedOutcome = RumorTransmissionOutcome.PartiallyBelieved,
                RequestedDistortionPolicy = RumorDistortionPolicy.ForcedConfidenceDecrease,
                DerivedRumorId = RumorScoped(context, "derived-distorted"),
                DeterministicSeed = "seed.12.4.distortion",
                SpeakerConfidence = 720,
                WorldTime = 30d
            });

            bool valid = created.Succeeded
                && transmitted.Succeeded
                && transmitted.Rumor != null
                && transmitted.Rumor.RumorId != created.Rumor.RumorId
                && transmitted.Rumor.RootRumorId == created.Rumor.RootRumorId
                && transmitted.Rumor.ParentRumorId == created.Rumor.RumorId
                && transmitted.Rumor.Confidence == created.Rumor.Confidence - 100
                && transmitted.Rumor.DistortionOperations.Contains(RumorDistortionOperation.ConfidenceDecreased)
                && runtime.QueryByRoot(created.Rumor.RootRumorId).Count == 2;
            return TestLabAssertions.True("step12-rumor-distortion", "Distortion creates a derived version with root lineage", valid, $"Create={created.Status} Transmit={transmitted.Status} Versions={runtime.QueryByRoot(created.Rumor?.RootRumorId).Count} Confidence={created.Rumor?.Confidence}->{transmitted.Rumor?.Confidence}");
        }

        private static TestLabAutomationStepResult RumorBoundedPropagation(TestLabAutomationContext context)
        {
            if (!TryGetRumorRuntime(context, out RumorRuntime runtime, out _, out string failure))
            {
                return TestLabAssertions.Fail("step12-rumor-propagation", "Propagate rumor to ordered listeners", "RumorRuntime", "MissingRuntime", failure);
            }

            RumorOperationResult created = CreateRootRumor(context, runtime, "propagation-root", PrototypeRumorDefinitionFactory.PublicNewsRumorId, "person.prototype.friend", RumorAuthenticity.Verified);
            string[] listeners = { "person.prototype.rival", context.ScenarioContext.Runtimes.PersonId, "person.prototype.mentor" };
            RumorPropagationResult propagated = runtime.Propagate(new RumorPropagationRequest
            {
                TransactionId = Tx(context, "propagate"),
                RumorVersionId = created.Rumor?.RumorId,
                SpeakerPersonId = "person.prototype.friend",
                ListenerPersonIds = listeners,
                ChannelId = PrototypeRumorDefinitionFactory.PublicSpeechChannelId,
                MaximumTransmissions = 3,
                DeterministicSeed = "seed.12.4.propagation",
                WorldTime = 40d
            });
            RumorPropagationMetrics metrics = runtime.GetMetrics(created.Rumor?.RootRumorId);
            string[] expectedListeners = listeners
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .Take(3)
                .ToArray();

            bool valid = created.Succeeded
                && propagated.Succeeded
                && propagated.Transmissions.Count == 3
                && propagated.Transmissions.All(result => result.Succeeded)
                && metrics.Transmissions == 3
                && metrics.AwarePeople == 4
                && runtime.QueryTransmissionsByRoot(created.Rumor.RootRumorId).Select(item => item.ListenerPersonId).SequenceEqual(expectedListeners);
            return TestLabAssertions.True("step12-rumor-propagation", "Propagation is bounded and deterministic", valid, $"Create={created.Status} Propagate={propagated.Succeeded} Transmissions={metrics.Transmissions} Aware={metrics.AwarePeople}");
        }

        private static TestLabAutomationStepResult RumorSocialBoundary(TestLabAutomationContext context)
        {
            if (!TryGetRumorRuntime(context, out RumorRuntime runtime, out _, out string failure))
            {
                return TestLabAssertions.Fail("step12-rumor-social-boundary", "Verify rumor separation from other social runtimes", "RumorRuntime", "MissingRuntime", failure);
            }

            int relationshipsBefore = context.ScenarioContext.Runtimes.Relationships.Count;
            int attitudesBefore = context.ScenarioContext.Runtimes.Attitudes.Count;
            int reputationBefore = context.ScenarioContext.Runtimes.Reputation.Count;
            RumorOperationResult created = CreateRootRumor(context, runtime, "boundary-root", PrototypeRumorDefinitionFactory.ReputationRumorId, "person.prototype.friend", RumorAuthenticity.Disputed);
            RumorOperationResult transmitted = runtime.Transmit(new RumorTransmissionRequest
            {
                TransactionId = Tx(context, "boundary-transmit"),
                TransmissionId = RumorScoped(context, "transmission-boundary"),
                RumorVersionId = created.Rumor?.RumorId,
                SpeakerPersonId = "person.prototype.friend",
                ListenerPersonId = context.ScenarioContext.Runtimes.PersonId,
                ChannelId = PrototypeRumorDefinitionFactory.ConversationChannelId,
                RequestedOutcome = RumorTransmissionOutcome.Uncertain,
                WorldTime = 50d
            });

            bool valid = created.Succeeded
                && transmitted.Succeeded
                && context.ScenarioContext.Runtimes.Relationships.Count == relationshipsBefore
                && context.ScenarioContext.Runtimes.Attitudes.Count == attitudesBefore
                && context.ScenarioContext.Runtimes.Reputation.Count == reputationBefore;
            return TestLabAssertions.True("step12-rumor-social-boundary", "Rumors do not mutate relationships, attitudes, or reputation directly", valid, $"Rumors={runtime.RumorCount}/{runtime.TransmissionCount} Relationships={relationshipsBefore}->{context.ScenarioContext.Runtimes.Relationships.Count} Attitudes={attitudesBefore}->{context.ScenarioContext.Runtimes.Attitudes.Count} Reputation={reputationBefore}->{context.ScenarioContext.Runtimes.Reputation.Count}");
        }

        private static TestLabAutomationStepResult RumorPersistenceValidation(TestLabAutomationContext context)
        {
            if (!TryGetRumorRuntime(context, out RumorRuntime runtime, out DefinitionRegistry registry, out string failure))
            {
                return TestLabAssertions.Fail("step12-rumor-persistence", "Save, restore, and reject invalid rumor payloads", "RumorRuntime", "MissingRuntime", failure);
            }

            RumorOperationResult created = CreateRootRumor(context, runtime, "persist-root", PrototypeRumorDefinitionFactory.FabricatedAccusationRumorId, "person.prototype.friend", RumorAuthenticity.Fabricated);
            RumorRuntimeSaveData save = runtime.CreateSaveData();
            RumorRuntime restored = new RumorRuntime();
            restored.Configure(registry, context.ScenarioContext.Runtimes.KnownPersonIds);
            RumorOperationResult restore = restored.RestoreFromSaveData(save, registry, context.ScenarioContext.Runtimes.KnownPersonIds, restoringState: true);
            RumorRuntimeSaveData corrupt = save.Clone();
            corrupt.rumors[0].definitionId = "rumor.missing";
            bool rejected = !RumorRuntime.ValidateSaveData(corrupt, registry, context.ScenarioContext.Runtimes.KnownPersonIds, out string validationFailure);

            bool valid = created.Succeeded
                && restore.Succeeded
                && restored.RumorCount == runtime.RumorCount
                && restored.TryGetRumor(created.Rumor.RumorId, out RumorSnapshot restoredRumor)
                && restoredRumor.Authenticity == RumorAuthenticity.Fabricated
                && rejected
                && runtime.RumorCount == 1;
            return TestLabAssertions.True("step12-rumor-persistence", "Rumors persist and reject corrupt restores", valid, $"Create={created.Status} Restore={restore.Status} Rejected={rejected} Failure='{validationFailure}' Counts={runtime.RumorCount}/{restored.RumorCount}");
        }

        private static bool TryGetRumorRuntime(TestLabAutomationContext context, out RumorRuntime runtime, out DefinitionRegistry registry, out string failure)
        {
            runtime = context?.ScenarioContext?.Runtimes?.Rumors;
            registry = context?.ScenarioContext?.Runtimes?.DefinitionRegistry;
            if (runtime == null || registry == null)
            {
                failure = runtime == null ? "Rumor runtime is missing from the Test Lab runtime bundle." : "Definition registry is missing from the Test Lab runtime bundle.";
                return false;
            }

            runtime.Configure(registry, context.ScenarioContext.Runtimes.KnownPersonIds, requestedPersonId => string.Equals(requestedPersonId, context.ScenarioContext.Runtimes.PersonId, StringComparison.Ordinal) ? context.ScenarioContext.Runtimes.Knowledge : null, requestedPersonId => string.Equals(requestedPersonId, context.ScenarioContext.Runtimes.PersonId, StringComparison.Ordinal) ? context.ScenarioContext.Runtimes.Memory : null);
            failure = string.Empty;
            return true;
        }

        private static RumorOperationResult CreateRootRumor(TestLabAutomationContext context, RumorRuntime runtime, string suffix, string definitionId, string originatorPersonId, RumorAuthenticity authenticity, RumorDisclosure? disclosure = null)
        {
            return runtime.CreateRumor(new RumorCreateRequest
            {
                TransactionId = Tx(context, suffix),
                RumorId = RumorScoped(context, suffix),
                DefinitionId = definitionId,
                Claim = BuildRumorClaim(context, suffix),
                OriginatorPersonId = originatorPersonId,
                OriginCategory = RumorOriginCategory.FirsthandObservation,
                OriginatingEventId = RumorScoped(context, $"source-{suffix}"),
                SourceAttributionPersonId = originatorPersonId,
                SourceNamed = true,
                Confidence = authenticity == RumorAuthenticity.Fabricated ? 380 : 720,
                Salience = 620,
                Memorability = 610,
                DisclosureOverride = disclosure,
                Authenticity = authenticity,
                WorldTime = 10d,
                Tags = new[] { "feature.12.4", suffix }
            });
        }

        private static KnowledgePropositionData BuildRumorClaim(TestLabAutomationContext context, string suffix)
        {
            return new KnowledgePropositionData
            {
                factDefinitionId = BuiltInKnowledgeFacts.EventOccurred,
                subjectType = KnowledgeSubjectType.Event,
                subjectId = RumorScoped(context, $"claim-{suffix}"),
                valueType = KnowledgeValueType.Boolean,
                booleanValue = true,
                sourceContextId = RumorScoped(context, $"source-context-{suffix}")
            };
        }

        private static TestLabAutomationStepResult InteractionReadinessAndPreview(TestLabAutomationContext context)
        {
            if (!TryGetInteractionRuntime(context, out SocialInteractionRuntime runtime, out DefinitionRegistry registry, out string failure))
            {
                return TestLabAssertions.Fail("step12-interaction-preview", "Preview interaction without mutation", "SocialInteractionRuntime", "MissingRuntime", failure);
            }

            bool definitions = registry.TryGet(PrototypeSocialInteractionDefinitionFactory.GreetId, out SocialInteractionDefinition _)
                && registry.TryGet(PrototypeSocialInteractionDefinitionFactory.ComplimentId, out SocialInteractionDefinition _)
                && registry.TryGet(PrototypeSocialInteractionDefinitionFactory.PromiseId, out SocialInteractionDefinition _);
            long before = runtime.Revision;
            SocialInteractionResult preview = runtime.Preview(InteractionRequest(context, PrototypeSocialInteractionDefinitionFactory.GreetId, "preview", worldTime: 10d));
            bool valid = runtime.IsReady
                && definitions
                && preview.Succeeded
                && preview.Preview
                && preview.Record != null
                && runtime.Revision == before
                && runtime.Count == 0;
            return TestLabAssertions.True("step12-interaction-preview", "Preview interaction without mutation", valid, $"Ready={runtime.IsReady} Definitions={definitions} Preview={preview.Status} Revision={before}->{runtime.Revision} Count={runtime.Count}");
        }

        private static TestLabAutomationStepResult InteractionAttitudeConsequences(TestLabAutomationContext context)
        {
            if (!TryGetInteractionRuntime(context, out SocialInteractionRuntime runtime, out _, out string failure))
            {
                return TestLabAssertions.Fail("step12-interaction-attitudes", "Execute attitude-producing interactions", "SocialInteractionRuntime", "MissingRuntime", failure);
            }

            string initiator = context.ScenarioContext.Runtimes.PersonId;
            string target = "person.prototype.friend";
            SocialInteractionResult compliment = runtime.Execute(InteractionRequest(context, PrototypeSocialInteractionDefinitionFactory.ComplimentId, "compliment", target, worldTime: 20d));
            SocialInteractionResult duplicate = runtime.Execute(InteractionRequest(context, PrototypeSocialInteractionDefinitionFactory.ComplimentId, "compliment", target, worldTime: 20d));
            SocialInteractionResult insult = runtime.Execute(InteractionRequest(context, PrototypeSocialInteractionDefinitionFactory.InsultId, "insult", target, worldTime: 30d));
            AttitudeEffectiveValueSnapshot affection = context.ScenarioContext.Runtimes.Attitudes.ResolveValue(target, initiator, PrototypeAttitudeDefinitionFactory.AffectionId);
            AttitudeEffectiveValueSnapshot hostility = context.ScenarioContext.Runtimes.Attitudes.ResolveValue(target, initiator, PrototypeAttitudeDefinitionFactory.HostilityId);
            bool valid = compliment.Succeeded
                && duplicate.Duplicate
                && insult.Succeeded
                && affection.EffectiveValue == -2
                && hostility.EffectiveValue > 0
                && runtime.QueryByPerson(target).Count >= 2;
            return TestLabAssertions.True("step12-interaction-attitudes", "Execute attitude-producing interactions", valid, $"Compliment={compliment.Status} Duplicate={duplicate.Status} Insult={insult.Status} Affection={affection.EffectiveValue} Hostility={hostility.EffectiveValue}");
        }

        private static TestLabAutomationStepResult InteractionPendingResponsePromise(TestLabAutomationContext context)
        {
            if (!TryGetInteractionRuntime(context, out SocialInteractionRuntime runtime, out _, out string failure))
            {
                return TestLabAssertions.Fail("step12-interaction-pending", "Create pending interaction and accept promise", "SocialInteractionRuntime", "MissingRuntime", failure);
            }

            string target = "person.prototype.friend";
            SocialInteractionResult pending = runtime.Execute(InteractionRequest(context, PrototypeSocialInteractionDefinitionFactory.PromiseId, "promise-pending", target, worldTime: 40d));
            SocialInteractionResult preview = runtime.RespondToPending(Tx(context, "promise-accept-preview"), pending.Pending?.PendingInteractionId, SocialInteractionResponse.Accept, 41d, preview: true);
            SocialInteractionResult accepted = runtime.RespondToPending(Tx(context, "promise-accept"), pending.Pending?.PendingInteractionId, SocialInteractionResponse.Accept, 42d);
            bool hasPromise = !string.IsNullOrWhiteSpace(accepted.Promise?.PromiseId) && runtime.TryGetPromise(accepted.Promise.PromiseId, out SocialPromiseSnapshot promise) && promise.Status == SocialPromiseStatus.Active;
            bool valid = pending.Succeeded
                && pending.Status == SocialInteractionStatus.Pending
                && pending.Pending != null
                && preview.Succeeded
                && preview.Preview
                && accepted.Succeeded
                && accepted.Record.Outcome == SocialInteractionOutcome.Accepted
                && hasPromise;
            return TestLabAssertions.True("step12-interaction-pending", "Create pending interaction and accept promise", valid, $"Pending={pending.Status} Preview={preview.Status} Accepted={accepted.Status} Promise={accepted.Promise?.PromiseId}");
        }

        private static TestLabAutomationStepResult InteractionPublicReputation(TestLabAutomationContext context)
        {
            if (!TryGetInteractionRuntime(context, out SocialInteractionRuntime runtime, out _, out string failure))
            {
                return TestLabAssertions.Fail("step12-interaction-reputation", "Execute public reputation consequences", "SocialInteractionRuntime", "MissingRuntime", failure);
            }

            string target = "person.prototype.friend";
            SocialInteractionResult praise = runtime.Execute(InteractionRequest(context, PrototypeSocialInteractionDefinitionFactory.PublicPraiseId, "public-praise", target, worldTime: 50d, visibility: SocialInteractionVisibility.Public));
            SocialInteractionResult threat = runtime.Execute(InteractionRequest(context, PrototypeSocialInteractionDefinitionFactory.ThreatenId, "threaten", target, worldTime: 60d, witnesses: new[] { "person.prototype.rival" }, visibility: SocialInteractionVisibility.Witnessed));
            ReputationEffectiveValueSnapshot targetEsteem = context.ScenarioContext.Runtimes.Reputation.ResolveValue(target, PrototypeReputationDefinitionFactory.GlobalPublicAudienceId, PrototypeReputationDefinitionFactory.EsteemId);
            ReputationEffectiveValueSnapshot initiatorDanger = context.ScenarioContext.Runtimes.Reputation.ResolveValue(context.ScenarioContext.Runtimes.PersonId, PrototypeReputationDefinitionFactory.GlobalPublicAudienceId, PrototypeReputationDefinitionFactory.PerceivedDangerId);
            bool valid = praise.Succeeded
                && threat.Succeeded
                && targetEsteem.EffectiveValue > 0
                && initiatorDanger.EffectiveValue > 0;
            return TestLabAssertions.True("step12-interaction-reputation", "Execute public reputation consequences", valid, $"Praise={praise.Status} Threat={threat.Status} Esteem={targetEsteem.EffectiveValue} Danger={initiatorDanger.EffectiveValue}");
        }

        private static TestLabAutomationStepResult InteractionRumorDelegation(TestLabAutomationContext context)
        {
            if (!TryGetInteractionRuntime(context, out SocialInteractionRuntime runtime, out _, out string failure))
            {
                return TestLabAssertions.Fail("step12-interaction-rumor", "Share existing rumor through interaction", "SocialInteractionRuntime", "MissingRuntime", failure);
            }

            RumorRuntime rumorsRuntime = context.ScenarioContext.Runtimes.Rumors;
            RumorOperationResult created = CreateRootRumor(context, rumorsRuntime, "interaction-share", PrototypeRumorDefinitionFactory.PublicNewsRumorId, context.ScenarioContext.Runtimes.PersonId, RumorAuthenticity.Verified);
            SocialInteractionRequest request = InteractionRequest(context, PrototypeSocialInteractionDefinitionFactory.ShareInformationId, "share-rumor", "person.prototype.friend", worldTime: 70d);
            request.Subject = new SocialInteractionSubjectData { kind = SocialInteractionSubjectKind.Rumor, subjectId = created.Rumor?.RumorId };
            SocialInteractionResult shared = runtime.Execute(request);
            bool valid = created.Succeeded
                && shared.Succeeded
                && !string.IsNullOrWhiteSpace(shared.Record?.Data.rumorTransmissionId)
                && rumorsRuntime.TransmissionCount > 0;
            return TestLabAssertions.True("step12-interaction-rumor", "Share existing rumor through interaction", valid, $"Create={created.Status} Shared={shared.Status} Transmission={shared.Record?.Data.rumorTransmissionId} Count={rumorsRuntime.TransmissionCount}");
        }

        private static TestLabAutomationStepResult InteractionPersistenceValidation(TestLabAutomationContext context)
        {
            if (!TryGetInteractionRuntime(context, out SocialInteractionRuntime runtime, out DefinitionRegistry registry, out string failure))
            {
                return TestLabAssertions.Fail("step12-interaction-persistence", "Save, restore, duplicate, and reject invalid payloads", "SocialInteractionRuntime", "MissingRuntime", failure);
            }

            SocialInteractionResult execute = runtime.Execute(InteractionRequest(context, PrototypeSocialInteractionDefinitionFactory.ThankId, "persist-thank", "person.prototype.friend", worldTime: 80d));
            SocialInteractionRuntimeSaveData save = runtime.CreateSaveData();
            SocialInteractionRuntime restored = new SocialInteractionRuntime();
            SocialInteractionResult restore = restored.RestoreFromSaveData(save, registry, context.ScenarioContext.Runtimes.KnownPersonIds, restoringState: true);
            SocialInteractionRuntimeSaveData corrupt = save.Clone();
            if (corrupt.records.Count > 0)
            {
                corrupt.records[0].interactionDefinitionId = "social-interaction.missing";
            }

            bool rejected = !SocialInteractionRuntime.ValidateSaveData(corrupt, registry, context.ScenarioContext.Runtimes.KnownPersonIds, out string validationFailure);
            bool valid = execute.Succeeded
                && restore.Succeeded
                && restored.Count == runtime.Count
                && rejected
                && runtime.Count == save.records.Count;
            return TestLabAssertions.True("step12-interaction-persistence", "Save, restore, duplicate, and reject invalid payloads", valid, $"Execute={execute.Status} Restore={restore.Status} Rejected={rejected} Failure='{validationFailure}' Counts={runtime.Count}/{restored.Count}");
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

        private static ITestLabAutomationScenario ReputationScenario(string scenarioId, string displayName, int order, params ITestLabScenarioStep[] steps)
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
                    PrototypeReputationDefinitionFactory.GlobalPublicAudienceId,
                    PrototypeReputationDefinitionFactory.PrototypeTownAudienceId,
                    PrototypeReputationDefinitionFactory.AdventurersGuildAudienceId,
                    PrototypeReputationDefinitionFactory.AdventurersGuildVeteransAudienceId,
                    PrototypeReputationDefinitionFactory.RoyalJurisdictionAudienceId,
                    PrototypeReputationDefinitionFactory.HiddenInvestigatorsAudienceId,
                    PrototypeReputationDefinitionFactory.RenownId,
                    PrototypeReputationDefinitionFactory.EsteemId,
                    PrototypeReputationDefinitionFactory.NotorietyId,
                    PrototypeReputationDefinitionFactory.CredibilityId,
                    PrototypeReputationDefinitionFactory.PerceivedDangerId,
                    PrototypeReputationDefinitionFactory.HonorId
                });
        }

        private static ITestLabAutomationScenario RumorScenario(string scenarioId, string displayName, int order, params ITestLabScenarioStep[] steps)
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
                    PrototypeRumorDefinitionFactory.PersonalConductRumorId,
                    PrototypeRumorDefinitionFactory.PublicNewsRumorId,
                    PrototypeRumorDefinitionFactory.FabricatedAccusationRumorId,
                    PrototypeRumorDefinitionFactory.SecretLeakRumorId,
                    PrototypeRumorDefinitionFactory.ReputationRumorId,
                    PrototypeRumorDefinitionFactory.ConversationChannelId,
                    PrototypeRumorDefinitionFactory.TavernGossipChannelId,
                    PrototypeRumorDefinitionFactory.PublicSpeechChannelId
                });
        }

        private static ITestLabAutomationScenario InteractionScenario(string scenarioId, string displayName, int order, params ITestLabScenarioStep[] steps)
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
                    PrototypeSocialInteractionDefinitionFactory.GreetId,
                    PrototypeSocialInteractionDefinitionFactory.ComplimentId,
                    PrototypeSocialInteractionDefinitionFactory.InsultId,
                    PrototypeSocialInteractionDefinitionFactory.PromiseId,
                    PrototypeSocialInteractionDefinitionFactory.PublicPraiseId,
                    PrototypeSocialInteractionDefinitionFactory.ThreatenId,
                    PrototypeSocialInteractionDefinitionFactory.ShareInformationId,
                    PrototypeAttitudeDefinitionFactory.AffectionId,
                    PrototypeAttitudeDefinitionFactory.HostilityId,
                    PrototypeReputationDefinitionFactory.GlobalPublicAudienceId,
                    PrototypeReputationDefinitionFactory.EsteemId,
                    PrototypeReputationDefinitionFactory.PerceivedDangerId,
                    PrototypeRumorDefinitionFactory.PublicNewsRumorId,
                    PrototypeRumorDefinitionFactory.ConversationChannelId
                });
        }

        private static TestLabAutomationStepResult NormReadinessAndPreview(TestLabAutomationContext context)
        {
            if (!TryGetNormRuntime(context, out SocialNormRuntime runtime, out DefinitionRegistry registry, out string failure))
            {
                return TestLabAssertions.Fail("step12-norm-preview", "Preview host greeting norm", "SocialNormRuntime", "MissingRuntime", failure);
            }

            long before = runtime.Revision;
            SocialNormEvaluationResult preview = runtime.Preview(NormRequest(
                context,
                "host-greeting-preview",
                PrototypeSocialInteractionDefinitionFactory.GreetId,
                requestedNormIds: new[] { PrototypeSocialNormDefinitionFactory.HostGreetingNormId },
                tags: new[] { "host-context" },
                placeId: "place.prototype.court"));
            bool definitions = registry.TryGet(PrototypeSocialNormDefinitionFactory.HostGreetingNormId, out SocialNormDefinition _)
                && registry.TryGet(PrototypeSocialNormDefinitionFactory.PublicInsultNormId, out SocialNormDefinition _)
                && registry.TryGet(PrototypeSocialNormDefinitionFactory.PromiseKeepingNormId, out SocialNormDefinition _);
            bool valid = definitions
                && preview.Succeeded
                && preview.Preview
                && preview.Assessments.Count > 0
                && preview.Assessments.Any(item => item.Classification == SocialNormAssessmentClassification.Satisfied)
                && runtime.Revision == before
                && runtime.Count == 0;
            return TestLabAssertions.True("step12-norm-preview", "Norm definitions resolve and previews do not mutate", valid, $"Definitions={definitions} Preview={preview.Status} Revision={before}->{runtime.Revision} Count={runtime.Count}");
        }

        private static TestLabAutomationStepResult NormVisibilityConsequences(TestLabAutomationContext context)
        {
            if (!TryGetNormRuntime(context, out SocialNormRuntime runtime, out _, out string failure))
            {
                return TestLabAssertions.Fail("step12-norm-visibility", "Assess private and public insult norms", "SocialNormRuntime", "MissingRuntime", failure);
            }

            SocialNormEvaluationResult privateInsult = runtime.Execute(NormRequest(
                context,
                "private-insult",
                PrototypeSocialInteractionDefinitionFactory.InsultId,
                requestedNormIds: new[] { PrototypeSocialNormDefinitionFactory.PrivateInsultNormId },
                visibility: SocialInteractionVisibility.Private));
            SocialNormEvaluationResult publicInsult = runtime.Execute(NormRequest(
                context,
                "public-insult",
                PrototypeSocialInteractionDefinitionFactory.InsultId,
                requestedNormIds: new[] { PrototypeSocialNormDefinitionFactory.PublicInsultNormId },
                witnesses: new[] { "person.prototype.rival" },
                visibility: SocialInteractionVisibility.Public,
                classification: SocialNormAssessmentClassification.Violation));
            SocialNormAssessmentSnapshot privateSnapshot = privateInsult.Assessments.FirstOrDefault();
            SocialNormAssessmentSnapshot publicSnapshot = publicInsult.Assessments.FirstOrDefault();
            bool publicHasRequiredConsequence = publicSnapshot != null && publicSnapshot.Consequences.Any(item => item.policy == SocialNormConsequencePolicy.Required && item.committed);
            bool valid = privateInsult.Succeeded
                && publicInsult.Succeeded
                && privateSnapshot != null
                && publicSnapshot != null
                && privateSnapshot.Severity < publicSnapshot.Severity
                && publicHasRequiredConsequence
                && runtime.QueryByObserver("person.prototype.rival").Count > 0;
            return TestLabAssertions.True("step12-norm-visibility", "Public and private etiquette produce deterministic consequence plans", valid, $"Private={privateInsult.Status}/{privateSnapshot?.Severity} Public={publicInsult.Status}/{publicSnapshot?.Severity} PublicRequired={publicHasRequiredConsequence}");
        }

        private static TestLabAutomationStepResult NormKnowledgeExceptionObservers(TestLabAutomationContext context)
        {
            if (!TryGetNormRuntime(context, out SocialNormRuntime runtime, out _, out string failure))
            {
                return TestLabAssertions.Fail("step12-norm-knowledge-exception", "Evaluate ignorance and witness context", "SocialNormRuntime", "MissingRuntime", failure);
            }

            SocialNormEvaluationResult ignorance = runtime.Execute(NormRequest(
                context,
                "ignorance",
                PrototypeSocialInteractionDefinitionFactory.CustomActionId,
                requestedNormIds: new[] { PrototypeSocialNormDefinitionFactory.IgnoranceMitigatedEtiquetteNormId },
                witnesses: new[] { "person.prototype.friend" },
                tags: new[] { "culture.prototype.formal", "actor-unaware" },
                actorKnowledge: SocialNormActorKnowledgeState.Unknown,
                classification: SocialNormAssessmentClassification.Violation));
            SocialNormEvaluationResult emergency = runtime.Execute(NormRequest(
                context,
                "emergency",
                PrototypeSocialInteractionDefinitionFactory.ShareInformationId,
                requestedNormIds: new[] { PrototypeSocialNormDefinitionFactory.EmergencyDisclosureNormId },
                witnesses: new[] { "person.prototype.friend" },
                tags: new[] { "secret-subject", "emergency" },
                visibility: SocialInteractionVisibility.Public,
                classification: SocialNormAssessmentClassification.SeriousViolation));
            SocialNormAssessmentSnapshot ignoranceSnapshot = ignorance.Assessments.FirstOrDefault();
            SocialNormAssessmentSnapshot emergencySnapshot = emergency.Assessments.FirstOrDefault();
            bool exceptionApplied = ignoranceSnapshot != null && ignoranceSnapshot.Data.exceptions.Any(item => item.applied && item.effect == SocialNormExceptionEffect.ReduceSeverity);
            bool observerRecorded = ignoranceSnapshot != null && ignoranceSnapshot.Observers.Any(item => string.Equals(item.observerPersonId, "person.prototype.friend", StringComparison.Ordinal));
            bool emergencyExcused = emergencySnapshot != null
                && emergencySnapshot.Classification == SocialNormAssessmentClassification.Excused
                && emergencySnapshot.Data.exceptions.Any(item => item.applied && item.effect == SocialNormExceptionEffect.ExcuseViolation);
            bool valid = ignorance.Succeeded
                && emergency.Succeeded
                && exceptionApplied
                && observerRecorded
                && emergencyExcused;
            return TestLabAssertions.True("step12-norm-knowledge-exception", "Actor knowledge, exceptions, and observer interpretation remain explicit", valid, $"Ignorance={ignoranceSnapshot?.Classification} Exception={exceptionApplied} Observer={observerRecorded} Emergency={emergencySnapshot?.Classification}");
        }

        private static TestLabAutomationStepResult NormConflictAndPromise(TestLabAutomationContext context)
        {
            if (!TryGetNormRuntime(context, out SocialNormRuntime runtime, out _, out string failure))
            {
                return TestLabAssertions.Fail("step12-norm-conflict-promise", "Assess conflict and promise breach", "SocialNormRuntime", "MissingRuntime", failure);
            }

            SocialNormEvaluationResult conflict = runtime.Execute(NormRequest(
                context,
                "conflict",
                PrototypeSocialInteractionDefinitionFactory.PublicPraiseId,
                requestedNormIds: new[] { PrototypeSocialNormDefinitionFactory.PraiseEnemyConflictNormId, PrototypeSocialNormDefinitionFactory.HospitalityOverrideNormId },
                witnesses: new[] { "person.prototype.friend" },
                tags: new[] { "audience.enemy-of-target", "hospitality-duty", "actor-role.host", "target-role.rival" },
                visibility: SocialInteractionVisibility.Public,
                classification: SocialNormAssessmentClassification.Satisfied));
            SocialNormEvaluationResult promise = runtime.Execute(NormRequest(
                context,
                "promise-breach",
                PrototypeSocialInteractionDefinitionFactory.PromiseId,
                requestedNormIds: new[] { PrototypeSocialNormDefinitionFactory.PromiseKeepingNormId },
                tags: new[] { "promise-context" },
                promiseId: NormScoped(context, "promise"),
                promiseState: SocialPromiseStatus.Breached.ToString(),
                classification: SocialNormAssessmentClassification.Violation));
            bool conflictSuppressed = conflict.Assessments.Any(item => item.Applicability == SocialNormApplicabilityStatus.SuppressedByConflict)
                && conflict.Assessments.Any(item => item.Conflicts.Count > 0);
            SocialNormAssessmentSnapshot promiseSnapshot = promise.Assessments.FirstOrDefault();
            bool promiseValid = promiseSnapshot != null
                && promiseSnapshot.PromiseId.Length > 0
                && promiseSnapshot.Classification == SocialNormAssessmentClassification.Violation
                && promiseSnapshot.Consequences.Any(item => item.targetRuntime == SocialNormConsequenceTargetRuntime.InterpersonalAttitude);
            bool valid = conflict.Succeeded && promise.Succeeded && conflictSuppressed && promiseValid;
            return TestLabAssertions.True("step12-norm-conflict-promise", "Norm conflicts and promise expectations resolve deterministically", valid, $"Conflict={conflict.Status} Suppressed={conflictSuppressed} Promise={promise.Status}/{promiseSnapshot?.Classification}");
        }

        private static TestLabAutomationStepResult NormPersistenceIdempotence(TestLabAutomationContext context)
        {
            if (!TryGetNormRuntime(context, out SocialNormRuntime runtime, out DefinitionRegistry registry, out string failure))
            {
                return TestLabAssertions.Fail("step12-norm-persistence", "Save, restore, duplicate, and reject invalid norm payloads", "SocialNormRuntime", "MissingRuntime", failure);
            }

            SocialNormEvaluationRequest request = NormRequest(
                context,
                "persist",
                PrototypeSocialInteractionDefinitionFactory.ThankId,
                requestedNormIds: new[] { PrototypeSocialNormDefinitionFactory.HostGreetingNormId },
                classification: SocialNormAssessmentClassification.Satisfied);
            SocialNormEvaluationResult execute = runtime.Execute(request);
            SocialNormEvaluationResult duplicate = runtime.Execute(request);
            SocialNormRuntimeSaveData save = runtime.CreateSaveData();
            SocialNormRuntime restored = new SocialNormRuntime();
            restored.Configure(registry, context.ScenarioContext.Runtimes.KnownPersonIds, context.ScenarioContext.Runtimes.Relationships, context.ScenarioContext.Runtimes.Attitudes, context.ScenarioContext.Runtimes.Reputation, context.ScenarioContext.Runtimes.Rumors, context.ScenarioContext.Runtimes.SocialInteractions);
            SocialNormEvaluationResult restore = restored.RestoreFromSaveData(save, registry, context.ScenarioContext.Runtimes.KnownPersonIds, restoringState: true);
            SocialNormRuntimeSaveData corrupt = save.Clone();
            corrupt.assessments[0].normDefinitionId = "social-norm.prototype.missing";
            SocialNormPersistenceParticipant participant = new SocialNormPersistenceParticipant(runtime, () => registry, () => context.ScenarioContext.Runtimes.KnownPersonIds.ToArray());
            PersistenceParticipantPrepareResult rejected = participant.PreparePayload(JsonUtility.ToJson(corrupt), SocialNormPersistenceParticipant.CurrentParticipantSchemaVersion);
            bool valid = execute.Succeeded
                && duplicate.Duplicate
                && restore.Succeeded
                && restored.Count == runtime.Count
                && rejected != null
                && !rejected.Succeeded;
            return TestLabAssertions.True("step12-norm-persistence", "Norm assessments persist and duplicate transactions are idempotent", valid, $"Execute={execute.Status} Duplicate={duplicate.Status}/{duplicate.Duplicate} Restore={restore.Status} Reject={rejected?.Succeeded}");
        }

        private static ITestLabAutomationScenario NormScenario(string scenarioId, string displayName, int order, params ITestLabScenarioStep[] steps)
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
                    PrototypeSocialNormDefinitionFactory.HostGreetingNormId,
                    PrototypeSocialNormDefinitionFactory.PublicInsultNormId,
                    PrototypeSocialNormDefinitionFactory.PrivateInsultNormId,
                    PrototypeSocialNormDefinitionFactory.IgnoranceMitigatedEtiquetteNormId,
                    PrototypeSocialNormDefinitionFactory.WitnessRespectNormId,
                    PrototypeSocialNormDefinitionFactory.EmergencyDisclosureNormId,
                    PrototypeSocialNormDefinitionFactory.PromiseKeepingNormId,
                    PrototypeSocialNormDefinitionFactory.PraiseEnemyConflictNormId,
                    PrototypeSocialNormDefinitionFactory.HospitalityOverrideNormId,
                    PrototypeSocialInteractionDefinitionFactory.GreetId,
                    PrototypeSocialInteractionDefinitionFactory.InsultId,
                    PrototypeSocialInteractionDefinitionFactory.PromiseId,
                    PrototypeAttitudeDefinitionFactory.RespectId,
                    PrototypeReputationDefinitionFactory.EsteemId
                });
        }

        private static bool TryGetNormRuntime(TestLabAutomationContext context, out SocialNormRuntime runtime, out DefinitionRegistry registry, out string failure)
        {
            runtime = context?.ScenarioContext?.Runtimes?.SocialNorms;
            registry = context?.ScenarioContext?.Runtimes?.DefinitionRegistry;
            if (runtime == null || registry == null)
            {
                failure = runtime == null ? "Social Norm runtime is missing from the Test Lab runtime bundle." : "Definition registry is missing from the Test Lab runtime bundle.";
                return false;
            }

            failure = string.Empty;
            return true;
        }

        private static SocialNormEvaluationRequest NormRequest(
            TestLabAutomationContext context,
            string suffix,
            string interactionDefinitionId,
            IReadOnlyList<string> requestedNormIds = null,
            string target = "person.prototype.friend",
            IReadOnlyList<string> witnesses = null,
            IReadOnlyList<string> tags = null,
            SocialInteractionVisibility visibility = SocialInteractionVisibility.Private,
            SocialNormAssessmentClassification classification = SocialNormAssessmentClassification.Unknown,
            SocialNormActorKnowledgeState actorKnowledge = SocialNormActorKnowledgeState.Knew,
            string promiseId = "",
            string promiseState = "",
            string placeId = "place.prototype.test-lab")
        {
            string[] contextTags = (tags ?? Array.Empty<string>())
                .Concat(string.IsNullOrWhiteSpace(promiseState) ? Array.Empty<string>() : new[] { $"promise-state.{promiseState}" })
                .ToArray();
            return new SocialNormEvaluationRequest
            {
                TransactionId = Tx(context, suffix),
                AssessmentRecordId = NormScoped(context, suffix),
                ActorPersonId = context.ScenarioContext.Runtimes.PersonId,
                TargetPersonId = target,
                InteractionRecordId = InteractionScoped(context, $"norm-{suffix}"),
                InteractionDefinitionId = interactionDefinitionId,
                PromiseId = promiseId,
                Subject = new SocialInteractionSubjectData
                {
                    kind = SocialInteractionSubjectKind.Person,
                    subjectId = target,
                    ownerPersonId = target,
                    tags = new[] { "test-lab", "social-norm" }
                },
                PlaceId = placeId,
                AudienceId = PrototypeReputationDefinitionFactory.GlobalPublicAudienceId,
                WitnessPersonIds = (witnesses ?? Array.Empty<string>()).ToArray(),
                ContextTags = contextTags,
                RequestedNormIds = (requestedNormIds ?? Array.Empty<string>()).ToArray(),
                Visibility = visibility,
                Channel = SocialInteractionCommunicationChannel.Conversation,
                ConductClassification = classification,
                ActorKnowledge = actorKnowledge,
                OccurrenceWorldTime = context.CurrentStepIndex + 1d,
                EvaluationWorldTime = context.CurrentStepIndex + 1d,
                DeterministicSeed = context.RunId
            };
        }

        private static bool TryGetInteractionRuntime(TestLabAutomationContext context, out SocialInteractionRuntime runtime, out DefinitionRegistry registry, out string failure)
        {
            runtime = context?.ScenarioContext?.Runtimes?.SocialInteractions;
            registry = context?.ScenarioContext?.Runtimes?.DefinitionRegistry;
            if (runtime == null || registry == null)
            {
                failure = runtime == null ? "Social Interaction runtime is missing from the Test Lab runtime bundle." : "Definition registry is missing from the Test Lab runtime bundle.";
                return false;
            }

            failure = string.Empty;
            return true;
        }

        private static SocialInteractionRequest InteractionRequest(TestLabAutomationContext context, string definitionId, string suffix, string target = "person.prototype.friend", double worldTime = 0d, IReadOnlyList<string> witnesses = null, SocialInteractionVisibility? visibility = null)
        {
            return new SocialInteractionRequest
            {
                TransactionId = Tx(context, suffix),
                InteractionRecordId = InteractionScoped(context, suffix),
                InteractionDefinitionId = definitionId,
                InitiatorPersonId = context.ScenarioContext.Runtimes.PersonId,
                TargetPersonId = target,
                WitnessPersonIds = witnesses ?? Array.Empty<string>(),
                AudienceId = PrototypeReputationDefinitionFactory.GlobalPublicAudienceId,
                PlaceId = "place.prototype.test-lab",
                Subject = new SocialInteractionSubjectData
                {
                    kind = SocialInteractionSubjectKind.Person,
                    subjectId = target,
                    ownerPersonId = target,
                    tags = new[] { "test-lab" }
                },
                Channel = SocialInteractionCommunicationChannel.Conversation,
                VisibilityOverride = visibility,
                WorldTime = worldTime,
                DeterministicSeed = context.RunId
            };
        }

        private static ITestLabScenarioStep Step(string stepId, string displayName, Func<TestLabAutomationContext, TestLabAutomationStepResult> run)
        {
            return new TestLabScenarioStep(stepId, displayName, run);
        }

        private static string Scoped(TestLabAutomationContext context, string suffix)
        {
            return $"relationship.automation.{context.RunId}.{context.CurrentScenarioId}.{suffix}";
        }

        private static string RepScoped(TestLabAutomationContext context, string suffix)
        {
            return $"reputation.automation.{context.RunId}.{context.CurrentScenarioId}.{suffix}";
        }

        private static string RumorScoped(TestLabAutomationContext context, string suffix)
        {
            return $"rumor.automation.{context.RunId}.{context.CurrentScenarioId}.{suffix}";
        }

        private static string InteractionScoped(TestLabAutomationContext context, string suffix)
        {
            return $"social-interaction.automation.{context.RunId}.{context.CurrentScenarioId}.{suffix}";
        }

        private static string NormScoped(TestLabAutomationContext context, string suffix)
        {
            return $"social-norm.automation.{context.RunId}.{context.CurrentScenarioId}.{suffix}";
        }

        private static string Tx(TestLabAutomationContext context, string suffix)
        {
            return context.TransactionIds.Create(context.CurrentSuiteId, context.CurrentScenarioId, context.RunId, context.CurrentStepIndex, suffix);
        }
    }
}
#endif
