using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Regroups GameScene's flat level objects into stable categories.
/// World positions, rotations, scales and serialized references are preserved.
/// Run from Tools/TapGJ/Organize GameScene Hierarchy with GameScene open.
/// </summary>
public static class GameSceneHierarchyOrganizer
{
    const string ScenePath = "Assets/Scene/GameScene.unity";
    const string UndoLabel = "Organize GameScene Hierarchy";

    const string Systems = "00 Systems";
    const string Flow = "Level Flow";
    const string Actors = "Actors";

    const string Terrain = "A_地形背景";
    const string Deco = "B_装饰";
    const string Platforms = "C_平台";
    const string Mechanisms = "D_机关";
    const string Interactables = "E_交互设施";
    const string Enemies = "F_敌人";
    const string Collisions = "G_碰撞边界";

    static readonly string[] ZoneNames =
    {
        "01 雨后泵站 · 地下水网",
        "02 共生菌林",
        "03 净化核心",
    };

    static readonly HashSet<string> GroupNames = new HashSet<string>
    {
        Terrain, Deco, Platforms, Mechanisms, Interactables, Enemies, Collisions,
    };

    [MenuItem("Tools/TapGJ/Organize GameScene Hierarchy")]
    public static void Organize()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Exit Play Mode first.");

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            throw new InvalidOperationException("Open " + ScenePath + " first.");

        if (!EditorUtility.DisplayDialog(
                "Organize GameScene",
                "Create the category groups and re-parent level objects?\n\n"
                + "World positions and serialized references are preserved. The scene will not be saved automatically.",
                "Organize", "Cancel"))
            return;

        Undo.IncrementCurrentGroup();
        var undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(UndoLabel);

        // Global groups.
        var systemsRoot = EnsureRoot(scene, Systems);
        MoveRoot(scene, systemsRoot, "Gameplay EventSystem");
        MoveRoot(scene, systemsRoot, "Story Session");
        MoveRoot(scene, systemsRoot, "Gameplay Canvas");
        MoveRoot(scene, systemsRoot, "Main Camera");

