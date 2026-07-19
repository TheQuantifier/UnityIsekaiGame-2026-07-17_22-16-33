using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityIsekaiGame.Combat;
using UnityIsekaiGame.Contracts;
using UnityIsekaiGame.Development;
using UnityIsekaiGame.Equipment;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.Gameplay;
using UnityIsekaiGame.Inventory;
using UnityIsekaiGame.Persistence;
using UnityIsekaiGame.Places;
using UnityIsekaiGame.Quests;
using UnityIsekaiGame.StatusEffects;
using UnityIsekaiGame.UI.Inventory;

namespace UnityIsekaiGame.Editor
{
    public static class PrototypeSceneUsabilitySetup
    {
        private const string ScenePath = "Assets/Scenes/PrototypeScene.unity";
        private const string MaterialsRoot = "Assets/Materials/PrototypeUsability";
        private const string GeneratedRootName = "4.4B Generated Usability Layout";

        [MenuItem("Tools/Prototype/Apply Scene Usability Layout")]
        public static void ApplySceneUsabilityLayout()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EnsureMaterialsFolder();

            Transform root = EnsureRoot("Prototype Scene");
            Transform environment = EnsureChild(root, "Environment");
            Transform zoneMarkers = EnsureChild(environment, "Zone Markers");
            Transform playerGroup = EnsureChild(root, "Player");
            Transform items = EnsureChild(root, "Items");
            Transform consumables = EnsureChild(items, "Consumables");
            Transform materials = EnsureChild(items, "Materials");
            Transform equipment = EnsureChild(items, "Equipment");
            Transform actors = EnsureChild(root, "Actors");
            Transform questContract = EnsureChild(root, "Quest and Contract");
            Transform testInteractables = EnsureChild(root, "Test Interactables");
            Transform testInfrastructure = EnsureChild(root, "Test Infrastructure");
            Transform generated = EnsureGeneratedRoot(root);

            ReparentIfFound("Prototype Player", playerGroup);
            ReparentIfFound("Prototype Player Spawn", playerGroup);
            ReparentIfFound("Prototype Enemy", actors);
            ReparentIfFound("Prototype Dialogue NPC", actors);
            ReparentIfFound("Prototype Quest Investigation Area", questContract);
            ReparentIfFound("Prototype Contract Board", questContract);
            ReparentIfFound("Prototype Health Potion Pickup", consumables);
            ReparentIfFound("Prototype Sword Pickup", equipment);
            ReparentIfFound("Prototype Helmet Pickup", equipment);
            DeleteObjectsWithPrefix("Label - Pickup -");
            DeleteObjectsWithPrefix("Label - Status Applicator");

            PositionExistingObject("Prototype Player Spawn", new Vector3(0f, 0.05f, 0f), Quaternion.Euler(0f, 0f, 0f));
            PositionExistingObject("Prototype Player", new Vector3(0f, 1.1f, 0f), Quaternion.Euler(0f, 0f, 0f));
            PositionExistingObject("Prototype Enemy", new Vector3(0f, 1f, 22f), Quaternion.Euler(0f, 180f, 0f));
            PositionExistingObject("Prototype Dialogue NPC", new Vector3(-18f, 1f, 14f), Quaternion.Euler(0f, 120f, 0f));
            PositionExistingObject("Prototype Contract Board", new Vector3(18f, 1f, 10f), Quaternion.Euler(0f, -120f, 0f));
            PositionExistingObject("Prototype Quest Investigation Area", new Vector3(0f, 0.05f, 38f), Quaternion.identity);
            RemoveColliderIfFound("Prototype Ground");

            CreateZones(zoneMarkers);
            CreateGlobalCollisionFloor(environment);
            EnsureSceneKey(root);
            EnsurePlayerTracking();
            CreateSigns(generated);
            CreatePickupGroups(consumables, materials, equipment);
            CreateStatusInteractables(testInteractables);
            CreateDeliveryTarget(questContract);
            CreateDamageDummy(actors);
            CreateTestPoints(testInfrastructure);
            WirePrototypePersistence(testInfrastructure);
            ImproveLighting();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Prototype scene usability layout applied.");
        }

