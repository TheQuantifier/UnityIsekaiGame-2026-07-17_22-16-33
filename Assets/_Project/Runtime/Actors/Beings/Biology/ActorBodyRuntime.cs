using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityIsekaiGame.ActorLifecycle;
using UnityIsekaiGame.Capabilities;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.Stats;
using UnityIsekaiGame.Traits;

namespace UnityIsekaiGame.Beings.Biology
{
    [DisallowMultipleComponent]
    public sealed class ActorBodyRuntime : MonoBehaviour
    {
        [SerializeField] private SpeciesDefinition defaultSpecies;
        [SerializeField] private string actorBodyIdOverride;
        [SerializeField] private string personIdOverride;

        private DefinitionRegistry registry;
        private CharacterTraitCollection traits;
        private CalculatedStatCollection calculatedStats;
        private string actorBodyId;
        private string personId;
        private string speciesDefinitionId;
        private string appliedSpeciesSourceId;
        private string appliedClassificationSourceId;
        private bool suppressEvents;

        public event Action<ActorBodyRuntime, BodyOperationResult, bool> BodyChanged;

        public BodyReadinessState Readiness { get; private set; } = BodyReadinessState.Uninitialized;
        public long BodyRevision { get; private set; }
        public string ActorBodyId => string.IsNullOrWhiteSpace(actorBodyIdOverride) ? actorBodyId ?? string.Empty : actorBodyIdOverride;
        public string PersonId => string.IsNullOrWhiteSpace(personIdOverride) ? personId ?? string.Empty : personIdOverride;
        public string SpeciesDefinitionId => speciesDefinitionId ?? string.Empty;
        public SpeciesDefinition Species => ResolveSpecies(SpeciesDefinitionId);
        public BiologicalClassificationDefinition BiologicalClassification => Species == null ? null : Species.BiologicalClassification;
        public BodyFormDefinition BodyForm => Species == null ? null : Species.BodyForm;
        public bool IsReady => Readiness == BodyReadinessState.Ready;

        private void OnDisable()
        {
            Readiness = BodyReadinessState.Disposed;
        }

        public void Configure(
            DefinitionRegistry definitionRegistry,
            string exactActorBodyId,
            string owningPersonId,
            CharacterTraitCollection traitCollection,
            CalculatedStatCollection statCollection,
            bool restoring = false)
        {
            registry = definitionRegistry ?? registry;
            actorBodyId = string.IsNullOrWhiteSpace(exactActorBodyId) ? actorBodyId : exactActorBodyId;
            personId = owningPersonId ?? string.Empty;
            traits = traitCollection == null ? traits == null ? GetComponent<CharacterTraitCollection>() : traits : traitCollection;
            calculatedStats = statCollection == null ? calculatedStats == null ? GetComponent<CalculatedStatCollection>() : calculatedStats : statCollection;

            if (string.IsNullOrWhiteSpace(SpeciesDefinitionId) && defaultSpecies != null)
            {
                AssignSpecies(defaultSpecies.Id, restoring, "Default body bootstrap");
                return;
            }

            if (!string.IsNullOrWhiteSpace(SpeciesDefinitionId))
            {
                ReapplyCurrentBiologicalSources(restoring);
            }

            ValidateBody(out _);
        }

        public BodyOperationResult PreviewAssignSpecies(string requestedSpeciesId)
        {
            return ResolveAssignment(requestedSpeciesId, preview: true, out _, out _, out _);
        }

