using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityIsekaiGame.GameData;

namespace UnityIsekaiGame.Social.Decisions
{
    [CreateAssetMenu(fileName = "SocialIntentionDefinition", menuName = "Unity Isekai Game/Social/Social Intention Definition")]
    public sealed class SocialIntentionDefinition : ScriptableObject, IGameDefinition, IDefinitionCatalogValidationParticipant
    {
        [SerializeField] private string intentionId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private SocialIntentionCategory category = SocialIntentionCategory.Custom;
        [SerializeField] private string[] eligibleInteractionDefinitionIds = Array.Empty<string>();
        [SerializeField] private int basePriority = 100;
        [SerializeField] private double cooldownSeconds = 10d;
        [SerializeField] private bool requiresTarget = true;
        [SerializeField] private bool allowNoInteractionSelection;
        [SerializeField] private string[] considerationIds = Array.Empty<string>();
        [SerializeField] private string[] tags = Array.Empty<string>();

        public string Id => intentionId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Description => description ?? string.Empty;
        public SocialIntentionCategory Category => category;
        public IReadOnlyList<string> EligibleInteractionDefinitionIds => eligibleInteractionDefinitionIds ?? Array.Empty<string>();
        public int BasePriority => basePriority;
        public double CooldownSeconds => Math.Max(0d, cooldownSeconds);
        public bool RequiresTarget => requiresTarget;
        public bool AllowNoInteractionSelection => allowNoInteractionSelection;
        public IReadOnlyList<string> ConsiderationIds => considerationIds ?? Array.Empty<string>();
        public IReadOnlyList<string> Tags => tags ?? Array.Empty<string>();

        public void DevelopmentConfigure(string id, string name, SocialIntentionCategory intentionCategory, IEnumerable<string> interactionIds, int priority, double cooldown, bool targetRequired, bool noInteraction, IEnumerable<string> considerations, IEnumerable<string> tagIds)
        {
            intentionId = id?.Trim();
            displayName = string.IsNullOrWhiteSpace(name) ? id : name.Trim();
            description = string.Empty;
            category = intentionCategory;
            eligibleInteractionDefinitionIds = Clean(interactionIds);
            basePriority = priority;
            cooldownSeconds = Math.Max(0d, cooldown);
            requiresTarget = targetRequired;
            allowNoInteractionSelection = noInteraction;
            considerationIds = Clean(considerations);
            tags = Clean(tagIds);
        }

        public void ValidateCatalogDefinition(IReadOnlyDictionary<string, IGameDefinition> definitionsById, DefinitionValidationReport report)
        {
            if (report == null) return;
            if (string.IsNullOrWhiteSpace(Id)) report.AddError($"Social Intention '{name}' is missing a stable ID.");
            if (!Enum.IsDefined(typeof(SocialIntentionCategory), category)) report.AddError($"Social Intention '{DisplayName}' has an invalid category.");
            if (basePriority < 0) report.AddError($"Social Intention '{DisplayName}' has a negative base priority.");
            if (double.IsNaN(cooldownSeconds) || double.IsInfinity(cooldownSeconds) || cooldownSeconds < 0d) report.AddError($"Social Intention '{DisplayName}' has an invalid cooldown.");
            if (!allowNoInteractionSelection && (eligibleInteractionDefinitionIds == null || eligibleInteractionDefinitionIds.Length == 0)) report.AddError($"Social Intention '{DisplayName}' must declare an interaction or allow no-interaction selection.");
            foreach (string interactionId in eligibleInteractionDefinitionIds ?? Array.Empty<string>())
            {
                if (!definitionsById.ContainsKey(interactionId)) report.AddError($"Social Intention '{DisplayName}' references missing Social Interaction '{interactionId}'.");
            }
            foreach (string considerationId in considerationIds ?? Array.Empty<string>())
            {
                if (!definitionsById.ContainsKey(considerationId)) report.AddError($"Social Intention '{DisplayName}' references missing Social Consideration '{considerationId}'.");
            }
        }

        private static string[] Clean(IEnumerable<string> values) => (values ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
    }

