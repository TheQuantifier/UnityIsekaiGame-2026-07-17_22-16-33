using UnityEngine;
using UnityIsekaiGame.Combat;
using UnityIsekaiGame.Contracts;
using UnityIsekaiGame.Dialogue;
using UnityIsekaiGame.Input;
using UnityIsekaiGame.Loot;
using UnityIsekaiGame.Magic;
using UnityIsekaiGame.StatusEffects;
using UnityIsekaiGame.UI.Inventory;

namespace UnityIsekaiGame.Gameplay
{
    public sealed class PrototypeTestController : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Transform player;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerStamina playerStamina;
        [SerializeField] private PlayerMana playerMana;
        [SerializeField] private PlayerMeleeCombat playerMeleeCombat;
        [SerializeField] private PlayerSpellcaster playerSpellcaster;
        [SerializeField] private StatusEffectController playerStatusEffects;
        [SerializeField] private DialogueController dialogueController;
        [SerializeField] private InventoryScreenController inventoryScreenController;
        [SerializeField] private Transform playerSpawnPoint;
        [SerializeField] private Transform prototypeEnemy;
        [SerializeField] private EnemyHealth enemyHealth;
        [SerializeField] private EnemyMeleeAttack enemyAttack;
        [SerializeField] private PrototypeEnemyController enemyController;
        [SerializeField] private EnemyContractTargetReporter enemyContractTargetReporter;
        [SerializeField] private EnemyLootDrop enemyLootDrop;
        [SerializeField] private StatusEffectController enemyStatusEffects;

        private Vector3 fallbackPlayerSpawnPosition;
        private Quaternion fallbackPlayerSpawnRotation;
        private Vector3 enemyStartPosition;
        private Quaternion enemyStartRotation;

        private void Awake()
        {
            if (input == null)
            {
                input = FindAnyObjectByType<PlayerInputReader>();
            }

            if (player == null && playerHealth != null)
            {
                player = playerHealth.transform;
            }

            if (playerHealth == null && player != null)
            {
                playerHealth = player.GetComponent<PlayerHealth>();
            }

            if (playerStamina == null && player != null)
            {
                playerStamina = player.GetComponent<PlayerStamina>();
            }

            if (playerMana == null && player != null)
            {
                playerMana = player.GetComponent<PlayerMana>();
            }

            if (playerMeleeCombat == null && player != null)
            {
                playerMeleeCombat = player.GetComponent<PlayerMeleeCombat>();
            }

            if (playerSpellcaster == null && player != null)
            {
                playerSpellcaster = player.GetComponent<PlayerSpellcaster>();
            }

            if (playerStatusEffects == null && player != null)
            {
                playerStatusEffects = player.GetComponent<StatusEffectController>();
            }

            if (dialogueController == null)
            {
                dialogueController = FindAnyObjectByType<DialogueController>();
            }

            if (inventoryScreenController == null)
            {
                inventoryScreenController = FindAnyObjectByType<InventoryScreenController>();
            }

            if (prototypeEnemy == null && enemyHealth != null)
            {
                prototypeEnemy = enemyHealth.transform;
            }

            if (enemyHealth == null && prototypeEnemy != null)
            {
                enemyHealth = prototypeEnemy.GetComponent<EnemyHealth>();
            }

            if (enemyAttack == null && prototypeEnemy != null)
            {
                enemyAttack = prototypeEnemy.GetComponent<EnemyMeleeAttack>();
            }

            if (enemyController == null && prototypeEnemy != null)
            {
                enemyController = prototypeEnemy.GetComponent<PrototypeEnemyController>();
            }

            if (enemyContractTargetReporter == null && prototypeEnemy != null)
            {
                enemyContractTargetReporter = prototypeEnemy.GetComponent<EnemyContractTargetReporter>();
            }

            if (enemyLootDrop == null && prototypeEnemy != null)
            {
                enemyLootDrop = prototypeEnemy.GetComponent<EnemyLootDrop>();
            }

            if (enemyStatusEffects == null && prototypeEnemy != null)
            {
                enemyStatusEffects = prototypeEnemy.GetComponent<StatusEffectController>();
            }

            fallbackPlayerSpawnPosition = player == null ? Vector3.zero : player.position;
            fallbackPlayerSpawnRotation = player == null ? Quaternion.identity : player.rotation;
            enemyStartPosition = prototypeEnemy == null ? Vector3.zero : prototypeEnemy.position;
            enemyStartRotation = prototypeEnemy == null ? Quaternion.identity : prototypeEnemy.rotation;
        }

        private void Update()
        {
            if (input != null && input.ConsumePrototypeReset())
            {
                ResetPrototypeState();
            }
        }

        public void ResetPrototypeState()
        {
            dialogueController?.EndDialogue();
            inventoryScreenController?.CloseForPrototypeReset();
            ResetPlayerPosition();
            playerStatusEffects?.ClearTemporaryStatuses();
            playerHealth?.ResetToMaximum();
            playerStamina?.RestoreToMaximum();
            playerMana?.RestoreToMaximum();
            playerMeleeCombat?.ResetCooldown();
            playerSpellcaster?.ResetSpellcasting();
            input?.SetDefeatedInputBlocked(false);
            input?.ClearGameplayActionQueues();

            ResetEnemy();
            Debug.Log("Prototype reset complete.");
            PrototypeHudMessageBus.Show("Prototype reset complete");
        }

        private void ResetPlayerPosition()
        {
            if (player == null)
            {
                return;
            }

            CharacterController characterController = player.GetComponent<CharacterController>();
            if (characterController != null)
            {
                characterController.enabled = false;
            }

            Transform spawn = playerSpawnPoint;
            player.SetPositionAndRotation(
                spawn == null ? fallbackPlayerSpawnPosition : spawn.position,
                spawn == null ? fallbackPlayerSpawnRotation : spawn.rotation);

            if (characterController != null)
            {
                characterController.enabled = true;
            }
        }

        private void ResetEnemy()
        {
            if (prototypeEnemy != null)
            {
                prototypeEnemy.SetPositionAndRotation(enemyStartPosition, enemyStartRotation);
            }

            enemyAttack?.ResetCooldown();
            enemyController?.ResetControllerState();
            enemyContractTargetReporter?.ResetReporter();
            enemyLootDrop?.ResetLootState();
            enemyStatusEffects?.ClearTemporaryStatuses();
            enemyHealth?.ResetToMaximum();
        }
    }
}
