using NUnit.Framework;
using UnityIsekaiGame.GameData;

namespace UnityIsekaiGame.Tests
{
    public sealed class DefinitionIdValidatorTests
    {
        [Test]
        public void Validate_AcceptsNormalizedNamespacedId()
        {
            DefinitionIdValidationResult result = DefinitionIdValidator.Validate("item.health-potion");

            Assert.That(result.IsValid, Is.True, result.GetSummary());
            Assert.That(result.WarningCount, Is.Zero);
        }

        [Test]
        public void Validate_AcceptsLegacyUnderscoreIdWithNamespaceWarning()
        {
            DefinitionIdValidationResult result = DefinitionIdValidator.Validate("health_potion");

            Assert.That(result.IsValid, Is.True, result.GetSummary());
            Assert.That(result.WarningCount, Is.EqualTo(1));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase(" item.health-potion")]
        [TestCase("Item.HealthPotion")]
        [TestCase("item health potion")]
        [TestCase("item..health-potion")]
        [TestCase("item.health-potion!")]
        public void Validate_RejectsInvalidIds(string id)
        {
            DefinitionIdValidationResult result = DefinitionIdValidator.Validate(id);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorCount, Is.GreaterThan(0));
        }

        [Test]
        public void CreateNormalizedSuggestion_TrimsLowercasesAndReplacesUnsupportedCharacters()
        {
            string suggestion = DefinitionIdValidator.CreateNormalizedSuggestion(" Item Health Potion! ");

            Assert.That(suggestion, Is.EqualTo("item-health-potion"));
        }
    }
}
