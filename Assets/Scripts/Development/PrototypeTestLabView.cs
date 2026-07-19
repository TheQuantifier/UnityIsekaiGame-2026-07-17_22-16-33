#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityIsekaiGame.Combat;
using UnityIsekaiGame.Contracts;
using UnityIsekaiGame.Factions;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.Inventory;
using UnityIsekaiGame.People;
using UnityIsekaiGame.Places;
using UnityIsekaiGame.Quests;
using UnityIsekaiGame.StatusEffects;

namespace UnityIsekaiGame.Development
{
    public sealed class PrototypeTestLabView : MonoBehaviour
    {
        private PrototypeTestLabService service;
        private Dropdown sectionDropdown;
        private Dropdown itemDropdown;
        private Dropdown statusDropdown;
        private Dropdown damageDropdown;
        private Dropdown questDropdown;
        private Dropdown contractDropdown;
        private Dropdown placeDropdown;
        private Dropdown personDropdown;
        private InputField quantityInput;
        private InputField amountInput;
        private Text overviewText;
        private Text diagnosticsText;
        private Text historyText;
        private readonly List<ItemDefinition> items = new List<ItemDefinition>();
        private readonly List<StatusEffectDefinition> statuses = new List<StatusEffectDefinition>();
        private readonly List<DamageTypeDefinition> damageTypes = new List<DamageTypeDefinition>();
        private readonly List<QuestDefinition> quests = new List<QuestDefinition>();
        private readonly List<ContractDefinition> contracts = new List<ContractDefinition>();
        private readonly List<PlaceDefinition> places = new List<PlaceDefinition>();
        private readonly List<PersonDefinition> people = new List<PersonDefinition>();

        private void OnEnable()
        {
            if (service != null)
            {
                service.HistoryChanged += Refresh;
            }
        }

        private void OnDisable()
        {
            if (service != null)
            {
                service.HistoryChanged -= Refresh;
            }
        }

        public void Initialize(PrototypeTestLabService testLabService)
        {
            if (service != null)
            {
                service.HistoryChanged -= Refresh;
            }

            service = testLabService;
            if (service != null)
            {
                service.HistoryChanged += Refresh;
            }

            BuildUi();
            RefreshSelectors();
            Refresh();
        }

        public void Refresh()
        {
            if (service == null)
            {
                return;
            }

            if (overviewText != null)
            {
                overviewText.text = service.BuildOverview();
            }

            if (historyText != null)
            {
                StringBuilder builder = new StringBuilder();
                foreach (PrototypeTestLabOperation operation in service.History)
                {
                    builder.Append(operation.Timestamp.ToString("HH:mm:ss"));
                    builder.Append(operation.Succeeded ? " OK " : " FAIL ");
                    builder.Append(operation.OperationName);
                    builder.Append(" [");
                    builder.Append(operation.Code);
                    builder.Append("] ");
                    builder.AppendLine(operation.Message);
                }

                historyText.text = builder.Length == 0 ? "No operations yet." : builder.ToString().TrimEnd();
            }
        }

