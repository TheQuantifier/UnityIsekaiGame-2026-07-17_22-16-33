#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityIsekaiGame.Abilities;
using UnityIsekaiGame.Combat;
using UnityIsekaiGame.Contracts;
using UnityIsekaiGame.Equipment;
using UnityIsekaiGame.Factions;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.GameData.Persistence;
using UnityIsekaiGame.Gameplay;
using UnityIsekaiGame.Inventory;
using UnityIsekaiGame.Magic;
using UnityIsekaiGame.People;
using UnityIsekaiGame.Places;
using UnityIsekaiGame.Progression;
using UnityIsekaiGame.Persistence;
using UnityIsekaiGame.Quests;
using UnityIsekaiGame.ResourceSystem;
using UnityIsekaiGame.Requirements;
using UnityIsekaiGame.Skills;
using UnityIsekaiGame.StatusEffects;
using UnityIsekaiGame.Stats;
using UnityIsekaiGame.Traits;
using UnityIsekaiGame.WorldEntities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityIsekaiGame.Development
{
    public sealed class PrototypeTestLabService
    {
        public const int DefaultHistoryLimit = 40;
        private const string PrototypeCatalogPath = "Assets/GameData/Prototype/PrototypeDefinitionCatalog.asset";
        private const string DevelopmentStatusSource = "development.prototype-test-lab";

        private readonly List<PrototypeTestLabOperation> history = new List<PrototypeTestLabOperation>();
        private readonly HashSet<string> pendingConfirmations = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<Type, List<IGameDefinition>> selectorCache = new Dictionary<Type, List<IGameDefinition>>();
        private PrototypeTestLabContext context;
        private DefinitionRegistry registry;
        private int historyLimit = DefaultHistoryLimit;
        private string lastSpawnedWorldEntityId;
        private ItemDefinition lastSpawnedWorldEntityItem;
        private string lastDestroyedWorldEntityId;
        private ItemDefinition lastDestroyedWorldEntityItem;
        private string lastWorldEntityOperationMessage;

        public event Action HistoryChanged;

        public IReadOnlyList<PrototypeTestLabOperation> History => history;
        public DefinitionRegistry Registry => registry;
        public string CurrentSlotId => context?.Persistence == null ? PersistenceService.PrototypeSlotId : context.Persistence.PrototypeSlotId;

        public void Configure(PrototypeTestLabContext newContext)
        {
            context = newContext;
            registry = CreateRegistry(context?.DefinitionCatalog);
            context?.IdentityProgression?.RegisterDefinitionCache(registry);
            if (EnsureResources(out CharacterResourceCollection resources))
            {
                resources.Configure(registry, context.PlayerCalculatedStats, PersistenceService.LocalPlayerId);
            }

            context?.PlayerSkills?.Configure(registry, context.PlayerCalculatedStats, context.SpellLoadout);
            if (EnsureTraits(out CharacterTraitCollection traits))
            {
                traits.Configure(registry, context.PlayerCalculatedStats, context.PlayerSkills, PersistenceService.LocalPlayerId);
            }
            selectorCache.Clear();
        }

        public IReadOnlyList<TDefinition> GetDefinitions<TDefinition>()
            where TDefinition : class, IGameDefinition
        {
            Type type = typeof(TDefinition);
            if (!selectorCache.TryGetValue(type, out List<IGameDefinition> cached))
            {
                cached = registry == null
                    ? new List<IGameDefinition>()
                    : registry.DefinitionsById.Values
                        .Where(definition => definition is TDefinition)
                        .OrderBy(definition => definition.DisplayName)
                        .ThenBy(definition => definition.Id)
                        .ToList();
                selectorCache.Add(type, cached);
            }

            return cached.Cast<TDefinition>().ToList();
        }

        public IReadOnlyList<PrototypeTestPoint> GetTestPoints()
        {
            return UnityEngine.Object.FindObjectsByType<PrototypeTestPoint>(FindObjectsInactive.Exclude)
                .Where(point => point != null && !string.IsNullOrWhiteSpace(point.TestPointId))
                .OrderBy(point => point.TestPointId)
                .ThenBy(point => point.DisplayName)
                .ToList();
        }

        public string BuildOverview()
        {
            if (context == null)
            {
                return "Test Lab context is missing.";
            }

            return string.Join(Environment.NewLine, new[]
            {
                "Prototype Systems Test Lab",
                $"Build Boundary: {(Application.isEditor ? "Editor" : "Development Build")}",
                $"Player: {(context.PlayerTransform == null ? "Missing" : context.PlayerTransform.name)}",
                $"Health: {FormatHealth()}",
                $"Stamina: {FormatResource(context.PlayerStamina == null ? 0f : context.PlayerStamina.CurrentStamina, context.PlayerStamina == null ? 0f : context.PlayerStamina.MaximumStamina)}",
                $"Mana: {FormatResource(context.PlayerMana == null ? 0f : context.PlayerMana.CurrentMana, context.PlayerMana == null ? 0f : context.PlayerMana.MaximumMana)}",
                $"Stats: ATK {FormatNumber(context.PlayerStats == null ? 0f : context.PlayerStats.AttackPower)}, DEF {FormatNumber(context.PlayerStats == null ? 0f : context.PlayerStats.Defense)}",
                $"Base Attributes: {(context.PlayerAttributes == null ? "Missing" : context.PlayerAttributes.AttributeValues.Count.ToString())}",
                $"Skills: {(context.PlayerSkills == null ? "Missing" : context.PlayerSkills.LearnedSkills.Count.ToString())}",
                $"Statuses: {FormatStatuses(context.PlayerStatuses)}",
                $"Inventory: {FormatInventory()}",
                $"Equipped: {CountEquipped()} item(s)",
                $"Selected Spell: {(context.SpellLoadout == null || context.SpellLoadout.SelectedSpell == null ? "None" : FormatDefinition(context.SpellLoadout.SelectedSpell))}",
                $"Quests: {(context.QuestLog == null ? 0 : context.QuestLog.Quests.Count)}",
                $"Contracts: {(context.ContractJournal == null ? 0 : context.ContractJournal.Contracts.Count)}",
                $"Identity: {FormatIdentityOneLine()}",
                $"Enemy: {FormatEnemy()}",
                $"Location: {FormatLocationOneLine()}",
                $"Definitions: {(registry == null ? 0 : registry.Count)}",
                $"Persistence Slot: {CurrentSlotId}",
                $"Modal Active: {PrototypeGameplayModalState.IsModalActive}"
            });
        }

        public string BuildIdentityProgressionSummary()
        {
            if (context?.IdentityProgression == null)
            {
                return "Player identity/progression component is missing.";
            }

            context.IdentityProgression.RegisterDefinitionCache(registry);
            return context.IdentityProgression.BuildDiagnosticSummary();
        }

        public string BuildAttributeCalculatedStatsSummary()
        {
            if (context?.PlayerAttributes == null || context.PlayerCalculatedStats == null)
            {
                return "Player Base Attributes or Calculated Stats component is missing.";
            }

            return string.Join(Environment.NewLine, new[]
            {
                context.PlayerAttributes.BuildDiagnosticSummary(),
                string.Empty,
                context.PlayerCalculatedStats.BuildDiagnosticSummary()
            });
        }

        public string BuildSkillsSummary(bool includeHidden)
        {
            if (context?.PlayerSkills == null)
            {
                return "Player Skill collection component is missing.";
            }

            context.PlayerSkills.Configure(registry, context.PlayerCalculatedStats, context.SpellLoadout);
            return context.PlayerSkills.BuildDiagnosticSummary(includeHidden);
        }

        public string BuildTraitsSummary(bool includeHidden)
        {
            if (!EnsureTraits(out CharacterTraitCollection traits))
            {
                return "Player Trait collection component is missing.";
            }

            return traits.BuildDiagnosticSummary(includeHidden);
        }

        public PrototypeTestLabOperation GrantTrait(TraitDefinition trait, TraitLifecycleState lifecycle, TraitDiscoveryState discovery)
        {
            if (!EnsureTraits(out CharacterTraitCollection traits))
            {
                return RecordFailure("Grant Trait", "Player Trait collection component is missing.", "MissingTraits");
            }

            if (trait == null)
            {
                return RecordFailure("Grant Trait", "Trait definition is missing.", "MissingTrait");
            }

            TraitOperationResult result = traits.GrantTrait(new TraitGrantRequest
            {
                OwnerId = PersistenceService.LocalPlayerId,
                TraitDefinitionId = trait.Id,
                RequestedLifecycle = lifecycle,
                RequestedDiscovery = discovery,
                SourceCategory = TraitSourceCategory.Development,
                SourceId = "test-lab",
                Reason = "Prototype Test Lab"
            });
            return Record(result.Succeeded, $"Grant Trait {lifecycle}", result.Code, result.Message);
        }

        public PrototypeTestLabOperation GrantTraitDuplicateProof(TraitDefinition trait)
        {
            if (trait == null)
            {
                return RecordFailure("Trait Duplicate Proof", "Trait definition is missing.", "MissingTrait");
            }

            GrantTrait(trait, TraitLifecycleState.Active, TraitDiscoveryState.Discovered);
            return GrantTrait(trait, TraitLifecycleState.Active, TraitDiscoveryState.Discovered);
        }

        public PrototypeTestLabOperation GrantTraitSecondSource(TraitDefinition trait)
        {
            if (!EnsureTraits(out CharacterTraitCollection traits))
            {
                return RecordFailure("Trait Second Source", "Player Trait collection component is missing.", "MissingTraits");
            }

            if (trait == null)
            {
                return RecordFailure("Trait Second Source", "Trait definition is missing.", "MissingTrait");
            }

            TraitOperationResult result = traits.GrantTrait(new TraitGrantRequest
            {
                OwnerId = PersistenceService.LocalPlayerId,
                TraitDefinitionId = trait.Id,
                RequestedLifecycle = TraitLifecycleState.Active,
                RequestedDiscovery = TraitDiscoveryState.Discovered,
                SourceCategory = TraitSourceCategory.Administrative,
                SourceId = "test-lab.second-source",
                Reason = "Prototype Test Lab second source"
            });
            return Record(result.Succeeded, "Trait Second Source", result.Code, result.Message);
        }

        public PrototypeTestLabOperation RemoveTraitTestLabSource(TraitDefinition trait)
        {
            if (!EnsureTraits(out CharacterTraitCollection traits))
            {
                return RecordFailure("Remove Trait Source", "Player Trait collection component is missing.", "MissingTraits");
            }

            if (trait == null)
            {
                return RecordFailure("Remove Trait Source", "Trait definition is missing.", "MissingTrait");
            }

            TraitOperationResult result = traits.RemoveTraitSource(trait.Id, TraitSourceCategory.Development, "test-lab");
            return Record(result.Succeeded, "Remove Trait Source", result.Code, result.Message);
        }

        public PrototypeTestLabOperation SuppressTrait(TraitDefinition trait)
        {
            return ChangeTrait(trait, "Suppress Trait", collection => collection.SuppressTrait(trait.Id));
        }

        public PrototypeTestLabOperation UnsuppressTrait(TraitDefinition trait)
        {
            return ChangeTrait(trait, "Unsuppress Trait", collection => collection.UnsuppressTrait(trait.Id));
        }

        public PrototypeTestLabOperation ActivateTrait(TraitDefinition trait)
        {
            return ChangeTrait(trait, "Activate Trait", collection => collection.ActivateTrait(trait.Id));
        }

        public PrototypeTestLabOperation SetTraitSuspected(TraitDefinition trait)
        {
            return ChangeTrait(trait, "Suspect Trait", collection => collection.SetDiscoveryState(trait.Id, TraitDiscoveryState.Suspected));
        }

        public PrototypeTestLabOperation SetTraitDiscovered(TraitDefinition trait)
        {
            return ChangeTrait(trait, "Discover Trait", collection => collection.SetDiscoveryState(trait.Id, TraitDiscoveryState.Discovered));
        }

        public PrototypeTestLabOperation ReplaceTrait(TraitDefinition replacement)
        {
            if (!EnsureTraits(out CharacterTraitCollection traits))
            {
                return RecordFailure("Replace Trait", "Player Trait collection component is missing.", "MissingTraits");
            }

            if (replacement == null)
            {
                return RecordFailure("Replace Trait", "Trait definition is missing.", "MissingTrait");
            }

            IReadOnlyList<string> blockers = traits.GetDevelopmentSnapshot()
                .Where(snapshot => snapshot.Definition != null
                    && snapshot.Definition.Id != replacement.Id
                    && (snapshot.Definition.ConflictGroupIds.Any(group => replacement.ConflictGroupIds.Contains(group))
                        || snapshot.Definition.IncompatibleTraits.Any(trait => trait != null && trait.Id == replacement.Id)
                        || replacement.IncompatibleTraits.Any(trait => trait != null && trait.Id == snapshot.Definition.Id)))
                .Select(snapshot => snapshot.Definition.Id)
                .ToList();
            TraitOperationResult result = traits.GrantTrait(new TraitGrantRequest
            {
                OwnerId = PersistenceService.LocalPlayerId,
                TraitDefinitionId = replacement.Id,
                RequestedLifecycle = TraitLifecycleState.Active,
                RequestedDiscovery = TraitDiscoveryState.Discovered,
                SourceCategory = TraitSourceCategory.Development,
                SourceId = "test-lab.replace",
                Reason = "Prototype Test Lab replacement",
                AllowConflictReplacement = true,
                TraitsAuthorizedForReplacement = blockers
            });
            return Record(result.Succeeded, "Replace Trait", result.Code, result.Message);
        }

        public PrototypeTestLabOperation RebuildTraitEffects()
        {
            if (!EnsureTraits(out CharacterTraitCollection traits))
            {
                return RecordFailure("Rebuild Traits", "Player Trait collection component is missing.", "MissingTraits");
            }

            TraitOperationResult result = traits.RebuildTraitEffects();
            return Record(result.Succeeded, "Rebuild Traits", result.Code, result.Message);
        }

        public PrototypeTestLabOperation SnapshotTraitsForPersistence()
        {
            if (!EnsureTraits(out CharacterTraitCollection traits))
            {
                return RecordFailure("Trait Save Snapshot", "Player Trait collection component is missing.", "MissingTraits");
            }

            PlayerTraitsSaveData saveData = traits.CreateSaveData(PersistenceService.LocalPlayerId, context?.IdentityProgression == null ? string.Empty : context.IdentityProgression.PersonId);
            bool valid = CharacterTraitCollection.ValidateSaveData(saveData, registry, PersistenceService.LocalPlayerId, out string failureReason);
            return Record(valid, "Trait Save Snapshot", valid ? "Valid" : "Invalid", valid ? $"Captured {saveData.traits.Count} Trait record(s)." : failureReason);
        }

        public PrototypeTestLabOperation EvaluateRequirement(RequirementSetDefinition requirement)
        {
            if (requirement == null)
            {
                return RecordFailure("Evaluate Requirement", "Requirement Set definition is missing.", "MissingRequirement");
            }

            RequirementEvaluationResult result = CapabilityRequirementEvaluator.Evaluate(requirement, BuildRequirementContext(testLab: true));
            string failures = result.Passed ? "All nodes passed." : string.Join("; ", result.TestLabFailureReasons);
            return Record(result.Passed, "Evaluate Requirement", result.Passed ? "Passed" : "Failed", failures);
        }

        public string BuildCurrentResourcesSummary()
        {
            if (!EnsureResources(out CharacterResourceCollection resources))
            {
                return "Player resource collection is missing.";
            }

            return string.Join(Environment.NewLine, new[]
            {
                resources.BuildDiagnosticSummary(),
                string.Empty,
                $"Wrapper Health: {FormatHealth()}",
                $"Wrapper Mana: {FormatResource(context.PlayerMana == null ? 0f : context.PlayerMana.CurrentMana, context.PlayerMana == null ? 0f : context.PlayerMana.MaximumMana)}",
                $"Wrapper Stamina: {FormatResource(context.PlayerStamina == null ? 0f : context.PlayerStamina.CurrentStamina, context.PlayerStamina == null ? 0f : context.PlayerStamina.MaximumStamina)}"
            });
        }

        public PrototypeTestLabOperation ReconcileResources()
        {
            if (!EnsureResources(out CharacterResourceCollection resources))
            {
                return RecordFailure("Reconcile Resources", "Player resource collection is missing.", "MissingResources");
            }

            int changed = 0;
            foreach (ResourceSnapshot snapshot in resources.GetSnapshots())
            {
                if (resources.ReconcileResource(snapshot.ResourceId))
                {
                    changed++;
                }
            }

            return RecordSuccess("Reconcile Resources", $"Reconciled {resources.GetSnapshots().Count} resource(s); {changed} current value(s) changed.");
        }

        public PrototypeTestLabOperation ProveResourceDuplicateEvent()
        {
            if (!EnsureResources(out CharacterResourceCollection resources))
            {
                return RecordFailure("Resource Duplicate Proof", "Player resource collection is missing.", "MissingResources");
            }

            string eventId = "resource.test-lab.duplicate-proof";
            ResourceChangeResult first = resources.TrySpend(ResourceIds.Mana, 1f, "test-lab", "Duplicate proof", eventId);
            ResourceChangeResult second = resources.TrySpend(ResourceIds.Mana, 1f, "test-lab", "Duplicate proof", eventId);
            bool passed = first.Succeeded && second.Succeeded && second.AppliedAmount <= CharacterResourceCollection.Epsilon;
            return Record(passed, "Resource Duplicate Proof", passed ? "Passed" : "Failed", $"First={first.AppliedAmount:0.###}, Second={second.AppliedAmount:0.###}, Mana={resources.GetCurrent(ResourceIds.Mana):0.###}/{resources.GetMaximum(ResourceIds.Mana):0.###}");
        }

        public PrototypeTestLabOperation TickResourceRegeneration()
        {
            if (!EnsureResources(out CharacterResourceCollection resources))
            {
                return RecordFailure("Resource Regen Tick", "Player resource collection is missing.", "MissingResources");
            }

            resources.TrySpend(ResourceIds.Stamina, Mathf.Max(1f, Mathf.Min(5f, resources.GetMaximum(ResourceIds.Stamina))), "test-lab", "Prepare regeneration tick");
            float before = resources.GetCurrent(ResourceIds.Stamina);
            resources.TickResources(1f, Time.time + 2f);
            float after = resources.GetCurrent(ResourceIds.Stamina);
            return RecordSuccess("Resource Regen Tick", $"Stamina {before:0.###} -> {after:0.###}.");
        }

        public PrototypeTestLabOperation SnapshotResourcesForPersistence()
        {
            if (!EnsureResources(out CharacterResourceCollection resources))
            {
                return RecordFailure("Resource Save Snapshot", "Player resource collection is missing.", "MissingResources");
            }

            PlayerResourcesSaveData saveData = resources.CreateSaveData(PersistenceService.LocalPlayerId, context?.IdentityProgression == null ? string.Empty : context.IdentityProgression.PersonId);
            bool valid = CharacterResourceCollection.ValidateSaveData(saveData, registry, context?.PlayerCalculatedStats, PersistenceService.LocalPlayerId, out string failureReason);
            return Record(valid, "Resource Save Snapshot", valid ? "Valid" : "Invalid", valid ? $"Captured {saveData.resources.Count} resource record(s)." : failureReason);
        }

        public PrototypeTestLabOperation SimulateSkillAction(SkillDefinition skill, bool executed, bool succeeded, string eventId = "")
        {
            if (!EnsureSkills(out CharacterSkillCollection skills))
            {
                return RecordFailure("Skill Action", "Player Skill collection component is missing.", "MissingSkills");
            }

            if (skill == null)
            {
                return RecordFailure("Skill Action", "Skill definition is missing.", "MissingSkill");
            }

            SkillActionExecutionEvent actionEvent = SkillActionExecutionEvent.Development(
                string.IsNullOrWhiteSpace(eventId) ? $"skill-action.test-lab.{Guid.NewGuid():N}" : eventId,
                skill.NaturalLearning == null ? SkillActionEventCategory.Development : skill.NaturalLearning.ActionCategory,
                skill.NaturalLearning == null ? skill.Id : skill.NaturalLearning.QualifyingEventId,
                executed,
                succeeded);
            SkillOperationResult result = skills.RecordQualifyingAction(actionEvent);
            return Record(result.Succeeded, executed ? succeeded ? "Skill Valid Action" : "Skill Missed Action" : "Skill Blocked Action", result.Code, result.Message);
        }

        public PrototypeTestLabOperation SimulateManySkillActions(SkillDefinition skill, int count)
        {
            if (!EnsureSkills(out CharacterSkillCollection skills))
            {
                return RecordFailure("Skill Multi Action", "Player Skill collection component is missing.", "MissingSkills");
            }

            if (skill == null)
            {
                return RecordFailure("Skill Multi Action", "Skill definition is missing.", "MissingSkill");
            }

            int amount = Mathf.Max(1, count);
            for (int i = 0; i < amount; i++)
            {
                SkillActionExecutionEvent actionEvent = SkillActionExecutionEvent.Development(
                    $"skill-action.test-lab.{Guid.NewGuid():N}",
                    skill.NaturalLearning == null ? SkillActionEventCategory.Development : skill.NaturalLearning.ActionCategory,
                    skill.NaturalLearning == null ? skill.Id : skill.NaturalLearning.QualifyingEventId,
                    executed: true,
                    succeeded: true);
                skills.RecordQualifyingAction(actionEvent);
            }

            return RecordSuccess("Skill Multi Action", $"Simulated {amount} qualifying action(s) for {skill.DisplayName}.");
        }

        public PrototypeTestLabOperation TestDuplicateSkillAction(SkillDefinition skill)
        {
            string eventId = $"skill-action.test-lab.duplicate.{Guid.NewGuid():N}";
            SimulateSkillAction(skill, executed: true, succeeded: true, eventId);
            return SimulateSkillAction(skill, executed: true, succeeded: true, eventId);
        }

        public PrototypeTestLabOperation GrantSkill(SkillDefinition skill, SkillGrade grade)
        {
            if (!EnsureSkills(out CharacterSkillCollection skills))
            {
                return RecordFailure("Grant Skill", "Player Skill collection component is missing.", "MissingSkills");
            }

            if (skill == null)
            {
                return RecordFailure("Grant Skill", "Skill definition is missing.", "MissingSkill");
            }

            SkillOperationResult result = skills.GrantSkill(skill, grade, SkillAcquisitionSource.Development, "Prototype Test Lab", "test-lab");
            return Record(result.Succeeded, $"Grant Skill {grade}", result.Code, result.Message);
        }

        public PrototypeTestLabOperation AwardSkillXp(SkillDefinition skill, int amount)
        {
            if (!EnsureSkills(out CharacterSkillCollection skills))
            {
                return RecordFailure("Award Skill XP", "Player Skill collection component is missing.", "MissingSkills");
            }

            if (skill == null)
            {
                return RecordFailure("Award Skill XP", "Skill definition is missing.", "MissingSkill");
            }

            SkillOperationResult result = skills.AwardSkillUse(skill.Id, amount: Mathf.Max(1, amount));
            return Record(result.Succeeded, "Award Skill XP", result.Code, result.Message);
        }

        public PrototypeTestLabOperation RebuildSkillEffects()
        {
            if (!EnsureSkills(out CharacterSkillCollection skills))
            {
                return RecordFailure("Rebuild Skills", "Player Skill collection component is missing.", "MissingSkills");
            }

            SkillOperationResult result = skills.RebuildSkillEffects();
            return Record(result.Succeeded, "Rebuild Skills", result.Code, result.Message);
        }

        public PrototypeTestLabOperation ClearSkillDevelopmentState(bool confirmed)
        {
            if (!RequireConfirmation("ClearSkillDevelopmentState", confirmed, out PrototypeTestLabOperation confirmation))
            {
                return confirmation;
            }

            if (!EnsureSkills(out CharacterSkillCollection skills))
            {
                return RecordFailure("Clear Skills", "Player Skill collection component is missing.", "MissingSkills");
            }

            SkillOperationResult result = skills.ClearDevelopmentState(confirmed: true);
            return Record(result.Succeeded, "Clear Skills", result.Code, result.Message);
        }

        public string BuildLocationSummary()
        {
            string details = context?.Persistence == null
                ? "Player location persistence is missing."
                : context.Persistence.BuildPlayerLocationDiagnosticSummary();
            return string.Join(Environment.NewLine, new[]
            {
                "Player Location Persistence",
                details,
                "Policy: same-scene restore is supported; cross-scene saves validate clearly and are not restored yet.",
                "Reach Location objectives are suppressed during persistence restore."
            });
        }

        public string BuildWorldEntitySummary()
        {
            return string.Join(Environment.NewLine, new[]
            {
                "Persistent World Entities",
                WorldEntityRegistry.BuildDiagnosticReport(),
                $"Last Spawned: {(string.IsNullOrWhiteSpace(lastSpawnedWorldEntityId) ? "None" : lastSpawnedWorldEntityId)}",
                $"Last Destroyed: {(string.IsNullOrWhiteSpace(lastDestroyedWorldEntityId) ? "None" : lastDestroyedWorldEntityId)}",
                $"Last Result: {(string.IsNullOrWhiteSpace(lastWorldEntityOperationMessage) ? "None" : lastWorldEntityOperationMessage)}"
            });
        }

        public string BuildSaveSlotSummary()
        {
            if (context?.Persistence == null)
            {
                return "Save slot persistence is missing.";
            }

            context.Persistence.EnsureInitialized();
            List<string> lines = new List<string>
            {
                "Save Slots, Autosave, and Load UI",
                context.Persistence.BuildSaveSlotDiagnosticSummary()
            };

            IReadOnlyList<SaveSlotDescriptor> descriptors = context.Persistence.BuildSaveSlotDescriptors();
            for (int i = 0; i < descriptors.Count; i++)
            {
                SaveSlotDescriptor descriptor = descriptors[i];
                lines.Add($"{descriptor.displayName}: {descriptor.compatibilityStatus} | {PrototypeSaveSlotCatalog.FormatLocalTimestamp(descriptor.lastSavedAtUtc)} | {PrototypeSaveSlotCatalog.FormatPlayTime(descriptor.playTimeSeconds)} | Backup={descriptor.backupExists}");
            }

            return string.Join(Environment.NewLine, lines);
        }

        public string BuildPersistenceIntegrationSummary()
        {
            return context?.Persistence == null
                ? "Persistence integration service is missing."
                : context.Persistence.BuildPersistenceIntegrationDiagnosticSummary();
        }

        public PrototypeTestLabOperation GrantItem(ItemDefinition item, int quantity)
        {
            if (context?.Inventory == null)
            {
                return RecordFailure("Grant Item", "Player inventory is missing.", "MissingInventory");
            }

            if (item == null)
            {
                return RecordFailure("Grant Item", "No item definition selected.", "MissingDefinition");
            }

            InventoryAddResult result = context.Inventory.AddItemOrInstances(item, Mathf.Max(1, quantity));
            return Record(result.AddedQuantity > 0, "Grant Item", result.Status.ToString(), $"Requested {result.RequestedQuantity} x {item.DisplayName}; added {result.AddedQuantity}.");
        }

        public PrototypeTestLabOperation GrantStatefulItem(ItemDefinition item)
        {
            if (context?.Inventory == null)
            {
                return RecordFailure("Grant Stateful Item", "Player inventory is missing.", "MissingInventory");
            }

            if (item == null)
            {
                return RecordFailure("Grant Stateful Item", "No item definition selected.", "MissingDefinition");
            }

            ItemInstanceCreationResult creation = ItemInstanceFactory.CreateStateful(item, ItemInstanceMetadata.WithoutInstanceState());
            if (!creation.Succeeded)
            {
                return RecordFailure("Grant Stateful Item", creation.Message, creation.Status.ToString());
            }

            InventoryInstanceOperationResult result = context.Inventory.AddItemInstance(creation.ItemInstance);
            return Record(result.Succeeded, "Grant Stateful Item", result.Succeeded ? "Added" : "Failed", result.Message);
        }

        public PrototypeTestLabOperation RemoveItem(ItemDefinition item, int quantity)
        {
            if (context?.Inventory == null)
            {
                return RecordFailure("Remove Item", "Player inventory is missing.", "MissingInventory");
            }

            if (item == null)
            {
                return RecordFailure("Remove Item", "No item definition selected.", "MissingDefinition");
            }

            bool removed = context.Inventory.RemoveItem(item, Mathf.Max(1, quantity));
            return Record(removed, "Remove Item", removed ? "Removed" : "NotFound", removed ? $"Removed {quantity} x {item.DisplayName}." : $"{item.DisplayName} quantity was not available.");
        }

        public PrototypeTestLabOperation FillInventory(ItemDefinition filler)
        {
            if (context?.Inventory == null)
            {
                return RecordFailure("Fill Inventory", "Player inventory is missing.", "MissingInventory");
            }

            if (filler == null)
            {
                return RecordFailure("Fill Inventory", "No filler item selected.", "MissingDefinition");
            }

            int safety = context.Inventory.SlotCapacity * Mathf.Max(1, filler.MaximumStackSize);
            int added = 0;
            for (int i = 0; i < safety && context.Inventory.DevelopmentOccupiedSlotCount() < context.Inventory.SlotCapacity; i++)
            {
                InventoryAddResult result = context.Inventory.AddItemOrInstances(filler, 1);
                if (result.AddedQuantity <= 0)
                {
                    break;
                }

                added += result.AddedQuantity;
            }

            return Record(added > 0, "Fill Inventory", added > 0 ? "Filled" : "NoChange", $"Added {added} filler item(s); occupied slots {context.Inventory.DevelopmentOccupiedSlotCount()}/{context.Inventory.SlotCapacity}.");
        }

        public PrototypeTestLabOperation ClearInventory(bool confirmed)
        {
            if (!RequireConfirmation("ClearInventory", confirmed, out PrototypeTestLabOperation confirmation))
            {
                return confirmation;
            }

            context?.Inventory?.DevelopmentClearInventory();
            return RecordSuccess("Clear Inventory", "Inventory cleared. Equipment was preserved.");
        }

        public PrototypeTestLabOperation EquipFirstCompatible(ItemDefinition item)
        {
            if (context?.Inventory == null || context.Equipment == null)
            {
                return RecordFailure("Equip Item", "Inventory or equipment is missing.", "MissingReference");
            }

            for (int i = 0; i < context.Inventory.Slots.Count; i++)
            {
                InventorySlot slot = context.Inventory.GetSlot(i);
                if (slot != null && !slot.IsEmpty && slot.Item == item)
                {
                    EquipmentOperationResult result = context.Equipment.EquipFromInventorySlot(i);
                    return Record(result.Succeeded, "Equip Item", result.Succeeded ? "Equipped" : "Failed", result.Message);
                }
            }

            return RecordFailure("Equip Item", "Selected item was not found in inventory.", "NotFound");
        }

        public PrototypeTestLabOperation UnequipAll(bool confirmed)
        {
            if (!RequireConfirmation("UnequipAll", confirmed, out PrototypeTestLabOperation confirmation))
            {
                return confirmation;
            }

            if (context?.Equipment == null)
            {
                return RecordFailure("Unequip All", "Equipment is missing.", "MissingEquipment");
            }

            int changed = 0;
            foreach (EquipmentSlotState slot in context.Equipment.Slots)
            {
                if (slot != null && !slot.IsEmpty && context.Equipment.Unequip(slot.SlotType).Succeeded)
                {
                    changed++;
                }
            }

            return RecordSuccess("Unequip All", $"Unequipped {changed} slot(s).");
        }

        public PrototypeTestLabOperation DamagePlayer(int amount)
        {
            if (context?.PlayerHealth == null)
            {
                return RecordFailure("Damage Player", "Player health is missing.", "MissingHealth");
            }

            int applied = context.PlayerHealth.Damage(Mathf.Max(0, amount));
            return RecordSuccess("Damage Player", $"Applied raw test damage {applied}. Health {context.PlayerHealth.CurrentHealth}/{context.PlayerHealth.MaximumHealth}.");
        }

        public PrototypeTestLabOperation HealPlayer(int amount)
        {
            if (context?.PlayerHealth == null)
            {
                return RecordFailure("Heal Player", "Player health is missing.", "MissingHealth");
            }

            int healed = context.PlayerHealth.Heal(Mathf.Max(0, amount));
            return RecordSuccess("Heal Player", $"Healed {healed}. Health {context.PlayerHealth.CurrentHealth}/{context.PlayerHealth.MaximumHealth}.");
        }

        public PrototypeTestLabOperation SetHealth(int value)
        {
            if (context?.PlayerHealth == null)
            {
                return RecordFailure("Set Health", "Player health is missing.", "MissingHealth");
            }

            bool restored = context.PlayerHealth.TryRestoreForPersistence(Mathf.Clamp(value, 1, context.PlayerHealth.MaximumHealth), out string failureReason);
            return Record(restored, "Set Health", restored ? "Clamped" : "Failed", restored ? $"Health set to {context.PlayerHealth.CurrentHealth}/{context.PlayerHealth.MaximumHealth}." : failureReason);
        }

        public PrototypeTestLabOperation RestoreVitals()
        {
            context?.PlayerHealth?.ResetToMaximum();
            context?.PlayerMana?.RestoreToMaximum();
            context?.PlayerStamina?.RestoreToMaximum();
            return RecordSuccess("Restore Vitals", "Health, mana, and stamina restored to maximum.");
        }

        public PrototypeTestLabOperation AddStrengthTraining()
        {
            return AddAttributeTraining(AttributeIds.Strength, 0.25f, "Strength Base Attribute Training");
        }

        public PrototypeTestLabOperation AddBalancedAttributeTraining()
        {
            if (context?.PlayerAttributes == null)
            {
                return RecordFailure("Balanced Base Attribute Training", "Player Base Attributes component is missing.", "MissingAttributes");
            }

            List<RuntimeAttributeSourceContribution> contributions = new List<RuntimeAttributeSourceContribution>();
            foreach (string attributeId in AttributeIds.AlphaAttributeIds)
            {
                contributions.Add(new RuntimeAttributeSourceContribution
                {
                    attributeId = attributeId,
                    sourceId = "development.test-lab.balanced-training",
                    sourceCategory = (int)CalculatedStatContributionSourceCategory.Development,
                    amount = 0.1f,
                    removable = false
                });
            }

            bool succeeded = context.PlayerAttributes.TryRecordTrainingEvent(
                $"development.attribute-growth.{Guid.NewGuid():N}",
                AttributeGrowthEventCategory.Development,
                contributions,
                "Prototype Test Lab",
                out string failureReason);
            return Record(succeeded, "Balanced Base Attribute Training", succeeded ? "Recorded" : "Failed", succeeded ? "Added +0.1 permanent growth to every alpha Base Attribute." : failureReason);
        }

        public PrototypeTestLabOperation SetStrengthAboveHundred()
        {
            if (context?.PlayerAttributes == null)
            {
                return RecordFailure("Set Strength Above 100", "Player Base Attributes component is missing.", "MissingAttributes");
            }

            string sourceId = "development.test-lab.strength-above-100";
            context.PlayerAttributes.RemovePermanentSource(sourceId, out _);
            bool succeeded = context.PlayerAttributes.TryAddPermanentSource(
                sourceId,
                CalculatedStatContributionSourceCategory.Development,
                AttributeIds.Strength,
                100f,
                removable: true,
                out string failureReason);
            return Record(succeeded, "Set Strength Above 100", succeeded ? "Applied" : "Failed", succeeded ? "Strength has a removable +100 permanent development source." : failureReason);
        }

        public PrototypeTestLabOperation AddPhysicalPowerFlat()
        {
            return AddCalculatedContribution(
                "development.test-lab.physical-power-flat",
                CalculatedStatIds.PhysicalPower,
                CalculatedStatContributionKind.Flat,
                CalculatedStatContributionDirection.Improve,
                5f,
                "Add Physical Power");
        }

        public PrototypeTestLabOperation AddPhysicalDefensePenalty()
        {
            return AddCalculatedContribution(
                "development.test-lab.physical-defense-penalty",
                CalculatedStatIds.PhysicalDefense,
                CalculatedStatContributionKind.Flat,
                CalculatedStatContributionDirection.Reduce,
                3f,
                "Add Defense Penalty");
        }

        public PrototypeTestLabOperation ClearFeature52Contributions()
        {
            if (context?.PlayerAttributes != null)
            {
                context.PlayerAttributes.RemovePermanentSource("development.test-lab.strength-above-100", out _);
            }

            bool removedPower = context?.PlayerCalculatedStats != null
                && context.PlayerCalculatedStats.RemoveContributionsFromSource(CalculatedStatContributionSourceCategory.Development, "development.test-lab.physical-power-flat");
            bool removedDefense = context?.PlayerCalculatedStats != null
                && context.PlayerCalculatedStats.RemoveContributionsFromSource(CalculatedStatContributionSourceCategory.Development, "development.test-lab.physical-defense-penalty");
            return RecordSuccess("Clear Feature 5.4a Contributions", $"Cleared development Base Attribute/Calculated Stat contributions. Power={removedPower} Defense={removedDefense}.");
        }

        public PrototypeTestLabOperation RecalculateFeature52Stats()
        {
            if (context?.PlayerCalculatedStats == null)
            {
                return RecordFailure("Rebuild Feature 5.4a Stats", "Player Calculated Stats component is missing.", "MissingCalculatedStats");
            }

            context.PlayerCalculatedStats.ForceRecalculateAll();
            return RecordSuccess("Rebuild Feature 5.4a Stats", "Calculated Stat cache rebuilt from Base Attributes and active contributions.");
        }

        public PrototypeTestLabOperation AttemptInvalidAttributeGrowth()
        {
            if (context?.PlayerAttributes == null)
            {
                return RecordFailure("Invalid Base Attribute Growth Proof", "Player Base Attributes component is missing.", "MissingAttributes");
            }

            bool succeeded = context.PlayerAttributes.TryRecordTrainingEvent(
                "development.invalid-growth-proof",
                AttributeGrowthEventCategory.Development,
                new[]
                {
                    new RuntimeAttributeSourceContribution
                    {
                        attributeId = AttributeIds.Strength,
                        sourceId = "development.invalid-growth-proof",
                        sourceCategory = (int)CalculatedStatContributionSourceCategory.Development,
                        amount = -1f
                    }
                },
                "Prototype Test Lab",
                out string failureReason);
            return Record(!succeeded, "Invalid Base Attribute Growth Proof", succeeded ? "UnexpectedSuccess" : "Rejected", succeeded ? "Invalid negative growth was unexpectedly accepted." : failureReason);
        }

        public PrototypeTestLabOperation DrainMana(float amount)
        {
            VitalChangeResult result = context?.PlayerMana == null
                ? VitalChangeResult.Failure(amount, "Player mana is missing.")
                : context.PlayerMana.Spend(Mathf.Max(0f, amount));
            return Record(result.Succeeded, "Drain Mana", result.Succeeded ? "Spent" : "Failed", result.Message);
        }

        private PrototypeTestLabOperation AddAttributeTraining(string attributeId, float amount, string operationName)
        {
            if (context?.PlayerAttributes == null)
            {
                return RecordFailure(operationName, "Player Base Attributes component is missing.", "MissingAttributes");
            }

            bool succeeded = context.PlayerAttributes.TryRecordTrainingEvent(
                $"development.attribute-growth.{Guid.NewGuid():N}",
                AttributeGrowthEventCategory.Development,
                new[]
                {
                    new RuntimeAttributeSourceContribution
                    {
                        attributeId = attributeId,
                        sourceId = $"development.test-lab.{attributeId}",
                        sourceCategory = (int)CalculatedStatContributionSourceCategory.Development,
                        amount = amount,
                        removable = false
                    }
                },
                "Prototype Test Lab",
                out string failureReason);
            return Record(succeeded, operationName, succeeded ? "Recorded" : "Failed", succeeded ? $"Added +{amount:0.###} to {attributeId}." : failureReason);
        }

        private PrototypeTestLabOperation AddCalculatedContribution(string sourceId, string statId, CalculatedStatContributionKind kind, CalculatedStatContributionDirection direction, float magnitude, string operationName)
        {
            if (context?.PlayerCalculatedStats == null)
            {
                return RecordFailure(operationName, "Player calculated stats component is missing.", "MissingCalculatedStats");
            }

            context.PlayerCalculatedStats.RemoveContributionsFromSource(CalculatedStatContributionSourceCategory.Development, sourceId);
            bool succeeded = context.PlayerCalculatedStats.AddContribution(new RuntimeCalculatedStatContribution
            {
                contributionId = sourceId,
                sourceId = sourceId,
                sourceCategory = (int)CalculatedStatContributionSourceCategory.Development,
                statId = statId,
                kind = (int)kind,
                direction = (int)direction,
                magnitude = magnitude
            }, out string failureReason);
            return Record(succeeded, operationName, succeeded ? "Applied" : "Failed", succeeded ? $"{direction} {statId} by {magnitude:0.###}." : failureReason);
        }

        public PrototypeTestLabOperation DrainStamina(float amount)
        {
            VitalChangeResult result = context?.PlayerStamina == null
                ? VitalChangeResult.Failure(amount, "Player stamina is missing.")
                : context.PlayerStamina.Spend(Mathf.Max(0f, amount), "Development test");
            return Record(result.Succeeded, "Drain Stamina", result.Succeeded ? "Spent" : "Failed", result.Message);
        }

        public PrototypeTestLabOperation ApplyStatus(StatusEffectDefinition status, bool toEnemy)
        {
            StatusEffectController controller = toEnemy ? context?.EnemyStatuses : context?.PlayerStatuses;
            if (controller == null)
            {
                return RecordFailure("Apply Status", "Target status controller is missing.", "MissingTarget");
            }

            if (status == null)
            {
                return RecordFailure("Apply Status", "No status definition selected.", "MissingDefinition");
            }

            StatusEffectApplicationRequest request = new StatusEffectApplicationRequest(status, null, DevelopmentStatusSource, 0f, string.Empty, Time.time);
            StatusApplicationResult result = controller.ApplyStatus(request);
            return Record(result.Succeeded, "Apply Status", result.Status.ToString(), result.Message);
        }

        public PrototypeTestLabOperation RemoveStatus(StatusEffectDefinition status, bool fromEnemy)
        {
            StatusEffectController controller = fromEnemy ? context?.EnemyStatuses : context?.PlayerStatuses;
            if (controller == null || status == null)
            {
                return RecordFailure("Remove Status", "Target status controller or status definition is missing.", "MissingReference");
            }

            bool removed = controller.RemoveStatusesByDefinition(status.Id);
            return Record(removed, "Remove Status", removed ? "Removed" : "NotFound", removed ? $"Removed {status.DisplayName}." : $"{status.DisplayName} was not active.");
        }

        public PrototypeTestLabOperation ClearTemporaryStatuses()
        {
            context?.PlayerStatuses?.ClearTemporaryStatuses();
            context?.EnemyStatuses?.ClearTemporaryStatuses();
            return RecordSuccess("Clear Temporary Statuses", "Temporary player and enemy statuses cleared.");
        }

        public PrototypeTestLabOperation ApplyTypedDamage(DamageTypeDefinition damageType, float amount, bool targetEnemy, bool sourcePlayer)
        {
            if (damageType == null)
            {
                return RecordFailure("Apply Typed Damage", "No damage type selected.", "MissingDefinition");
            }

            IDamageable damageable = targetEnemy ? context?.EnemyHealth : context?.PlayerHealth;
            Transform targetTransform = targetEnemy ? context?.EnemyTransform : context?.PlayerTransform;
            GameObject source = sourcePlayer ? context?.PlayerTransform?.gameObject : context?.EnemyTransform?.gameObject;
            if (damageable == null || targetTransform == null)
            {
                return RecordFailure("Apply Typed Damage", "Damage target is missing.", "MissingTarget");
            }

            float rawAmount = Mathf.Max(0f, amount);
            DamageComponent component = new DamageComponent(damageType, rawAmount);
            DamagePacket packet = DamagePacket.Single(source, component);
            DamageInfo info = new DamageInfo(rawAmount, source, targetTransform.position, Vector3.forward, DamageType.Physical, packet);
            DamageResult result = damageable.ApplyDamage(in info);
            return Record(result.Applied, "Apply Typed Damage", result.Applied ? "Applied" : "Failed", result.Message);
        }

        public PrototypeTestLabOperation ResetEnemy()
        {
            context?.EnemyAttack?.ResetCooldown();
            context?.EnemyController?.ResetControllerState();
            context?.EnemyStatuses?.ClearTemporaryStatuses();
            context?.EnemyHealth?.ResetToMaximum();
            return RecordSuccess("Reset Enemy", "Enemy health, cooldown, controller state, and temporary statuses reset.");
        }

        public PrototypeTestLabOperation DefeatEnemy(DamageTypeDefinition damageType)
        {
            float amount = context?.EnemyHealth == null ? 9999f : context.EnemyHealth.MaximumHealth + 9999f;
            return ApplyTypedDamage(damageType ?? GetDefinitions<DamageTypeDefinition>().FirstOrDefault(), amount, targetEnemy: true, sourcePlayer: true);
        }

        public PrototypeTestLabOperation StartQuest(QuestDefinition quest)
        {
            if (context?.QuestLog == null || quest == null)
            {
                return RecordFailure("Start Quest", "Quest log or quest definition is missing.", "MissingReference");
            }

            QuestOperationResult result = context.QuestLog.StartQuest(quest);
            return Record(result.Succeeded, "Start Quest", result.Succeeded ? "Started" : "Failed", result.Message);
        }

        public PrototypeTestLabOperation ReportTalk(PersonDefinition person)
        {
            if (person == null)
            {
                return RecordFailure("Report Talk", "No person definition selected.", "MissingDefinition");
            }

            QuestObjectiveSignalBus.ReportTalk(person.Id);
            return RecordSuccess("Report Talk", $"Reported talk with {FormatDefinition(person)}.");
        }

        public PrototypeTestLabOperation ReportReach(PlaceDefinition place)
        {
            if (place == null)
            {
                return RecordFailure("Report Reach Location", "No place definition selected.", "MissingDefinition");
            }

            QuestObjectiveSignalBus.ReportReachLocation(place);
            return RecordSuccess("Report Reach Location", $"Reported reach location {FormatDefinition(place)}.");
        }

        public PrototypeTestLabOperation ReportDefeat(string targetCategory)
        {
            if (string.IsNullOrWhiteSpace(targetCategory))
            {
                targetCategory = "prototype_enemy";
            }

            GameObject temporary = new GameObject("Development Contract Objective Target");
            try
            {
                ContractObjectiveTarget target = temporary.AddComponent<ContractObjectiveTarget>();
                target.DevelopmentSetTargetCategory(targetCategory);
                context?.QuestLog?.RecordDefeat(target);
                context?.ContractJournal?.RecordDefeat(target);
                return RecordSuccess("Report Defeat", $"Reported defeat target '{targetCategory}'.");
            }
            finally
            {
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(temporary);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(temporary);
                }
            }
        }

        public PrototypeTestLabOperation ClearQuestLog(bool confirmed)
        {
            if (!RequireConfirmation("ClearQuestLog", confirmed, out PrototypeTestLabOperation confirmation))
            {
                return confirmation;
            }

            context?.QuestLog?.DevelopmentClearQuestLog();
            return RecordSuccess("Clear Quest Log", "Quest log cleared.");
        }

        public PrototypeTestLabOperation AcceptContract(ContractDefinition contract)
        {
            if (context?.ContractJournal == null || contract == null)
            {
                return RecordFailure("Accept Contract", "Contract journal or contract definition is missing.", "MissingReference");
            }

            ContractOperationResult result = context.ContractJournal.AcceptContract(contract);
            return Record(result.Succeeded, "Accept Contract", result.Succeeded ? "Accepted" : "Failed", result.Message);
        }

        public PrototypeTestLabOperation ClearContractJournal(bool confirmed)
        {
            if (!RequireConfirmation("ClearContractJournal", confirmed, out PrototypeTestLabOperation confirmation))
            {
                return confirmation;
            }

            context?.ContractJournal?.DevelopmentClearContractJournal();
            return RecordSuccess("Clear Contract Journal", "Contract journal cleared.");
        }

        public PrototypeTestLabOperation Save()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Save Prototype Slot", "Persistence service is missing.", "MissingPersistence");
            }

            PersistenceSaveResult result = persistence.SavePrototypeSlot();
            return Record(result.Succeeded, "Save Prototype Slot", result.Status.ToString(), result.Message);
        }

        public PrototypeTestLabOperation Load()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Load Prototype Slot", "Persistence service is missing.", "MissingPersistence");
            }

            PersistenceLoadResult result = persistence.LoadPrototypeSlot();
            return Record(result.Succeeded, "Load Prototype Slot", result.Status.ToString(), result.Message);
        }

        public PrototypeTestLabOperation ValidateSave()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Validate Prototype Slot", "Persistence service is missing.", "MissingPersistence");
            }

            PersistenceValidationResult result = persistence.ValidatePrototypeSlot();
            return Record(result.Succeeded, "Validate Prototype Slot", result.Status.ToString(), $"{result.Message} BackupAvailable={result.BackupAvailable}");
        }

        public PrototypeTestLabOperation DeleteSave(bool confirmed)
        {
            if (!RequireConfirmation("DeleteSave", confirmed, out PrototypeTestLabOperation confirmation))
            {
                return confirmation;
            }

            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Delete Prototype Slot", "Persistence service is missing.", "MissingPersistence");
            }

            PersistenceDeleteResult result = persistence.DeletePrototypeSlot();
            return Record(result.Succeeded, "Delete Prototype Slot", result.Status.ToString(), result.Message);
        }

        public PrototypeTestLabOperation ForceAutosave()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Force Autosave", "Persistence service is missing.", "MissingPersistence");
            }

            PersistenceSaveResult result = persistence.ForceAutosave("TestLab");
            return Record(result.Succeeded, "Force Autosave", result.Status.ToString(), result.Message);
        }

        public PrototypeTestLabOperation SetShortAutosaveInterval()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Set Autosave Interval", "Persistence service is missing.", "MissingPersistence");
            }

            persistence.SetAutosaveIntervalForTesting(15f);
            return RecordSuccess("Set Autosave Interval", "Autosave interval set to 15 seconds for local testing.");
        }

        public PrototypeTestLabOperation MarkSaveDirty()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Mark Save Dirty", "Persistence service is missing.", "MissingPersistence");
            }

            persistence.DirtyTracker?.DevelopmentSetDirty(true, "Test Lab marked save dirty.");
            return RecordSuccess("Mark Save Dirty", "Save dirty state set for confirmation and autosave testing.");
        }

        public PrototypeTestLabOperation MarkSaveClean()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Mark Save Clean", "Persistence service is missing.", "MissingPersistence");
            }

            persistence.DirtyTracker?.DevelopmentSetDirty(false, "Test Lab marked save clean.");
            return RecordSuccess("Mark Save Clean", "Save dirty state cleared.");
        }

        public PrototypeTestLabOperation SaveManualSlotOne()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Save Manual Slot 1", "Persistence service is missing.", "MissingPersistence");
            }

            PersistenceSaveResult result = persistence.SaveManualSlot(0);
            return Record(result.Succeeded, "Save Manual Slot 1", result.Status.ToString(), result.Message);
        }

        public PrototypeTestLabOperation LoadManualSlotOneBackup()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Load Manual Slot 1 Backup", "Persistence service is missing.", "MissingPersistence");
            }

            PersistenceLoadResult result = persistence.LoadSaveSlot(PrototypeSaveSlotCatalog.ManualSlotId(0), loadBackup: true);
            return Record(result.Succeeded, "Load Manual Slot 1 Backup", result.Status.ToString(), result.Message);
        }

        public PrototypeTestLabOperation ValidateManualSlotOneBackup()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Validate Manual Slot 1 Backup", "Persistence service is missing.", "MissingPersistence");
            }

            PersistenceValidationResult result = persistence.ValidateSaveSlot(PrototypeSaveSlotCatalog.ManualSlotId(0), validateBackup: true);
            return Record(result.Succeeded, "Validate Manual Slot 1 Backup", result.Status.ToString(), result.Message);
        }

        public PrototypeTestLabOperation RunRecoveryScan()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Run Recovery Scan", "Persistence service is missing.", "MissingPersistence");
            }

            SaveRecoveryScanReport report = persistence.RunRecoveryScan();
            return RecordSuccess("Run Recovery Scan", $"{report.candidates.Length} candidate(s). {report.recommendation}");
        }

        public PrototypeTestLabOperation PromoteManualSlotOneBackup(bool confirmed)
        {
            if (!RequireConfirmation("PromoteManualSlotOneBackup", confirmed, out PrototypeTestLabOperation confirmation))
            {
                return confirmation;
            }

            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Promote Manual Slot 1 Backup", "Persistence service is missing.", "MissingPersistence");
            }

            PersistenceSaveResult result = persistence.PromoteBackup(PrototypeSaveSlotCatalog.ManualSlotId(0));
            return Record(result.Succeeded, "Promote Manual Slot 1 Backup", result.Status.ToString(), result.Message);
        }

        public PrototypeTestLabOperation QuarantineManualSlotOnePrimary(bool confirmed)
        {
            if (!RequireConfirmation("QuarantineManualSlotOnePrimary", confirmed, out PrototypeTestLabOperation confirmation))
            {
                return confirmation;
            }

            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Quarantine Manual Slot 1", "Persistence service is missing.", "MissingPersistence");
            }

            PersistenceSaveResult result = persistence.QuarantinePrimary(PrototypeSaveSlotCatalog.ManualSlotId(0));
            return Record(result.Succeeded, "Quarantine Manual Slot 1", result.Status.ToString(), result.Message);
        }

        public PrototypeTestLabOperation CleanupTemporarySaves(bool confirmed)
        {
            if (!RequireConfirmation("CleanupTemporarySaves", confirmed, out PrototypeTestLabOperation confirmation))
            {
                return confirmation;
            }

            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Cleanup Temporary Saves", "Persistence service is missing.", "MissingPersistence");
            }

            PersistenceDeleteResult result = persistence.CleanupStaleTemporaryFiles();
            return Record(result.Succeeded, "Cleanup Temporary Saves", result.Status.ToString(), result.Message);
        }

        public PrototypeTestLabOperation InjectPrepareFailure()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Inject Prepare Failure", "Persistence service is missing.", "MissingPersistence");
            }

            persistence.InjectNextPersistenceFault(PersistenceFaultInjectionPoint.LoadPrepare);
            return RecordSuccess("Inject Prepare Failure", "Next load prepare phase will fail once.");
        }

        public PrototypeTestLabOperation InjectCommitFailure()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Inject Commit Failure", "Persistence service is missing.", "MissingPersistence");
            }

            persistence.InjectNextPersistenceFault(PersistenceFaultInjectionPoint.LoadCommit);
            return RecordSuccess("Inject Commit Failure", "Next load commit phase will fail once and attempt rollback.");
        }

        public PrototypeTestLabOperation InjectAuditFailure()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Inject Audit Failure", "Persistence service is missing.", "MissingPersistence");
            }

            persistence.InjectNextPersistenceFault(PersistenceFaultInjectionPoint.ConsistencyAudit);
            return RecordSuccess("Inject Audit Failure", "Next consistency audit will fail once and attempt rollback.");
        }

        public PrototypeTestLabOperation RecordFingerprint()
        {
            if (!EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence))
            {
                return RecordFailure("Record Fingerprint", "Persistence service is missing.", "MissingPersistence");
            }

            return RecordSuccess("Record Fingerprint", persistence.BuildRuntimeStateFingerprint());
        }

        public PrototypeTestLabOperation Teleport(PrototypeTestPoint point)
        {
            if (context?.PlayerTransform == null || point == null)
            {
                return RecordFailure("Teleport", "Player transform or test point is missing.", "MissingReference");
            }

            CharacterController characterController = context.PlayerTransform.GetComponent<CharacterController>();
            if (characterController != null)
            {
                characterController.enabled = false;
            }

            context.PlayerTransform.SetPositionAndRotation(point.transform.position, point.transform.rotation);

            if (characterController != null)
            {
                characterController.enabled = true;
            }

            return RecordSuccess("Teleport", $"Teleported to {point.DisplayName} ({point.TestPointId}).");
        }

        public PrototypeTestLabOperation ValidateCurrentLocation()
        {
            if (context?.Persistence == null)
            {
                return RecordFailure("Validate Current Location", "Persistence service is missing.", "MissingPersistence");
            }

            string summary = context.Persistence.BuildPlayerLocationDiagnosticSummary();
            return RecordSuccess("Validate Current Location", summary.Replace(Environment.NewLine, " | "));
        }

        public PrototypeTestLabOperation ValidateIdentityProgression()
        {
            if (!EnsureIdentityProgression(out PlayerIdentityProgression progression))
            {
                return RecordFailure("Validate Identity", "Player identity/progression component is missing.", "MissingIdentityProgression");
            }

            bool valid = progression.ValidateIdentity(out string failureReason);
            return Record(valid, "Validate Identity", valid ? "Valid" : "Invalid", valid ? "Identity IDs are distinct and well-formed." : failureReason);
        }

        public PrototypeTestLabOperation GenerateOrigin(int seed)
        {
            if (!EnsureIdentityProgression(out PlayerIdentityProgression progression))
            {
                return RecordFailure("Generate Origin", "Player identity/progression component is missing.", "MissingIdentityProgression");
            }

            if (registry == null)
            {
                return RecordFailure("Generate Origin", "Definition registry is missing.", "MissingRegistry");
            }

            int effectiveSeed = seed == 0 ? Environment.TickCount : seed;
            ProgressionOperationResult result = progression.AssignRandomOrigin(registry, effectiveSeed);
            return Record(result.Succeeded, "Generate Origin", result.Code, result.Message);
        }

        public PrototypeTestLabOperation ProveOriginAssignmentIsOnceOnly()
        {
            if (!EnsureIdentityProgression(out PlayerIdentityProgression progression))
            {
                return RecordFailure("Duplicate Origin Proof", "Player identity/progression component is missing.", "MissingIdentityProgression");
            }

            if (registry == null)
            {
                return RecordFailure("Duplicate Origin Proof", "Definition registry is missing.", "MissingRegistry");
            }

            ProgressionOperationResult result = progression.AssignRandomOrigin(registry, Environment.TickCount);
            bool expectedFailure = !result.Succeeded && string.Equals(result.Code, "OriginAlreadyAssigned", StringComparison.Ordinal);
            return Record(expectedFailure, "Duplicate Origin Proof", expectedFailure ? "Rejected" : result.Code, expectedFailure ? "Second origin assignment was correctly rejected." : result.Message);
        }

        public PrototypeTestLabOperation ResetIdentityProgression(bool confirmed)
        {
            if (!RequireConfirmation("ResetIdentityProgression", confirmed, out PrototypeTestLabOperation confirmation))
            {
                return confirmation;
            }

            if (!EnsureIdentityProgression(out PlayerIdentityProgression progression))
            {
                return RecordFailure("Reset Identity", "Player identity/progression component is missing.", "MissingIdentityProgression");
            }

            ProgressionOperationResult result = progression.ResetIdentityProgressionForDevelopment();
            return Record(result.Succeeded, "Reset Identity", result.Code, result.Message);
        }

        public PrototypeTestLabOperation AdvanceBirthGiftProgress(float seconds)
        {
            if (!EnsureIdentityProgression(out PlayerIdentityProgression progression))
            {
                return RecordFailure("Advance Birth Gift", "Player identity/progression component is missing.", "MissingIdentityProgression");
            }

            ProgressionOperationResult result = progression.AdvanceBirthGiftProgressForTesting(Mathf.Max(0f, seconds), registry);
            return Record(result.Succeeded, "Advance Birth Gift", result.Code, result.Message);
        }

        public PrototypeTestLabOperation ForceBirthGiftAwakening()
        {
            if (!EnsureIdentityProgression(out PlayerIdentityProgression progression))
            {
                return RecordFailure("Awaken Birth Gift", "Player identity/progression component is missing.", "MissingIdentityProgression");
            }

            ProgressionOperationResult result = progression.ForceBirthGiftAwakening(registry);
            return Record(result.Succeeded, "Awaken Birth Gift", result.Code, result.Message);
        }

        public PrototypeTestLabOperation AddRole(RoleDefinition role, bool acceptConflicts)
        {
            if (!EnsureIdentityProgression(out PlayerIdentityProgression progression))
            {
                return RecordFailure("Add Role", "Player identity/progression component is missing.", "MissingIdentityProgression");
            }

            RoleAcquisitionResult result = progression.AddRole(role, "test-lab", "manual-test-lab", primary: false, acceptConflicts: acceptConflicts);
            string message = result.Conflict != null && result.Conflict.HasConflict
                ? $"{result.Message} Blockers={string.Join(", ", result.Conflict.Blockers.Select(blocker => blocker.roleDefinitionId))}"
                : result.Message;
            return Record(result.Succeeded, acceptConflicts ? "Add Role Accepting Conflicts" : "Add Role", result.Code, message);
        }

        public PrototypeTestLabOperation SuspendFirstActiveRole()
        {
            if (!TryGetFirstActiveRole(out PlayerIdentityProgression progression, out RuntimeRoleRecord role, out PrototypeTestLabOperation failure))
            {
                return failure;
            }

            ProgressionOperationResult result = progression.SuspendRole(role.recordId);
            return Record(result.Succeeded, "Suspend Role", result.Code, result.Message);
        }

        public PrototypeTestLabOperation RevokeFirstActiveRole()
        {
            if (!TryGetFirstActiveRole(out PlayerIdentityProgression progression, out RuntimeRoleRecord role, out PrototypeTestLabOperation failure))
            {
                return failure;
            }

            ProgressionOperationResult result = progression.RevokeRole(role.recordId);
            return Record(result.Succeeded, "Revoke Role", result.Code, result.Message);
        }

        public PrototypeTestLabOperation AbandonFirstActiveRole()
        {
            if (!TryGetFirstActiveRole(out PlayerIdentityProgression progression, out RuntimeRoleRecord role, out PrototypeTestLabOperation failure))
            {
                return failure;
            }

            ProgressionOperationResult result = progression.AbandonRole(role.recordId);
            return Record(result.Succeeded, "Abandon Role", result.Code, result.Message);
        }

        public PrototypeTestLabOperation AddGlobalSocialStatus(SocialStatusDefinition status)
        {
            return AddSocialStatus(status, SocialStatusContextKind.Global, string.Empty, "Add Global Status");
        }

        public PrototypeTestLabOperation AddPlaceSocialStatus(SocialStatusDefinition status, PlaceDefinition place)
        {
            string placeId = place == null ? string.Empty : place.Id;
            return AddSocialStatus(status, SocialStatusContextKind.Place, placeId, "Add Place Status");
        }

        public PrototypeTestLabOperation ResolveFirstActiveSocialStatus()
        {
            if (!EnsureIdentityProgression(out PlayerIdentityProgression progression))
            {
                return RecordFailure("Resolve Social Status", "Player identity/progression component is missing.", "MissingIdentityProgression");
            }

            RuntimeSocialStatusRecord status = progression.SocialStatuses.FirstOrDefault(record => record.lifecycleState == SocialStatusLifecycleState.Active);
            if (status == null)
            {
                return RecordFailure("Resolve Social Status", "No active social status exists.", "MissingActiveStatus");
            }

            ProgressionOperationResult result = progression.ResolveSocialStatus(status.recordId, "test-lab-resolved");
            return Record(result.Succeeded, "Resolve Social Status", result.Code, result.Message);
        }

        public PrototypeTestLabOperation AddCurrency(CurrencyDefinition currency, long amount)
        {
            if (!EnsureIdentityProgression(out PlayerIdentityProgression progression))
            {
                return RecordFailure("Add Currency", "Player identity/progression component is missing.", "MissingIdentityProgression");
            }

            ProgressionOperationResult result = progression.AddCurrency(currency, Math.Max(0L, amount));
            return Record(result.Succeeded, "Add Currency", result.Code, result.Message);
        }

        public PrototypeTestLabOperation SpendCurrency(CurrencyDefinition currency, long amount)
        {
            if (!EnsureIdentityProgression(out PlayerIdentityProgression progression))
            {
                return RecordFailure("Spend Currency", "Player identity/progression component is missing.", "MissingIdentityProgression");
            }

            ProgressionOperationResult result = progression.SpendCurrency(currency, Math.Max(0L, amount));
            return Record(result.Succeeded, "Spend Currency", result.Code, result.Message);
        }

        public PrototypeTestLabOperation RecordSuccessfulActivity(float difficulty)
        {
            return RecordActivity(ActivityOutcome.Success, difficulty, "Record Success Activity");
        }

        public PrototypeTestLabOperation RecordFailedActivity(float difficulty)
        {
            return RecordActivity(ActivityOutcome.Failure, difficulty, "Record Failure Activity");
        }

        public PrototypeTestLabOperation RecordParticipation()
        {
            if (!EnsureIdentityProgression(out PlayerIdentityProgression progression))
            {
                return RecordFailure("Record Participation", "Player identity/progression component is missing.", "MissingIdentityProgression");
            }

            ProgressionOperationResult result = progression.RecordParticipation($"participation.test-lab.{Guid.NewGuid():N}", "test-lab", "PrototypeTestLab");
            return Record(result.Succeeded, "Record Participation", result.Code, result.Message);
        }

        public PrototypeTestLabOperation RefreshWorldEntityDiagnostics()
        {
            return RecordSuccess("World Entity Diagnostics", $"Registered {WorldEntityRegistry.Count} world entity identity object(s).");
        }

        public PrototypeTestLabOperation SpawnPersistentWorldLoot(ItemDefinition item)
        {
            if (item == null)
            {
                return RecordFailure("Spawn Persistent World Loot", "No item definition selected.", "MissingDefinition");
            }

            Vector3 position = context?.PlayerTransform == null ? Vector3.zero : context.PlayerTransform.position + context.PlayerTransform.forward * 2f + Vector3.up * 0.25f;
            GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pickup.name = $"Persistent Test Loot - {item.DisplayName}";
            pickup.transform.SetPositionAndRotation(position, Quaternion.identity);
            pickup.transform.localScale = Vector3.one * 0.35f;
            pickup.AddComponent<WorldItemPickup>().Configure(item, 1);
            WorldEntitySpawnResult result = WorldEntityIdentityFactory.CreateRuntimeIdentity(pickup, "scene.prototype", PersistenceService.LocalWorldId, item.Id);
            if (!result.Succeeded)
            {
                DestroyTestObject(pickup);
                return RecordFailure("Spawn Persistent World Loot", result.Message, result.Code);
            }

            lastSpawnedWorldEntityId = result.Identity.EntityId;
            lastSpawnedWorldEntityItem = item;
            return RecordWorldEntityResult("Spawn Persistent World Loot", $"Spawned {item.DisplayName} as {lastSpawnedWorldEntityId}.");
        }

        public PrototypeTestLabOperation SpawnTransientWorldLoot(ItemDefinition item)
        {
            if (item == null)
            {
                return RecordFailure("Spawn Transient World Loot", "No item definition selected.", "MissingDefinition");
            }

            Vector3 position = context?.PlayerTransform == null ? Vector3.zero : context.PlayerTransform.position + context.PlayerTransform.right * 2f + Vector3.up * 0.25f;
            GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pickup.name = $"Transient Test Loot - {item.DisplayName}";
            pickup.transform.SetPositionAndRotation(position, Quaternion.identity);
            pickup.transform.localScale = Vector3.one * 0.35f;
            pickup.AddComponent<WorldItemPickup>().Configure(item, 1);
            WorldEntityIdentity identity = pickup.AddComponent<WorldEntityIdentity>();
            identity.TryMarkTransient(out _);
            return RecordWorldEntityResult("Spawn Transient World Loot", $"Spawned transient {item.DisplayName}; it is intentionally not persistently registered.");
        }

        public PrototypeTestLabOperation DestroyLastSpawnedWorldLoot()
        {
            if (string.IsNullOrWhiteSpace(lastSpawnedWorldEntityId) || !WorldEntityRegistry.TryResolve(lastSpawnedWorldEntityId, out WorldEntityIdentity identity))
            {
                return RecordFailure("Destroy Spawned World Loot", "No spawned world entity is currently registered.", "MissingEntity");
            }

            WorldItemPickup pickup = identity.GetComponent<WorldItemPickup>();
            lastDestroyedWorldEntityId = identity.EntityId;
            lastDestroyedWorldEntityItem = pickup == null ? null : pickup.Item;
            WorldEntityRegistry.Unregister(identity);
            DestroyTestObject(identity.gameObject);
            return RecordWorldEntityResult("Destroy Spawned World Loot", $"Destroyed {lastDestroyedWorldEntityId}.");
        }

        public PrototypeTestLabOperation RecreateDestroyedWorldLoot()
        {
            if (string.IsNullOrWhiteSpace(lastDestroyedWorldEntityId) || lastDestroyedWorldEntityItem == null)
            {
                return RecordFailure("Recreate World Loot", "No destroyed persistent world loot is available to recreate.", "MissingSnapshot");
            }

            Vector3 position = context?.PlayerTransform == null ? Vector3.zero : context.PlayerTransform.position + context.PlayerTransform.forward * 2f + Vector3.up * 0.25f;
            GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pickup.name = $"Restored Test Loot - {lastDestroyedWorldEntityItem.DisplayName}";
            pickup.transform.SetPositionAndRotation(position, Quaternion.identity);
            pickup.transform.localScale = Vector3.one * 0.35f;
            pickup.AddComponent<WorldItemPickup>().Configure(lastDestroyedWorldEntityItem, 1);
            WorldEntitySpawnResult result = WorldEntityIdentityFactory.RestoreRuntimeIdentity(pickup, lastDestroyedWorldEntityId, "scene.prototype", PersistenceService.LocalWorldId, lastDestroyedWorldEntityItem.Id);
            if (!result.Succeeded)
            {
                DestroyTestObject(pickup);
                return RecordFailure("Recreate World Loot", result.Message, result.Code);
            }

            lastSpawnedWorldEntityId = result.Identity.EntityId;
            lastSpawnedWorldEntityItem = lastDestroyedWorldEntityItem;
            return RecordWorldEntityResult("Recreate World Loot", $"Recreated {lastSpawnedWorldEntityId}.");
        }

        public PrototypeTestLabOperation AttemptDuplicateWorldEntityRegistration()
        {
            if (!TryResolveLastSpawnedOrRegisteredTestLoot(out WorldEntityIdentity existingIdentity, out ItemDefinition item, out string failureReason))
            {
                return RecordWorldEntityFailure("Duplicate World Entity Proof", failureReason, "MissingEntity");
            }

            GameObject duplicate = new GameObject("Duplicate World Entity Proof");
            duplicate.name = "Duplicate World Entity Proof";
            duplicate.AddComponent<WorldItemPickup>().Configure(item, 1);
            WorldEntitySpawnResult result = WorldEntityIdentityFactory.RestoreRuntimeIdentity(duplicate, lastSpawnedWorldEntityId, existingIdentity.SceneKey, existingIdentity.WorldId, item.Id);
            if (result.Succeeded)
            {
                WorldEntityRegistry.Unregister(result.Identity);
                DestroyTestObject(duplicate);
                return RecordWorldEntityFailure("Duplicate World Entity Proof", "Duplicate registration unexpectedly succeeded.", "UnexpectedSuccess");
            }

            DestroyTestObject(duplicate);
            return RecordWorldEntityResult("Duplicate World Entity Proof", $"Duplicate rejected: {result.Code}.");
        }

        private bool TryResolveLastSpawnedOrRegisteredTestLoot(out WorldEntityIdentity identity, out ItemDefinition item, out string failureReason)
        {
            identity = null;
            item = null;
            failureReason = string.Empty;

            if (!string.IsNullOrWhiteSpace(lastSpawnedWorldEntityId)
                && WorldEntityRegistry.TryResolve(lastSpawnedWorldEntityId, out identity))
            {
                WorldItemPickup pickup = identity.GetComponent<WorldItemPickup>();
                item = pickup == null ? lastSpawnedWorldEntityItem : pickup.Item;
                if (item != null)
                {
                    return true;
                }

                failureReason = "The spawned world entity has no item definition to duplicate.";
                return false;
            }

            foreach (WorldEntityIdentity candidate in WorldEntityRegistry.RegisteredEntities)
            {
                if (candidate == null
                    || candidate.IdentityKind == WorldEntityIdentityKind.Transient
                    || (!candidate.name.StartsWith("Persistent Test Loot", StringComparison.Ordinal)
                        && !candidate.name.StartsWith("Restored Test Loot", StringComparison.Ordinal)))
                {
                    continue;
                }

                WorldItemPickup pickup = candidate.GetComponent<WorldItemPickup>();
                if (pickup == null || pickup.Item == null)
                {
                    continue;
                }

                identity = candidate;
                item = pickup.Item;
                lastSpawnedWorldEntityId = identity.EntityId;
                lastSpawnedWorldEntityItem = item;
                return true;
            }

            failureReason = string.IsNullOrWhiteSpace(lastSpawnedWorldEntityId)
                ? "Spawn persistent world loot first."
                : $"World entity '{lastSpawnedWorldEntityId}' is no longer registered. Spawn persistent world loot again.";
            return false;
        }

        private PrototypeTestLabOperation RecordWorldEntityResult(string operationName, string message)
        {
            lastWorldEntityOperationMessage = message;
            Debug.Log($"{operationName}: {message}");
            return RecordSuccess(operationName, message);
        }

        private PrototypeTestLabOperation RecordWorldEntityFailure(string operationName, string message, string code)
        {
            lastWorldEntityOperationMessage = message;
            return RecordFailure(operationName, message, code);
        }

        private static void DestroyTestObject(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(gameObject);
                return;
            }

            UnityEngine.Object.DestroyImmediate(gameObject);
        }

        public PrototypeTestLabOperation RunScenario(string scenarioId, ItemDefinition item, QuestDefinition quest, ContractDefinition contract, DamageTypeDefinition damageType)
        {
            switch (scenarioId)
            {
                case "clean":
                    context?.TestController?.ResetPrototypeState();
                    return RecordSuccess("Scenario: Clean Baseline", "Prototype reset executed; persistent player collections preserved.");
                case "combat":
                    context?.TestController?.ResetPrototypeState();
                    if (item != null)
                    {
                        GrantStatefulItem(item);
                        EquipFirstCompatible(item);
                    }

                    return RecordSuccess("Scenario: Combat Ready", "Reset vitals/enemy and attempted to grant/equip selected item.");
                case "full-inventory":
                    return FillInventory(item);
                case "quest":
                    if (quest != null)
                    {
                        StartQuest(quest);
                    }

                    return RecordSuccess("Scenario: Quest Midpoint", "Started selected quest. Use Talk/Reach/Defeat actions to progress through normal events.");
                case "contract":
                    if (contract != null)
                    {
                        AcceptContract(contract);
                    }

                    return RecordSuccess("Scenario: Contract Testing", "Accepted selected contract if available.");
                case "persistence":
                    RestoreVitals();
                    if (item != null)
                    {
                        GrantItem(item, 2);
                    }

                    if (quest != null)
                    {
                        StartQuest(quest);
                    }

                    if (contract != null)
                    {
                        AcceptContract(contract);
                    }

                    return RecordSuccess("Scenario: Persistence Round Trip", "Prepared representative player state for save/load testing.");
                default:
                    return RecordFailure("Scenario", $"Unknown scenario '{scenarioId}'.", "UnknownScenario");
            }
        }

        public string RunDiagnostics()
        {
            List<string> lines = new List<string>
            {
                "Diagnostics",
                $"Definitions loaded: {(registry == null ? 0 : registry.Count)}"
            };

            AddDuplicateInstanceDiagnostics(lines);
            AddDuplicateStatusDiagnostics(lines, "Player", context?.PlayerStatuses);
            AddDuplicateStatusDiagnostics(lines, "Enemy", context?.EnemyStatuses);
            AddReferenceDiagnostic(lines, "Inventory", context?.Inventory);
            AddReferenceDiagnostic(lines, "Equipment", context?.Equipment);
            AddReferenceDiagnostic(lines, "Quest Log", context?.QuestLog);
            AddReferenceDiagnostic(lines, "Contract Journal", context?.ContractJournal);
            AddReferenceDiagnostic(lines, "Persistence", context?.Persistence);
            AddReferenceDiagnostic(lines, "Enemy Health", context?.EnemyHealth);

            string result = string.Join(Environment.NewLine, lines);
            RecordSuccess("Refresh Diagnostics", "Diagnostics refreshed.");
            return result;
        }

        public void ClearConfirmation(string confirmationKey)
        {
            if (!string.IsNullOrWhiteSpace(confirmationKey))
            {
                pendingConfirmations.Remove(confirmationKey);
            }
        }

        private bool EnsurePersistence(out PrototypePersistenceServiceBehaviour persistence)
        {
            persistence = context?.Persistence;
            if (persistence == null)
            {
                return false;
            }

            persistence.EnsureInitialized();
            return true;
        }

        private bool EnsureIdentityProgression(out PlayerIdentityProgression progression)
        {
            progression = context?.IdentityProgression;
            if (progression == null)
            {
                return false;
            }

            progression.RegisterDefinitionCache(registry);
            return true;
        }

        private bool EnsureSkills(out CharacterSkillCollection skills)
        {
            skills = context?.PlayerSkills;
            if (skills == null)
            {
                return false;
            }

            skills.Configure(registry, context.PlayerCalculatedStats, context.SpellLoadout);
            return true;
        }

        private bool EnsureResources(out CharacterResourceCollection resources)
        {
            resources = context?.PlayerResources;
            if (resources == null && context?.PlayerTransform != null)
            {
                resources = context.PlayerTransform.GetComponentInParent<CharacterResourceCollection>();
            }

            if (resources == null && context?.PlayerTransform != null)
            {
                resources = context.PlayerTransform.gameObject.AddComponent<CharacterResourceCollection>();
            }

            if (resources == null)
            {
                return false;
            }

            context.PlayerResources = resources;
            resources.Configure(registry, context.PlayerCalculatedStats, PersistenceService.LocalPlayerId);
            return true;
        }

        private bool EnsureTraits(out CharacterTraitCollection traits)
        {
            traits = context?.PlayerTraits;
            if (traits == null && context?.PlayerTransform != null)
            {
                traits = context.PlayerTransform.GetComponentInParent<CharacterTraitCollection>();
            }

            if (traits == null && context?.PlayerTransform != null)
            {
                traits = context.PlayerTransform.gameObject.AddComponent<CharacterTraitCollection>();
            }

            if (traits == null)
            {
                return false;
            }

            context.PlayerTraits = traits;
            traits.Configure(registry, context.PlayerCalculatedStats, context.PlayerSkills, PersistenceService.LocalPlayerId);
            return true;
        }

        private PrototypeTestLabOperation ChangeTrait(TraitDefinition trait, string operationName, Func<CharacterTraitCollection, TraitOperationResult> action)
        {
            if (!EnsureTraits(out CharacterTraitCollection traits))
            {
                return RecordFailure(operationName, "Player Trait collection component is missing.", "MissingTraits");
            }

            if (trait == null)
            {
                return RecordFailure(operationName, "Trait definition is missing.", "MissingTrait");
            }

            TraitOperationResult result = action(traits);
            return Record(result.Succeeded, operationName, result.Code, result.Message);
        }

        private RequirementEvaluationContext BuildRequirementContext(bool testLab)
        {
            EnsureTraits(out CharacterTraitCollection traits);
            EnsureResources(out CharacterResourceCollection resources);
            EnsureSkills(out CharacterSkillCollection skills);
            return new RequirementEvaluationContext
            {
                Attributes = context?.PlayerAttributes,
                CalculatedStats = context?.PlayerCalculatedStats,
                Resources = resources,
                Skills = skills,
                Traits = traits,
                Identity = context?.IdentityProgression,
                Inventory = context?.Inventory,
                Equipment = context?.Equipment,
                Statuses = context?.PlayerStatuses,
                TestLabDiagnostics = testLab
            };
        }

        private bool TryGetFirstActiveRole(out PlayerIdentityProgression progression, out RuntimeRoleRecord role, out PrototypeTestLabOperation failure)
        {
            role = null;
            if (!EnsureIdentityProgression(out progression))
            {
                failure = RecordFailure("Role Operation", "Player identity/progression component is missing.", "MissingIdentityProgression");
                return false;
            }

            role = progression.Roles.FirstOrDefault(record => record.lifecycleState == RoleLifecycleState.Active);
            if (role != null)
            {
                failure = default;
                return true;
            }

            failure = RecordFailure("Role Operation", "No active role exists.", "MissingActiveRole");
            return false;
        }

        private PrototypeTestLabOperation AddSocialStatus(SocialStatusDefinition status, SocialStatusContextKind contextKind, string contextTargetId, string operationName)
        {
            if (!EnsureIdentityProgression(out PlayerIdentityProgression progression))
            {
                return RecordFailure(operationName, "Player identity/progression component is missing.", "MissingIdentityProgression");
            }

            ProgressionOperationResult result = progression.AddSocialStatus(status, contextKind, contextTargetId, "test-lab", "manual-test-lab");
            return Record(result.Succeeded, operationName, result.Code, result.Message);
        }

        private PrototypeTestLabOperation RecordActivity(ActivityOutcome outcome, float difficulty, string operationName)
        {
            if (!EnsureIdentityProgression(out PlayerIdentityProgression progression))
            {
                return RecordFailure(operationName, "Player identity/progression component is missing.", "MissingIdentityProgression");
            }

            ProgressionOperationResult result = progression.RecordActivityOutcome(
                $"activity.test-lab.{Guid.NewGuid():N}",
                ActivityType.DevelopmentTest,
                outcome,
                Mathf.Clamp01(difficulty),
                "test-lab",
                "PrototypeTestLab");
            return Record(result.Succeeded, operationName, result.Code, result.Message);
        }

        private bool RequireConfirmation(string key, bool confirmed, out PrototypeTestLabOperation result)
        {
            result = default;
            if (confirmed || pendingConfirmations.Remove(key))
            {
                return true;
            }

            pendingConfirmations.Add(key);
            result = RecordFailure("Confirmation Required", $"Press the same destructive action again to confirm '{key}'.", "ConfirmationRequired");
            return false;
        }

        private PrototypeTestLabOperation RecordSuccess(string operationName, string message)
        {
            return Record(true, operationName, "Success", message);
        }

        private PrototypeTestLabOperation RecordFailure(string operationName, string message, string code)
        {
            return Record(false, operationName, code, message);
        }

        private PrototypeTestLabOperation Record(bool succeeded, string operationName, string code, string message)
        {
            PrototypeTestLabOperation operation = new PrototypeTestLabOperation(DateTime.Now, operationName, succeeded, code, message);
            history.Insert(0, operation);
            while (history.Count > historyLimit)
            {
                history.RemoveAt(history.Count - 1);
            }

            if (!succeeded && !string.Equals(code, "ConfirmationRequired", StringComparison.Ordinal))
            {
                Debug.LogWarning($"{operationName}: {message}");
            }

            HistoryChanged?.Invoke();
            return operation;
        }

        private DefinitionRegistry CreateRegistry(DefinitionCatalog catalog)
        {
            if (catalog != null)
            {
                return catalog.CreateRegistry();
            }

#if UNITY_EDITOR
            DefinitionCatalog loaded = AssetDatabase.LoadAssetAtPath<DefinitionCatalog>(PrototypeCatalogPath);
            return loaded == null ? null : loaded.CreateRegistry();
#else
            return null;
#endif
        }

        private string FormatHealth()
        {
            return context?.PlayerHealth == null
                ? "Missing"
                : $"{context.PlayerHealth.CurrentHealth}/{context.PlayerHealth.MaximumHealth} Defeated={context.PlayerHealth.IsDefeated}";
        }

        private static string FormatResource(float current, float maximum)
        {
            return $"{current:0.#}/{maximum:0.#}";
        }

        private static string FormatNumber(float value)
        {
            return value.ToString("0.##");
        }

        public static string FormatDefinition(IGameDefinition definition)
        {
            return definition == null ? "None" : $"{definition.DisplayName} ({definition.Id})";
        }

        private string FormatStatuses(StatusEffectController controller)
        {
            if (controller == null || controller.ActiveStatuses.Count == 0)
            {
                return "None";
            }

            return string.Join(", ", controller.ActiveStatuses.Select(status => $"{status.Definition.DisplayName} x{status.StackCount} [{status.ApplicationId}]"));
        }

        private string FormatInventory()
        {
            if (context?.Inventory == null)
            {
                return "Missing";
            }

            return $"{context.Inventory.DevelopmentOccupiedSlotCount()}/{context.Inventory.SlotCapacity} slots";
        }

        private int CountEquipped()
        {
            if (context?.Equipment == null)
            {
                return 0;
            }

            int count = 0;
            foreach (EquipmentSlotState slot in context.Equipment.Slots)
            {
                if (slot != null && !slot.IsEmpty)
                {
                    count++;
                }
            }

            return count;
        }

        private string FormatEnemy()
        {
            return context?.EnemyHealth == null
                ? "Missing"
                : $"{context.EnemyHealth.CurrentHealth:0.#}/{context.EnemyHealth.MaximumHealth:0.#} Defeated={context.EnemyHealth.IsDefeated}";
        }

        private string FormatIdentityOneLine()
        {
            if (context?.IdentityProgression == null)
            {
                return "Missing";
            }

            RuntimeOriginAssignmentRecord origin = context.IdentityProgression.Origin;
            RuntimeBirthGiftRecord gift = context.IdentityProgression.BirthGift;
            OverallLevelBreakdown level = context.IdentityProgression.CalculateOverallLevel();
            string originId = origin != null && origin.assigned ? origin.originId : "Unassigned";
            string giftId = string.IsNullOrWhiteSpace(gift?.giftDefinitionId) ? "None" : $"{gift.giftDefinitionId}:{gift.state}";
            return $"{originId} | Gift={giftId} | Level={level.OverallLevel}";
        }

        private string FormatLocationOneLine()
        {
            if (context?.Persistence == null)
            {
                return "Missing";
            }

            return context.Persistence.BuildPlayerLocationDiagnosticSummary().Replace(Environment.NewLine, " | ");
        }

        private void AddDuplicateInstanceDiagnostics(List<string> lines)
        {
            HashSet<string> ids = new HashSet<string>();
            HashSet<string> duplicates = new HashSet<string>();
            if (context?.Inventory != null)
            {
                foreach (InventorySlot slot in context.Inventory.Slots)
                {
                    string id = slot == null || !slot.IsStateful || slot.ItemInstance == null ? string.Empty : slot.ItemInstance.InstanceId;
                    if (!string.IsNullOrWhiteSpace(id) && !ids.Add(id))
                    {
                        duplicates.Add(id);
                    }
                }
            }

            if (context?.Equipment != null)
            {
                foreach (EquipmentSlotState slot in context.Equipment.Slots)
                {
                    string id = slot == null || !slot.IsStateful || slot.ItemInstance == null ? string.Empty : slot.ItemInstance.InstanceId;
                    if (!string.IsNullOrWhiteSpace(id) && !ids.Add(id))
                    {
                        duplicates.Add(id);
                    }
                }
            }

            lines.Add(duplicates.Count == 0 ? "Duplicate item instance IDs: none" : $"Duplicate item instance IDs: {string.Join(", ", duplicates)}");
        }

        private static void AddDuplicateStatusDiagnostics(List<string> lines, string label, StatusEffectController controller)
        {
            if (controller == null)
            {
                lines.Add($"{label} statuses: missing controller");
                return;
            }

            HashSet<string> ids = new HashSet<string>();
            HashSet<string> duplicates = new HashSet<string>();
            foreach (RuntimeStatusEffect status in controller.ActiveStatuses)
            {
                if (!ids.Add(status.ApplicationId))
                {
                    duplicates.Add(status.ApplicationId);
                }
            }

            lines.Add(duplicates.Count == 0 ? $"{label} duplicate status IDs: none" : $"{label} duplicate status IDs: {string.Join(", ", duplicates)}");
        }

        private static void AddReferenceDiagnostic(List<string> lines, string label, UnityEngine.Object value)
        {
            lines.Add($"{label}: {(value == null ? "Missing" : "OK")}");
        }
    }
}
#endif
