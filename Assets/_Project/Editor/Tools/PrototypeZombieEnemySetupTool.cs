using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityIsekaiGame.ActorLifecycle;
using UnityIsekaiGame.Combat;
using UnityIsekaiGame.GameData.Persistence;
using UnityIsekaiGame.ResourceSystem;
using UnityIsekaiGame.Stats;
using UnityIsekaiGame.StatusEffects;
using UnityIsekaiGame.WorldEntities;

namespace UnityIsekaiGame.Editor
{
    public static class PrototypeZombieEnemySetupTool
    {
        private const string PrototypeScenePath = "Assets/_Project/Scenes/Prototype/PrototypeScene.unity";
        private const string CombatPath = "PrototypeScene/Gameplay/Combat";
        private const string EnemyName = "Prototype Zombie Enemy";
        private const string ZombiePrefabPath = "Assets/_Project/Prototype/Models/Characters/Zombie/Prefabs/Zombie2.prefab";
        private const string AnimatorDirectory = "Assets/_Project/Prototype/Animations/Characters/Zombie";
        private const string AnimatorControllerPath = AnimatorDirectory + "/Prototype Zombie Enemy.controller";
        private const string IdleClipPath = "Assets/_Project/Prototype/Models/Characters/Zombie/Animations/Z_Idle.anim";
        private const string WalkClipPath = "Assets/_Project/Prototype/Models/Characters/Zombie/Animations/Z_Walk_InPlace.anim";
        private const string AttackClipPath = "Assets/_Project/Prototype/Models/Characters/Zombie/Animations/Z_Attack.anim";
        private const string DefeatedClipPath = "Assets/_Project/Prototype/Models/Characters/Zombie/Animations/Z_FallingBack.anim";
        private const string MaterialDirectory = "Assets/_Project/Prototype/Materials/Characters/Zombie";
        private const string ZombieRuntimeMaterialPath = MaterialDirectory + "/Prototype Zombie URP.mat";
        private const string ZombieAlbedoPath = "Assets/_Project/Prototype/Models/Characters/Zombie/Textures/Zombie.tga";
        private const string ZombieNormalPath = "Assets/_Project/Prototype/Models/Characters/Zombie/Textures/Zombie_nm.png";
        private const string ZombieOcclusionPath = "Assets/_Project/Prototype/Models/Characters/Zombie/Textures/Zombie_ao.png";
        private const string ZombieMetallicPath = "Assets/_Project/Prototype/Models/Characters/Zombie/Textures/Zombie_metallic.tga";
        private const float DesiredHeightMeters = 3.7f;

        [MenuItem("Tools/Prototype Scene/Setup Zombie Enemy")]
        public static void SetupZombieEnemy()
        {
            Scene scene = EditorSceneManager.OpenScene(PrototypeScenePath, OpenSceneMode.Single);
            GameObject combatRoot = FindOrCreatePath(CombatPath);
            GameObject player = GameObject.Find("Prototype Player");
            if (player == null)
            {
                Debug.LogWarning("Prototype Zombie setup could not find Prototype Player; enemy target will be left empty.");
            }

            DestroyExistingEnemy(combatRoot.transform);

            GameObject enemy = new GameObject(EnemyName);
            Undo.RegisterCreatedObjectUndo(enemy, "Create prototype zombie enemy");
            enemy.transform.SetParent(combatRoot.transform, worldPositionStays: false);
            enemy.transform.position = ResolveGroundedPosition(new Vector3(100f, 100f, 0f));
            enemy.transform.rotation = ResolveFacingOrigin(enemy.transform.position);

            WorldEntityIdentity identity = enemy.AddComponent<WorldEntityIdentity>();
            identity.TrySetAuthoredIdentity("enemy.prototype-zombie", "scene.prototype", PersistenceScope.RegionOrScene, "actor.prototype-zombie", out _);
            SetSerializedString(identity, "expectedEntityType", "Actor");

            enemy.AddComponent<CharacterAttributes>();
            CalculatedStatCollection calculatedStats = enemy.AddComponent<CalculatedStatCollection>();
            ActorStats stats = enemy.AddComponent<ActorStats>();
            CharacterResourceCollection resources = enemy.AddComponent<CharacterResourceCollection>();
            ActorLifecycleController lifecycle = enemy.AddComponent<ActorLifecycleController>();
            StatusEffectController statusEffects = enemy.AddComponent<StatusEffectController>();
            EnemyHealth health = enemy.AddComponent<EnemyHealth>();
            PrototypeEnemyController controller = enemy.AddComponent<PrototypeEnemyController>();
            EnemyMeleeAttack melee = enemy.AddComponent<EnemyMeleeAttack>();
            PrototypeEnemyAnimationDriver animationDriver = enemy.AddComponent<PrototypeEnemyAnimationDriver>();

            ConfigureCharacterController(enemy);

            ConfigureEnemyHealth(health, stats, resources);
            ConfigureController(controller, player == null ? null : player.transform, health, melee);
            ConfigureMelee(melee, health);
            ConfigureLifecycle(lifecycle, resources);

            RuntimeAnimatorController runtimeController = EnsureAnimatorController();
            GameObject visual = CreateVisual(enemy.transform, runtimeController);
            ConfigureAnimationDriver(animationDriver, visual.GetComponentInChildren<Animator>(), health, melee);

            EditorUtility.SetDirty(enemy);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Prototype zombie enemy setup complete.");
        }

