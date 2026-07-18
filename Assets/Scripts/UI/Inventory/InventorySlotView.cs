using UnityEngine;
using UnityEngine.UI;

namespace UnityIsekaiGame.UI.Inventory
{
    public sealed class InventorySlotView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Text itemNameText;
        [SerializeField] private Text quantityText;
        [SerializeField] private string emptyLabel = "Empty";

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

        private void ApplyTextLayout()
        {
            ConfigureNameText();
            ConfigureQuantityText();
        }

        private void ConfigureNameText()
        {
            if (itemNameText == null)
            {
                return;
            }

            itemNameText.horizontalOverflow = HorizontalWrapMode.Wrap;
            itemNameText.verticalOverflow = VerticalWrapMode.Truncate;
            itemNameText.alignment = TextAnchor.UpperLeft;

            RectTransform rectTransform = itemNameText.rectTransform;
            rectTransform.anchorMin = new Vector2(0f, 0.48f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.offsetMin = new Vector2(78f, 8f);
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
