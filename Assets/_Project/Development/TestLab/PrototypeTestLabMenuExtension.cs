#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityIsekaiGame.ActorLifecycle;
using UnityIsekaiGame.Combat;
using UnityIsekaiGame.Combat.CombatState;
using UnityIsekaiGame.Combat.OngoingEffects;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.Gameplay;
using UnityIsekaiGame.Magic;
using UnityIsekaiGame.ResourceSystem;
using UnityIsekaiGame.StatusEffects;
using UnityIsekaiGame.UI.Inventory;

namespace UnityIsekaiGame.Development
{
    public sealed class PrototypeTestLabMenuExtension : MonoBehaviour, IInventoryMenuExtension
    {
        private const string ExtensionKey = "development.test-lab";

        [SerializeField] private InventoryScreenView menuView;
        [SerializeField] private InventoryScreenController menuController;

        private PrototypeTestLabService service;
        private PrototypeTestLabView testLabView;
        private bool registered;

        public string ExtensionId => ExtensionKey;
        public string DisplayName => "Test Lab";
        public int Order => 100;
        public bool IsAvailable => Debug.isDebugBuild || Application.isEditor;
        public bool SuppressFeedbackText => true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSceneRegistration()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RegisterLoadedScene()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            AttachToInventoryMenus();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            AttachToInventoryMenus();
        }

        private static void AttachToInventoryMenus()
        {
            InventoryScreenView[] views = Object.FindObjectsByType<InventoryScreenView>(FindObjectsInactive.Include);
            for (int i = 0; i < views.Length; i++)
            {
                if (views[i] == null || views[i].GetComponent<PrototypeTestLabMenuExtension>() != null)
                {
                    continue;
                }

                PrototypeTestLabMenuExtension extension = views[i].gameObject.AddComponent<PrototypeTestLabMenuExtension>();
                extension.menuView = views[i];
            }
        }

        private void OnEnable()
        {
            ResolveMenuReferences();

            if (menuView != null && !registered)
            {
                registered = menuView.RegisterMenuExtension(this);
            }
        }

        private void OnDisable()
        {
            if (menuView != null && registered)
            {
                menuView.UnregisterMenuExtension(this);
            }

            registered = false;
        }

        public void Initialize(InventoryMenuExtensionContext context)
        {
            ResolveMenuReferences();

            service ??= new PrototypeTestLabService();
            if (context != null && context.ContentRoot != null)
            {
                testLabView = context.ContentRoot.GetComponent<PrototypeTestLabView>();
                if (testLabView == null)
                {
                    testLabView = context.ContentRoot.gameObject.AddComponent<PrototypeTestLabView>();
                }
            }

            ConfigureService();
            testLabView?.Initialize(service);
        }

        public void Refresh()
        {
            ConfigureService();
            testLabView?.Refresh();
        }

        public void Show()
        {
            if (testLabView != null)
            {
                testLabView.gameObject.SetActive(true);
            }
        }

        public void Hide()
        {
        }

        public void Dispose()
        {
        }

        private void ResolveMenuReferences()
        {
            if (menuView == null)
            {
                menuView = GetComponent<InventoryScreenView>();
            }

            if (menuController == null && menuView != null)
            {
                menuController = menuView.GetComponent<InventoryScreenController>();
            }
        }

        private void ConfigureService()
        {
            if (service == null || menuController == null)
            {
                return;
            }

            DefinitionCatalog catalog = menuController.RuntimeDefinitionCatalog;
            PrototypePersistenceServiceBehaviour persistence = menuController.ResolveRuntimePersistence();
            EnemyHealth enemyHealth = Object.FindAnyObjectByType<EnemyHealth>();
            Transform playerTransform = menuController.ItemUser == null
                ? menuController.Inventory == null ? null : menuController.Inventory.transform
                : menuController.ItemUser.transform;
            Transform enemyTransform = enemyHealth == null ? null : enemyHealth.transform;
            CombatStateService combatState = playerTransform == null ? Object.FindAnyObjectByType<CombatStateService>() : playerTransform.GetComponentInParent<CombatStateService>(includeInactive: true);
            if (combatState == null && playerTransform != null && playerTransform.gameObject.activeInHierarchy)
            {
                combatState = playerTransform.gameObject.AddComponent<CombatStateService>();
            }

            OngoingEffectService playerOngoingEffects = playerTransform == null ? null : playerTransform.GetComponent<OngoingEffectService>() ?? playerTransform.GetComponentInParent<OngoingEffectService>(includeInactive: true);
            if (playerOngoingEffects == null && playerTransform != null && playerTransform.gameObject.activeInHierarchy)
            {
                playerOngoingEffects = playerTransform.gameObject.AddComponent<OngoingEffectService>();
            }

            OngoingEffectService enemyOngoingEffects = enemyTransform == null ? null : enemyTransform.GetComponent<OngoingEffectService>() ?? enemyTransform.GetComponentInParent<OngoingEffectService>(includeInactive: true);
            if (enemyOngoingEffects == null && enemyTransform != null && enemyTransform.gameObject.activeInHierarchy)
            {
                enemyOngoingEffects = enemyTransform.gameObject.AddComponent<OngoingEffectService>();
            }

            service.Configure(new PrototypeTestLabContext
            {
                DefinitionCatalog = catalog,
                Inventory = menuController.Inventory,
                Equipment = menuController.Equipment,
                PlayerStats = menuController.PlayerStats,
                PlayerHealth = menuController.PlayerHealth,
                PlayerMana = menuController.PlayerMana,
                PlayerStamina = menuController.PlayerStamina,
                PlayerAttributes = menuController.PlayerStats == null ? null : menuController.PlayerStats.CharacterAttributes,
                PlayerCalculatedStats = menuController.PlayerStats == null ? null : menuController.PlayerStats.CalculatedStats,
                PlayerResources = playerTransform == null ? null : playerTransform.GetComponentInParent<CharacterResourceCollection>(),
                PlayerLifecycle = playerTransform == null ? null : playerTransform.GetComponentInParent<ActorLifecycleController>(),
                CombatState = combatState,
                PlayerOngoingEffects = playerOngoingEffects,
                PlayerSkills = menuController.RuntimeSkills,
                PlayerTraits = menuController.RuntimeTraits,
                CharacterSystem = menuController.RuntimeCharacterSystem,
                PlayerStatuses = menuController.StatusEffects,
                IdentityProgression = menuController.IdentityProgression,
                Spellcaster = playerTransform == null ? null : playerTransform.GetComponentInParent<PlayerSpellcaster>(),
                SpellLoadout = menuController.SpellLoadout,
                QuestLog = menuController.QuestLog,
                ContractJournal = menuController.ContractJournal,
                TestController = Object.FindAnyObjectByType<PrototypeTestController>(),
                Persistence = persistence,
                PlayerTransform = playerTransform,
                EnemyHealth = enemyHealth,
                EnemyController = enemyTransform == null ? null : enemyTransform.GetComponent<PrototypeEnemyController>(),
                EnemyAttack = enemyTransform == null ? null : enemyTransform.GetComponent<EnemyMeleeAttack>(),
                EnemyLifecycle = enemyTransform == null ? null : enemyTransform.GetComponent<ActorLifecycleController>(),
                EnemyOngoingEffects = enemyOngoingEffects,
                EnemyStatuses = enemyTransform == null ? null : enemyTransform.GetComponent<StatusEffectController>(),
                EnemyTransform = enemyTransform
            });
        }
    }
}
#endif
