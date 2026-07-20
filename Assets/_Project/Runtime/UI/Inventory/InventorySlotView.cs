using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityIsekaiGame.UI.Inventory
{
    public sealed class InventorySlotView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private Text itemNameText;
        [SerializeField] private Text quantityText;
        [SerializeField] private string emptyLabel = "Empty";
        [SerializeField] private Color normalColor = new Color(0.18f, 0.2f, 0.22f, 0.95f);
        [SerializeField] private Color selectedColor = new Color(0.35f, 0.52f, 0.68f, 0.95f);

        private int slotIndex = -1;
        private System.Action<int> selected;
        private System.Action<int, bool> hovered;

        private void Awake()
        {
            ApplyTextLayout();
        }

        private void OnValidate()
        {
            ApplyTextLayout();
        }

        public void Render(UnityIsekaiGame.Inventory.InventorySlot slot)
        {
            ApplyTextLayout();

            if (slot == null || slot.IsEmpty)
            {
                RenderEmpty();
                return;
            }

            if (itemNameText != null)
            {
                itemNameText.text = slot.Item.DisplayName;
            }

            if (quantityText != null)
            {
                quantityText.text = slot.Quantity.ToString();
            }

            if (iconImage != null)
            {
                iconImage.sprite = slot.Item.Icon;
                iconImage.enabled = slot.Item.Icon != null;
            }
        }

        public void RenderEmpty()
        {
            ApplyTextLayout();

            if (itemNameText != null)
            {
                itemNameText.text = emptyLabel;
            }

            if (quantityText != null)
            {
                quantityText.text = string.Empty;
            }

            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }
        }

        public void Initialize(int index, System.Action<int> onSelected, System.Action<int, bool> onHovered = null)
        {
            slotIndex = index;
            selected = onSelected;
            hovered = onHovered;
            ResolveBackgroundImage();
        }

        public void SetSelected(bool isSelected)
        {
            ResolveBackgroundImage();

            if (backgroundImage != null)
            {
                backgroundImage.color = isSelected ? selectedColor : normalColor;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            selected?.Invoke(slotIndex);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered?.Invoke(slotIndex, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered?.Invoke(slotIndex, false);
        }

        private void ApplyTextLayout()
        {
            ResolveBackgroundImage();
            ConfigureNameText();
            ConfigureQuantityText();
        }

        private void ResolveBackgroundImage()
        {
            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
            }
        }

        private void ConfigureNameText()
        {
            if (itemNameText == null)
            {
                return;
            }

            itemNameText.horizontalOverflow = HorizontalWrapMode.Wrap;
            itemNameText.verticalOverflow = VerticalWrapMode.Truncate;
            itemNameText.alignment = TextAnchor.MiddleCenter;

            RectTransform rectTransform = itemNameText.rectTransform;
            rectTransform.anchorMin = new Vector2(0f, 0.42f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.offsetMin = new Vector2(8f, 8f);
            rectTransform.offsetMax = new Vector2(-8f, -8f);
        }

        private void ConfigureQuantityText()
        {
            if (quantityText == null)
            {
                return;
            }

            quantityText.horizontalOverflow = HorizontalWrapMode.Overflow;
            quantityText.verticalOverflow = VerticalWrapMode.Truncate;
            quantityText.alignment = TextAnchor.LowerRight;

            RectTransform rectTransform = quantityText.rectTransform;
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0.42f);
            rectTransform.offsetMin = new Vector2(78f, 8f);
            rectTransform.offsetMax = new Vector2(-8f, -6f);
        }
    }
}