        private static void CreateZones(Transform parent)
        {
            DeleteGeneratedChild(parent, "Zone - Central Hub");
            CreateZone(parent, "Zone - Inventory and Items", new Vector3(-18f, 0f, 0f), new Vector3(14f, 0.04f, 12f), Mat("Items", new Color(0.16f, 0.28f, 0.18f, 1f)));
            CreateZone(parent, "Zone - Equipment", new Vector3(-18f, 0f, -12f), new Vector3(14f, 0.04f, 9f), Mat("Equipment", new Color(0.20f, 0.21f, 0.28f, 1f)));
            CreateZone(parent, "Zone - Combat", new Vector3(0f, 0f, 22f), new Vector3(18f, 0.04f, 18f), Mat("Combat", new Color(0.34f, 0.16f, 0.16f, 1f)));
            CreateZone(parent, "Zone - Magic and Status", new Vector3(18f, 0f, -8f), new Vector3(14f, 0.04f, 12f), Mat("MagicStatus", new Color(0.17f, 0.19f, 0.34f, 1f)));
            CreateZone(parent, "Zone - Dialogue and Quests", new Vector3(-18f, 0f, 14f), new Vector3(14f, 0.04f, 10f), Mat("Quest", new Color(0.28f, 0.22f, 0.13f, 1f)));
            CreateZone(parent, "Zone - Contracts", new Vector3(18f, 0f, 10f), new Vector3(14f, 0.04f, 10f), Mat("Contracts", new Color(0.13f, 0.28f, 0.26f, 1f)));
            CreateZone(parent, "Zone - Investigation Area", new Vector3(0f, 0f, 38f), new Vector3(12f, 0.04f, 10f), Mat("Investigation", new Color(0.36f, 0.31f, 0.08f, 1f)));
            CreateZone(parent, "Zone - Persistence and Test Lab", new Vector3(18f, 0f, -20f), new Vector3(14f, 0.04f, 8f), Mat("Persistence", new Color(0.25f, 0.17f, 0.28f, 1f)));
        }

        private static void CreateSigns(Transform parent)
        {
            CreateSign(parent, "Sign - Central Hub", "Central Hub", new Vector3(0f, 2.2f, -4f), 0f);
            CreateSign(parent, "Sign - Inventory and Items", "Inventory and Items", new Vector3(-18f, 2.2f, -5f), 0f);
            CreateSign(parent, "Sign - Equipment", "Equipment", new Vector3(-18f, 2.2f, -16f), 0f);
            CreateSign(parent, "Sign - Combat", "Combat", new Vector3(0f, 2.2f, 13f), 180f);
            CreateSign(parent, "Sign - Magic and Statuses", "Magic and Statuses", new Vector3(18f, 2.2f, -14f), 0f);
            CreateSign(parent, "Sign - Dialogue and Quests", "Dialogue and Quests", new Vector3(-18f, 2.2f, 9f), 180f);
            CreateSign(parent, "Sign - Contracts", "Contracts", new Vector3(18f, 2.2f, 5f), 180f);
            CreateSign(parent, "Sign - Investigation Area", "Investigation Area", new Vector3(0f, 2.2f, 33f), 180f);
            CreateSign(parent, "Sign - Persistence Test Lab", "Persistence / Test Lab", new Vector3(18f, 2.2f, -23.5f), 0f);
        }