    [CreateAssetMenu(fileName = "SocialDecisionProfileDefinition", menuName = "Unity Isekai Game/Social/Social Decision Profile Definition")]
    public sealed class SocialDecisionProfileDefinition : ScriptableObject, IGameDefinition, IDefinitionCatalogValidationParticipant
    {
        [SerializeField] private string profileId;
        [SerializeField] private string displayName;
        [SerializeField] private string[] enabledIntentionIds = Array.Empty<string>();
        [SerializeField] private string[] considerationIds = Array.Empty<string>();
        [SerializeField] private double evaluationIntervalSeconds = 15d;
        [SerializeField] private int maximumTargets = 8;
        [SerializeField] private int maximumIntentions = 8;
        [SerializeField] private int maximumCandidates = 24;
        [SerializeField] private int maximumDiagnostics = 24;
        [SerializeField] private int scoreThreshold = 100;
        [SerializeField] private int maximumActionsPerWindow = 1;
        [SerializeField] private double actionWindowSeconds = 30d;
        [SerializeField] private bool allowPlayerControlled;
        [SerializeField] private SocialDecisionExecutionMode defaultExecutionMode = SocialDecisionExecutionMode.EvaluateOnly;
        [SerializeField] private string[] tags = Array.Empty<string>();

        public string Id => profileId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public IReadOnlyList<string> EnabledIntentionIds => enabledIntentionIds ?? Array.Empty<string>();
        public IReadOnlyList<string> ConsiderationIds => considerationIds ?? Array.Empty<string>();
        public double EvaluationIntervalSeconds => Math.Max(0d, evaluationIntervalSeconds);
        public int MaximumTargets => Math.Max(0, maximumTargets);
        public int MaximumIntentions => Math.Max(0, maximumIntentions);
        public int MaximumCandidates => Math.Max(0, maximumCandidates);
        public int MaximumDiagnostics => Math.Max(0, maximumDiagnostics);
        public int ScoreThreshold => scoreThreshold;
        public int MaximumActionsPerWindow => Math.Max(0, maximumActionsPerWindow);
        public double ActionWindowSeconds => Math.Max(0d, actionWindowSeconds);
        public bool AllowPlayerControlled => allowPlayerControlled;
        public SocialDecisionExecutionMode DefaultExecutionMode => defaultExecutionMode;
        public IReadOnlyList<string> Tags => tags ?? Array.Empty<string>();

        public void DevelopmentConfigure(string id, string name, IEnumerable<string> intentions, IEnumerable<string> considerations, double interval, int maxTargets, int maxIntentions, int maxCandidates, int threshold, SocialDecisionExecutionMode mode, bool playersAllowed, IEnumerable<string> tagIds)
        {
            profileId = id?.Trim();
            displayName = string.IsNullOrWhiteSpace(name) ? id : name.Trim();
            enabledIntentionIds = Clean(intentions);
            considerationIds = Clean(considerations);
            evaluationIntervalSeconds = Math.Max(0d, interval);
            maximumTargets = Math.Max(0, maxTargets);
            maximumIntentions = Math.Max(0, maxIntentions);
            maximumCandidates = Math.Max(0, maxCandidates);
            maximumDiagnostics = Math.Max(maxCandidates, 1);
            scoreThreshold = threshold;
            defaultExecutionMode = mode;
            allowPlayerControlled = playersAllowed;
            maximumActionsPerWindow = 1;
            actionWindowSeconds = Math.Max(interval, 1d);
            tags = Clean(tagIds);
        }