        public BodyOperationResult AssignSpecies(string requestedSpeciesId, bool restoring = false, string reason = "Species assignment")
        {
            BodyOperationResult resolved = ResolveAssignment(requestedSpeciesId, preview: false, out SpeciesDefinition species, out BiologicalClassificationDefinition classification, out BodyFormDefinition bodyForm);
            if (!resolved.Succeeded)
            {
                return resolved;
            }

            if (string.Equals(SpeciesDefinitionId, species.Id, StringComparison.Ordinal) && Readiness == BodyReadinessState.Ready)
            {
                BodyOperationResult duplicate = BodyOperationResult.Success($"Species '{species.Id}' is already assigned to body '{ActorBodyId}'.", CreateSnapshot(), duplicate: true);
                RaiseChanged(duplicate, restoring);
                return duplicate;
            }

            string previousSpecies = speciesDefinitionId;
            string previousSpeciesSource = appliedSpeciesSourceId;
            string previousClassificationSource = appliedClassificationSourceId;
            long previousRevision = BodyRevision;
            BodyReadinessState previousReadiness = Readiness;

            try
            {
                Readiness = BodyReadinessState.ApplyingBiologicalContributions;
                string speciesSourceId = SpeciesSourceId(ActorBodyId, species.Id);
                string classificationSourceId = ClassificationSourceId(ActorBodyId, classification.Id);
                ClearBiologicalSources(previousSpeciesSource, previousClassificationSource, restoring);
                ClearBiologicalSources(speciesSourceId, classificationSourceId, restoring);
                BodyOperationResult applyClassification = ApplyBiologicalSources(
                    classification.DefaultTraitGrants,
                    classification.DefaultCapabilityGrants,
                    classification.DefaultStatContributions,
                    TraitSourceCategory.BiologicalClassification,
                    CapabilitySourceCategory.BiologicalClassification,
                    CalculatedStatContributionSourceCategory.BiologicalClassification,
                    classificationSourceId,
                    restoring);
                if (!applyClassification.Succeeded)
                {
                    throw new InvalidOperationException(applyClassification.Message);
                }

                BodyOperationResult applySpecies = ApplyBiologicalSources(
                    species.DefaultBodyTraits,
                    species.DefaultBooleanCapabilities.Concat(species.DefaultNumericCapabilities).ToArray(),
                    species.CalculatedStatContributions,
                    TraitSourceCategory.Species,
                    CapabilitySourceCategory.Species,
                    CalculatedStatContributionSourceCategory.Species,
                    speciesSourceId,
                    restoring);
                if (!applySpecies.Succeeded)
                {
                    throw new InvalidOperationException(applySpecies.Message);
                }

                speciesDefinitionId = species.Id;
                appliedSpeciesSourceId = speciesSourceId;
                appliedClassificationSourceId = classificationSourceId;
                Readiness = BodyReadinessState.Ready;
                BodyRevision++;

                BodyOperationResult result = BodyOperationResult.Success($"Assigned Species '{species.Id}' to body '{ActorBodyId}'.", CreateSnapshot());
                RaiseChanged(result, restoring);
                return result;
            }
            catch (Exception exception)
            {
                ClearBiologicalSources(appliedSpeciesSourceId, appliedClassificationSourceId, restoring);
                speciesDefinitionId = previousSpecies;
                appliedSpeciesSourceId = previousSpeciesSource;
                appliedClassificationSourceId = previousClassificationSource;
                BodyRevision = previousRevision;
                Readiness = previousReadiness;
                if (!string.IsNullOrWhiteSpace(previousSpecies))
                {
                    SpeciesDefinition rollbackSpecies = ResolveSpecies(previousSpecies);
                    if (rollbackSpecies != null && rollbackSpecies.BiologicalClassification != null)
                    {
                        ApplyBiologicalSources(
                            rollbackSpecies.BiologicalClassification.DefaultTraitGrants,
                            rollbackSpecies.BiologicalClassification.DefaultCapabilityGrants,
                            rollbackSpecies.BiologicalClassification.DefaultStatContributions,
                            TraitSourceCategory.BiologicalClassification,
                            CapabilitySourceCategory.BiologicalClassification,
                            CalculatedStatContributionSourceCategory.BiologicalClassification,
                            previousClassificationSource,
                            restoring);
                        ApplyBiologicalSources(
                            rollbackSpecies.DefaultBodyTraits,
                            rollbackSpecies.DefaultBooleanCapabilities.Concat(rollbackSpecies.DefaultNumericCapabilities).ToArray(),
                            rollbackSpecies.CalculatedStatContributions,
                            TraitSourceCategory.Species,
                            CapabilitySourceCategory.Species,
                            CalculatedStatContributionSourceCategory.Species,
                            previousSpeciesSource,
                            restoring);
                    }
                }

                return BodyOperationResult.Failure(BodyOperationResultCode.ContributionApplicationFailure, $"Species assignment rolled back: {exception.Message}", snapshot: CreateSnapshot());
            }
        }

        public BodySaveData CreateSaveData()
        {
            return new BodySaveData
            {
                schemaVersion = BodySaveData.CurrentSchemaVersion,
                actorBodyId = ActorBodyId,
                personId = PersonId,
                speciesDefinitionId = SpeciesDefinitionId,
                bodyRevision = BodyRevision
            };
        }

