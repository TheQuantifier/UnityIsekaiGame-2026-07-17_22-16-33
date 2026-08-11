using UnityEngine;

namespace UnityIsekaiGame.WorldLocations.SceneBinding
{
    public sealed class WorldSceneBindingBootstrap : MonoBehaviour
    {
        [SerializeField] private WorldSceneBindingBootstrapMode bootstrapMode = WorldSceneBindingBootstrapMode.ProductionBindOnly;
        [SerializeField] private bool registerChildrenOnEnable = true;
        [SerializeField] private bool syncAfterRegister = true;

        public WorldSceneBindingBootstrapMode BootstrapMode => bootstrapMode;
        public WorldSceneBindingValidationReport LastReport { get; private set; }

        private void OnEnable()
        {
            if (registerChildrenOnEnable)
            {
                RegisterChildren(WorldSceneBindingRuntime.Default);
            }
        }

        public WorldSceneBindingValidationReport RegisterChildren(WorldSceneBindingRuntime runtime)
        {
            WorldSceneBindingRuntime target = runtime ?? WorldSceneBindingRuntime.Default;
            foreach (WorldSceneBindingComponent binding in GetComponentsInChildren<WorldSceneBindingComponent>(true))
            {
                binding.Register(target);
            }

            LastReport = syncAfterRegister ? target.SyncAllFromAuthoritative(true) : target.Validate();
            return LastReport;
        }
    }
}