        private static void DestroyExistingEnemy(Transform combatRoot)
        {
            Transform existing = combatRoot.Find(EnemyName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }
        }

        private static void ConfigureEnemyHealth(EnemyHealth health, ActorStats stats, CharacterResourceCollection resources)
        {
            SerializedObject serialized = new SerializedObject(health);
            serialized.FindProperty("maximumHealth").floatValue = 50f;
            serialized.FindProperty("defense").floatValue = 0f;
            serialized.FindProperty("stats").objectReferenceValue = stats;
            serialized.FindProperty("resources").objectReferenceValue = resources;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureController(PrototypeEnemyController controller, Transform target, EnemyHealth health, EnemyMeleeAttack melee)
        {
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("target").objectReferenceValue = target;
            serialized.FindProperty("health").objectReferenceValue = health;
            serialized.FindProperty("meleeAttack").objectReferenceValue = melee;
            serialized.FindProperty("characterController").objectReferenceValue = controller.GetComponent<CharacterController>();
            serialized.FindProperty("detectionRadius").floatValue = 10f;
            serialized.FindProperty("moveSpeed").floatValue = 2f;
            serialized.FindProperty("stoppingDistance").floatValue = 1.35f;
            serialized.FindProperty("turnSpeed").floatValue = 12f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static CharacterController ConfigureCharacterController(GameObject enemy)
        {
            foreach (CapsuleCollider capsule in enemy.GetComponents<CapsuleCollider>())
            {
                Object.DestroyImmediate(capsule);
            }

            CharacterController controller = enemy.GetComponent<CharacterController>();
            if (controller == null)
            {
                controller = enemy.AddComponent<CharacterController>();
            }

            controller.radius = 0.55f;
            controller.height = 3.8f;
            controller.center = new Vector3(0f, 1.9f, 0f);
            controller.slopeLimit = 45f;
            controller.stepOffset = 0.35f;
            controller.skinWidth = 0.08f;
            controller.minMoveDistance = 0f;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void ConfigureMelee(EnemyMeleeAttack melee, EnemyHealth health)
        {
            SerializedObject serialized = new SerializedObject(melee);
            serialized.FindProperty("health").objectReferenceValue = health;
            serialized.FindProperty("damage").floatValue = 12f;
            serialized.FindProperty("attackRange").floatValue = 1.6f;
            serialized.FindProperty("attackCooldown").floatValue = 1.25f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureLifecycle(ActorLifecycleController lifecycle, CharacterResourceCollection resources)
        {
            SerializedObject serialized = new SerializedObject(lifecycle);
            SerializedProperty resourceProperty = serialized.FindProperty("resources");
            if (resourceProperty != null)
            {
                resourceProperty.objectReferenceValue = resources;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureAnimationDriver(PrototypeEnemyAnimationDriver driver, Animator animator, EnemyHealth health, EnemyMeleeAttack melee)
        {
            SerializedObject serialized = new SerializedObject(driver);
            serialized.FindProperty("animator").objectReferenceValue = animator;
            serialized.FindProperty("health").objectReferenceValue = health;
            serialized.FindProperty("meleeAttack").objectReferenceValue = melee;
            serialized.FindProperty("speedParameter").stringValue = "Speed";
            serialized.FindProperty("attackTrigger").stringValue = "Attack";
            serialized.FindProperty("defeatedParameter").stringValue = "IsDefeated";
            serialized.FindProperty("speedDampTime").floatValue = 0.08f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateVisual(Transform parent, RuntimeAnimatorController controller)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ZombiePrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"Zombie prefab '{ZombiePrefabPath}' was not found. Creating an empty visual child.");
                return new GameObject("Zombie Visual") { transform = { parent = parent } };
            }

            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            visual.name = "Zombie Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            foreach (Collider childCollider in visual.GetComponentsInChildren<Collider>(includeInactive: true))
            {
                Object.DestroyImmediate(childCollider);
            }

            Material zombieMaterial = EnsureZombieMaterial();
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                Material[] materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    renderer.sharedMaterial = zombieMaterial;
                    continue;
                }

                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = zombieMaterial;
                }

                renderer.sharedMaterials = materials;
            }

            Animator animator = visual.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
            }

            FitVisualToRoot(visual.transform, parent, DesiredHeightMeters);
            return visual;
        }