        private static void CreatePickupGroups(Transform consumables, Transform materialsParent, Transform equipmentParent)
        {
            ItemDefinition potion = Load<ItemDefinition>("Assets/Items/Prototype/HealthPotion.asset");
            ItemDefinition ore = Load<ItemDefinition>("Assets/Items/Prototype/PrototypeIronOre.asset");
            ItemDefinition sword = Load<ItemDefinition>("Assets/Items/Prototype/PrototypeSword.asset");
            ItemDefinition helmet = Load<ItemDefinition>("Assets/Items/Prototype/PrototypeHelmet.asset");
            RequireAsset(potion, "Health Potion");
            RequireAsset(ore, "Prototype Iron Ore");
            RequireAsset(sword, "Prototype Sword");
            RequireAsset(helmet, "Prototype Helmet");

            CreatePickup(consumables, "Pickup - Health Potion Single A", potion, 1, PrimitiveType.Capsule, new Vector3(-22f, 0.7f, -2.5f), new Vector3(0.35f, 0.7f, 0.35f), Mat("Potion", new Color(0.7f, 0.08f, 0.08f, 1f)));
            CreatePickup(consumables, "Pickup - Health Potion Single B", potion, 1, PrimitiveType.Capsule, new Vector3(-20.5f, 0.7f, -2.5f), new Vector3(0.35f, 0.7f, 0.35f), Mat("Potion", new Color(0.7f, 0.08f, 0.08f, 1f)));
            CreatePickup(consumables, "Pickup - Health Potion Single C", potion, 1, PrimitiveType.Capsule, new Vector3(-19f, 0.7f, -2.5f), new Vector3(0.35f, 0.7f, 0.35f), Mat("Potion", new Color(0.7f, 0.08f, 0.08f, 1f)));
            CreatePickup(consumables, "Pickup - Health Potion Bundle", potion, 8, PrimitiveType.Cylinder, new Vector3(-16.5f, 0.45f, -2.5f), new Vector3(0.8f, 0.35f, 0.8f), Mat("PotionBundle", new Color(0.55f, 0.05f, 0.05f, 1f)));
            CreatePickup(consumables, "Pickup - Health Potion Full Inventory Set", potion, 16, PrimitiveType.Cube, new Vector3(-13.5f, 0.45f, -2.5f), new Vector3(1f, 0.45f, 1f), Mat("PotionCrate", new Color(0.46f, 0.03f, 0.03f, 1f)));

            CreatePickup(materialsParent, "Pickup - Prototype Iron Ore A", ore, 1, PrimitiveType.Sphere, new Vector3(-22f, 0.45f, 2f), new Vector3(0.55f, 0.45f, 0.55f), Mat("Ore", new Color(0.13f, 0.13f, 0.14f, 1f)));
            CreatePickup(materialsParent, "Pickup - Prototype Iron Ore B", ore, 3, PrimitiveType.Sphere, new Vector3(-20.4f, 0.45f, 2f), new Vector3(0.7f, 0.5f, 0.7f), Mat("Ore", new Color(0.13f, 0.13f, 0.14f, 1f)));
            CreatePickup(materialsParent, "Pickup - Prototype Iron Ore Stack", ore, 12, PrimitiveType.Cube, new Vector3(-18.2f, 0.45f, 2f), new Vector3(1.1f, 0.45f, 0.8f), Mat("OreCrate", new Color(0.09f, 0.09f, 0.1f, 1f)));

            CreatePickup(equipmentParent, "Pickup - Prototype Sword Instance A", sword, 1, PrimitiveType.Cube, new Vector3(-22f, 0.55f, -12f), new Vector3(0.18f, 0.18f, 1.45f), Mat("Sword", new Color(0.48f, 0.52f, 0.56f, 1f)));
            CreatePickup(equipmentParent, "Pickup - Prototype Sword Instance B", sword, 1, PrimitiveType.Cube, new Vector3(-20f, 0.55f, -12f), new Vector3(0.18f, 0.18f, 1.45f), Mat("Sword", new Color(0.48f, 0.52f, 0.56f, 1f)));
            CreatePickup(equipmentParent, "Pickup - Prototype Helmet Instance A", helmet, 1, PrimitiveType.Sphere, new Vector3(-17.5f, 0.55f, -12f), new Vector3(0.85f, 0.45f, 0.85f), Mat("Helmet", new Color(0.22f, 0.24f, 0.28f, 1f)));
            CreatePickup(equipmentParent, "Pickup - Prototype Helmet Instance B", helmet, 1, PrimitiveType.Sphere, new Vector3(-15.5f, 0.55f, -12f), new Vector3(0.85f, 0.45f, 0.85f), Mat("Helmet", new Color(0.22f, 0.24f, 0.28f, 1f)));
        }

