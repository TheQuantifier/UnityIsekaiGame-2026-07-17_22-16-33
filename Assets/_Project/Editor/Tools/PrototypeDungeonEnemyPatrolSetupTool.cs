using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityIsekaiGame.Combat;
using UnityIsekaiGame.GameData.Persistence;
using UnityIsekaiGame.Persistence;
using UnityIsekaiGame.WorldEntities;

namespace UnityIsekaiGame.Editor
{
    public static class PrototypeDungeonEnemyPatrolSetupTool
    {
        private const string PrototypeScenePath = "Assets/_Project/Scenes/Prototype/PrototypeScene.unity";
        private const string EnemyPrefabDirectory = "Assets/_Project/Prototype/Prefabs/Characters";
        private const string EnemyPrefabPath = EnemyPrefabDirectory + "/Prototype Zombie Enemy.prefab";
        private const string SourceEnemyName = "Prototype Zombie Enemy";
        private const string DungeonInstanceName = "Dungeon1";
        private const string PatrolEnemyName = "Prototype Zombie Dungeon Patrol";
        private const string PatrolPathName = "Prototype Zombie Dungeon Patrol Path";
        private const string CombatPath = "PrototypeScene/Gameplay/Combat";

        [MenuItem("Tools/Prototype Scene/Setup Dungeon Enemy Patrol")]
        public static void SetupDungeonEnemyPatrol()
        {
            Scene scene = EditorSceneManager.OpenScene(PrototypeScenePath, OpenSceneMode.Single);
            GameObject sourceEnemy = GameObject.Find(SourceEnemyName);
            if (sourceEnemy == null)
            {
                PrototypeZombieEnemySetupTool.SetupZombieEnemy();
                sourceEnemy = GameObject.Find(SourceEnemyName);
            }

            if (sourceEnemy == null)
            {
                Debug.LogError($"Cannot create dungeon enemy patrol because '{SourceEnemyName}' was not found.");
                return;
            }

            ConfigureCharacterController(sourceEnemy);
            ConfigureEnemyController(sourceEnemy);
            EnsureDirectory(EnemyPrefabDirectory);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(sourceEnemy, EnemyPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"Failed to save prototype enemy prefab at '{EnemyPrefabPath}'.");
                return;
            }

            GameObject combatRoot = FindOrCreatePath(CombatPath);
            RemoveExistingChild(combatRoot.transform, PatrolEnemyName);
            RemoveExistingChild(combatRoot.transform, PatrolPathName);

            GameObject pathRoot = new GameObject(PatrolPathName);
            pathRoot.transform.SetParent(combatRoot.transform, worldPositionStays: false);

            Transform[] waypoints = CreateDungeonWaypoints(pathRoot.transform);
            GameObject patrolEnemy = (GameObject)PrefabUtility.InstantiatePrefab(prefab, combatRoot.transform);
            patrolEnemy.name = PatrolEnemyName;
            patrolEnemy.transform.position = waypoints.Length == 0 ? ResolveDungeonCenter() : waypoints[0].position;
            patrolEnemy.transform.rotation = ResolveInitialRotation(waypoints);

            ConfigureCharacterController(patrolEnemy);
            ConfigureEnemyIdentity(patrolEnemy);
            ConfigureEnemyController(patrolEnemy);
            ConfigurePatrolController(patrolEnemy, waypoints);
            SnapToGround(patrolEnemy.transform);

            EditorUtility.SetDirty(pathRoot);
            EditorUtility.SetDirty(patrolEnemy);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Created '{EnemyPrefabPath}' and placed '{PatrolEnemyName}' with {waypoints.Length} dungeon patrol waypoint(s).");
        }

        private static Transform[] CreateDungeonWaypoints(Transform pathRoot)
        {
            Bounds bounds = ResolveDungeonBounds();
            Vector3 center = bounds.center;
            float radiusX = Mathf.Clamp(bounds.extents.x * 0.25f, 3f, 8f);
            float radiusZ = Mathf.Clamp(bounds.extents.z * 0.25f, 3f, 8f);
            float y = bounds.min.y + 0.25f;

            Vector3[] positions =
            {
                new(center.x - radiusX, y, center.z - radiusZ),
                new(center.x + radiusX, y, center.z - radiusZ),
                new(center.x + radiusX, y, center.z + radiusZ),
                new(center.x - radiusX, y, center.z + radiusZ)
            };

            Transform[] waypoints = new Transform[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                GameObject waypoint = new GameObject($"Waypoint {i + 1:00}");
                waypoint.transform.SetParent(pathRoot, worldPositionStays: true);
                waypoint.transform.position = positions[i];
                SnapToGround(waypoint.transform);
                waypoints[i] = waypoint.transform;
            }

            return waypoints;
        }

