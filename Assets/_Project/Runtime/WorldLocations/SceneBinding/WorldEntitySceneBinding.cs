using UnityEngine;
using UnityIsekaiGame.GameData.Persistence;
using UnityIsekaiGame.Persistence;

namespace UnityIsekaiGame.WorldLocations.SceneBinding
{
    public sealed class WorldEntitySceneBinding : WorldSceneBindingComponent
    {
        [SerializeField] private LocationOccupantEntityType entityType = LocationOccupantEntityType.Person;
        [SerializeField] private string entityId;
        [SerializeField] private bool snapToGroundAfterMaterialization = true;
        [SerializeField] private float groundProbeHeight = 25f;
        [SerializeField] private float groundProbeDistance = 80f;

        public override WorldSceneBindingCategory Category => WorldSceneBindingCategory.Entity;
        public bool SnapToGroundAfterMaterialization => snapToGroundAfterMaterialization;
        public float GroundProbeHeight => groundProbeHeight;
        public float GroundProbeDistance => groundProbeDistance;
        public EntityLocationReferenceData EntityReference => new EntityLocationReferenceData { entityType = entityType, entityId = N(entityId), worldId = WorldId };

        public void ConfigureEntity(LocationOccupantEntityType type, string id, string sceneBindingKey, string scene, string world, bool snapToGround = true)
        {
            entityType = type;
            entityId = N(id);
            ConfigureBinding(EntityLocationReferenceKey.Build(type, entityId, string.IsNullOrWhiteSpace(world) ? PersistenceService.LocalWorldId : world), sceneBindingKey, scene, world, WorldSceneBindingRole.Primary, false);
            snapToGroundAfterMaterialization = snapToGround;
        }

        public override void SyncFromAuthoritative(WorldSceneBindingRuntime bindingRuntime, bool initialSync)
        {
            SceneBindingMaterializationResult result = bindingRuntime.MaterializeEntity(this);
            if (!result.Succeeded)
            {
                ApplyBindingResolution(WorldSceneBindingStatus.Degraded, result.Message);
            }
        }
    }
}