        private static void CreateStatusInteractables(Transform parent)
        {
            StatusEffectDefinition might = Load<StatusEffectDefinition>("Assets/StatusEffects/Prototype/PrototypeMightStatus.asset");
            StatusEffectDefinition weakened = Load<StatusEffectDefinition>("Assets/StatusEffects/Prototype/PrototypeWeakenedStatus.asset");
            RequireAsset(might, "Prototype Might status");
            RequireAsset(weakened, "Prototype Weakened status");
            CreateStatusPedestal(parent, "Status Applicator - Prototype Might", might, new Vector3(15.5f, 0.55f, -8f), Mat("Might", new Color(0.18f, 0.45f, 0.8f, 1f)));
            CreateStatusPedestal(parent, "Status Applicator - Prototype Weakened", weakened, new Vector3(20.5f, 0.55f, -8f), Mat("Weakened", new Color(0.62f, 0.18f, 0.72f, 1f)));
            CreateSign(parent, "Sign - Resistance Testing", "Use Test Lab: Arcane resistance / weakness", new Vector3(18f, 1.6f, -4.4f), 180f);
        }

        private static void CreateDeliveryTarget(Transform parent)
        {
            GameObject target = ReplaceGenerated("Prototype Delivery Crate", parent);
            target.transform.SetPositionAndRotation(new Vector3(21.5f, 0.5f, 12.5f), Quaternion.Euler(0f, -25f, 0f));
            target.transform.localScale = new Vector3(1.2f, 0.8f, 1f);
            MeshFilter mesh = target.AddComponent<MeshFilter>();
            mesh.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            MeshRenderer renderer = target.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = Mat("Delivery", new Color(0.32f, 0.22f, 0.08f, 1f));
            BoxCollider collider = target.AddComponent<BoxCollider>();
            collider.size = Vector3.one;
            ContractDeliveryInteractable delivery = target.AddComponent<ContractDeliveryInteractable>();
            SerializedObject serialized = new SerializedObject(delivery);
            serialized.FindProperty("destinationId").stringValue = "prototype_delivery_crate";
            serialized.FindProperty("interactionPrompt").stringValue = "Deliver to prototype crate";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            CreateSign(parent, "Sign - Delivery Crate", "Delivery Target", new Vector3(21.5f, 1.65f, 12.5f), 180f);
        }

        private static void CreateDamageDummy(Transform parent)
        {
            GameObject dummy = ReplaceGenerated("Prototype Damage Dummy", parent);
            dummy.transform.SetPositionAndRotation(new Vector3(5.5f, 1f, 22f), Quaternion.identity);
            dummy.transform.localScale = new Vector3(1f, 2f, 1f);
            MeshFilter mesh = dummy.AddComponent<MeshFilter>();
            mesh.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            MeshRenderer renderer = dummy.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = Mat("Dummy", new Color(0.45f, 0.32f, 0.18f, 1f));
            dummy.AddComponent<BoxCollider>();
            dummy.AddComponent<EnemyHealth>();
            ContractObjectiveTarget target = dummy.AddComponent<ContractObjectiveTarget>();
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty("targetCategory").stringValue = "prototype_damage_dummy";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            CreateSign(parent, "Sign - Damage Dummy", "Damage Dummy", new Vector3(5.5f, 2.6f, 22f), 180f);
        }

        private static void CreateTestPoints(Transform parent)
        {
            CreateTestPoint(parent, "test-point.spawn", "Spawn / Hub", new Vector3(0f, 1.1f, -2f), 0f);
            CreateTestPoint(parent, "test-point.items", "Inventory and Items", new Vector3(-18f, 1.1f, -5f), 0f);
            CreateTestPoint(parent, "test-point.equipment", "Equipment", new Vector3(-18f, 1.1f, -16f), 0f);
            CreateTestPoint(parent, "test-point.combat", "Combat", new Vector3(0f, 1.1f, 13f), 0f);
            CreateTestPoint(parent, "test-point.magic-status", "Magic and Status", new Vector3(18f, 1.1f, -14f), 0f);
            CreateTestPoint(parent, "test-point.npc-quest", "Dialogue and Quests", new Vector3(-18f, 1.1f, 9f), 0f);
            CreateTestPoint(parent, "test-point.contract-board", "Contracts", new Vector3(18f, 1.1f, 5f), 0f);
            CreateTestPoint(parent, "test-point.investigation-area", "Investigation Area", new Vector3(0f, 1.1f, 33f), 0f);
        }

