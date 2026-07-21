#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityIsekaiGame.Combat;
using UnityIsekaiGame.Combat.CombatState;
using UnityIsekaiGame.Combat.OngoingEffects;
using UnityIsekaiGame.Contracts;
using UnityIsekaiGame.Factions;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.Inventory;
using UnityIsekaiGame.People;
using UnityIsekaiGame.Places;
using UnityIsekaiGame.Progression;
using UnityIsekaiGame.Quests;
using UnityIsekaiGame.Requirements;
using UnityIsekaiGame.Skills;
using UnityIsekaiGame.Stats;
using UnityIsekaiGame.StatusEffects;
using UnityIsekaiGame.Traits;

namespace UnityIsekaiGame.Development
{
    public sealed class PrototypeTestLabView : MonoBehaviour
    {
        private static readonly Color PanelColor = new Color(0.055f, 0.065f, 0.075f, 0.96f);
        private static readonly Color FieldColor = new Color(0.10f, 0.12f, 0.14f, 1f);
        private static readonly Color ButtonColor = new Color(0.17f, 0.25f, 0.29f, 1f);
        private static readonly Color ActiveButtonColor = new Color(0.20f, 0.42f, 0.55f, 1f);

        private static readonly string[] SectionNames =
        {
            "Overview",
            "Player",
            "Character 5.6",
            "Identity 5.1",
            "Numbers 5.4a",
            "Resources 5.4b",
            "Traits 5.5",
            "Skills 5.3",
            "Inventory",
            "Combat",
            "Combat State 6.5",
            "Lifecycle 6.3",
            "Ongoing 6.4",
            "Statuses",
            "Quests",
            "Persistence",
            "Location",
            "World Entities",
            "Scenarios",
            "Diagnostics"
        };

        private PrototypeTestLabService service;
        private InputField quantityInput;
        private InputField amountInput;
        private InputField attackHitChanceInput;
        private InputField attackHitRollInput;
        private InputField attackCriticalChanceInput;
        private InputField attackCriticalRollInput;
        private InputField attackCriticalMultiplierInput;
        private InputField attackDistanceInput;
        private InputField attackMaximumRangeInput;
        private InputField ongoingAmountInput;
        private InputField ongoingIntervalInput;
        private InputField ongoingDurationInput;
        private InputField ongoingTickCountInput;
        private InputField ongoingStackInput;
        private Text overviewText;
        private Text latestOperationText;
        private Text diagnosticsText;
        private Text historyText;
        private Text locationText;
        private Text worldEntityText;
        private Text persistenceText;
        private Text persistenceIntegrationText;
        private Text characterSystemText;
        private Text identityProgressionText;
        private Text attributesCalculatedStatsText;
        private Text resourcesText;
        private Text lifecycleText;
        private Text combatStateText;
        private Text ongoingEffectsText;
        private Text traitsText;
        private Text skillsText;
        private Text itemValueText;
        private Text skillValueText;
        private Text traitValueText;
        private Text requirementValueText;
        private Text statusValueText;
        private Text roleValueText;
        private Text socialStatusValueText;
        private Text currencyValueText;
        private Text damageValueText;
        private Text ongoingEffectValueText;
        private Text questValueText;
        private Text contractValueText;
        private Text placeValueText;
        private Text personValueText;
        private Text testPointValueText;
        private ScrollRect bodyScrollRect;

        private int activeSectionIndex;
        private int selectedItemIndex;
        private int selectedStatusIndex;
        private int selectedRoleIndex;
        private int selectedSocialStatusIndex;
        private int selectedCurrencyIndex;
        private int selectedDamageIndex;
        private int selectedOngoingEffectIndex;
        private int selectedQuestIndex;
        private int selectedContractIndex;
        private int selectedPlaceIndex;
        private int selectedPersonIndex;
        private int selectedTestPointIndex;
        private int selectedSkillIndex;
        private int selectedTraitIndex;
        private int selectedRequirementIndex;

        private readonly List<ItemDefinition> items = new List<ItemDefinition>();
        private readonly List<StatusEffectDefinition> statuses = new List<StatusEffectDefinition>();
        private readonly List<RoleDefinition> roles = new List<RoleDefinition>();
        private readonly List<SocialStatusDefinition> socialStatuses = new List<SocialStatusDefinition>();
        private readonly List<CurrencyDefinition> currencies = new List<CurrencyDefinition>();
        private readonly List<DamageTypeDefinition> damageTypes = new List<DamageTypeDefinition>();
        private readonly List<OngoingEffectDefinition> ongoingEffects = new List<OngoingEffectDefinition>();
        private readonly List<QuestDefinition> quests = new List<QuestDefinition>();
        private readonly List<ContractDefinition> contracts = new List<ContractDefinition>();
        private readonly List<PlaceDefinition> places = new List<PlaceDefinition>();
        private readonly List<PersonDefinition> people = new List<PersonDefinition>();
        private readonly List<PrototypeTestPoint> testPoints = new List<PrototypeTestPoint>();
        private readonly List<SkillDefinition> skills = new List<SkillDefinition>();
        private readonly List<TraitDefinition> traits = new List<TraitDefinition>();
        private readonly List<RequirementSetDefinition> requirements = new List<RequirementSetDefinition>();
        private readonly List<GameObject> sectionRoots = new List<GameObject>();
        private readonly List<Button> sectionButtons = new List<Button>();

        private void OnEnable()
        {
            if (service != null)
            {
                service.HistoryChanged += Refresh;
            }
        }

        private void OnDisable()
        {
            if (service != null)
            {
                service.HistoryChanged -= Refresh;
            }
        }

        public void Initialize(PrototypeTestLabService testLabService)
        {
            if (service != null)
            {
                service.HistoryChanged -= Refresh;
            }

            service = testLabService;
            if (service != null)
            {
                service.HistoryChanged += Refresh;
            }

            BuildUi();
            RefreshSelectors();
            Refresh();
            SetActiveSection(activeSectionIndex);
        }

        public void Refresh()
        {
            if (service == null)
            {
                return;
            }

            if (overviewText != null)
            {
                overviewText.text = service.BuildOverview();
            }

            if (locationText != null)
            {
                locationText.text = service.BuildLocationSummary();
            }

            if (worldEntityText != null)
            {
                worldEntityText.text = service.BuildWorldEntitySummary();
            }

            if (persistenceText != null)
            {
                persistenceText.text = service.BuildSaveSlotSummary();
            }

            if (persistenceIntegrationText != null)
            {
                persistenceIntegrationText.text = service.BuildPersistenceIntegrationSummary();
            }

            if (identityProgressionText != null)
            {
                identityProgressionText.text = service.BuildIdentityProgressionSummary();
            }

            if (characterSystemText != null)
            {
                characterSystemText.text = service.BuildCharacterSystemSummary(developmentView: true);
            }

            if (attributesCalculatedStatsText != null)
            {
                attributesCalculatedStatsText.text = service.BuildAttributeCalculatedStatsSummary();
            }

            if (resourcesText != null)
            {
                resourcesText.text = service.BuildCurrentResourcesSummary();
            }

            if (lifecycleText != null)
            {
                lifecycleText.text = service.BuildLifecycleSummary();
            }

            if (combatStateText != null)
            {
                combatStateText.text = service.BuildCombatStateSummary();
            }

            if (ongoingEffectsText != null)
            {
                ongoingEffectsText.text = service.BuildOngoingEffectsSummary();
            }

            if (skillsText != null)
            {
                skillsText.text = service.BuildSkillsSummary(includeHidden: true);
            }

            if (traitsText != null)
            {
                traitsText.text = service.BuildTraitsSummary(includeHidden: true);
            }

            UpdateSelectorLabels();
            UpdateLatestOperation();
            UpdateHistory();
            UpdateSectionButtonStates();
        }