        var flowRoot = EnsureRoot(scene, Flow);
        foreach (var sign in Object.FindObjectsByType<WorldSign>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            Move(sign.transform, flowRoot);

        var actorsRoot = EnsureRoot(scene, Actors);
        var player = Object.FindFirstObjectByType<PlayerController>();
        if (player != null)
            Move(player.transform.root, actorsRoot);

        // Per-zone groups.
        var grouped = 0;
        var unclassified = new List<string>();
        for (var zoneIndex = 0; zoneIndex < ZoneNames.Length; zoneIndex++)
        {
            var zone = FindRoot(scene, ZoneNames[zoneIndex]);
            if (zone == null)
            {
                Debug.LogWarning("[SceneOrganizer] Missing zone root: " + ZoneNames[zoneIndex]);
                continue;
            }

            // Snapshot before adding the category groups.
            var children = new List<Transform>();
            for (var i = 0; i < zone.childCount; i++)
                children.Add(zone.GetChild(i));

            var terrain = EnsureChild(zone, Terrain);
            var deco = EnsureChild(zone, Deco);
            var platformGroup = EnsureChild(zone, Platforms);
            var mechanisms = EnsureChild(zone, Mechanisms);
            var interactables = EnsureChild(zone, Interactables);
            var enemyGroup = EnsureChild(zone, Enemies);
            var collisions = EnsureChild(zone, Collisions);

            var bodies = new List<Transform>();
            var visuals = new List<Transform>();

            foreach (var child in children)
            {
                if (child == null || GroupNames.Contains(child.name)) continue;

                if (child.GetComponentInChildren<EnemyAIController>(true) != null)
                {
                    Move(child, enemyGroup);
                    grouped++;
                    continue;
                }

                if (child.name == "单向支路平台" || child.GetComponent<OneWayPlatform>() != null)
                {
                    bodies.Add(child);
                    continue;
                }

                if (child.name == "平台" || child.name == "岩沿托底")
                {
                    visuals.Add(child);
                    continue;
                }

                var target = CategoryOf(child.name, terrain, deco, mechanisms, interactables, collisions);
                if (target == null)
                {
                    unclassified.Add(ZoneNames[zoneIndex] + " / " + child.name);
                    continue;
                }

                Move(child, target);
                grouped++;
            }

            // Pair each one-way platform with its two visual siblings.
            bodies.Sort((a, b) =>
            {
                var x = a.localPosition.x.CompareTo(b.localPosition.x);
                return x != 0 ? x : a.localPosition.y.CompareTo(b.localPosition.y);
            });

            var index = 0;
            foreach (var body in bodies)
            {
                var group = new GameObject("平台_" + (zoneIndex + 1).ToString("00") + "_" + (++index).ToString("00"));
                Undo.RegisterCreatedObjectUndo(group, UndoLabel);
                Undo.SetTransformParent(group.transform, platformGroup, UndoLabel);
                group.transform.position = body.position;
                group.transform.rotation = Quaternion.identity;

                Move(body, group.transform);

                var surface = TakeBest(visuals, body, "平台");
                if (surface != null) Move(surface, group.transform);

                var baseRock = TakeBest(visuals, body, "岩沿托底");
                if (baseRock != null) Move(baseRock, group.transform);

                grouped++;
            }

            // Visuals without a matching collider stay directly under C_平台.
            var orphans = visuals.Count;
            foreach (var visual in visuals)
            {
                Move(visual, platformGroup);
                grouped++;
            }
            if (orphans > 0)
                Debug.LogWarning("[SceneOrganizer] " + ZoneNames[zoneIndex] + ": " + orphans
                    + " platform visual(s) had no matching collider and stay directly under " + Platforms + ".");
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[SceneOrganizer] Re-parented " + grouped + " objects.");
        if (unclassified.Count > 0)
            Debug.LogWarning("[SceneOrganizer] Left unclassified:\n" + string.Join("\n", unclassified));
        Debug.Log("[SceneOrganizer] CampaignFlow / CompleteLevelFlow stay on the zone root: moving components would create new component instances and break serialized references.");
    }

    static Transform CategoryOf(string name, Transform terrain, Transform deco,
        Transform mechanisms, Transform interactables, Transform collisions)
    {
        if (name == "中央基因终端" || name == "中央安全点" || name == "中央恢复补给"
            || name == "最终回流阀" || name.StartsWith("支路冷库"))
            return interactables;

        if (name == "重量阀板" || name == "压板" || name == "可推动箱体"
            || name == "导电接收器" || name == "间歇电源" || name == "电源指示"
            || name.StartsWith("左侧蓄压罐") || name.StartsWith("左侧母菌")
            || name.StartsWith("左侧核心热泵") || name.StartsWith("右侧菌落")
            || name.StartsWith("连锁菌壳") || name.StartsWith("可借力的菌林水罐")
            || name.StartsWith("活体水网") || name.StartsWith("清水唤醒的菌冠捷径")
            || name.StartsWith("区域1 · ") || name.StartsWith("区域2 · ") || name.StartsWith("区域3 · "))
            return mechanisms;

        if (name.StartsWith("环境幕布") || name == "地层" || name == "苔沿"
            || name == "洞壁边缘" || name == "后景地层" || name == "后景管线支座"
            || name == "Chapter Light")
            return terrain;

        if (name.StartsWith("环境群落") || name.StartsWith("管网") || name == "沿路群落"
            || name == "区域连续输水管")
            return deco;

        if (name == "地面碰撞" || name == "场景边界")
            return collisions;

        return null;
    }

    static Transform TakeBest(List<Transform> candidates, Transform body, string wanted)
    {
        var best = -1;
        var bestDistance = float.MaxValue;
        for (var i = 0; i < candidates.Count; i++)
        {
            var candidate = candidates[i];
            if (candidate == null || candidate.name != wanted) continue;

            var dx = Mathf.Abs(candidate.localPosition.x - body.localPosition.x);
            if (dx > 0.1f) continue;

            var dy = candidate.localPosition.y - body.localPosition.y;
            // Platforms sit flush with the collider; the rock trim sits ~0.32 below it.
            if (wanted == "平台")
            {
                if (Mathf.Abs(dy) > 0.25f) continue;
            }
            else
            {
                if (dy > 0.1f || dy < -0.6f) continue;
            }

            var distance = dx + Mathf.Abs(dy);
            if (distance >= bestDistance) continue;
            bestDistance = distance;
            best = i;
        }

        if (best < 0) return null;
        var result = candidates[best];
        candidates.RemoveAt(best);
        return result;
    }

    static Transform EnsureRoot(UnityEngine.SceneManagement.Scene scene, string name)
    {
        var existing = FindRoot(scene, name);
        if (existing != null) return existing;

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, UndoLabel);
        go.transform.position = Vector3.zero;
        go.transform.rotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        return go.transform;
    }

    static Transform EnsureChild(Transform parent, string name)
    {
        for (var i = 0; i < parent.childCount; i++)
            if (parent.GetChild(i).name == name)
                return parent.GetChild(i);

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, UndoLabel);
        Undo.SetTransformParent(go.transform, parent, UndoLabel);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        return go.transform;
    }

    static Transform FindRoot(UnityEngine.SceneManagement.Scene scene, string name)
    {
        foreach (var go in scene.GetRootGameObjects())
            if (go.name == name)
                return go.transform;
        return null;
    }

    static void MoveRoot(UnityEngine.SceneManagement.Scene scene, Transform parent, string name)
    {
        var child = FindRoot(scene, name);
        if (child != null) Move(child, parent);
    }

    static void Move(Transform child, Transform parent)
    {
        if (child == null || parent == null || child == parent || child.parent == parent) return;

        var position = child.position;
        var rotation = child.rotation;
        Undo.SetTransformParent(child, parent, UndoLabel);
        child.position = position;
        child.rotation = rotation;
    }
}