        private static void CreateZone(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject zone = ReplaceGenerated(name, parent);
            scale.y = Mathf.Max(0.05f, scale.y);
            position.y = scale.y * -0.5f;
            zone.transform.position = position;
            zone.transform.localScale = scale;
            MeshFilter mesh = zone.AddComponent<MeshFilter>();
            mesh.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            MeshRenderer renderer = zone.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
        }

        private static void CreatePickup(Transform parent, string name, ItemDefinition item, int quantity, PrimitiveType primitive, Vector3 position, Vector3 scale, Material material)
        {
            GameObject pickup = ReplaceGenerated(name, parent);
            GameObject visual = GameObject.CreatePrimitive(primitive);
            visual.name = "Prototype Visual";
            visual.transform.SetParent(pickup.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = scale;
            Collider visualCollider = visual.GetComponent<Collider>();
            if (visualCollider != null)
            {
                Object.DestroyImmediate(visualCollider);
            }

            if (visual.TryGetComponent(out Renderer renderer))
            {
                renderer.sharedMaterial = material;
            }

            pickup.transform.position = position;
            WorldItemPickup worldPickup = pickup.AddComponent<WorldItemPickup>();
            SerializedObject serializedPickup = new SerializedObject(worldPickup);
            serializedPickup.FindProperty("item").objectReferenceValue = item;
            serializedPickup.FindProperty("quantity").intValue = Mathf.Max(1, quantity);
            serializedPickup.FindProperty("disableOnCollected").boolValue = false;
            serializedPickup.ApplyModifiedPropertiesWithoutUndo();
            BoxCollider collider = pickup.AddComponent<BoxCollider>();
            collider.size = new Vector3(
                Mathf.Max(0.8f, scale.x),
                Mathf.Max(0.8f, scale.y),
                Mathf.Max(0.8f, scale.z));
            collider.center = Vector3.zero;
        }

        private static void CreateStatusPedestal(Transform parent, string name, StatusEffectDefinition status, Vector3 position, Material material)
        {
            GameObject pedestal = ReplaceGenerated(name, parent);
            pedestal.transform.position = position;
            pedestal.transform.localScale = new Vector3(1f, 0.5f, 1f);
            MeshFilter mesh = pedestal.AddComponent<MeshFilter>();
            mesh.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
            MeshRenderer renderer = pedestal.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            pedestal.AddComponent<CapsuleCollider>();
            PrototypeStatusEffectInteractable interactable = pedestal.AddComponent<PrototypeStatusEffectInteractable>();
            SerializedObject serialized = new SerializedObject(interactable);
            serialized.FindProperty("statusEffect").objectReferenceValue = status;
            serialized.FindProperty("applyToThisObject").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateTestPoint(Transform parent, string id, string displayName, Vector3 position, float yaw)
        {
            GameObject point = ReplaceGenerated(id, parent);
            point.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            PrototypeTestPoint testPoint = point.AddComponent<PrototypeTestPoint>();
            PlayerSpawnPoint spawnPoint = point.AddComponent<PlayerSpawnPoint>();
            SerializedObject serialized = new SerializedObject(testPoint);
            serialized.FindProperty("testPointId").stringValue = id;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject serializedSpawn = new SerializedObject(spawnPoint);
            serializedSpawn.FindProperty("spawnPointId").stringValue = id switch
            {
                "test-point.spawn" => "spawn.prototype.default",
                "test-point.items" => "spawn.prototype.items",
                "test-point.combat" => "spawn.prototype.combat",
                "test-point.investigation-area" => "spawn.prototype.quest-area",
                _ => $"spawn.prototype.{id.Replace("test-point.", string.Empty)}"
            };
            serializedSpawn.FindProperty("place").objectReferenceValue = id == "test-point.investigation-area"
                ? Load<PlaceDefinition>("Assets/GameData/Prototype/Places/DisturbanceSitePlace.asset")
                : null;
            serializedSpawn.FindProperty("priority").intValue = id == "test-point.spawn" ? 100 : 10;
            serializedSpawn.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WirePrototypePersistence(Transform parent)
        {
            DefinitionCatalog catalog = Load<DefinitionCatalog>("Assets/GameData/Prototype/PrototypeDefinitionCatalog.asset");
            RequireAsset(catalog, "Prototype definition catalog");

            PrototypePersistenceServiceBehaviour persistence = FindFirst<PrototypePersistenceServiceBehaviour>();
            if (persistence == null)
            {
                GameObject persistenceObject = new GameObject("Prototype Persistence Service");
                persistenceObject.transform.SetParent(parent, false);
                persistence = persistenceObject.AddComponent<PrototypePersistenceServiceBehaviour>();
            }
            else
            {
                persistence.transform.SetParent(parent, true);
            }

            PrototypePersistenceState persistenceState = persistence.GetComponent<PrototypePersistenceState>();
            if (persistenceState == null)
            {
                persistenceState = persistence.gameObject.AddComponent<PrototypePersistenceState>();
            }

            SerializedObject serializedPersistence = new SerializedObject(persistence);
            serializedPersistence.FindProperty("prototypeState").objectReferenceValue = persistenceState;
            serializedPersistence.FindProperty("definitionCatalog").objectReferenceValue = catalog;
            serializedPersistence.FindProperty("playerInventory").objectReferenceValue = FindFirst<PlayerInventory>();
            serializedPersistence.FindProperty("playerEquipment").objectReferenceValue = FindFirst<PlayerEquipment>();
            serializedPersistence.FindProperty("playerStats").objectReferenceValue = FindFirst<PlayerStats>();
            serializedPersistence.FindProperty("playerHealth").objectReferenceValue = FindFirst<PlayerHealth>();
            serializedPersistence.FindProperty("playerMana").objectReferenceValue = FindFirst<PlayerMana>();
            serializedPersistence.FindProperty("playerStamina").objectReferenceValue = FindFirst<PlayerStamina>();
            serializedPersistence.FindProperty("statusEffectController").objectReferenceValue = FindFirst<StatusEffectController>();
            serializedPersistence.FindProperty("playerQuestLog").objectReferenceValue = FindFirst<PlayerQuestLog>();
            serializedPersistence.FindProperty("playerContractJournal").objectReferenceValue = FindFirst<PlayerContractJournal>();
            serializedPersistence.FindProperty("playerRoot").objectReferenceValue = FindFirst<PlayerInventory>() == null ? null : FindFirst<PlayerInventory>().transform;
            serializedPersistence.FindProperty("playerInput").objectReferenceValue = FindFirst<UnityIsekaiGame.Input.PlayerInputReader>();
            serializedPersistence.FindProperty("inventoryScreenController").objectReferenceValue = FindFirst<InventoryScreenController>();
            serializedPersistence.FindProperty("currentPlaceTracker").objectReferenceValue = FindFirst<CurrentPlaceTracker>();
            serializedPersistence.FindProperty("sceneKey").stringValue = "scene.prototype";
            serializedPersistence.FindProperty("defaultSpawnPointId").stringValue = "spawn.prototype.default";
            serializedPersistence.ApplyModifiedPropertiesWithoutUndo();

            InventoryScreenController inventoryController = FindFirst<InventoryScreenController>();
            if (inventoryController != null)
            {
                SerializedObject serializedController = new SerializedObject(inventoryController);
                serializedController.FindProperty("testLabDefinitionCatalog").objectReferenceValue = catalog;
                serializedController.FindProperty("testLabPersistence").objectReferenceValue = persistence;
                serializedController.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void CreateSign(Transform parent, string name, string text, Vector3 position, float yaw, float characterSize = 0.28f)
        {
            GameObject sign = ReplaceGenerated(name, parent);
            sign.transform.position = position;
            FaceBasePlatform(sign.transform);
            sign.transform.localScale = Vector3.one * 0.018f;

            RectTransform signRect = sign.AddComponent<RectTransform>();
            signRect.sizeDelta = new Vector2(360f, 86f);

            Canvas canvas = sign.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            CanvasScaler scaler = sign.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 12f;

            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(sign.transform, false);
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            background.GetComponent<Image>().color = new Color(0.04f, 0.045f, 0.05f, 0.86f);

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(sign.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(14f, 8f);
            labelRect.offsetMax = new Vector2(-14f, -8f);

            Text label = labelObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text;
            label.fontSize = 30;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private static void CreateGlobalCollisionFloor(Transform parent)
        {
            GameObject floor = ReplaceGenerated("Prototype Layout Collision Floor", parent);
            floor.transform.position = new Vector3(0f, -0.055f, 9f);
            BoxCollider collider = floor.AddComponent<BoxCollider>();
            collider.size = new Vector3(58f, 0.1f, 76f);
        }

        private static void EnsureSceneKey(Transform root)
        {
            SceneKeyIdentity identity = root.GetComponent<SceneKeyIdentity>();
            if (identity == null)
            {
                identity = root.gameObject.AddComponent<SceneKeyIdentity>();
            }

            SerializedObject serialized = new SerializedObject(identity);
            serialized.FindProperty("sceneKey").stringValue = "scene.prototype";
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsurePlayerTracking()
        {
            PlayerInventory inventory = FindFirst<PlayerInventory>();
            if (inventory != null && inventory.GetComponent<CurrentPlaceTracker>() == null)
            {
                inventory.gameObject.AddComponent<CurrentPlaceTracker>();
            }
        }

        private static void FaceBasePlatform(Transform transform)
        {
            Vector3 target = new Vector3(0f, transform.position.y, 0f);
            Vector3 direction = transform.position - target;
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = Vector3.forward;
            }

            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private static GameObject ReplaceGenerated(string name, Transform parent)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static void DeleteGeneratedChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }
        }

        private static Transform EnsureGeneratedRoot(Transform root)
        {
            Transform existing = root.Find(GeneratedRootName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            return EnsureChild(root, GeneratedRootName);
        }

        private static Transform EnsureRoot(string name)
        {
            GameObject found = GameObject.Find(name);
            if (found != null)
            {
                return found.transform;
            }

            return new GameObject(name).transform;
        }

        private static Transform EnsureChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                return child;
            }

            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            return obj.transform;
        }

        private static void ReparentIfFound(string name, Transform parent)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null)
            {
                obj.transform.SetParent(parent, true);
            }
        }

        private static void DeleteObjectsWithPrefix(string prefix)
        {
            Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
            foreach (Transform transform in transforms)
            {
                if (transform != null && transform.name.StartsWith(prefix))
                {
                    Object.DestroyImmediate(transform.gameObject);
                }
            }
        }

        private static void PositionExistingObject(string name, Vector3 position, Quaternion rotation)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null)
            {
                obj.transform.SetPositionAndRotation(position, rotation);
            }
        }

        private static void RemoveColliderIfFound(string name)
        {
            GameObject obj = GameObject.Find(name);
            if (obj == null)
            {
                return;
            }

            Collider collider = obj.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }
        }

        private static void ImproveLighting()
        {
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i].type == LightType.Directional)
                {
                    lights[i].intensity = Mathf.Max(lights[i].intensity, 1.25f);
                    lights[i].transform.rotation = Quaternion.Euler(50f, -35f, 0f);
                }
            }
        }

        private static T Load<T>(string path) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private static T FindFirst<T>() where T : Object
        {
            T[] objects = Object.FindObjectsByType<T>(FindObjectsInactive.Include);
            return objects.Length == 0 ? null : objects[0];
        }

        private static void RequireAsset(Object asset, string label)
        {
            if (asset == null)
            {
                throw new FileNotFoundException($"Required prototype usability asset was not found: {label}");
            }
        }

        private static Material Mat(string name, Color color)
        {
            string path = $"{MaterialsRoot}/Prototype{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            return material;
        }

        private static void EnsureMaterialsFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }

            if (!AssetDatabase.IsValidFolder(MaterialsRoot))
            {
                AssetDatabase.CreateFolder("Assets/Materials", "PrototypeUsability");
            }
        }
    }
}
