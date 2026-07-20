using System.Collections;
using UnityEngine;
using UnityIsekaiGame.Equipment;
using UnityIsekaiGame.Inventory;

namespace UnityIsekaiGame.Combat
{
    public sealed class FirstPersonWeaponPresenter : MonoBehaviour
    {
        [SerializeField] private PlayerEquipment equipment;
        [SerializeField] private PlayerMeleeCombat combat;
        [SerializeField] private GameObject weaponRoot;
        [SerializeField] private Transform swingRoot;
        [SerializeField] private Vector3 swingEulerOffset = new Vector3(25f, -20f, 10f);
        [SerializeField, Min(0.01f)] private float swingDuration = 0.18f;

        private Coroutine swingRoutine;
        private Quaternion restingRotation;

        private void Awake()
        {
            if (equipment == null)
            {
                equipment = GetComponentInParent<PlayerEquipment>();
            }

            if (combat == null)
            {
                combat = GetComponentInParent<PlayerMeleeCombat>();
            }

            if (swingRoot == null && weaponRoot != null)
            {
                swingRoot = weaponRoot.transform;
            }

            restingRotation = swingRoot == null ? Quaternion.identity : swingRoot.localRotation;
            RefreshVisibility();
        }

        private void OnEnable()
        {
            if (equipment != null)
            {
                equipment.EquipmentChanged += RefreshVisibility;
            }

            if (combat != null)
            {
                combat.AttackResolved += OnAttackResolved;
            }

            RefreshVisibility();
        }

        private void OnDisable()
        {
            if (equipment != null)
            {
                equipment.EquipmentChanged -= RefreshVisibility;
            }

            if (combat != null)
            {
                combat.AttackResolved -= OnAttackResolved;
            }
        }

        private void RefreshVisibility()
        {
            if (weaponRoot == null)
            {
                return;
            }

            EquipmentSlotState mainHand = equipment == null ? null : equipment.GetSlot(EquipmentSlotType.MainHand);
            ItemDefinition item = mainHand == null ? null : mainHand.Item;
            bool shouldShow = item != null
                && item.IsEquippable
                && item.Equipment.SlotType == EquipmentSlotType.MainHand
                && item.Equipment.MeleeWeapon != null
                && item.Equipment.MeleeWeapon.IsWeapon;

            weaponRoot.SetActive(shouldShow);
        }

        private void OnAttackResolved(MeleeAttackResult result)
        {
            if (!result.Started || weaponRoot == null || !weaponRoot.activeInHierarchy || swingRoot == null)
            {
                return;
            }

            if (swingRoutine != null)
            {
                StopCoroutine(swingRoutine);
            }

            swingRoutine = StartCoroutine(Swing());
        }

        private IEnumerator Swing()
        {
            Quaternion swingRotation = restingRotation * Quaternion.Euler(swingEulerOffset);
            float halfDuration = swingDuration * 0.5f;

            for (float elapsed = 0f; elapsed < halfDuration; elapsed += Time.deltaTime)
            {
                swingRoot.localRotation = Quaternion.Slerp(restingRotation, swingRotation, elapsed / halfDuration);
                yield return null;
            }

            for (float elapsed = 0f; elapsed < halfDuration; elapsed += Time.deltaTime)
            {
                swingRoot.localRotation = Quaternion.Slerp(swingRotation, restingRotation, elapsed / halfDuration);
                yield return null;
            }

            swingRoot.localRotation = restingRotation;
            swingRoutine = null;
        }
    }
}
