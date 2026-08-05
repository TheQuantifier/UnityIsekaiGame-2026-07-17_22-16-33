using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityIsekaiGame.Editor
{
    public static class PrototypeDungeonStairBuilderTool
    {
        private const string FloorPrefabPath = "Assets/_Project/Prototype/Prefabs/Building Parts/HandPaintedDungeon/Floor Variant.prefab";
        private const string MeshDirectory = "Assets/_Project/Prototype/Models/Building Parts/HandPaintedDungeon";
        private const string MeshPath = MeshDirectory + "/Stairs Variant Mesh.asset";
        private const string PrefabDirectory = "Assets/_Project/Prototype/Prefabs/Building Parts/HandPaintedDungeon";
        private const string PrefabPath = PrefabDirectory + "/Stairs Variant.prefab";
        private const int StepCount = 6;

        [MenuItem("Tools/Prototype Scene/Dungeon Kits/Create Hand Painted Stairs Variant")]
        public static void CreateHandPaintedStairsVariant()
        {
            EnsureFolder(MeshDirectory);
            EnsureFolder(PrefabDirectory);

            var footprint = ResolveFloorFootprint();
            var floorRenderer = ResolveFloorRenderer();
            var uvRegion = ResolveFloorUvRegion();
            var mesh = BuildStairMesh(footprint.x, footprint.y, Mathf.Max(0.5f, footprint.y * 0.5f), StepCount, uvRegion);
            mesh.name = "Stairs Variant Mesh";

            var existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            if (existingMesh == null)
            {
                AssetDatabase.CreateAsset(mesh, MeshPath);
            }
            else
            {
                EditorUtility.CopySerialized(mesh, existingMesh);
                EditorUtility.SetDirty(existingMesh);
                Object.DestroyImmediate(mesh);
                mesh = existingMesh;
            }

            var root = new GameObject("Stairs Variant");
            try
            {
                root.isStatic = true;
                var meshFilter = root.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = mesh;

                var renderer = root.AddComponent<MeshRenderer>();
                CopyFloorRendererSettings(floorRenderer, renderer);

                var collider = root.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Created hand-painted dungeon stairs prefab at {PrefabPath}.");
        }

        private static Vector2 ResolveFloorFootprint()
        {
            var floor = AssetDatabase.LoadAssetAtPath<GameObject>(FloorPrefabPath);
            if (floor == null)
            {
                Debug.LogWarning($"Floor prefab '{FloorPrefabPath}' was not found. Falling back to a 3x3 meter stair footprint.");
                return new Vector2(3f, 3f);
            }

            var collider = floor.GetComponentInChildren<BoxCollider>(true);
            if (collider != null)
            {
                var scale = collider.transform.lossyScale;
                var size = collider.size;
                return new Vector2(Mathf.Abs(size.x * scale.x), Mathf.Abs(size.z * scale.z));
            }

            var renderers = floor.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Vector2(3f, 3f);
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return new Vector2(Mathf.Max(0.1f, bounds.size.x), Mathf.Max(0.1f, bounds.size.z));
        }

        private static Renderer ResolveFloorRenderer()
        {
            var floor = AssetDatabase.LoadAssetAtPath<GameObject>(FloorPrefabPath);
            if (floor == null)
            {
                return null;
            }

            foreach (var renderer in floor.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.sharedMaterial != null)
                {
                    return renderer;
                }
            }

            return null;
        }

        private static Rect ResolveFloorUvRegion()
        {
            var floor = AssetDatabase.LoadAssetAtPath<GameObject>(FloorPrefabPath);
            var meshFilter = floor == null ? null : floor.GetComponentInChildren<MeshFilter>(true);
            var mesh = meshFilter == null ? null : meshFilter.sharedMesh;
            var uvs = mesh == null ? null : mesh.uv;
            if (uvs == null || uvs.Length == 0)
            {
                return new Rect(0f, 0f, 1f, 1f);
            }

            var min = uvs[0];
            var max = uvs[0];
            for (var i = 1; i < uvs.Length; i++)
            {
                min = Vector2.Min(min, uvs[i]);
                max = Vector2.Max(max, uvs[i]);
            }

            var size = max - min;
            if (size.x <= 0.0001f || size.y <= 0.0001f)
            {
                return new Rect(0f, 0f, 1f, 1f);
            }

            return new Rect(min.x, min.y, size.x, size.y);
        }

        private static void CopyFloorRendererSettings(Renderer source, MeshRenderer target)
        {
            if (source == null || target == null)
            {
                return;
            }

            target.sharedMaterials = source.sharedMaterials;
            target.shadowCastingMode = source.shadowCastingMode;
            target.receiveShadows = source.receiveShadows;
            target.motionVectorGenerationMode = source.motionVectorGenerationMode;
            target.lightProbeUsage = source.lightProbeUsage;
            target.reflectionProbeUsage = source.reflectionProbeUsage;
            target.probeAnchor = source.probeAnchor;
            target.lightProbeProxyVolumeOverride = source.lightProbeProxyVolumeOverride;
            target.renderingLayerMask = source.renderingLayerMask;
            target.rendererPriority = source.rendererPriority;
            CopySerializedRendererProperty(source, target, "m_ScaleInLightmap");
            CopySerializedRendererProperty(source, target, "m_ReceiveGI");
            CopySerializedRendererProperty(source, target, "m_StitchLightmapSeams");
        }

        private static void CopySerializedRendererProperty(Renderer source, Renderer target, string propertyName)
        {
            var sourceObject = new SerializedObject(source);
            var targetObject = new SerializedObject(target);
            var sourceProperty = sourceObject.FindProperty(propertyName);
            var targetProperty = targetObject.FindProperty(propertyName);
            if (sourceProperty == null || targetProperty == null)
            {
                return;
            }

            switch (sourceProperty.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    targetProperty.boolValue = sourceProperty.boolValue;
                    break;
                case SerializedPropertyType.Integer:
                    targetProperty.intValue = sourceProperty.intValue;
                    break;
                case SerializedPropertyType.Float:
                    targetProperty.floatValue = sourceProperty.floatValue;
                    break;
            }

            targetObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Mesh BuildStairMesh(float width, float length, float height, int stepCount, Rect uvRegion)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();
            var stepDepth = length / stepCount;
            var stepHeight = height / stepCount;
            var halfWidth = width * 0.5f;
            var halfLength = length * 0.5f;

            for (var step = 0; step < stepCount; step++)
            {
                var zMin = -halfLength + step * stepDepth;
                var zMax = zMin + stepDepth;
                var yMax = (step + 1) * stepHeight;
                AddBox(vertices, triangles, uvs, -halfWidth, halfWidth, 0f, yMax, zMin, zMax, uvRegion);
            }

            var mesh = new Mesh
            {
                name = "Stairs Variant Mesh"
            };

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddBox(
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs,
            float xMin,
            float xMax,
            float yMin,
            float yMax,
            float zMin,
            float zMax,
            Rect uvRegion)
        {
            AddQuad(vertices, triangles, uvs,
                new Vector3(xMin, yMax, zMin),
                new Vector3(xMax, yMax, zMin),
                new Vector3(xMax, yMax, zMax),
                new Vector3(xMin, yMax, zMax),
                uvRegion);

            AddQuad(vertices, triangles, uvs,
                new Vector3(xMin, yMin, zMax),
                new Vector3(xMax, yMin, zMax),
                new Vector3(xMax, yMin, zMin),
                new Vector3(xMin, yMin, zMin),
                uvRegion);

            AddQuad(vertices, triangles, uvs,
                new Vector3(xMin, yMin, zMax),
                new Vector3(xMin, yMax, zMax),
                new Vector3(xMax, yMax, zMax),
                new Vector3(xMax, yMin, zMax),
                uvRegion);

            AddQuad(vertices, triangles, uvs,
                new Vector3(xMax, yMin, zMin),
                new Vector3(xMax, yMax, zMin),
                new Vector3(xMin, yMax, zMin),
                new Vector3(xMin, yMin, zMin),
                uvRegion);

            AddQuad(vertices, triangles, uvs,
                new Vector3(xMin, yMin, zMin),
                new Vector3(xMin, yMax, zMin),
                new Vector3(xMin, yMax, zMax),
                new Vector3(xMin, yMin, zMax),
                uvRegion);

            AddQuad(vertices, triangles, uvs,
                new Vector3(xMax, yMin, zMax),
                new Vector3(xMax, yMax, zMax),
                new Vector3(xMax, yMax, zMin),
                new Vector3(xMax, yMin, zMin),
                uvRegion);
        }

        private static void AddQuad(
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs,
            Vector3 a,
            Vector3 b,
            Vector3 c,
            Vector3 d,
            Rect uvRegion)
        {
            var start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            vertices.Add(d);

            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 1);
            triangles.Add(start);
            triangles.Add(start + 3);
            triangles.Add(start + 2);

            uvs.Add(new Vector2(uvRegion.xMin, uvRegion.yMin));
            uvs.Add(new Vector2(uvRegion.xMax, uvRegion.yMin));
            uvs.Add(new Vector2(uvRegion.xMax, uvRegion.yMax));
            uvs.Add(new Vector2(uvRegion.xMin, uvRegion.yMax));
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            var parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            var name = Path.GetFileName(folder);
            if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