        public void ValidateCatalogDefinition(IReadOnlyDictionary<string, IGameDefinition> definitionsById, DefinitionValidationReport report)
        {
            if (report == null) return;
            if (string.IsNullOrWhiteSpace(Id)) report.AddError($"Social Decision Profile '{name}' is missing a stable ID.");
            if (evaluationIntervalSeconds < 0d || double.IsNaN(evaluationIntervalSeconds) || double.IsInfinity(evaluationIntervalSeconds)) report.AddError($"Social Decision Profile '{DisplayName}' has an invalid evaluation interval.");
            if (maximumTargets < 0 || maximumIntentions < 0 || maximumCandidates < 0 || maximumActionsPerWindow < 0) report.AddError($"Social Decision Profile '{DisplayName}' has invalid limits.");
            foreach (string intentionId in enabledIntentionIds ?? Array.Empty<string>())
            {
                if (!definitionsById.ContainsKey(intentionId)) report.AddError($"Social Decision Profile '{DisplayName}' references missing Social Intention '{intentionId}'.");
            }
            foreach (string considerationId in considerationIds ?? Array.Empty<string>())
            {
                if (!definitionsById.ContainsKey(considerationId)) report.AddError($"Social Decision Profile '{DisplayName}' references missing Social Consideration '{considerationId}'.");
            }
        }

        private static string[] Clean(IEnumerable<string> values) => (values ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
    }

    [CreateAssetMenu(fileName = "SocialConsiderationDefinition", menuName = "Unity Isekai Game/Social/Social Consideration Definition")]
    public sealed class SocialConsiderationDefinition : ScriptableObject, IGameDefinition, IDefinitionCatalogValidationParticipant
    {
        [SerializeField] private string considerationId;
        [SerializeField] private string displayName;
        [SerializeField] private SocialDecisionConsiderationInput input = SocialDecisionConsiderationInput.Constant;
        [SerializeField] private SocialDecisionResponseCurve responseCurve = SocialDecisionResponseCurve.Linear;
        [SerializeField] private SocialDecisionMissingDataPolicy missingDataPolicy = SocialDecisionMissingDataPolicy.Neutral;
        [SerializeField] private int inputMinimum;
        [SerializeField] private int inputMaximum = 100;
        [SerializeField] private int weight = 100;
        [SerializeField] private bool required;
        [SerializeField] private string[] tags = Array.Empty<string>();

        public string Id => considerationId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public SocialDecisionConsiderationInput Input => input;
        public SocialDecisionResponseCurve ResponseCurve => responseCurve;
        public SocialDecisionMissingDataPolicy MissingDataPolicy => missingDataPolicy;
        public int InputMinimum => inputMinimum;
        public int InputMaximum => inputMaximum;
        public int Weight => weight;
        public bool Required => required;
        public IReadOnlyList<string> Tags => tags ?? Array.Empty<string>();

        public void DevelopmentConfigure(string id, string name, SocialDecisionConsiderationInput source, SocialDecisionResponseCurve curve, int minimum, int maximum, int authoredWeight, SocialDecisionMissingDataPolicy missingPolicy, bool requiredInput, IEnumerable<string> tagIds)
        {
            considerationId = id?.Trim();
            displayName = string.IsNullOrWhiteSpace(name) ? id : name.Trim();
            input = source;
            responseCurve = curve;
            inputMinimum = minimum;
            inputMaximum = maximum;
            weight = authoredWeight;
            missingDataPolicy = missingPolicy;
            required = requiredInput;
            tags = Clean(tagIds);
        }

        public void ValidateCatalogDefinition(IReadOnlyDictionary<string, IGameDefinition> definitionsById, DefinitionValidationReport report)
        {
            if (report == null) return;
            if (string.IsNullOrWhiteSpace(Id)) report.AddError($"Social Consideration '{name}' is missing a stable ID.");
            if (!Enum.IsDefined(typeof(SocialDecisionConsiderationInput), input)) report.AddError($"Social Consideration '{DisplayName}' has an unknown input source.");
            if (!Enum.IsDefined(typeof(SocialDecisionResponseCurve), responseCurve)) report.AddError($"Social Consideration '{DisplayName}' has an unknown response curve.");
            if (!Enum.IsDefined(typeof(SocialDecisionMissingDataPolicy), missingDataPolicy)) report.AddError($"Social Consideration '{DisplayName}' has an unknown missing-data policy.");
            if (inputMaximum <= inputMinimum) report.AddError($"Social Consideration '{DisplayName}' has an invalid input range.");
            if (weight < -1000 || weight > 1000) report.AddError($"Social Consideration '{DisplayName}' has an out-of-range weight.");
        }

        private static string[] Clean(IEnumerable<string> values) => (values ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
    }
}