        private void BuildUi()
        {
            if (sectionRoots.Count > 0)
            {
                return;
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform root = GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = new Vector2(14f, 14f);
            root.offsetMax = new Vector2(-14f, -14f);

            GameObject layoutRoot = CreateChild("Test Lab Layout", root, typeof(VerticalLayoutGroup));
            VerticalLayoutGroup rootLayout = layoutRoot.GetComponent<VerticalLayoutGroup>();
            rootLayout.spacing = 8f;
            rootLayout.padding = new RectOffset(10, 10, 10, 10);
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = false;

            Image background = layoutRoot.AddComponent<Image>();
            background.color = PanelColor;

            AddHeader(layoutRoot.transform, font, "Development Actions - Editor/Development Builds Only");
            AddSectionTabs(layoutRoot.transform, font);
            bodyScrollRect = AddBodyScroll(layoutRoot.transform);
            latestOperationText = AddText(layoutRoot.transform, font, "Last Result: No operations yet.", 12, 44);
            latestOperationText.color = new Color(0.86f, 0.92f, 0.96f, 1f);

            Transform content = bodyScrollRect.content;
            Transform overviewSection = AddSection(content, "Overview Section");
            Transform playerSection = AddSection(content, "Player Section");
            Transform feature56Section = AddSection(content, "Character 5.6 Section");
            Transform identitySection = AddSection(content, "Identity 5.1 Section");
            Transform feature52Section = AddSection(content, "Numbers 5.4a Section");
            Transform feature54bSection = AddSection(content, "Resources 5.4b Section");
            Transform feature55Section = AddSection(content, "Traits 5.5 Section");
            Transform feature53Section = AddSection(content, "Skills 5.3 Section");
            Transform inventorySection = AddSection(content, "Inventory Section");
            Transform combatSection = AddSection(content, "Combat Section");
            Transform combatStateSection = AddSection(content, "Combat State Section");
            Transform lifecycleSection = AddSection(content, "Lifecycle Section");
            Transform ongoingEffectsSection = AddSection(content, "Ongoing Effects Section");
            Transform statusSection = AddSection(content, "Statuses Section");
            Transform questSection = AddSection(content, "Quests Section");
            Transform persistenceSection = AddSection(content, "Persistence Section");
            Transform locationSection = AddSection(content, "Location Section");
            Transform worldEntitySection = AddSection(content, "World Entities Section");
            Transform scenarioSection = AddSection(content, "Scenarios Section");
            Transform diagnosticsSection = AddSection(content, "Diagnostics Section");

            BuildOverviewSection(overviewSection, font);
            BuildPlayerSection(playerSection, font);
            BuildFeature56Section(feature56Section, font);
            BuildIdentityProgressionSection(identitySection, font);
            BuildFeature52Section(feature52Section, font);
            BuildFeature54bSection(feature54bSection, font);
            BuildFeature55Section(feature55Section, font);
            BuildFeature53Section(feature53Section, font);
            BuildInventorySection(inventorySection, font);
            BuildCombatSection(combatSection, font);
            BuildCombatStateSection(combatStateSection, font);
            BuildLifecycleSection(lifecycleSection, font);
            BuildOngoingEffectsSection(ongoingEffectsSection, font);
            BuildStatusSection(statusSection, font);
            BuildQuestSection(questSection, font);
            BuildPersistenceSection(persistenceSection, font);
            BuildLocationSection(locationSection, font);
            BuildWorldEntitySection(worldEntitySection, font);
            BuildScenarioSection(scenarioSection, font);
            BuildDiagnosticsSection(diagnosticsSection, font);
        }

        private void BuildOverviewSection(Transform parent, Font font)
        {
            itemValueText = AddSelectorRow(parent, font, "Item", () => CycleSelection(ref selectedItemIndex, items.Count, -1), () => CycleSelection(ref selectedItemIndex, items.Count, 1));
            statusValueText = AddSelectorRow(parent, font, "Status", () => CycleSelection(ref selectedStatusIndex, statuses.Count, -1), () => CycleSelection(ref selectedStatusIndex, statuses.Count, 1));
            damageValueText = AddSelectorRow(parent, font, "Damage", () => CycleSelection(ref selectedDamageIndex, damageTypes.Count, -1), () => CycleSelection(ref selectedDamageIndex, damageTypes.Count, 1));
            ongoingEffectValueText = AddSelectorRow(parent, font, "Ongoing", () => CycleSelection(ref selectedOngoingEffectIndex, ongoingEffects.Count, -1), () => CycleSelection(ref selectedOngoingEffectIndex, ongoingEffects.Count, 1));
            questValueText = AddSelectorRow(parent, font, "Quest", () => CycleSelection(ref selectedQuestIndex, quests.Count, -1), () => CycleSelection(ref selectedQuestIndex, quests.Count, 1));
            contractValueText = AddSelectorRow(parent, font, "Contract", () => CycleSelection(ref selectedContractIndex, contracts.Count, -1), () => CycleSelection(ref selectedContractIndex, contracts.Count, 1));
            placeValueText = AddSelectorRow(parent, font, "Place", () => CycleSelection(ref selectedPlaceIndex, places.Count, -1), () => CycleSelection(ref selectedPlaceIndex, places.Count, 1));
            personValueText = AddSelectorRow(parent, font, "Person", () => CycleSelection(ref selectedPersonIndex, people.Count, -1), () => CycleSelection(ref selectedPersonIndex, people.Count, 1));
            testPointValueText = AddSelectorRow(parent, font, "Test Point", () => CycleSelection(ref selectedTestPointIndex, testPoints.Count, -1), () => CycleSelection(ref selectedTestPointIndex, testPoints.Count, 1));
            quantityInput = AddInputRow(parent, font, "Quantity", "1");
            amountInput = AddInputRow(parent, font, "Amount", "25");
            AddButtonRow(parent, font,
                ("Refresh", Refresh),
                ("Restore Vitals", () => service.RestoreVitals()),
                ("Run Diagnostics", () => RunDiagnostics()));
            overviewText = AddText(parent, font, "Overview", 13, 220);
        }

        private void BuildPlayerSection(Transform parent, Font font)
        {
            AddButtonRow(parent, font,
                ("Damage Player", () => service.DamagePlayer(GetInt(amountInput, 25))),
                ("Heal Player", () => service.HealPlayer(GetInt(amountInput, 25))),
                ("Set Health", () => service.SetHealth(GetInt(amountInput, 100))));
            AddButtonRow(parent, font,
                ("Drain Mana", () => service.DrainMana(GetFloat(amountInput, 25f))),
                ("Drain Stamina", () => service.DrainStamina(GetFloat(amountInput, 25f))),
                ("Restore Vitals", () => service.RestoreVitals()));
        }

        private void BuildFeature56Section(Transform parent, Font font)
        {
            AddButtonRow(parent, font,
                ("Initialize", () => service.InitializeCharacterSystem()),
                ("Full Rebuild", () => service.RebuildCharacterSystem()),
                ("Integrity", () => service.ValidateCharacterSystemIntegrity()),
                ("Snapshot", () => service.SnapshotCharacterSystem()));
            AddButtonRow(parent, font,
                ("Refresh", Refresh),
                ("Diagnostics", () => RunDiagnostics()));
            characterSystemText = AddText(parent, font, "Character System not available.", 12, 420);
        }

        private void BuildIdentityProgressionSection(Transform parent, Font font)
        {
            roleValueText = AddSelectorRow(parent, font, "Role", () => CycleSelection(ref selectedRoleIndex, roles.Count, -1), () => CycleSelection(ref selectedRoleIndex, roles.Count, 1));
            socialStatusValueText = AddSelectorRow(parent, font, "Social", () => CycleSelection(ref selectedSocialStatusIndex, socialStatuses.Count, -1), () => CycleSelection(ref selectedSocialStatusIndex, socialStatuses.Count, 1));
            currencyValueText = AddSelectorRow(parent, font, "Currency", () => CycleSelection(ref selectedCurrencyIndex, currencies.Count, -1), () => CycleSelection(ref selectedCurrencyIndex, currencies.Count, 1));
            AddButtonRow(parent, font,
                ("Validate IDs", () => service.ValidateIdentityProgression()),
                ("Generate Origin", () => service.GenerateOrigin(GetInt(amountInput, 0))),
                ("Duplicate Proof", () => service.ProveOriginAssignmentIsOnceOnly()),
                ("Reset Identity", () => service.ResetIdentityProgression(confirmed: false)));
            AddButtonRow(parent, font,
                ("Advance Gift", () => service.AdvanceBirthGiftProgress(GetFloat(amountInput, 300f))),
                ("Awaken Gift", () => service.ForceBirthGiftAwakening()));
            AddButtonRow(parent, font,
                ("Add Role", () => service.AddRole(GetSelected(roles, selectedRoleIndex), acceptConflicts: false)),
                ("Accept Conflict", () => service.AddRole(GetSelected(roles, selectedRoleIndex), acceptConflicts: true)),
                ("Suspend Role", () => service.SuspendFirstActiveRole()),
                ("Revoke Role", () => service.RevokeFirstActiveRole()),
                ("Abandon Role", () => service.AbandonFirstActiveRole()));
            AddButtonRow(parent, font,
                ("Add Global Status", () => service.AddGlobalSocialStatus(GetSelected(socialStatuses, selectedSocialStatusIndex))),
                ("Add Place Status", () => service.AddPlaceSocialStatus(GetSelected(socialStatuses, selectedSocialStatusIndex), GetSelected(places, selectedPlaceIndex))),
                ("Resolve Status", () => service.ResolveFirstActiveSocialStatus()));
            AddButtonRow(parent, font,
                ("Add Money", () => service.AddCurrency(GetSelected(currencies, selectedCurrencyIndex), GetInt(amountInput, 25))),
                ("Spend Money", () => service.SpendCurrency(GetSelected(currencies, selectedCurrencyIndex), GetInt(amountInput, 25))));
            AddButtonRow(parent, font,
                ("Record Success", () => service.RecordSuccessfulActivity(GetFloat(amountInput, 0.5f))),
                ("Record Failure", () => service.RecordFailedActivity(GetFloat(amountInput, 0.5f))),
                ("Participation", () => service.RecordParticipation()));
            identityProgressionText = AddText(parent, font, "Identity/progression not available.", 12, 320);
        }

        private void BuildFeature52Section(Transform parent, Font font)
        {
            AddButtonRow(parent, font,
                ("Base +Str", () => service.AddStrengthTraining()),
                ("Base +All", () => service.AddBalancedAttributeTraining()),
                ("Strength 100+", () => service.SetStrengthAboveHundred()),
                ("Invalid Base", () => service.AttemptInvalidAttributeGrowth()));
            AddButtonRow(parent, font,
                ("Add Phys Power", () => service.AddPhysicalPowerFlat()),
                ("Defense Penalty", () => service.AddPhysicalDefensePenalty()),
                ("Clear 5.4a", () => service.ClearFeature52Contributions()),
                ("Rebuild Stats", () => service.RecalculateFeature52Stats()));
            attributesCalculatedStatsText = AddText(parent, font, "Base Attributes and Calculated Stats not available.", 12, 360);
        }

        private void BuildFeature53Section(Transform parent, Font font)
        {
            skillValueText = AddSelectorRow(parent, font, "Skill", () => CycleSelection(ref selectedSkillIndex, skills.Count, -1), () => CycleSelection(ref selectedSkillIndex, skills.Count, 1));
            AddButtonRow(parent, font,
                ("Valid Use", () => service.SimulateSkillAction(GetSelected(skills, selectedSkillIndex), executed: true, succeeded: true)),
                ("Missed Use", () => service.SimulateSkillAction(GetSelected(skills, selectedSkillIndex), executed: true, succeeded: false)),
                ("Blocked", () => service.SimulateSkillAction(GetSelected(skills, selectedSkillIndex), executed: false, succeeded: false)));
            AddButtonRow(parent, font,
                ("Duplicate", () => service.TestDuplicateSkillAction(GetSelected(skills, selectedSkillIndex))),
                ("Many Uses", () => service.SimulateManySkillActions(GetSelected(skills, selectedSkillIndex), GetInt(quantityInput, 25))),
                ("Grant F", () => service.GrantSkill(GetSelected(skills, selectedSkillIndex), SkillGrade.F)));
            AddButtonRow(parent, font,
                ("Grant C", () => service.GrantSkill(GetSelected(skills, selectedSkillIndex), SkillGrade.C)),
                ("Grant A", () => service.GrantSkill(GetSelected(skills, selectedSkillIndex), SkillGrade.A)),
                ("Grant AAA", () => service.GrantSkill(GetSelected(skills, selectedSkillIndex), SkillGrade.AAA)));
            AddButtonRow(parent, font,
                ("XP +1", () => service.AwardSkillXp(GetSelected(skills, selectedSkillIndex), 1)),
                ("XP Many", () => service.AwardSkillXp(GetSelected(skills, selectedSkillIndex), GetInt(quantityInput, 25))),
                ("Rebuild", () => service.RebuildSkillEffects()),
                ("Clear Skills", () => service.ClearSkillDevelopmentState(confirmed: false)));
            skillsText = AddText(parent, font, "Skills not available.", 12, 360);
        }

        private void BuildFeature55Section(Transform parent, Font font)
        {
            traitValueText = AddSelectorRow(parent, font, "Trait", () => CycleSelection(ref selectedTraitIndex, traits.Count, -1), () => CycleSelection(ref selectedTraitIndex, traits.Count, 1));
            requirementValueText = AddSelectorRow(parent, font, "Requirement", () => CycleSelection(ref selectedRequirementIndex, requirements.Count, -1), () => CycleSelection(ref selectedRequirementIndex, requirements.Count, 1));
            AddButtonRow(parent, font,
                ("Grant Active", () => service.GrantTrait(GetSelected(traits, selectedTraitIndex), TraitLifecycleState.Active, TraitDiscoveryState.Discovered)),
                ("Grant Dormant", () => service.GrantTrait(GetSelected(traits, selectedTraitIndex), TraitLifecycleState.Dormant, TraitDiscoveryState.Undiscovered)),
                ("Duplicate", () => service.GrantTraitDuplicateProof(GetSelected(traits, selectedTraitIndex))),
                ("Second Source", () => service.GrantTraitSecondSource(GetSelected(traits, selectedTraitIndex))));
            AddButtonRow(parent, font,
                ("Remove Source", () => service.RemoveTraitTestLabSource(GetSelected(traits, selectedTraitIndex))),
                ("Activate", () => service.ActivateTrait(GetSelected(traits, selectedTraitIndex))),
                ("Suppress", () => service.SuppressTrait(GetSelected(traits, selectedTraitIndex))),
                ("Unsuppress", () => service.UnsuppressTrait(GetSelected(traits, selectedTraitIndex))));
            AddButtonRow(parent, font,
                ("Suspect", () => service.SetTraitSuspected(GetSelected(traits, selectedTraitIndex))),
                ("Discover", () => service.SetTraitDiscovered(GetSelected(traits, selectedTraitIndex))),
                ("Replace", () => service.ReplaceTrait(GetSelected(traits, selectedTraitIndex))),
                ("Rebuild", () => service.RebuildTraitEffects()));
            AddButtonRow(parent, font,
                ("Evaluate Req", () => service.EvaluateRequirement(GetSelected(requirements, selectedRequirementIndex))),
                ("Save Snapshot", () => service.SnapshotTraitsForPersistence()));
            traitsText = AddText(parent, font, "Traits not available.", 12, 380);
        }

        private void BuildFeature54bSection(Transform parent, Font font)
        {
            AddButtonRow(parent, font,
                ("Reconcile", () => service.ReconcileResources()),
                ("Duplicate Event", () => service.ProveResourceDuplicateEvent()),
                ("Regen Tick", () => service.TickResourceRegeneration()),
                ("Save Snapshot", () => service.SnapshotResourcesForPersistence()));
            AddButtonRow(parent, font,
                ("Damage Health", () => service.DamagePlayer(GetInt(amountInput, 25))),
                ("Heal Health", () => service.HealPlayer(GetInt(amountInput, 25))),
                ("Drain Mana", () => service.DrainMana(GetFloat(amountInput, 25f))),
                ("Drain Stamina", () => service.DrainStamina(GetFloat(amountInput, 25f))));
            resourcesText = AddText(parent, font, "Current Resources not available.", 12, 360);
        }

        private void BuildInventorySection(Transform parent, Font font)
        {
            AddButtonRow(parent, font,
                ("Grant Item", () => service.GrantItem(GetSelected(items, selectedItemIndex), GetInt(quantityInput, 1))),
                ("Grant Stateful", () => service.GrantStatefulItem(GetSelected(items, selectedItemIndex))),
                ("Remove Item", () => service.RemoveItem(GetSelected(items, selectedItemIndex), GetInt(quantityInput, 1))));
            AddButtonRow(parent, font,
                ("Fill Inventory", () => service.FillInventory(GetSelected(items, selectedItemIndex))),
                ("Equip Selected", () => service.EquipFirstCompatible(GetSelected(items, selectedItemIndex))),
                ("Unequip All", () => service.UnequipAll(confirmed: false)),
                ("Clear Inventory", () => service.ClearInventory(confirmed: false)));
        }

        private void BuildCombatSection(Transform parent, Font font)
        {
            AddHeader(parent, font, "Feature 6.2 Attack Resolution");
            attackHitChanceInput = AddInputRow(parent, font, "Hit Chance", "0.75");
            attackHitRollInput = AddInputRow(parent, font, "Hit Roll", "0.10");
            attackCriticalChanceInput = AddInputRow(parent, font, "Crit Chance", "0.25");
            attackCriticalRollInput = AddInputRow(parent, font, "Crit Roll", "0.10");
            attackCriticalMultiplierInput = AddInputRow(parent, font, "Crit Mult", "1.5");
            attackDistanceInput = AddInputRow(parent, font, "Distance", "1");
            attackMaximumRangeInput = AddInputRow(parent, font, "Max Range", "2");
            AddButtonRow(parent, font,
                ("Fresh Tx", () => service.GenerateAttackTransaction()),
                ("Preview P>E", () => service.PreviewAttackResolution(GetSelected(damageTypes, selectedDamageIndex), GetFloat(amountInput, 25f), GetFloat(attackHitChanceInput, 0.75f), GetFloat(attackHitRollInput, 0.1f), GetFloat(attackCriticalChanceInput, 0.25f), GetFloat(attackCriticalRollInput, 0.1f), GetFloat(attackCriticalMultiplierInput, 1.5f), GetFloat(attackDistanceInput, 1f), GetFloat(attackMaximumRangeInput, 2f), targetEnemy: true, sourcePlayer: true)),
                ("Execute P>E", () => service.ExecuteAttackResolution(GetSelected(damageTypes, selectedDamageIndex), GetFloat(amountInput, 25f), GetFloat(attackHitChanceInput, 0.75f), GetFloat(attackHitRollInput, 0.1f), GetFloat(attackCriticalChanceInput, 0.25f), GetFloat(attackCriticalRollInput, 0.1f), GetFloat(attackCriticalMultiplierInput, 1.5f), GetFloat(attackDistanceInput, 1f), GetFloat(attackMaximumRangeInput, 2f), targetEnemy: true, sourcePlayer: true, reuseTransaction: false)),
                ("Reuse Tx", () => service.ExecuteAttackResolution(GetSelected(damageTypes, selectedDamageIndex), GetFloat(amountInput, 25f), GetFloat(attackHitChanceInput, 0.75f), GetFloat(attackHitRollInput, 0.1f), GetFloat(attackCriticalChanceInput, 0.25f), GetFloat(attackCriticalRollInput, 0.1f), GetFloat(attackCriticalMultiplierInput, 1.5f), GetFloat(attackDistanceInput, 1f), GetFloat(attackMaximumRangeInput, 2f), targetEnemy: true, sourcePlayer: true, reuseTransaction: true)));
            AddButtonRow(parent, font,
                ("Preview E>P", () => service.PreviewAttackResolution(GetSelected(damageTypes, selectedDamageIndex), GetFloat(amountInput, 25f), GetFloat(attackHitChanceInput, 0.75f), GetFloat(attackHitRollInput, 0.1f), GetFloat(attackCriticalChanceInput, 0.25f), GetFloat(attackCriticalRollInput, 0.1f), GetFloat(attackCriticalMultiplierInput, 1.5f), GetFloat(attackDistanceInput, 1f), GetFloat(attackMaximumRangeInput, 2f), targetEnemy: false, sourcePlayer: false)),
                ("Execute E>P", () => service.ExecuteAttackResolution(GetSelected(damageTypes, selectedDamageIndex), GetFloat(amountInput, 25f), GetFloat(attackHitChanceInput, 0.75f), GetFloat(attackHitRollInput, 0.1f), GetFloat(attackCriticalChanceInput, 0.25f), GetFloat(attackCriticalRollInput, 0.1f), GetFloat(attackCriticalMultiplierInput, 1.5f), GetFloat(attackDistanceInput, 1f), GetFloat(attackMaximumRangeInput, 2f), targetEnemy: false, sourcePlayer: false, reuseTransaction: false)),
                ("Env>P", () => service.ExecuteEnvironmentalAttack(GetSelected(damageTypes, selectedDamageIndex), GetFloat(amountInput, 25f), GetFloat(attackHitRollInput, 0.1f))),
                ("Out Range", () => service.PreviewAttackResolution(GetSelected(damageTypes, selectedDamageIndex), GetFloat(amountInput, 25f), 0.95f, 0.1f, 0f, 0.5f, 1.5f, 999f, 1f, targetEnemy: true, sourcePlayer: true)));
            AddHeader(parent, font, "Feature 6.1 Damage/Healing");
            AddButtonRow(parent, font,
                ("Preview 6.1", () => service.PreviewPipelineDamage(GetSelected(damageTypes, selectedDamageIndex), GetFloat(amountInput, 25f), targetPlayer: true)),
                ("Damage 6.1", () => service.ApplyPipelineDamage(GetSelected(damageTypes, selectedDamageIndex), GetFloat(amountInput, 25f), targetPlayer: true)),
                ("Heal 6.1", () => service.ApplyPipelineHealing(GetFloat(amountInput, 25f), targetPlayer: true)),
                ("Duplicate 6.1", () => service.ProvePipelineDuplicate(GetSelected(damageTypes, selectedDamageIndex), GetFloat(amountInput, 25f))));
            AddButtonRow(parent, font,
                ("Damage Enemy", () => service.ApplyTypedDamage(GetSelected(damageTypes, selectedDamageIndex), GetFloat(amountInput, 25f), targetEnemy: true, sourcePlayer: true)),
                ("Damage Player", () => service.ApplyTypedDamage(GetSelected(damageTypes, selectedDamageIndex), GetFloat(amountInput, 25f), targetEnemy: false, sourcePlayer: false)),
                ("Defeat Enemy", () => service.DefeatEnemy(GetSelected(damageTypes, selectedDamageIndex))),
                ("Reset Enemy", () => service.ResetEnemy()));
        }

        private void BuildCombatStateSection(Transform parent, Font font)
        {
            AddHeader(parent, font, "Feature 6.5 Combat State / Engagements / Encounters");
            AddButtonRow(parent, font,
                ("Fresh Tx", () => service.GenerateCombatStateTransaction()),
                ("Preview", () => service.PreviewExplicitCombatEngagement()),
                ("Engage", () => service.ExecuteExplicitCombatEngagement(reuseTransaction: false)),
                ("Reuse", () => service.ExecuteExplicitCombatEngagement(reuseTransaction: true)));
            AddButtonRow(parent, font,
                ("Miss", () => service.ExecuteCombatStateAttack(GetSelected(damageTypes, selectedDamageIndex), miss: true, blocked: false, prevented: false)),
                ("Hit", () => service.ExecuteCombatStateAttack(GetSelected(damageTypes, selectedDamageIndex), miss: false, blocked: false, prevented: false)),
                ("Blocked", () => service.ExecuteCombatStateAttack(GetSelected(damageTypes, selectedDamageIndex), miss: false, blocked: true, prevented: false)),
                ("Prevented", () => service.ExecuteCombatStateAttack(GetSelected(damageTypes, selectedDamageIndex), miss: false, blocked: false, prevented: true)));
            AddButtonRow(parent, font,
                ("Advance <T", () => service.AdvanceCombatState(9.5f)),
                ("Advance T", () => service.AdvanceCombatState(0.5f)),
                ("Advance 10s", () => service.AdvanceCombatState(10f)),
                ("End Encounter", () => service.EndCurrentCombatEncounter()));
            AddButtonRow(parent, font,
                ("Exit Player", () => service.ForceCombatExit(targetEnemy: false)),
                ("Exit Enemy", () => service.ForceCombatExit(targetEnemy: true)),
                ("Kill Enemy", () => service.ExecuteDeathLifecycle(targetEnemy: true, reuseTransaction: false)),
                ("Revive Enemy", () => service.ExecuteRevivalLifecycle(targetEnemy: true, GetFloat(amountInput, 25f), reuseTransaction: false)));
            AddButtonRow(parent, font,
                ("Prep A-D", () => service.PrepareCombatStateSplitParticipants()),
                ("A-B", () => service.EngageCombatStateParticipants("A", "B")),
                ("B-C", () => service.EngageCombatStateParticipants("B", "C")),
                ("C-D", () => service.EngageCombatStateParticipants("C", "D")));
            AddButtonRow(parent, font,
                ("End B-C", () => service.EndCombatStateEngagement("B", "C", reuseTransaction: false)),
                ("Reuse Split", () => service.EndCombatStateEngagement("B", "C", reuseTransaction: true)),
                ("Process Graph", () => service.ProcessCombatStateConnectivity()),
                ("Validate", () => service.ValidateCombatStateIntegrity()));
            AddButtonRow(parent, font,
                ("Exit B", () => service.ForceCombatStateParticipantExit("B")),
                ("Exit C", () => service.ForceCombatStateParticipantExit("C")),
                ("Kill B", () => service.KillCombatStateParticipant("B")),
                ("Kill C", () => service.KillCombatStateParticipant("C")));
            combatStateText = AddText(parent, font, "Combat state not available.", 12, 320);
        }

        private void BuildLifecycleSection(Transform parent, Font font)
        {
            AddHeader(parent, font, "Feature 6.3 Defeat / Recovery / Death / Revival");
            AddButtonRow(parent, font,
                ("Fresh Tx", () => service.GenerateLifecycleTransaction()),
                ("Zero P", () => service.ApplyZeroHealthLifecycleDamage(GetSelected(damageTypes, selectedDamageIndex), targetEnemy: false)),
                ("Zero E", () => service.ApplyZeroHealthLifecycleDamage(GetSelected(damageTypes, selectedDamageIndex), targetEnemy: true)),
                ("Reuse Defeat P", () => service.ExecuteDefeatLifecycle(targetEnemy: false, reuseTransaction: true)));
            AddButtonRow(parent, font,
                ("Preview Defeat P", () => service.PreviewDefeatLifecycle(targetEnemy: false)),
                ("Defeat P", () => service.ExecuteDefeatLifecycle(targetEnemy: false, reuseTransaction: false)),
                ("Preview Defeat E", () => service.PreviewDefeatLifecycle(targetEnemy: true)),
                ("Defeat E", () => service.ExecuteDefeatLifecycle(targetEnemy: true, reuseTransaction: false)));
            AddButtonRow(parent, font,
                ("Preview Recover P", () => service.PreviewRecoveryLifecycle(false, GetFloat(amountInput, 25f))),
                ("Recover P", () => service.ExecuteRecoveryLifecycle(false, GetFloat(amountInput, 25f), reuseTransaction: false)),
                ("Preview Recover E", () => service.PreviewRecoveryLifecycle(true, GetFloat(amountInput, 25f))),
                ("Recover E", () => service.ExecuteRecoveryLifecycle(true, GetFloat(amountInput, 25f), reuseTransaction: false)));
            AddButtonRow(parent, font,
                ("Preview Death P", () => service.PreviewDeathLifecycle(targetEnemy: false)),
                ("Kill P", () => service.ExecuteDeathLifecycle(targetEnemy: false, reuseTransaction: false)),
                ("Preview Death E", () => service.PreviewDeathLifecycle(targetEnemy: true)),
                ("Kill E", () => service.ExecuteDeathLifecycle(targetEnemy: true, reuseTransaction: false)));
            AddButtonRow(parent, font,
                ("Preview Revive P", () => service.PreviewRevivalLifecycle(false, GetFloat(amountInput, 25f))),
                ("Revive P", () => service.ExecuteRevivalLifecycle(false, GetFloat(amountInput, 25f), reuseTransaction: false)),
                ("Preview Revive E", () => service.PreviewRevivalLifecycle(true, GetFloat(amountInput, 25f))),
                ("Revive E", () => service.ExecuteRevivalLifecycle(true, GetFloat(amountInput, 25f), reuseTransaction: false)));
            lifecycleText = AddText(parent, font, "Lifecycle not available.", 12, 260);
        }

        private void BuildOngoingEffectsSection(Transform parent, Font font)
        {
            AddHeader(parent, font, "Feature 6.4 Ongoing Damage / Healing / Recovery");
            ongoingAmountInput = AddInputRow(parent, font, "Tick Amount", "0");
            ongoingIntervalInput = AddInputRow(parent, font, "Interval", "0");
            ongoingDurationInput = AddInputRow(parent, font, "Duration", "0");
            ongoingTickCountInput = AddInputRow(parent, font, "Tick Count", "0");
            ongoingStackInput = AddInputRow(parent, font, "Stacks", "1");
            AddButtonRow(parent, font,
                ("Fresh Tx", () => service.GenerateOngoingEffectTransaction()),
                ("Preview P", () => service.PreviewOngoingEffect(GetSelected(ongoingEffects, selectedOngoingEffectIndex), targetEnemy: false, GetFloat(ongoingAmountInput, 0f), GetFloat(ongoingIntervalInput, 0f), GetFloat(ongoingDurationInput, 0f), GetInt(ongoingTickCountInput, 0), GetInt(ongoingStackInput, 1))),
                ("Apply P", () => service.ApplyOngoingEffect(GetSelected(ongoingEffects, selectedOngoingEffectIndex), targetEnemy: false, GetFloat(ongoingAmountInput, 0f), GetFloat(ongoingIntervalInput, 0f), GetFloat(ongoingDurationInput, 0f), GetInt(ongoingTickCountInput, 0), GetInt(ongoingStackInput, 1), reuseTransaction: false)),
                ("Reuse P", () => service.ApplyOngoingEffect(GetSelected(ongoingEffects, selectedOngoingEffectIndex), targetEnemy: false, GetFloat(ongoingAmountInput, 0f), GetFloat(ongoingIntervalInput, 0f), GetFloat(ongoingDurationInput, 0f), GetInt(ongoingTickCountInput, 0), GetInt(ongoingStackInput, 1), reuseTransaction: true)));
            AddButtonRow(parent, font,
                ("Preview E", () => service.PreviewOngoingEffect(GetSelected(ongoingEffects, selectedOngoingEffectIndex), targetEnemy: true, GetFloat(ongoingAmountInput, 0f), GetFloat(ongoingIntervalInput, 0f), GetFloat(ongoingDurationInput, 0f), GetInt(ongoingTickCountInput, 0), GetInt(ongoingStackInput, 1))),
                ("Apply E", () => service.ApplyOngoingEffect(GetSelected(ongoingEffects, selectedOngoingEffectIndex), targetEnemy: true, GetFloat(ongoingAmountInput, 0f), GetFloat(ongoingIntervalInput, 0f), GetFloat(ongoingDurationInput, 0f), GetInt(ongoingTickCountInput, 0), GetInt(ongoingStackInput, 1), reuseTransaction: false)),
                ("Stack E", () => service.ApplyOngoingEffect(GetSelected(ongoingEffects, selectedOngoingEffectIndex), targetEnemy: true, GetFloat(ongoingAmountInput, 0f), GetFloat(ongoingIntervalInput, 0f), GetFloat(ongoingDurationInput, 0f), GetInt(ongoingTickCountInput, 0), GetInt(ongoingStackInput, 1), reuseTransaction: false)),
                ("Cancel E", () => service.CancelFirstOngoingEffect(targetEnemy: true, preview: false)));
            AddButtonRow(parent, font,
                ("Tick Now", () => service.ProcessOngoingEffectsNow()),
                ("Advance <1", () => service.AdvanceOngoingEffects(Mathf.Max(0.1f, GetFloat(ongoingIntervalInput, 1f) * 0.5f))),
                ("Advance 1x", () => service.AdvanceOngoingEffects(Mathf.Max(0.1f, GetFloat(ongoingIntervalInput, 1f)))),
                ("Advance 5x", () => service.AdvanceOngoingEffects(Mathf.Max(0.1f, GetFloat(ongoingIntervalInput, 1f)) * 5f)));
            AddButtonRow(parent, font,
                ("Cancel P", () => service.CancelFirstOngoingEffect(targetEnemy: false, preview: false)),
                ("Preview Cancel P", () => service.CancelFirstOngoingEffect(targetEnemy: false, preview: true)),
                ("Save Active", () => service.Save()),
                ("Load Active", () => service.Load()));
            ongoingEffectsText = AddText(parent, font, "Ongoing effects not available.", 12, 320);
        }

        private void BuildStatusSection(Transform parent, Font font)
        {
            AddButtonRow(parent, font,
                ("Apply To Player", () => service.ApplyStatus(GetSelected(statuses, selectedStatusIndex), toEnemy: false)),
                ("Apply To Enemy", () => service.ApplyStatus(GetSelected(statuses, selectedStatusIndex), toEnemy: true)),
                ("Remove From Player", () => service.RemoveStatus(GetSelected(statuses, selectedStatusIndex), fromEnemy: false)),
                ("Clear Temp Statuses", () => service.ClearTemporaryStatuses()));
        }

        private void BuildQuestSection(Transform parent, Font font)
        {
            AddButtonRow(parent, font,
                ("Start Quest", () => service.StartQuest(GetSelected(quests, selectedQuestIndex))),
                ("Report Talk", () => service.ReportTalk(GetSelected(people, selectedPersonIndex))),
                ("Report Reach", () => service.ReportReach(GetSelected(places, selectedPlaceIndex))),
                ("Report Defeat", () => service.ReportDefeat("prototype_enemy")));
            AddButtonRow(parent, font,
                ("Accept Contract", () => service.AcceptContract(GetSelected(contracts, selectedContractIndex))),
                ("Clear Quests", () => service.ClearQuestLog(confirmed: false)),
                ("Clear Contracts", () => service.ClearContractJournal(confirmed: false)));
        }

        private void BuildPersistenceSection(Transform parent, Font font)
        {
            AddButtonRow(parent, font,
                ("Save", () => service.Save()),
                ("Load", () => service.Load()),
                ("Validate Save", () => service.ValidateSave()),
                ("Delete Save", () => service.DeleteSave(confirmed: false)));
            AddButtonRow(parent, font,
                ("Save Manual 1", () => service.SaveManualSlotOne()),
                ("Force Autosave", () => service.ForceAutosave()),
                ("Short Autosave", () => service.SetShortAutosaveInterval()));
            AddButtonRow(parent, font,
                ("Mark Dirty", () => service.MarkSaveDirty()),
                ("Mark Clean", () => service.MarkSaveClean()),
                ("Validate Backup", () => service.ValidateManualSlotOneBackup()),
                ("Load Backup", () => service.LoadManualSlotOneBackup()));
            AddButtonRow(parent, font,
                ("Fingerprint", () => service.RecordFingerprint()),
                ("Recovery Scan", () => service.RunRecoveryScan()),
                ("Promote Backup", () => service.PromoteManualSlotOneBackup(confirmed: false)),
                ("Quarantine", () => service.QuarantineManualSlotOnePrimary(confirmed: false)));
            AddButtonRow(parent, font,
                ("Clean Temps", () => service.CleanupTemporarySaves(confirmed: false)),
                ("Fail Prepare", () => service.InjectPrepareFailure()),
                ("Fail Commit", () => service.InjectCommitFailure()),
                ("Fail Audit", () => service.InjectAuditFailure()));
            persistenceText = AddText(parent, font, "Save slots not available.", 12, 260);
            persistenceIntegrationText = AddText(parent, font, "Persistence integration not available.", 12, 260);
        }

        private void BuildLocationSection(Transform parent, Font font)
        {
            AddButtonRow(parent, font,
                ("Teleport To Point", () => service.Teleport(GetSelected(testPoints, selectedTestPointIndex))),
                ("Save Location", () => service.Save()),
                ("Load Location", () => service.Load()),
                ("Validate Position", () => service.ValidateCurrentLocation()));
            locationText = AddText(parent, font, "Location not available.", 13, 210);
        }

        private void BuildWorldEntitySection(Transform parent, Font font)
        {
            AddButtonRow(parent, font,
                ("Refresh", () => service.RefreshWorldEntityDiagnostics()),
                ("Spawn Persistent", () => service.SpawnPersistentWorldLoot(GetSelected(items, selectedItemIndex))),
                ("Spawn Transient", () => service.SpawnTransientWorldLoot(GetSelected(items, selectedItemIndex))));
            AddButtonRow(parent, font,
                ("Destroy Spawned", () => service.DestroyLastSpawnedWorldLoot()),
                ("Recreate Saved", () => service.RecreateDestroyedWorldLoot()),
                ("Duplicate Proof", () => service.AttemptDuplicateWorldEntityRegistration()));
            worldEntityText = AddText(parent, font, "World entities not available.", 12, 260);
        }

        private void BuildScenarioSection(Transform parent, Font font)
        {
            AddButtonRow(parent, font,
                ("Clean", () => service.RunScenario("clean", GetSelected(items, selectedItemIndex), GetSelected(quests, selectedQuestIndex), GetSelected(contracts, selectedContractIndex), GetSelected(damageTypes, selectedDamageIndex))),
                ("Combat", () => service.RunScenario("combat", GetSelected(items, selectedItemIndex), GetSelected(quests, selectedQuestIndex), GetSelected(contracts, selectedContractIndex), GetSelected(damageTypes, selectedDamageIndex))),
                ("Full Inventory", () => service.RunScenario("full-inventory", GetSelected(items, selectedItemIndex), GetSelected(quests, selectedQuestIndex), GetSelected(contracts, selectedContractIndex), GetSelected(damageTypes, selectedDamageIndex))));
            AddButtonRow(parent, font,
                ("Quest", () => service.RunScenario("quest", GetSelected(items, selectedItemIndex), GetSelected(quests, selectedQuestIndex), GetSelected(contracts, selectedContractIndex), GetSelected(damageTypes, selectedDamageIndex))),
                ("Contract", () => service.RunScenario("contract", GetSelected(items, selectedItemIndex), GetSelected(quests, selectedQuestIndex), GetSelected(contracts, selectedContractIndex), GetSelected(damageTypes, selectedDamageIndex))),
                ("Persistence", () => service.RunScenario("persistence", GetSelected(items, selectedItemIndex), GetSelected(quests, selectedQuestIndex), GetSelected(contracts, selectedContractIndex), GetSelected(damageTypes, selectedDamageIndex))));
        }

        private void BuildDiagnosticsSection(Transform parent, Font font)
        {
            AddButtonRow(parent, font,
                ("Run Diagnostics", () => RunDiagnostics()),
                ("Refresh", Refresh));
            diagnosticsText = AddText(parent, font, "Diagnostics not run.", 13, 210);
            historyText = AddText(parent, font, "No operations yet.", 12, 260);
        }

        private void RefreshSelectors()
        {
            if (service == null)
            {
                return;
            }

            SetOptions(items, service.GetDefinitions<ItemDefinition>(), ref selectedItemIndex);
            SetOptions(statuses, service.GetDefinitions<StatusEffectDefinition>(), ref selectedStatusIndex);
            SetOptions(roles, service.GetDefinitions<RoleDefinition>(), ref selectedRoleIndex);
            SetOptions(socialStatuses, service.GetDefinitions<SocialStatusDefinition>(), ref selectedSocialStatusIndex);
            SetOptions(currencies, service.GetDefinitions<CurrencyDefinition>(), ref selectedCurrencyIndex);
            SetOptions(damageTypes, service.GetDefinitions<DamageTypeDefinition>(), ref selectedDamageIndex);
            SetOptions(ongoingEffects, service.GetDefinitions<OngoingEffectDefinition>(), ref selectedOngoingEffectIndex);
            SetOptions(quests, service.GetDefinitions<QuestDefinition>(), ref selectedQuestIndex);
            SetOptions(contracts, service.GetDefinitions<ContractDefinition>(), ref selectedContractIndex);
            SetOptions(places, service.GetDefinitions<PlaceDefinition>(), ref selectedPlaceIndex);
            SetOptions(people, service.GetDefinitions<PersonDefinition>(), ref selectedPersonIndex);
            SetOptions(testPoints, service.GetTestPoints(), ref selectedTestPointIndex);
            SetOptions(skills, service.GetDefinitions<SkillDefinition>(), ref selectedSkillIndex);
            SetOptions(traits, service.GetDefinitions<TraitDefinition>(), ref selectedTraitIndex);
            SetOptions(requirements, service.GetDefinitions<RequirementSetDefinition>(), ref selectedRequirementIndex);
        }

        private void UpdateSelectorLabels()
        {
            SetValue(itemValueText, FormatSelected(items, selectedItemIndex, PrototypeTestLabService.FormatDefinition));
            SetValue(statusValueText, FormatSelected(statuses, selectedStatusIndex, PrototypeTestLabService.FormatDefinition));
            SetValue(roleValueText, FormatSelected(roles, selectedRoleIndex, PrototypeTestLabService.FormatDefinition));
            SetValue(socialStatusValueText, FormatSelected(socialStatuses, selectedSocialStatusIndex, PrototypeTestLabService.FormatDefinition));
            SetValue(currencyValueText, FormatSelected(currencies, selectedCurrencyIndex, PrototypeTestLabService.FormatDefinition));
            SetValue(damageValueText, FormatSelected(damageTypes, selectedDamageIndex, PrototypeTestLabService.FormatDefinition));
            SetValue(ongoingEffectValueText, FormatSelected(ongoingEffects, selectedOngoingEffectIndex, PrototypeTestLabService.FormatDefinition));
            SetValue(questValueText, FormatSelected(quests, selectedQuestIndex, PrototypeTestLabService.FormatDefinition));
            SetValue(contractValueText, FormatSelected(contracts, selectedContractIndex, PrototypeTestLabService.FormatDefinition));
            SetValue(placeValueText, FormatSelected(places, selectedPlaceIndex, PrototypeTestLabService.FormatDefinition));
            SetValue(personValueText, FormatSelected(people, selectedPersonIndex, PrototypeTestLabService.FormatDefinition));
            SetValue(testPointValueText, FormatSelected(testPoints, selectedTestPointIndex, point => $"{point.DisplayName} ({point.TestPointId})"));
            SetValue(skillValueText, FormatSelected(skills, selectedSkillIndex, PrototypeTestLabService.FormatDefinition));
            SetValue(traitValueText, FormatSelected(traits, selectedTraitIndex, PrototypeTestLabService.FormatDefinition));
            SetValue(requirementValueText, FormatSelected(requirements, selectedRequirementIndex, PrototypeTestLabService.FormatDefinition));
        }

        private void UpdateHistory()
        {
            if (historyText == null)
            {
                return;
            }

            StringBuilder builder = new StringBuilder();
            foreach (PrototypeTestLabOperation operation in service.History)
            {
                builder.Append(operation.Timestamp.ToString("HH:mm:ss"));
                builder.Append(operation.Succeeded ? " OK " : " FAIL ");
                builder.Append(operation.OperationName);
                builder.Append(" [");
                builder.Append(operation.Code);
                builder.Append("] ");
                builder.AppendLine(operation.Message);
            }

            historyText.text = builder.Length == 0 ? "No operations yet." : builder.ToString().TrimEnd();
        }

        private void UpdateLatestOperation()
        {
            if (latestOperationText == null)
            {
                return;
            }

            if (service == null || service.History.Count == 0)
            {
                latestOperationText.text = "Last Result: No operations yet.";
                return;
            }

            PrototypeTestLabOperation operation = service.History[0];
            string status = operation.Succeeded ? "OK" : "FAIL";
            latestOperationText.text = $"Last Result: {status} {operation.OperationName} [{operation.Code}] {operation.Message}";
        }

        private void RunDiagnostics()
        {
            if (diagnosticsText != null)
            {
                diagnosticsText.text = service.RunDiagnostics();
            }
        }

        private static string FormatSelected<T>(IReadOnlyList<T> values, int selectedIndex, Func<T, string> formatter)
            where T : class
        {
            T selected = GetSelected(values, selectedIndex);
            return selected == null ? "None" : formatter(selected);
        }

        private static void SetOptions<T>(List<T> target, IReadOnlyList<T> values, ref int selectedIndex)
            where T : class
        {
            target.Clear();
            if (values != null)
            {
                target.AddRange(values);
            }

            selectedIndex = target.Count == 0 ? 0 : Mathf.Clamp(selectedIndex, 0, target.Count - 1);
        }

        private static T GetSelected<T>(IReadOnlyList<T> values, int selectedIndex)
            where T : class
        {
            return values == null || selectedIndex < 0 || selectedIndex >= values.Count ? null : values[selectedIndex];
        }

        private static int GetInt(InputField input, int fallback)
        {
            return input != null && int.TryParse(input.text, out int value) ? value : fallback;
        }

        private static float GetFloat(InputField input, float fallback)
        {
            return input != null && float.TryParse(input.text, out float value) ? value : fallback;
        }

        private void CycleSelection(ref int selectedIndex, int count, int direction)
        {
            if (count <= 0)
            {
                return;
            }

            selectedIndex = (selectedIndex + direction) % count;
            if (selectedIndex < 0)
            {
                selectedIndex += count;
            }

            Refresh();
        }

        private void SetActiveSection(int sectionIndex)
        {
            if (sectionIndex < 0 || sectionIndex >= sectionRoots.Count)
            {
                return;
            }

            activeSectionIndex = sectionIndex;
            for (int i = 0; i < sectionRoots.Count; i++)
            {
                sectionRoots[i].SetActive(i == activeSectionIndex);
            }

            UpdateSectionButtonStates();
            Canvas.ForceUpdateCanvases();
            if (bodyScrollRect != null)
            {
                bodyScrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private void AddSectionTabs(Transform parent, Font font)
        {
            sectionButtons.Clear();
            for (int start = 0; start < SectionNames.Length; start += 5)
            {
                GameObject row = CreateRow("Test Lab Tabs", parent, 36f);
                int end = Mathf.Min(start + 5, SectionNames.Length);
                for (int i = start; i < end; i++)
                {
                    int sectionIndex = i;
                    Button button = AddButton(row.transform, font, SectionNames[i], 10);
                    button.onClick.AddListener(() => SetActiveSection(sectionIndex));
                    sectionButtons.Add(button);
                }
            }
        }

        private void UpdateSectionButtonStates()
        {
            for (int i = 0; i < sectionButtons.Count; i++)
            {
                Image image = sectionButtons[i] == null ? null : sectionButtons[i].GetComponent<Image>();
                if (image != null)
                {
                    image.color = i == activeSectionIndex ? ActiveButtonColor : ButtonColor;
                }
            }
        }

        private Transform AddSection(Transform parent, string name)
        {
            GameObject section = CreateChild(name, parent, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            VerticalLayoutGroup layout = section.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            section.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sectionRoots.Add(section);
            return section.transform;
        }

        private ScrollRect AddBodyScroll(Transform parent)
        {
            GameObject scrollObject = CreateChild("Test Lab Body", parent, typeof(Image), typeof(Mask), typeof(ScrollRect), typeof(LayoutElement));
            scrollObject.GetComponent<Image>().color = new Color(0.025f, 0.028f, 0.035f, 0.92f);
            scrollObject.GetComponent<Mask>().showMaskGraphic = true;
            LayoutElement scrollLayout = scrollObject.GetComponent<LayoutElement>();
            scrollLayout.minHeight = 220f;
            scrollLayout.flexibleHeight = 1f;

            GameObject content = CreateChild("Test Lab Body Content", scrollObject.transform, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = new Vector2(10f, 0f);
            contentRect.offsetMax = new Vector2(-24f, -10f);
            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 10, 10);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scrollRect = scrollObject.GetComponent<ScrollRect>();
            scrollRect.content = contentRect;
            scrollRect.viewport = scrollObject.GetComponent<RectTransform>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 48f;
            scrollRect.verticalScrollbar = AddVerticalScrollbar(scrollObject.transform);
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarSpacing = -2f;
            return scrollRect;
        }

        private static Scrollbar AddVerticalScrollbar(Transform parent)
        {
            GameObject scrollbarObject = CreateChild("Vertical Scrollbar", parent, typeof(Image), typeof(Scrollbar));
            Image track = scrollbarObject.GetComponent<Image>();
            track.color = new Color(0.08f, 0.09f, 0.10f, 0.95f);
            RectTransform scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = Vector2.one;
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.offsetMin = new Vector2(-12f, 4f);
            scrollbarRect.offsetMax = new Vector2(-4f, -4f);

            GameObject handleObject = CreateChild("Handle", scrollbarObject.transform, typeof(Image));
            Image handle = handleObject.GetComponent<Image>();
            handle.color = new Color(0.24f, 0.42f, 0.50f, 1f);
            Scrollbar scrollbar = scrollbarObject.GetComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.targetGraphic = handle;
            scrollbar.handleRect = handleObject.GetComponent<RectTransform>();
            return scrollbar;
        }

        private static void AddHeader(Transform parent, Font font, string text)
        {
            Text header = AddText(parent, font, text, 16, 28, FontStyle.Bold);
            header.color = new Color(0.92f, 0.95f, 0.98f, 1f);
        }

        private static Text AddSelectorRow(Transform parent, Font font, string label, Action previous, Action next)
        {
            GameObject row = CreateRow("Selector - " + label, parent, 32f);
            Text labelText = AddText(row.transform, font, label, 12, 28, FontStyle.Bold);
            SetElement(labelText.gameObject, 78f, 28f, 0f);
            Text valueText = AddText(row.transform, font, "None", 12, 28);
            valueText.color = new Color(0.86f, 0.92f, 0.96f, 1f);
            SetElement(valueText.gameObject, 0f, 28f, 1f);
            AddFixedButton(row.transform, font, "Prev", previous, 58f);
            AddFixedButton(row.transform, font, "Next", next, 58f);
            return valueText;
        }

        private static InputField AddInputRow(Transform parent, Font font, string label, string value)
        {
            GameObject row = CreateRow("Input - " + label, parent, 32f);
            Text labelText = AddText(row.transform, font, label, 12, 28, FontStyle.Bold);
            SetElement(labelText.gameObject, 78f, 28f, 0f);
            GameObject inputObject = CreateChild(label, row.transform, typeof(Image), typeof(InputField), typeof(LayoutElement));
            inputObject.GetComponent<Image>().color = FieldColor;
            SetElement(inputObject, 0f, 30f, 1f);
            Text text = AddText(inputObject.transform, font, value, 12, 28);
            text.rectTransform.offsetMin = new Vector2(8f, 2f);
            text.rectTransform.offsetMax = new Vector2(-8f, -2f);
            InputField input = inputObject.GetComponent<InputField>();
            input.textComponent = text;
            input.text = value;
            return input;
        }

        private static void AddButtonRow(Transform parent, Font font, params (string Label, Action Action)[] buttons)
        {
            GameObject row = CreateRow("Action Row", parent, 34f);
            foreach ((string label, Action action) in buttons)
            {
                Button button = AddButton(row.transform, font, label, 11);
                button.onClick.AddListener(() => action?.Invoke());
            }
        }

        private static Button AddFixedButton(Transform parent, Font font, string label, Action action, float width)
        {
            Button button = AddButton(parent, font, label, 11);
            SetElement(button.gameObject, width, 30f, 0f);
            button.onClick.AddListener(() => action?.Invoke());
            return button;
        }

        private static Button AddButton(Transform parent, Font font, string label, int fontSize)
        {
            GameObject root = CreateChild(label, parent, typeof(Image), typeof(Button), typeof(LayoutElement));
            root.GetComponent<Image>().color = ButtonColor;
            SetElement(root, 96f, 32f, 1f);
            Text text = AddText(root.transform, font, label, fontSize, 28, FontStyle.Bold);
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.rectTransform.offsetMin = new Vector2(4f, 2f);
            text.rectTransform.offsetMax = new Vector2(-4f, -2f);
            return root.GetComponent<Button>();
        }

        private static Text AddText(Transform parent, Font font, string text, int size, float height, FontStyle style = FontStyle.Normal)
        {
            GameObject obj = CreateChild(text, parent, typeof(Text), typeof(LayoutElement));
            Text label = obj.GetComponent<Text>();
            label.font = font;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = Color.white;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.text = text;
            SetElement(obj, 0f, height, 1f);
            return label;
        }

        private static GameObject CreateRow(string name, Transform parent, float height)
        {
            GameObject row = CreateChild(name, parent, typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            SetElement(row, 0f, height, 1f);
            return row;
        }

        private static void SetElement(GameObject obj, float preferredWidth, float preferredHeight, float flexibleWidth)
        {
            LayoutElement layoutElement = obj.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = obj.AddComponent<LayoutElement>();
            }

            layoutElement.preferredWidth = preferredWidth <= 0f ? -1f : preferredWidth;
            layoutElement.preferredHeight = preferredHeight;
            layoutElement.flexibleWidth = flexibleWidth;
            layoutElement.flexibleHeight = 0f;
        }

        private static void SetValue(Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        private static GameObject CreateChild(string name, Transform parent, params Type[] components)
        {
            GameObject obj = new GameObject(name, components);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            return obj;
        }
    }
}
#endif