        private static Material EnsureZombieMaterial()
        {
            if (!Directory.Exists(MaterialDirectory))
            {
                Directory.CreateDirectory(MaterialDirectory);
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(ZombieRuntimeMaterialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, ZombieRuntimeMaterialPath);
            }

            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(ZombieAlbedoPath);
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(ZombieNormalPath);
            Texture2D occlusion = AssetDatabase.LoadAssetAtPath<Texture2D>(ZombieOcclusionPath);
            Texture2D metallic = AssetDatabase.LoadAssetAtPath<Texture2D>(ZombieMetallicPath);

            if (albedo != null)
            {
                SetTexture(material, "_BaseMap", "_MainTex", albedo);
            }

            if (normal != null)
            {
                SetTexture(material, "_BumpMap", null, normal);
                material.EnableKeyword("_NORMALMAP");
            }

            if (occlusion != null)
            {
                SetTexture(material, "_OcclusionMap", null, occlusion);
            }

            if (metallic != null)
            {
                SetTexture(material, "_MetallicGlossMap", null, metallic);
                SetFloat(material, "_Metallic", 0f);
                SetFloat(material, "_Smoothness", 0.25f);
                SetFloat(material, "_Glossiness", 0.25f);
            }

            SetColor(material, "_BaseColor", "_Color", Color.white);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static void SetTexture(Material material, string primaryProperty, string fallbackProperty, Texture texture)
        {
            if (material.HasProperty(primaryProperty))
            {
                material.SetTexture(primaryProperty, texture);
            }
            else if (!string.IsNullOrEmpty(fallbackProperty) && material.HasProperty(fallbackProperty))
            {
                material.SetTexture(fallbackProperty, texture);
            }
        }

        private static void SetColor(Material material, string primaryProperty, string fallbackProperty, Color color)
        {
            if (material.HasProperty(primaryProperty))
            {
                material.SetColor(primaryProperty, color);
            }
            else if (!string.IsNullOrEmpty(fallbackProperty) && material.HasProperty(fallbackProperty))
            {
                material.SetColor(fallbackProperty, color);
            }
        }

        private static void SetFloat(Material material, string propertyName, float value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }

        private static RuntimeAnimatorController EnsureAnimatorController()
        {
            if (!Directory.Exists(AnimatorDirectory))
            {
                Directory.CreateDirectory(AnimatorDirectory);
            }

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
            if (controller != null)
            {
                return controller;
            }

            controller = AnimatorController.CreateAnimatorControllerAtPath(AnimatorControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("IsDefeated", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState idle = stateMachine.AddState("Idle");
            AnimatorState walk = stateMachine.AddState("Walk");
            AnimatorState attack = stateMachine.AddState("Attack");
            AnimatorState defeated = stateMachine.AddState("Defeated");

            idle.motion = LoadClip(IdleClipPath);
            walk.motion = LoadClip(WalkClipPath);
            attack.motion = LoadClip(AttackClipPath);
            defeated.motion = LoadClip(DefeatedClipPath);
            stateMachine.defaultState = idle;

            AnimatorStateTransition idleToWalk = idle.AddTransition(walk);
            idleToWalk.hasExitTime = false;
            idleToWalk.duration = 0.12f;
            idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.05f, "Speed");

            AnimatorStateTransition walkToIdle = walk.AddTransition(idle);
            walkToIdle.hasExitTime = false;
            walkToIdle.duration = 0.12f;
            walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.05f, "Speed");

            AnimatorStateTransition anyToAttack = stateMachine.AddAnyStateTransition(attack);
            anyToAttack.hasExitTime = false;
            anyToAttack.duration = 0.05f;
            anyToAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");

            AnimatorStateTransition attackToIdle = attack.AddTransition(idle);
            attackToIdle.hasExitTime = true;
            attackToIdle.exitTime = 0.85f;
            attackToIdle.duration = 0.1f;

            AnimatorStateTransition anyToDefeated = stateMachine.AddAnyStateTransition(defeated);
            anyToDefeated.hasExitTime = false;
            anyToDefeated.duration = 0.12f;
            anyToDefeated.AddCondition(AnimatorConditionMode.If, 0f, "IsDefeated");

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimationClip LoadClip(string path)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                Debug.LogWarning($"Zombie animation clip '{path}' was not found.");
            }

            return clip;
        }

