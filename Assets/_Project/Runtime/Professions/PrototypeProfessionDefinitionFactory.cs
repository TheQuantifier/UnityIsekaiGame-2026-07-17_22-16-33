using System;
using System.Collections.Generic;
using UnityEngine;
using UnityIsekaiGame.Knowledge.Access;

namespace UnityIsekaiGame.Professions
{
    public static class PrototypeProfessionDefinitionFactory
    {
        public const string BlacksmithProfessionId = "profession.blacksmith";
        public const string FieldMedicProfessionId = "profession.field-medic";
        public const string ScoutProfessionId = "profession.scout";
        public const string SpyProfessionId = "profession.spy";
        public const string WeaponsmithSpecializationId = "profession-specialization.blacksmith.weaponsmith";
        public const string TraumaSpecializationId = "profession-specialization.field-medic.trauma-care";
        public const string AccessPublicId = "information-access.profession.public";
        public const string AccessSecretId = "information-access.profession.secret";

        public static IReadOnlyList<ScriptableObject> CreateDefinitions()
        {
            List<ScriptableObject> definitions = new List<ScriptableObject>();
            definitions.Add(AccessPolicy(AccessPublicId, "Profession Public Access", InformationVisibilityClassification.Public, InformationDetailVisibilityPolicy.All));
            definitions.Add(AccessPolicy(AccessSecretId, "Profession Secret Access", InformationVisibilityClassification.Secret, InformationDetailVisibilityPolicy.Selected, new[] { "profession-id", "state" }, ProfessionInformationSubject.ProtectedFields));

            ProfessionSpecializationDefinition weaponsmith = Specialization(WeaponsmithSpecializationId, BlacksmithProfessionId, "Weaponsmith", ProfessionRecognitionForm.Either, new[] { "skill.smithing" }, new[] { "knowledge.subject.weapons" }, activities: new[] { "production.activity.weapon-crafting" });
            ProfessionSpecializationDefinition trauma = Specialization(TraumaSpecializationId, FieldMedicProfessionId, "Trauma Care", ProfessionRecognitionForm.Formal, new[] { "skill.healing-magic" }, new[] { "knowledge.subject.injury-treatment" }, activities: new[] { "production.activity.medical-treatment" });
            definitions.Add(weaponsmith);
            definitions.Add(trauma);

            definitions.Add(Profession(BlacksmithProfessionId, "Blacksmith", ProfessionCategory.Craft, ProfessionRecognitionForm.Either, new[] { "skill.smithing" }, new[] { "knowledge.subject.metalwork" }, activities: new[] { "production.activity.forging" }, specializations: new[] { WeaponsmithSpecializationId }, authorities: new[] { "authority.guild.prototype" }));
            definitions.Add(Profession(FieldMedicProfessionId, "Field Medic", ProfessionCategory.Medical, ProfessionRecognitionForm.Formal, new[] { "skill.healing-magic" }, new[] { "knowledge.subject.first-aid" }, activities: new[] { "production.activity.medical-treatment" }, specializations: new[] { TraumaSpecializationId }, authorities: new[] { "authority.medical.prototype" }, allowSelf: true));
            definitions.Add(Profession(ScoutProfessionId, "Scout", ProfessionCategory.Exploration, ProfessionRecognitionForm.Informal, new[] { "skill.appraisal" }, new[] { "knowledge.subject.terrain" }, activities: new[] { "production.activity.reconnaissance" }, allowFormal: false));
            definitions.Add(Profession(SpyProfessionId, "Spy", ProfessionCategory.Criminal, ProfessionRecognitionForm.Informal, new[] { "skill.appraisal" }, new[] { "knowledge.subject.secrets" }, activities: new[] { "production.activity.infiltration" }, allowFormal: false, allowSecret: true, restricted: true, accessPolicy: AccessSecretId));
            return definitions;
        }

        private static ProfessionDefinition Profession(string id, string name, ProfessionCategory category, ProfessionRecognitionForm form, string[] skills = null, string[] knowledge = null, string[] capabilities = null, string[] activities = null, string[] specializations = null, string[] authorities = null, bool allowSelf = true, bool allowFormal = true, bool allowSecret = false, bool illegal = false, bool restricted = false, string accessPolicy = AccessPublicId)
        {
            ProfessionDefinition definition = ScriptableObject.CreateInstance<ProfessionDefinition>();
            definition.name = name.Replace(" ", string.Empty) + "Profession";
            definition.DevelopmentConfigure(id, name, category, form, skills, knowledge, capabilities, activities, specializations, authorities, allowSelf, allowFormal, allowSecret, illegal, restricted, accessPolicy);
            return definition;
        }

        private static ProfessionSpecializationDefinition Specialization(string id, string parentId, string name, ProfessionRecognitionForm form, string[] skills = null, string[] knowledge = null, string[] capabilities = null, string[] activities = null, string accessPolicy = AccessPublicId)
        {
            ProfessionSpecializationDefinition definition = ScriptableObject.CreateInstance<ProfessionSpecializationDefinition>();
            definition.name = name.Replace(" ", string.Empty) + "Specialization";
            definition.DevelopmentConfigure(id, parentId, name, form, skills, knowledge, capabilities, activities, accessPolicy);
            return definition;
        }

        private static InformationAccessPolicyDefinition AccessPolicy(string id, string name, InformationVisibilityClassification visibility, InformationDetailVisibilityPolicy detailVisibility, string[] visibleDetails = null, string[] hiddenDetails = null)
        {
            InformationAccessPolicyDefinition definition = ScriptableObject.CreateInstance<InformationAccessPolicyDefinition>();
            definition.name = name.Replace(" ", string.Empty);
            definition.DevelopmentConfigure(
                id,
                name,
                InformationSubjectType.Custom,
                visibility,
                InformationDisclosurePolicy.SameAsAccess,
                visibility == InformationVisibilityClassification.Secret ? InformationResharingPolicy.NoResharing : InformationResharingPolicy.FreelyReshareable,
                visibility == InformationVisibilityClassification.Secret ? InformationSourceVisibilityPolicy.PrivilegedOnly : InformationSourceVisibilityPolicy.Reveal,
                detailVisibility,
                InformationAuditPolicy.AuditDenied,
                visibleDetails,
                null,
                hiddenDetails,
                requiresDiscovery: false,
                allowRedactedAccess: true);
            return definition;
        }
    }
}
