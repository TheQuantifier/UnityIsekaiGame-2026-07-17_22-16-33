using UnityEngine;
using UnityIsekaiGame.Equipment;
using UnityIsekaiGame.Input;
using UnityIsekaiGame.Inventory;
using UnityIsekaiGame.Magic;

namespace UnityIsekaiGame.UI.Inventory
{
    public sealed class InventoryScreenController : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private PlayerEquipment equipment;
        [SerializeField] private PlayerSpellLoadout spellLoadout;
        [SerializeField] private InventoryScreenView view;
        [SerializeField] private SpellManagementView spellManagementView;
        [SerializeField] private GameObject itemUser;
        [SerializeField, Min(1)] private int columns = 4;

        private CursorLockMode previousLockState;
        private bool previousCursorVisible;
        private bool isOpen;
        private int selectedSlotIndex;
        private EquipmentSlotType selectedEquipmentSlot;
        private int selectedKnownSpellIndex;

        private void Awake()
        {
            if (equipment == null && inventory != null)
            {
                equipment = inventory.GetComponent<PlayerEquipment>();
            }

            if (spellLoadout == null && inventory != null)
            {
                spellLoadout = inventory.GetComponent<PlayerSpellLoadout>();
            }

            if (itemUser == null && inventory != null)
            {
                itemUser = inventory.gameObject;
            }

            if (view != null)
            {
                view.Initialize(SelectSlot, UseSelectedItem, SelectEquipmentSlot, EquipSelectedItem, UnequipSelectedEquipment);
            }

            if (spellManagementView != null)
            {
                spellManagementView.Initialize(SelectKnownSpell, AssignSelectedSpellToSlot, ClearSpellSlot);
            }

            Close(false);
            Refresh();
        }

        private void OnEnable()
        {
            if (inventory != null)
            {
                inventory.InventoryChanged += Refresh;
            }

            if (equipment != null)
            {
                equipment.EquipmentChanged += Refresh;
            }

            if (spellLoadout != null)
            {
                spellLoadout.SlotChanged += OnSpellSlotChanged;
                spellLoadout.ActiveSlotChanged += OnActiveSpellSlotChanged;
            }
        }

        private void Update()
        {
            if (input == null)
            {
                return;
            }

            if (input.ConsumeInventory())
            {
                SetOpen(!isOpen);
                return;
            }

            if (isOpen && input.ConsumeCancel())
            {
                SetOpen(false);
                return;
            }

            if (!isOpen)
            {
                return;
            }

            if (input.ConsumeInventoryNavigate(out Vector2 direction))
            {
                MoveSelection(direction);
            }

            if (input.ConsumeInventoryUse())
            {
                UseSelectedItem();
            }
        }

        private void OnDisable()
        {
            if (inventory != null)
            {
                inventory.InventoryChanged -= Refresh;
            }

            if (equipment != null)
            {
                equipment.EquipmentChanged -= Refresh;
            }

            if (spellLoadout != null)
            {
                spellLoadout.SlotChanged -= OnSpellSlotChanged;
                spellLoadout.ActiveSlotChanged -= OnActiveSpellSlotChanged;
            }

            if (isOpen)
            {
                Close(true);
            }
        }

        private void SetOpen(bool open)
        {
            if (open)
            {
                Open();
                return;
            }

            Close(true);
        }