        private void BuildUi()
        {
            if (sectionDropdown != null)
            {
                return;
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform root = gameObject.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = new Vector2(18f, 18f);
            root.offsetMax = new Vector2(-18f, -18f);

            GameObject scrollObject = CreateChild("Test Lab Scroll", root, typeof(ScrollRect), typeof(Image));
            RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = Vector2.zero;
            scrollRectTransform.anchorMax = Vector2.one;
            scrollRectTransform.offsetMin = Vector2.zero;
            scrollRectTransform.offsetMax = Vector2.zero;
            scrollObject.GetComponent<Image>().color = new Color(0.055f, 0.065f, 0.075f, 0.96f);

            GameObject content = CreateChild("Test Lab Content", scrollObject.transform, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = new Vector2(8f, 0f);
            contentRect.offsetMax = new Vector2(-8f, 0f);
            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(10, 10, 10, 10);
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ScrollRect scrollRect = scrollObject.GetComponent<ScrollRect>();
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;

            AddHeader(content.transform, font, "Development Actions - Editor/Development Builds Only");
            sectionDropdown = AddDropdown(content.transform, font, new[] { "Overview", "Player", "Inventory & Equipment", "Combat & Enemy", "Statuses", "Quests & Contracts", "Persistence", "Location", "Scenarios", "Diagnostics" });
            itemDropdown = AddDropdown(content.transform, font, Array.Empty<string>());
            statusDropdown = AddDropdown(content.transform, font, Array.Empty<string>());
            damageDropdown = AddDropdown(content.transform, font, Array.Empty<string>());
            questDropdown = AddDropdown(content.transform, font, Array.Empty<string>());
            contractDropdown = AddDropdown(content.transform, font, Array.Empty<string>());
            placeDropdown = AddDropdown(content.transform, font, Array.Empty<string>());
            personDropdown = AddDropdown(content.transform, font, Array.Empty<string>());

            AddButtonRow(content.transform, font,
                ("Next Item", () => Cycle(itemDropdown, items.Count)),
                ("Next Status", () => Cycle(statusDropdown, statuses.Count)),
                ("Next Damage", () => Cycle(damageDropdown, damageTypes.Count)),
                ("Next Quest", () => Cycle(questDropdown, quests.Count)));

            AddButtonRow(content.transform, font,
                ("Next Contract", () => Cycle(contractDropdown, contracts.Count)),
                ("Next Place", () => Cycle(placeDropdown, places.Count)),
                ("Next Person", () => Cycle(personDropdown, people.Count)));

            quantityInput = AddInput(content.transform, font, "Quantity", "1");
            amountInput = AddInput(content.transform, font, "Amount", "25");

            AddButtonRow(content.transform, font,
                ("Refresh", Refresh),
                ("Diagnostics", () => diagnosticsText.text = service.RunDiagnostics()),
                ("Restore Vitals", () => service.RestoreVitals()));

            AddButtonRow(content.transform, font,
                ("Grant Item", () => service.GrantItem(GetSelected(items, itemDropdown), GetInt(quantityInput, 1))),
                ("Grant Stateful", () => service.GrantStatefulItem(GetSelected(items, itemDropdown))),
                ("Remove Item", () => service.RemoveItem(GetSelected(items, itemDropdown), GetInt(quantityInput, 1))));

            AddButtonRow(content.transform, font,
                ("Fill Inventory", () => service.FillInventory(GetSelected(items, itemDropdown))),
                ("Clear Inventory", () => service.ClearInventory(confirmed: false)),
                ("Equip Selected Def", () => service.EquipFirstCompatible(GetSelected(items, itemDropdown))),
                ("Unequip All", () => service.UnequipAll(confirmed: false)));

            AddButtonRow(content.transform, font,
                ("Damage Player", () => service.DamagePlayer(GetInt(amountInput, 25))),
                ("Heal Player", () => service.HealPlayer(GetInt(amountInput, 25))),
                ("Set Health", () => service.SetHealth(GetInt(amountInput, 100))),
                ("Drain Mana", () => service.DrainMana(GetFloat(amountInput, 25f))),
                ("Drain Stamina", () => service.DrainStamina(GetFloat(amountInput, 25f))));

            AddButtonRow(content.transform, font,
                ("Apply Status Player", () => service.ApplyStatus(GetSelected(statuses, statusDropdown), toEnemy: false)),
                ("Apply Status Enemy", () => service.ApplyStatus(GetSelected(statuses, statusDropdown), toEnemy: true)),
                ("Remove Status Player", () => service.RemoveStatus(GetSelected(statuses, statusDropdown), fromEnemy: false)),
                ("Clear Temp Statuses", () => service.ClearTemporaryStatuses()));

            AddButtonRow(content.transform, font,
                ("Damage Enemy", () => service.ApplyTypedDamage(GetSelected(damageTypes, damageDropdown), GetFloat(amountInput, 25f), targetEnemy: true, sourcePlayer: true)),
                ("Damage Player Typed", () => service.ApplyTypedDamage(GetSelected(damageTypes, damageDropdown), GetFloat(amountInput, 25f), targetEnemy: false, sourcePlayer: false)),
                ("Defeat Enemy", () => service.DefeatEnemy(GetSelected(damageTypes, damageDropdown))),
                ("Reset Enemy", () => service.ResetEnemy()));

            AddButtonRow(content.transform, font,
                ("Start Quest", () => service.StartQuest(GetSelected(quests, questDropdown))),
                ("Report Talk", () => service.ReportTalk(GetSelected(people, personDropdown))),
                ("Report Reach", () => service.ReportReach(GetSelected(places, placeDropdown))),
                ("Report Defeat", () => service.ReportDefeat("prototype_enemy")));

            AddButtonRow(content.transform, font,
                ("Clear Quests", () => service.ClearQuestLog(confirmed: false)),
                ("Accept Contract", () => service.AcceptContract(GetSelected(contracts, contractDropdown))),
                ("Clear Contracts", () => service.ClearContractJournal(confirmed: false)));

            AddButtonRow(content.transform, font,
                ("Save", () => service.Save()),
                ("Load", () => service.Load()),
                ("Validate Save", () => service.ValidateSave()),
                ("Delete Save", () => service.DeleteSave(confirmed: false)));

            AddButtonRow(content.transform, font,
                ("Scenario Clean", () => service.RunScenario("clean", GetSelected(items, itemDropdown), GetSelected(quests, questDropdown), GetSelected(contracts, contractDropdown), GetSelected(damageTypes, damageDropdown))),
                ("Scenario Combat", () => service.RunScenario("combat", GetSelected(items, itemDropdown), GetSelected(quests, questDropdown), GetSelected(contracts, contractDropdown), GetSelected(damageTypes, damageDropdown))),
                ("Scenario Full Inv", () => service.RunScenario("full-inventory", GetSelected(items, itemDropdown), GetSelected(quests, questDropdown), GetSelected(contracts, contractDropdown), GetSelected(damageTypes, damageDropdown))));

            AddButtonRow(content.transform, font,
                ("Scenario Quest", () => service.RunScenario("quest", GetSelected(items, itemDropdown), GetSelected(quests, questDropdown), GetSelected(contracts, contractDropdown), GetSelected(damageTypes, damageDropdown))),
                ("Scenario Contract", () => service.RunScenario("contract", GetSelected(items, itemDropdown), GetSelected(quests, questDropdown), GetSelected(contracts, contractDropdown), GetSelected(damageTypes, damageDropdown))),
                ("Scenario Persist", () => service.RunScenario("persistence", GetSelected(items, itemDropdown), GetSelected(quests, questDropdown), GetSelected(contracts, contractDropdown), GetSelected(damageTypes, damageDropdown))));

            overviewText = AddText(content.transform, font, "Overview", 14, 220);
            diagnosticsText = AddText(content.transform, font, "Diagnostics not run.", 14, 160);
            historyText = AddText(content.transform, font, "No operations yet.", 13, 260);
        }

        private void RefreshSelectors()
        {
            if (service == null)
            {
                return;
            }

            SetOptions(itemDropdown, items, service.GetDefinitions<ItemDefinition>());
            SetOptions(statusDropdown, statuses, service.GetDefinitions<StatusEffectDefinition>());
            SetOptions(damageDropdown, damageTypes, service.GetDefinitions<DamageTypeDefinition>());
            SetOptions(questDropdown, quests, service.GetDefinitions<QuestDefinition>());
            SetOptions(contractDropdown, contracts, service.GetDefinitions<ContractDefinition>());
            SetOptions(placeDropdown, places, service.GetDefinitions<PlaceDefinition>());
            SetOptions(personDropdown, people, service.GetDefinitions<PersonDefinition>());
        }

        private static void SetOptions<T>(Dropdown dropdown, List<T> target, IReadOnlyList<T> values)
            where T : class, IGameDefinition
        {
            target.Clear();
            if (values != null)
            {
                target.AddRange(values);
            }

            dropdown.ClearOptions();
            dropdown.AddOptions(target.Count == 0
                ? new List<string> { "None" }
                : target.ConvertAll(PrototypeTestLabService.FormatDefinition));
        }

        private static T GetSelected<T>(IReadOnlyList<T> values, Dropdown dropdown)
            where T : class
        {
            return values == null || dropdown == null || dropdown.value < 0 || dropdown.value >= values.Count ? null : values[dropdown.value];
        }

        private static int GetInt(InputField input, int fallback)
        {
            return input != null && int.TryParse(input.text, out int value) ? value : fallback;
        }

        private static float GetFloat(InputField input, float fallback)
        {
            return input != null && float.TryParse(input.text, out float value) ? value : fallback;
        }

        private static void Cycle(Dropdown dropdown, int count)
        {
            if (dropdown == null || count <= 0)
            {
                return;
            }

            dropdown.value = (dropdown.value + 1) % count;
            dropdown.RefreshShownValue();
        }

        private static void AddHeader(Transform parent, Font font, string text)
        {
            AddText(parent, font, text, 18, 32, FontStyle.Bold);
        }

        private static Text AddText(Transform parent, Font font, string text, int size, float height, FontStyle style = FontStyle.Normal)
        {
            GameObject obj = CreateChild(text, parent, typeof(Text), typeof(LayoutElement));
            Text label = obj.GetComponent<Text>();
            label.font = font;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = TextAnchor.UpperLeft;
            label.color = Color.white;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.text = text;
            obj.GetComponent<LayoutElement>().preferredHeight = height;
            return label;
        }

        private static InputField AddInput(Transform parent, Font font, string label, string value)
        {
            GameObject root = CreateChild(label, parent, typeof(Image), typeof(InputField), typeof(LayoutElement));
            root.GetComponent<Image>().color = new Color(0.11f, 0.13f, 0.15f, 1f);
            root.GetComponent<LayoutElement>().preferredHeight = 32f;
            Text text = AddText(root.transform, font, value, 14, 28);
            text.rectTransform.offsetMin = new Vector2(8f, 2f);
            text.rectTransform.offsetMax = new Vector2(-8f, -2f);
            InputField input = root.GetComponent<InputField>();
            input.textComponent = text;
            input.text = value;
            return input;
        }

        private static Dropdown AddDropdown(Transform parent, Font font, IReadOnlyList<string> options)
        {
            GameObject root = CreateChild("Dropdown", parent, typeof(Image), typeof(Dropdown), typeof(LayoutElement));
            root.GetComponent<Image>().color = new Color(0.11f, 0.13f, 0.15f, 1f);
            root.GetComponent<LayoutElement>().preferredHeight = 32f;
            Text label = AddText(root.transform, font, string.Empty, 14, 28);
            label.rectTransform.offsetMin = new Vector2(8f, 2f);
            label.rectTransform.offsetMax = new Vector2(-8f, -2f);
            Dropdown dropdown = root.GetComponent<Dropdown>();
            dropdown.captionText = label;
            dropdown.targetGraphic = root.GetComponent<Image>();
            dropdown.AddOptions(new List<string>(options));
            return dropdown;
        }

        private static void AddButtonRow(Transform parent, Font font, params (string Label, Action Action)[] buttons)
        {
            GameObject row = CreateChild("Button Row", parent, typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.GetComponent<LayoutElement>().preferredHeight = 34f;
            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            foreach ((string label, Action action) in buttons)
            {
                Button button = AddButton(row.transform, font, label);
                button.onClick.AddListener(() => action?.Invoke());
            }
        }

        private static Button AddButton(Transform parent, Font font, string label)
        {
            GameObject root = CreateChild(label, parent, typeof(Image), typeof(Button), typeof(LayoutElement));
            root.GetComponent<Image>().color = new Color(0.17f, 0.25f, 0.29f, 1f);
            root.GetComponent<LayoutElement>().preferredWidth = 150f;
            Text text = AddText(root.transform, font, label, 12, 28, FontStyle.Bold);
            text.alignment = TextAnchor.MiddleCenter;
            text.rectTransform.offsetMin = new Vector2(4f, 2f);
            text.rectTransform.offsetMax = new Vector2(-4f, -2f);
            return root.GetComponent<Button>();
        }

        private static GameObject CreateChild(string name, Transform parent, params Type[] components)
        {
            GameObject obj = new GameObject(name, components);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            return obj;
        }
    }
}
#endif
