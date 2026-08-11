using UnityEngine;

namespace UnityIsekaiGame.WorldLocations.SceneBinding
{
    public sealed class LocationSceneBinding : WorldSceneBindingComponent
    {
        [SerializeField] private string locationDefinitionId;

        public override WorldSceneBindingCategory Category => WorldSceneBindingCategory.Location;
        public string LocationDefinitionId => locationDefinitionId ?? string.Empty;

        public void ConfigureLocation(string locationId, string sceneBindingKey, string scene, string world, string expectedDefinitionId = "", WorldSceneBindingRole bindingRole = WorldSceneBindingRole.Primary, bool requiredBinding = false)
        {
            ConfigureBinding(locationId, sceneBindingKey, scene, world, bindingRole, requiredBinding);
            locationDefinitionId = N(expectedDefinitionId);
        }
    }
}