        private void Open()
        {
            if (isOpen)
            {
                return;
            }

            previousLockState = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            isOpen = true;

            if (input != null)
            {
                input.SetGameplayInputBlocked(true);
                input.ClearCancel();
                input.ClearInventoryUiActions();
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Refresh();

            if (view != null)
            {
                view.Show();
            }
        }

        private void Close(bool restoreCursor)
        {
            if (input != null)
            {
                input.ClearGameplayActionQueues();
                input.ClearInventoryUiActions();
                input.SetGameplayInputBlocked(false);
            }

            if (view != null)
            {
                view.Hide();
            }

            if (restoreCursor)
            {
                Cursor.lockState = previousLockState;
                Cursor.visible = previousCursorVisible;
            }

            isOpen = false;
        }

        private void Refresh()
        {
            if (view != null && inventory != null)
            {
                view.Render(inventory.Slots);
                view.RenderEquipment(equipment == null ? null : equipment.Slots);
                ClampSelection();
                view.SetSelectedSlot(selectedSlotIndex);
                view.SetSelectedEquipmentSlot(selectedEquipmentSlot);
                UpdateEquipmentActions();
            }

            if (spellManagementView != null)
            {
                ClampKnownSpellSelection();
                spellManagementView.Render(spellLoadout, selectedKnownSpellIndex);
            }
        }

        public void UseSelectedItem()
        {
            if (!isOpen || inventory == null)
            {
                return;
            }

            ItemUseResult result = inventory.UseItem(selectedSlotIndex, itemUser);
            if (!result.Succeeded)
            {
                Debug.Log(result.Message);
            }

            if (view != null)
            {
                view.SetFeedback(result.Message);
            }

            Refresh();
        }

        public void EquipSelectedItem()
        {
            if (!isOpen || equipment == null)
            {
                return;
            }

            EquipmentOperationResult result = equipment.EquipFromInventorySlot(selectedSlotIndex);
            Debug.Log(result.Message);

            if (view != null)
            {
                view.SetFeedback(result.Message);
            }

            Refresh();
        }

        public void UnequipSelectedEquipment()
        {
            if (!isOpen || equipment == null)
            {
                return;
            }

            EquipmentOperationResult result = equipment.Unequip(selectedEquipmentSlot);
            Debug.Log(result.Message);

            if (view != null)
            {
                view.SetFeedback(result.Message);
            }

            Refresh();
        }

        private void SelectSlot(int slotIndex)
        {
            selectedSlotIndex = Mathf.Max(0, slotIndex);

            if (view != null)
            {
                view.SetSelectedSlot(selectedSlotIndex);
                view.SetFeedback(string.Empty);
                UpdateEquipmentActions();
            }
        }

        private void SelectEquipmentSlot(EquipmentSlotType slotType)
        {
            selectedEquipmentSlot = slotType;

            if (view != null)
            {
                view.SetSelectedEquipmentSlot(selectedEquipmentSlot);
                view.SetFeedback(string.Empty);
                UpdateEquipmentActions();
            }
        }

        private void MoveSelection(Vector2 direction)
        {
            if (view == null || view.SlotCount <= 0)
            {
                return;
            }

            int delta;
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                delta = direction.x > 0f ? 1 : -1;
            }
            else
            {
                delta = direction.y > 0f ? -columns : columns;
            }

            selectedSlotIndex = Mathf.Clamp(selectedSlotIndex + delta, 0, view.SlotCount - 1);
            view.SetSelectedSlot(selectedSlotIndex);
            view.SetFeedback(string.Empty);
            UpdateEquipmentActions();
        }

        private void ClampSelection()
        {
            int slotCount = view == null ? 0 : view.SlotCount;
            selectedSlotIndex = slotCount <= 0 ? 0 : Mathf.Clamp(selectedSlotIndex, 0, slotCount - 1);
        }

        private void UpdateEquipmentActions()
        {
            if (view == null)
            {
                return;
            }

            InventorySlot selectedInventorySlot = inventory == null ? null : inventory.GetSlot(selectedSlotIndex);
            bool canEquip = selectedInventorySlot != null
                && !selectedInventorySlot.IsEmpty
                && selectedInventorySlot.Item != null
                && selectedInventorySlot.Item.IsEquippable;

            EquipmentSlotState selectedEquipment = equipment == null ? null : equipment.GetSlot(selectedEquipmentSlot);
            bool canUnequip = selectedEquipment != null && !selectedEquipment.IsEmpty;

            view.SetEquipmentActions(canEquip, canUnequip);
        }

        private void SelectKnownSpell(int knownSpellIndex)
        {
            selectedKnownSpellIndex = Mathf.Max(0, knownSpellIndex);
            if (view != null)
            {
                view.SetFeedback(string.Empty);
            }

            Refresh();
        }

        private void AssignSelectedSpellToSlot(int slotIndex)
        {
            if (!isOpen || spellLoadout == null)
            {
                return;
            }

            SpellDefinition spell = selectedKnownSpellIndex >= 0 && selectedKnownSpellIndex < spellLoadout.KnownSpells.Count
                ? spellLoadout.KnownSpells[selectedKnownSpellIndex]
                : null;
            SpellLoadoutOperationResult result = spellLoadout.AssignSpell(slotIndex, spell);
            Debug.Log(result.Message);
            view?.SetFeedback(result.Message);
            Refresh();
        }

        private void ClearSpellSlot(int slotIndex)
        {
            if (!isOpen || spellLoadout == null)
            {
                return;
            }

            SpellLoadoutOperationResult result = spellLoadout.ClearSlot(slotIndex);
            Debug.Log(result.Message);
            view?.SetFeedback(result.Message);
            Refresh();
        }

        private void ClampKnownSpellSelection()
        {
            int knownCount = spellLoadout == null || spellLoadout.KnownSpells == null ? 0 : spellLoadout.KnownSpells.Count;
            selectedKnownSpellIndex = knownCount <= 0 ? 0 : Mathf.Clamp(selectedKnownSpellIndex, 0, knownCount - 1);
        }

        private void OnSpellSlotChanged(SpellLoadoutSlotChangedEventArgs args)
        {
            Refresh();
        }

        private void OnActiveSpellSlotChanged(int slotIndex, SpellDefinition spell)
        {
            Refresh();
        }
    }
}