        public BodyOperationResult RestoreFromSaveData(BodySaveData saveData, DefinitionRegistry definitionRegistry, string expectedActorBodyId, string expectedPersonId, bool restoring)
        {
            if (!ValidateSaveData(saveData, definitionRegistry, expectedActorBodyId, expectedPersonId, out string failureReason))
            {
                return BodyOperationResult.Failure(BodyOperationResultCode.RestoreResolutionFailure, failureReason);
            }

            registry = definitionRegistry ?? registry;
            Readiness = BodyReadinessState.Restoring;
            BodyOperationResult result;
            using (SuppressEvents())
            {
                result = AssignSpecies(saveData.speciesDefinitionId, restoring: true, "Body restore");
            }

            if (!result.Succeeded)
            {
                return result;
            }

            BodyRevision = Math.Max(1L, saveData.bodyRevision);
            Readiness = BodyReadinessState.Ready;
            return BodyOperationResult.Success("Body restored.", CreateSnapshot());
        }

        public static bool ValidateSaveData(BodySaveData saveData, DefinitionRegistry registry, string expectedActorBodyId, string expectedPersonId, out string failureReason)
        {
            failureReason = string.Empty;
            if (saveData == null)
            {
                failureReason = "Body save data is missing.";
                return false;
            }

            if (saveData.schemaVersion != BodySaveData.CurrentSchemaVersion)
            {
                failureReason = $"Unsupported body schema version {saveData.schemaVersion}.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(saveData.actorBodyId))
            {
                failureReason = "Body save data is missing an Actor/body ID.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(expectedActorBodyId) && !string.Equals(saveData.actorBodyId, expectedActorBodyId, StringComparison.Ordinal))
            {
                failureReason = $"Saved body actor '{saveData.actorBodyId}' does not match current actor '{expectedActorBodyId}'.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(expectedPersonId) && !string.IsNullOrWhiteSpace(saveData.personId) && !string.Equals(saveData.personId, expectedPersonId, StringComparison.Ordinal))
            {
                failureReason = $"Saved body person '{saveData.personId}' does not match current person '{expectedPersonId}'.";
                return false;
            }

            if (registry == null || !registry.TryGet(saveData.speciesDefinitionId, out SpeciesDefinition species) || species == null)
            {
                failureReason = $"Saved body references unknown Species '{saveData.speciesDefinitionId}'.";
                return false;
            }

            if (species.BiologicalClassification == null || species.BodyForm == null)
            {
                failureReason = $"Saved body Species '{species.Id}' has incomplete classification or body form.";
                return false;
            }

            return true;
        }

        public BodySnapshot CreateSnapshot()
        {
            SpeciesDefinition species = Species;
            BiologicalClassificationDefinition classification = species == null ? null : species.BiologicalClassification;
            BodyFormDefinition bodyForm = species == null ? null : species.BodyForm;
            List<string> diagnostics = new List<string>();
            bool coherent = ValidateBody(out string validationFailure);
            if (!string.IsNullOrWhiteSpace(validationFailure))
            {
                diagnostics.Add(validationFailure);
            }

            List<string> tagIds = species == null
                ? new List<string>()
                : species.Tags.Where(tag => tag != null).Select(tag => tag.Id).OrderBy(id => id, StringComparer.Ordinal).ToList();

            List<BodyCapabilitySummary> capabilitySummaries = BuildCapabilitySummaries();
            List<BodyTraitSummary> traitSummaries = BuildTraitSummaries(species);
            List<BodyStatContributionSummary> statSummaries = BuildStatSummaries(species, classification);

            return new BodySnapshot(
                ActorBodyId,
                PersonId,
                species == null ? string.Empty : species.Id,
                species == null ? string.Empty : species.DisplayName,
                classification == null ? string.Empty : classification.Id,
                bodyForm == null ? string.Empty : bodyForm.Id,
                Readiness,
                BodyRevision,
                tagIds,
                capabilitySummaries,
                traitSummaries,
                statSummaries,
                species == null || species.DefaultDefeatPolicy == null ? string.Empty : species.DefaultDefeatPolicy.Id,
                EvaluateBoolean(BiologyCapabilityIds.RequiresBreathing),
                EvaluateBoolean(BiologyCapabilityIds.HasBlood),
                EvaluateBoolean(ActorLifecycleCapabilityIds.CanBecomeUnconscious),
                EvaluateBoolean(ActorLifecycleCapabilityIds.CanDie),
                EvaluateBoolean(ActorLifecycleCapabilityIds.CanBeRevived),
                EvaluateBoolean(BiologyCapabilityIds.AcceptsBiologicalHealing),
                EvaluateBoolean(BiologyCapabilityIds.AcceptsRepair),
                EvaluateBoolean(BiologyCapabilityIds.HasPhysicalBody),
                coherent,
                diagnostics);
        }

        public bool ValidateBody(out string failureReason)
        {
            failureReason = string.Empty;
            if (string.IsNullOrWhiteSpace(ActorBodyId))
            {
                Readiness = BodyReadinessState.Invalid;
                failureReason = "Body runtime is missing an exact Actor/body ID.";
                return false;
            }

            if (registry == null)
            {
                Readiness = BodyReadinessState.Invalid;
                failureReason = "Body runtime is missing a Definition registry.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(SpeciesDefinitionId))
            {
                Readiness = BodyReadinessState.Invalid;
                failureReason = "Body runtime is missing a Species assignment.";
                return false;
            }

            SpeciesDefinition species = ResolveSpecies(SpeciesDefinitionId);
            if (species == null)
            {
                Readiness = BodyReadinessState.Invalid;
                failureReason = $"Species '{SpeciesDefinitionId}' could not be resolved.";
                return false;
            }

            if (species.BiologicalClassification == null)
            {
                Readiness = BodyReadinessState.Invalid;
                failureReason = $"Species '{species.Id}' is missing a biological classification.";
                return false;
            }

            if (species.BodyForm == null)
            {
                Readiness = BodyReadinessState.Invalid;
                failureReason = $"Species '{species.Id}' is missing a body form.";
                return false;
            }

            if (Readiness != BodyReadinessState.Restoring && Readiness != BodyReadinessState.ApplyingBiologicalContributions)
            {
                Readiness = BodyReadinessState.Ready;
            }

            return true;
        }

        private BodyOperationResult ResolveAssignment(string requestedSpeciesId, bool preview, out SpeciesDefinition species, out BiologicalClassificationDefinition classification, out BodyFormDefinition bodyForm)
        {
            species = null;
            classification = null;
            bodyForm = null;

            if (string.IsNullOrWhiteSpace(ActorBodyId))
            {
                return BodyOperationResult.Failure(BodyOperationResultCode.MissingActorBody, "Body assignment requires an exact Actor/body ID.", preview);
            }

            if (registry == null)
            {
                return BodyOperationResult.Failure(BodyOperationResultCode.RuntimeNotReady, "Body assignment requires a Definition registry.", preview);
            }

            if (string.IsNullOrWhiteSpace(requestedSpeciesId) || !registry.TryGet(requestedSpeciesId, out species) || species == null)
            {
                return BodyOperationResult.Failure(BodyOperationResultCode.MissingSpecies, $"Species '{requestedSpeciesId}' is not configured.", preview);
            }

            classification = species.BiologicalClassification;
            if (classification == null)
            {
                return BodyOperationResult.Failure(BodyOperationResultCode.MissingClassification, $"Species '{species.Id}' is missing a biological classification.", preview);
            }

            bodyForm = species.BodyForm;
            if (bodyForm == null)
            {
                return BodyOperationResult.Failure(BodyOperationResultCode.MissingBodyForm, $"Species '{species.Id}' is missing a body form.", preview);
            }

            if (preview)
            {
                return BodyOperationResult.Success($"Preview resolved Species '{species.Id}' for body '{ActorBodyId}'.", CreateSnapshot(), preview: true);
            }

            return BodyOperationResult.Success("Assignment resolved.", null);
        }

        private BodyOperationResult ApplyBiologicalSources(
            IReadOnlyList<BiologicalTraitGrantDefinition> traitGrants,
            IReadOnlyList<BiologicalCapabilityGrantDefinition> capabilityGrants,
            IReadOnlyList<BiologicalStatContributionDefinition> statContributions,
            TraitSourceCategory traitSourceCategory,
            CapabilitySourceCategory capabilitySourceCategory,
            CalculatedStatContributionSourceCategory statSourceCategory,
            string sourceId,
            bool restoring)
        {
            if (traits == null)
            {
                return BodyOperationResult.Failure(BodyOperationResultCode.TraitGrantFailure, "Body assignment requires CharacterTraitCollection.");
            }

            if (calculatedStats == null)
            {
                return BodyOperationResult.Failure(BodyOperationResultCode.CalculatedStatContributionFailure, "Body assignment requires CalculatedStatCollection.");
            }

            foreach (BiologicalTraitGrantDefinition grant in traitGrants ?? Array.Empty<BiologicalTraitGrantDefinition>())
            {
                if (grant == null || !grant.AlphaEnabled)
                {
                    continue;
                }

                if (grant.Trait == null)
                {
                    return BodyOperationResult.Failure(BodyOperationResultCode.TraitGrantFailure, "Biological Trait grant is missing a Trait definition.");
                }

                TraitOperationResult traitResult = traits.GrantTrait(new TraitGrantRequest
                {
                    OwnerId = PersonId,
                    TraitDefinitionId = grant.Trait.Id,
                    RequestedLifecycle = grant.Lifecycle,
                    RequestedDiscovery = grant.Discovery,
                    SourceCategory = traitSourceCategory,
                    SourceId = sourceId,
                    Reason = "Biological body source",
                    Restoration = restoring,
                    Authority = "body-runtime",
                    RevokeOnSourceRemoval = true
                });

                if (!traitResult.Succeeded && traitResult.Code != "TraitConflict")
                {
                    return BodyOperationResult.Failure(BodyOperationResultCode.TraitGrantFailure, traitResult.Message);
                }
            }

            foreach (BiologicalCapabilityGrantDefinition grant in capabilityGrants ?? Array.Empty<BiologicalCapabilityGrantDefinition>())
            {
                if (grant == null || !grant.AlphaEnabled)
                {
                    continue;
                }

                if (grant.Capability == null)
                {
                    return BodyOperationResult.Failure(BodyOperationResultCode.CapabilityApplicationFailure, "Biological Capability grant is missing a Capability definition.");
                }

                if (string.IsNullOrWhiteSpace(grant.RuntimeCapabilityKey))
                {
                    return BodyOperationResult.Failure(BodyOperationResultCode.CapabilityApplicationFailure, $"Biological Capability grant '{grant.EntryId}' is missing a runtime Capability key.");
                }

                bool added = traits.Capabilities.Add(new RuntimeCapabilityContribution
                {
                    capabilityId = grant.RuntimeCapabilityKey,
                    valueType = (int)grant.Capability.ValueType,
                    boolValue = grant.BooleanValue,
                    numericValue = grant.NumericValue,
                    aggregationPolicy = (int)grant.Capability.AggregationPolicy,
                    sourceCategory = (int)capabilitySourceCategory,
                    sourceId = sourceId,
                    entryId = grant.EntryId,
                    priority = grant.Priority,
                    blocker = grant.Blocker
                });
                if (!added)
                {
                    return BodyOperationResult.Failure(BodyOperationResultCode.CapabilityApplicationFailure, $"Capability '{grant.Capability.Id}' could not be applied.");
                }
            }

            BiologicalStatContributionDefinition[] enabledStatContributions = (statContributions ?? Array.Empty<BiologicalStatContributionDefinition>())
                .Where(contribution => contribution != null && contribution.AlphaEnabled)
                .ToArray();
            if (enabledStatContributions.Length > 0)
            {
                calculatedStats.RemoveContributionsFromSource(statSourceCategory, sourceId, restoring);
            }

            BodyOperationResult statApplyResult = ApplyStatContributions(enabledStatContributions, statSourceCategory, sourceId, restoring);
            if (!statApplyResult.Succeeded)
            {
                calculatedStats.RemoveContributionsFromSource(statSourceCategory, sourceId, restoring);
                statApplyResult = ApplyStatContributions(enabledStatContributions, statSourceCategory, sourceId, restoring);
                if (!statApplyResult.Succeeded)
                {
                    return statApplyResult;
                }
            }

            return BodyOperationResult.Success("Biological sources applied.", null);
        }

        private BodyOperationResult ApplyStatContributions(
            IReadOnlyList<BiologicalStatContributionDefinition> statContributions,
            CalculatedStatContributionSourceCategory statSourceCategory,
            string sourceId,
            bool restoring)
        {
            foreach (BiologicalStatContributionDefinition contribution in statContributions ?? Array.Empty<BiologicalStatContributionDefinition>())
            {
                if (contribution.CalculatedStat == null)
                {
                    return BodyOperationResult.Failure(BodyOperationResultCode.CalculatedStatContributionFailure, "Biological stat contribution is missing a Calculated Stat definition.");
                }

                bool added = calculatedStats.AddContribution(new RuntimeCalculatedStatContribution
                {
                    contributionId = contribution.ContributionId,
                    statId = contribution.CalculatedStat.Id,
                    sourceId = sourceId,
                    sourceCategory = (int)statSourceCategory,
                    kind = (int)contribution.Kind,
                    direction = (int)contribution.Direction,
                    magnitude = contribution.Magnitude,
                    priority = contribution.Priority
                }, out string failureReason, restoring);

                if (!added)
                {
                    return BodyOperationResult.Failure(BodyOperationResultCode.CalculatedStatContributionFailure, failureReason);
                }
            }

            return BodyOperationResult.Success("Biological stat sources applied.", null);
        }

        private void ClearBiologicalSources(string speciesSourceId, string classificationSourceId, bool restoring)
        {
            if (traits != null)
            {
                if (!string.IsNullOrWhiteSpace(speciesSourceId))
                {
                    ClearTraitSources(TraitSourceCategory.Species, speciesSourceId);
                    traits.Capabilities.ClearSource(CapabilitySourceCategory.Species, speciesSourceId);
                }

                if (!string.IsNullOrWhiteSpace(classificationSourceId))
                {
                    ClearTraitSources(TraitSourceCategory.BiologicalClassification, classificationSourceId);
                    traits.Capabilities.ClearSource(CapabilitySourceCategory.BiologicalClassification, classificationSourceId);
                }
            }

            calculatedStats?.RemoveContributionsFromSource(CalculatedStatContributionSourceCategory.Species, speciesSourceId, restoring);
            calculatedStats?.RemoveContributionsFromSource(CalculatedStatContributionSourceCategory.BiologicalClassification, classificationSourceId, restoring);
        }

        private void ClearTraitSources(TraitSourceCategory category, string sourceId)
        {
            if (traits == null || string.IsNullOrWhiteSpace(sourceId))
            {
                return;
            }

            foreach (TraitSnapshot snapshot in traits.GetDevelopmentSnapshot())
            {
                if (snapshot?.Definition == null || snapshot.Record?.sourceRecords == null)
                {
                    continue;
                }

                if (snapshot.Record.sourceRecords.Any(source => source.sourceCategory == (int)category && string.Equals(source.sourceId, sourceId, StringComparison.Ordinal)))
                {
                    traits.RemoveTraitSource(snapshot.Definition.Id, category, sourceId, TraitFinalSourcePolicy.Remove);
                }
            }
        }

        private void ReapplyCurrentBiologicalSources(bool restoring)
        {
            SpeciesDefinition species = ResolveSpecies(SpeciesDefinitionId);
            if (species == null || species.BiologicalClassification == null)
            {
                return;
            }

            string speciesSourceId = SpeciesSourceId(ActorBodyId, species.Id);
            string classificationSourceId = ClassificationSourceId(ActorBodyId, species.BiologicalClassification.Id);
            ClearBiologicalSources(speciesSourceId, classificationSourceId, restoring);
            ApplyBiologicalSources(
                species.BiologicalClassification.DefaultTraitGrants,
                species.BiologicalClassification.DefaultCapabilityGrants,
                species.BiologicalClassification.DefaultStatContributions,
                TraitSourceCategory.BiologicalClassification,
                CapabilitySourceCategory.BiologicalClassification,
                CalculatedStatContributionSourceCategory.BiologicalClassification,
                classificationSourceId,
                restoring);
            ApplyBiologicalSources(
                species.DefaultBodyTraits,
                species.DefaultBooleanCapabilities.Concat(species.DefaultNumericCapabilities).ToArray(),
                species.CalculatedStatContributions,
                TraitSourceCategory.Species,
                CapabilitySourceCategory.Species,
                CalculatedStatContributionSourceCategory.Species,
                speciesSourceId,
                restoring);
            appliedSpeciesSourceId = speciesSourceId;
            appliedClassificationSourceId = classificationSourceId;
        }

        private SpeciesDefinition ResolveSpecies(string speciesId)
        {
            if (string.IsNullOrWhiteSpace(speciesId) || registry == null)
            {
                return null;
            }

            return registry.TryGet(speciesId, out SpeciesDefinition species) ? species : null;
        }

        private bool EvaluateBoolean(string capabilityId)
        {
            return traits != null && traits.Capabilities.Evaluate(capabilityId).BooleanValue;
        }

        private List<BodyCapabilitySummary> BuildCapabilitySummaries()
        {
            if (traits == null)
            {
                return new List<BodyCapabilitySummary>();
            }

            string[] ids =
            {
                BiologyCapabilityIds.IsLiving,
                BiologyCapabilityIds.IsUndead,
                BiologyCapabilityIds.IsConstruct,
                BiologyCapabilityIds.IsSpirit,
                BiologyCapabilityIds.HasBlood,
                BiologyCapabilityIds.RequiresBreathing,
                BiologyCapabilityIds.CanBePoisoned,
                BiologyCapabilityIds.CanContractDisease,
                BiologyCapabilityIds.AcceptsBiologicalHealing,
                BiologyCapabilityIds.AcceptsRepair,
                BiologyCapabilityIds.IsCorporeal,
                BiologyCapabilityIds.HasPhysicalBody,
                ActorLifecycleCapabilityIds.CanBecomeUnconscious,
                ActorLifecycleCapabilityIds.CanDie,
                ActorLifecycleCapabilityIds.CanBeRevived,
                BiologyCapabilityIds.CanBreathe,
                BiologyCapabilityIds.CanBleed
            };

            return ids.Select(id => traits.Capabilities.Evaluate(id))
                .Select(snapshot => new BodyCapabilitySummary(snapshot.CapabilityId, snapshot.BooleanValue, snapshot.NumericValue, snapshot.Blocked))
                .ToList();
        }

        private static List<BodyTraitSummary> BuildTraitSummaries(SpeciesDefinition species)
        {
            return species == null
                ? new List<BodyTraitSummary>()
                : species.DefaultBodyTraits
                    .Where(grant => grant?.Trait != null && grant.AlphaEnabled)
                    .Select(grant => new BodyTraitSummary(grant.Trait.Id, grant.Trait.DisplayName))
                    .ToList();
        }

        private static List<BodyStatContributionSummary> BuildStatSummaries(SpeciesDefinition species, BiologicalClassificationDefinition classification)
        {
            List<BodyStatContributionSummary> summaries = new List<BodyStatContributionSummary>();
            if (classification != null)
            {
                summaries.AddRange(classification.DefaultStatContributions
                    .Where(contribution => contribution?.CalculatedStat != null && contribution.AlphaEnabled)
                    .Select(contribution => new BodyStatContributionSummary(contribution.CalculatedStat.Id, classification.Id, contribution.Magnitude, contribution.Direction.ToString())));
            }

            if (species != null)
            {
                summaries.AddRange(species.CalculatedStatContributions
                    .Where(contribution => contribution?.CalculatedStat != null && contribution.AlphaEnabled)
                    .Select(contribution => new BodyStatContributionSummary(contribution.CalculatedStat.Id, species.Id, contribution.Magnitude, contribution.Direction.ToString())));
            }

            return summaries;
        }

        private void RaiseChanged(BodyOperationResult result, bool restoring)
        {
            if (!suppressEvents)
            {
                BodyChanged?.Invoke(this, result, restoring);
            }
        }

        private IDisposable SuppressEvents()
        {
            suppressEvents = true;
            return new Suppression(this);
        }

        private static string SpeciesSourceId(string actorBodyId, string speciesId)
        {
            return $"body.species.{actorBodyId}.{speciesId}";
        }

        private static string ClassificationSourceId(string actorBodyId, string classificationId)
        {
            return $"body.classification.{actorBodyId}.{classificationId}";
        }

        private sealed class Suppression : IDisposable
        {
            private readonly ActorBodyRuntime owner;

            public Suppression(ActorBodyRuntime owner)
            {
                this.owner = owner;
            }

            public void Dispose()
            {
                if (owner != null)
                {
                    owner.suppressEvents = false;
                }
            }
        }
    }
}