        private static void FitVisualToRoot(Transform visual, Transform root, float desiredHeight)
        {
            if (!TryGetRendererBounds(visual, out Bounds bounds) || bounds.size.y <= 0.001f)
            {
                return;
            }

            float scaleFactor = desiredHeight / bounds.size.y;
            visual.localScale *= scaleFactor;

            if (!TryGetRendererBounds(visual, out bounds))
            {
                return;
            }

            Vector3 offset = new Vector3(
                root.position.x - bounds.center.x,
                root.position.y - bounds.min.y,
                root.position.z - bounds.center.z);
            visual.position += offset;
        }

        private static bool TryGetRendererBounds(Transform root, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(includeInactive: true);
            bounds = default;
            bool initialized = false;
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                if (!initialized)
                {
                    bounds = renderer.bounds;
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return initialized;
        }

        private static Vector3 ResolveGroundedPosition(Vector3 probePosition)
        {
            foreach (Terrain terrain in Terrain.activeTerrains)
            {
                if (terrain == null || terrain.terrainData == null)
                {
                    continue;
                }

                Vector3 terrainPosition = terrain.transform.position;
                Vector3 terrainSize = terrain.terrainData.size;
                bool inside = probePosition.x >= terrainPosition.x
                    && probePosition.x <= terrainPosition.x + terrainSize.x
                    && probePosition.z >= terrainPosition.z
                    && probePosition.z <= terrainPosition.z + terrainSize.z;
                if (!inside)
                {
                    continue;
                }

                probePosition.y = terrainPosition.y + terrain.SampleHeight(probePosition);
                return probePosition;
            }

            if (Physics.Raycast(probePosition, Vector3.down, out RaycastHit hit, 500f, ~0, QueryTriggerInteraction.Ignore))
            {
                return hit.point;
            }

            probePosition.y = 0f;
            return probePosition;
        }

        private static Quaternion ResolveFacingOrigin(Vector3 position)
        {
            Vector3 direction = -position;
            direction.y = 0f;
            return direction.sqrMagnitude <= 0.001f ? Quaternion.identity : Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private static GameObject FindOrCreatePath(string path)
        {
            string[] parts = path.Split('/');
            GameObject current = GameObject.Find(parts[0]);
            if (current == null)
            {
                current = new GameObject(parts[0]);
            }

            for (int i = 1; i < parts.Length; i++)
            {
                Transform child = current.transform.Find(parts[i]);
                if (child == null)
                {
                    GameObject created = new GameObject(parts[i]);
                    created.transform.SetParent(current.transform, worldPositionStays: false);
                    current = created;
                }
                else
                {
                    current = child.gameObject;
                }
            }

            return current;
        }

        private static void SetSerializedString(Object target, string fieldName, string value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property != null)
            {
                property.stringValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