        private static Bounds ResolveDungeonBounds()
        {
            GameObject dungeon = GameObject.Find(DungeonInstanceName);
            if (dungeon == null)
            {
                return new Bounds(new Vector3(-52.22f, 8f, -81.72f), new Vector3(12f, 4f, 12f));
            }

            Renderer[] renderers = dungeon.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(dungeon.transform.position, new Vector3(12f, 4f, 12f));
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static Vector3 ResolveDungeonCenter()
        {
            return ResolveDungeonBounds().center;
        }

        private static Quaternion ResolveInitialRotation(Transform[] waypoints)
        {
            if (waypoints.Length < 2)
            {
                return Quaternion.identity;
            }

            Vector3 direction = waypoints[1].position - waypoints[0].position;
            direction.y = 0f;
            return direction.sqrMagnitude <= 0.001f
                ? Quaternion.identity
                : Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private static void ConfigureEnemyIdentity(GameObject enemy)
        {
            WorldEntityIdentity identity = enemy.GetComponent<WorldEntityIdentity>();
            if (identity == null)
            {
                return;
            }

            identity.TrySetAuthoredIdentity("enemy.prototype-zombie.dungeon-patrol", "scene.prototype", PersistenceScope.RegionOrScene, "actor.prototype-zombie", out _);
        }

        private static void ConfigureEnemyController(GameObject enemy)
        {
            PrototypeEnemyController controller = enemy.GetComponent<PrototypeEnemyController>();
            if (controller == null)
            {
                return;
            }

            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("target").objectReferenceValue = FindPlayerTransform();
            serialized.FindProperty("characterController").objectReferenceValue = enemy.GetComponent<CharacterController>();
            serialized.FindProperty("detectionRadius").floatValue = 10f;
            serialized.FindProperty("moveSpeed").floatValue = 2f;
            serialized.FindProperty("stoppingDistance").floatValue = 1.35f;
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

        private static void ConfigurePatrolController(GameObject enemy, Transform[] waypoints)
        {
            PrototypeEnemyPatrolController patrol = enemy.GetComponent<PrototypeEnemyPatrolController>();
            if (patrol == null)
            {
                patrol = enemy.AddComponent<PrototypeEnemyPatrolController>();
            }

            SerializedObject serialized = new SerializedObject(patrol);
            serialized.FindProperty("enemyController").objectReferenceValue = enemy.GetComponent<PrototypeEnemyController>();
            serialized.FindProperty("health").objectReferenceValue = enemy.GetComponent<EnemyHealth>();
            serialized.FindProperty("characterController").objectReferenceValue = enemy.GetComponent<CharacterController>();
            SerializedProperty waypointArray = serialized.FindProperty("waypoints");
            waypointArray.arraySize = waypoints.Length;
            for (int i = 0; i < waypoints.Length; i++)
            {
                waypointArray.GetArrayElementAtIndex(i).objectReferenceValue = waypoints[i];
            }

            serialized.FindProperty("moveSpeed").floatValue = 1.25f;
            serialized.FindProperty("waypointRadius").floatValue = 0.35f;
            serialized.FindProperty("waypointWaitSeconds").floatValue = 0.4f;
            serialized.FindProperty("turnSpeed").floatValue = 10f;
            serialized.FindProperty("loop").boolValue = true;
            serialized.FindProperty("snapToGround").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform FindPlayerTransform()
        {
            GameObject player = GameObject.Find("Prototype Player");
            return player == null ? null : player.transform;
        }

        private static void SnapToGround(Transform transform)
        {
            if (SpawnGroundingUtility.TrySnapToNearestSolidSurface(transform.position, transform, out Vector3 groundedPosition, out _))
            {
                transform.position = groundedPosition;
            }
        }

        private static void RemoveExistingChild(Transform parent, string childName)
        {
            Transform existing = parent.Find(childName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }
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

        private static void EnsureDirectory(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}
