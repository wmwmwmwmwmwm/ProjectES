#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Deterministic terrain-aware snow-fir groves; retains prefab links and Undo.</summary>
public static class GlacierTreePlacement
{
    const string ScenePath = "Assets/0Game/Glacier/Glacier.unity";
    const string TreeFolder = "Assets/BK/PureNature_Glacier/Prefabs/Trees";
    const string RootName = "Glacier Tree Clusters";
    const int Seed = 20260911;
    const int ClusterCount = 24;
    const float Spacing = 3.6f;
    struct ProtectedArea { public Vector3 position; public float radius; }
    struct Plant { public Vector3 position; public float scale, yaw; public int variant, grove; }

    [MenuItem("Tools/Echo Sword/Glacier/Place Tree Clusters")]
    public static void PlaceTreeClusters()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before placing trees.");
        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        var roots = scene.GetRootGameObjects();
        // Existing groups may have been hand-edited. Never silently replace them.
        if (roots.Any(g => g.name == RootName))
            throw new InvalidOperationException("Tree clusters already exist. Undo the previous placement before regenerating.");
        var terrain = roots.SelectMany(g => g.GetComponentsInChildren<Terrain>()).Single();
        var td = terrain.terrainData;
        var origin = terrain.transform.position;
        var prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { TreeFolder })
            .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p, StringComparer.Ordinal)
            .Select(AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();
        if (prefabs.Length == 0) throw new InvalidOperationException("No tree prefabs found.");

        var protectedAreas = new List<ProtectedArea>();
        foreach (var obj in roots.SelectMany(g => g.GetComponentsInChildren<Transform>(true)))
        {
            if (obj.name == "StartPosition")
                protectedAreas.Add(new ProtectedArea { position = obj.position, radius = 20f });
            else if (obj.parent != null && (obj.parent.name.EndsWith("Doors") || obj.parent.name.EndsWith("Keys") || obj.parent.name == "Enemys"))
                protectedAreas.Add(new ProtectedArea { position = obj.position, radius = obj.parent.name == "Enemys" ? 12f : 14f });
        }
        var alpha = td.GetAlphamaps(0, 0, td.alphamapWidth, td.alphamapHeight);
        var iceLayers = td.terrainLayers.Select((layer, i) => new { layer, i })
            .Where(x => x.layer != null && x.layer.name.IndexOf("ice", StringComparison.OrdinalIgnoreCase) >= 0)
            .Select(x => x.i).ToArray();
        var obstacles = roots.SelectMany(g => g.GetComponentsInChildren<Collider>())
            .Where(c => c.enabled && !c.isTrigger && !(c is TerrainCollider)).ToArray();
        Physics.SyncTransforms();
        var random = new System.Random(Seed);
        var centers = new List<Vector3>();
        var plants = new List<Plant>();
        int attempts = 0;
        while (centers.Count < ClusterCount && attempts++ < 12000)
        {
            var center = origin + new Vector3(Range(0.06f, 0.94f) * td.size.x, 0, Range(0.06f, 0.94f) * td.size.z);
            if (!TryGround(ref center)) continue;
            if (centers.Any(p => FlatDistance(p, center) < 55f)) continue;
            int grove = centers.Count;
            float radius = Range(13f, 26f), aspect = Range(0.5f, 0.85f), direction = Range(0, Mathf.PI * 2);
            int count = random.Next(14, 27), dominant = random.Next(prefabs.Length);
            var candidates = new List<Plant>();
            for (int trial = 0; trial < 900 && candidates.Count < count; trial++)
            {
                // Denser core, sparse fringe, and elongated shapes avoid circular stamp patterns.
                float r = radius * Mathf.Pow(Range(0, 1), 0.85f), angle = Range(0, Mathf.PI * 2);
                float x = Mathf.Cos(angle) * r, z = Mathf.Sin(angle) * r * aspect;
                var point = center + new Vector3(x * Mathf.Cos(direction) - z * Mathf.Sin(direction), 0,
                    x * Mathf.Sin(direction) + z * Mathf.Cos(direction));
                if (!TryGround(ref point)) continue;
                if (plants.Any(p => FlatDistance(p.position, point) < Spacing) ||
                    candidates.Any(p => FlatDistance(p.position, point) < Spacing)) continue;
                candidates.Add(new Plant { position = point, scale = Range(0.8f, 1.2f), yaw = Range(0, 360),
                    variant = random.NextDouble() < 0.55 ? dominant : random.Next(prefabs.Length), grove = grove });
            }
            if (candidates.Count < 7) continue;
            centers.Add(center);
            plants.AddRange(candidates);
        }
        if (centers.Count != ClusterCount) throw new InvalidOperationException($"Only {centers.Count} suitable groves found; scene was not changed.");

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Place Glacier tree clusters");
        try
        {
            var root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Create tree clusters");
            var groups = centers.Select((center, i) => {
                var group = new GameObject($"Grove_{i + 1:00}");
                SceneManager.MoveGameObjectToScene(group, scene);
                group.transform.SetParent(root.transform);
                group.transform.position = center;
                Undo.RegisterCreatedObjectUndo(group, "Create grove");
                return group.transform;
            }).ToArray();
            foreach (var plant in plants)
            {
                var tree = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[plant.variant], scene);
                tree.transform.SetParent(groups[plant.grove], true);
                // Upright growth on slopes; slightly buried base prevents floating roots.
                tree.transform.SetPositionAndRotation(plant.position - Vector3.up * 0.06f, Quaternion.Euler(0, plant.yaw, 0));
                tree.transform.localScale = Vector3.one * plant.scale;
                foreach (var child in tree.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = terrain.gameObject.layer;
                PrefabUtility.RecordPrefabInstancePropertyModifications(tree.transform);
                foreach (var child in tree.GetComponentsInChildren<Transform>(true)) PrefabUtility.RecordPrefabInstancePropertyModifications(child.gameObject);
                Undo.RegisterCreatedObjectUndo(tree, "Plant snow fir");
            }
            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save Glacier scene.");
            Debug.Log($"Glacier trees: {plants.Count} linked prefab instances / {centers.Count} groves / scale {plants.Min(p => p.scale):F4}–{plants.Max(p => p.scale):F4}. Seed {Seed}.");
        }
        catch { Undo.RevertAllDownToGroup(undoGroup); throw; }

        float Range(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());
        bool TryGround(ref Vector3 point)
        {
            float u = (point.x - origin.x) / td.size.x, v = (point.z - origin.z) / td.size.z;
            if (u < 0.04f || v < 0.04f || u > 0.96f || v > 0.96f) return false;
            if (td.IsHole(Mathf.Min((int)(u * td.holesResolution), td.holesResolution - 1), Mathf.Min((int)(v * td.holesResolution), td.holesResolution - 1))) return false;
            if (td.GetSteepness(u, v) > 28f) return false;
            int ax = Mathf.Clamp((int)(u * td.alphamapWidth), 0, td.alphamapWidth - 1);
            int az = Mathf.Clamp((int)(v * td.alphamapHeight), 0, td.alphamapHeight - 1);
            float ice = 0;
            foreach (int layer in iceLayers) ice += alpha[az, ax, layer];
            if (ice > 0.25f) return false;
            point.y = td.GetInterpolatedHeight(u, v) + origin.y;
            foreach (var area in protectedAreas) if (FlatDistance(point, area.position) < area.radius) return false;
            // Mesh bounds include large empty areas around irregular cliffs. Test actual geometry.
            foreach (var nearby in Physics.OverlapCapsule(point + Vector3.up * 2.5f, point + Vector3.up * 8f, 1.7f, ~0, QueryTriggerInteraction.Ignore))
                if (!(nearby is TerrainCollider) && obstacles.Contains(nearby)) return false;
            foreach (var collider in obstacles)
            {
                // Reject terrain buried below rock shelves or other solid surfaces.
                if (collider.Raycast(new Ray(point + Vector3.up * 650f, Vector3.down), out var hit, 649.8f)) return false;
            }
            return true;
        }
    }
    static float FlatDistance(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;
}
#endif
