#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class EnemyCombatPolishSetup
{
    public static readonly string[] MeleePrefabs =
    {
        "Assets/Resources/Prefab/Enemy/Melee/Monster3.prefab",
        "Assets/Resources/Prefab/Enemy/Melee/Monster5.prefab"
    };

    [MenuItem("Tools/TapGJ/Enemy/Apply Fair Melee Combat")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before enemy prefab setup.");
        foreach (string path in MeleePrefabs)
        {
            GameObject prefab = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ConfigureSceneEnemy(prefab.GetComponent<EnemyAIController>());
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }
        // Any additional ordinary melee prefab also gets safe combat defaults.
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources/Prefab/Enemy" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (MeleePrefabs.Contains(path)) continue;
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            EnemyAIController enemy = asset.GetComponent<EnemyAIController>();
            if (enemy == null || enemy.Kind != EnemyAIController.AttackType.Melee) continue;
            GameObject prefab = PrefabUtility.LoadPrefabContents(path);
            try { ConfigureSceneEnemy(prefab.GetComponent<EnemyAIController>()); PrefabUtility.SaveAsPrefabAsset(prefab, path); }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Fair melee combat configured: Monster3 fire rat and Monster5 crystal crawler. Body contact has no damage.");
    }

    public static void ConfigureSceneEnemy(EnemyAIController enemy)
    {
        if (enemy == null || enemy.Kind != EnemyAIController.AttackType.Melee) return;
        var serialized = new SerializedObject(enemy);
        Set(serialized, "contactDamage", false); Set(serialized, "meleeDamage", 2f);
        Set(serialized, "damageMultiplier", 1f); Set(serialized, "attackWindup", .35f);
        Set(serialized, "meleeStrikeDelay", .15f); Set(serialized, "attackDuration", .85f);
        Set(serialized, "attackCooldown", .95f); Set(serialized, "attackRange", .45f);
        Set(serialized, "patrolSpeed", 1.2f); Set(serialized, "chaseSpeed", 2.2f);
        serialized.FindProperty("obstacleMask").intValue = LayerMask.GetMask("Default", "Ground", "Wall", "OneWayPlatform");
        serialized.ApplyModifiedPropertiesWithoutUndo();
        if (enemy.GetComponent<EnemyCombatPresentation>() == null) enemy.gameObject.AddComponent<EnemyCombatPresentation>();
        Animator animator = enemy.GetComponentInChildren<Animator>(true);
        AnimatorController controller = animator != null ? animator.runtimeAnimatorController as AnimatorController : null;
        if (controller != null)
        {
            BuildAttackClips(enemy, controller);
            animator.applyRootMotion = false; EditorUtility.SetDirty(animator);
        }
        EditorUtility.SetDirty(enemy);
    }

    private static void Set(SerializedObject serialized, string name, object value)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (value is bool flag) property.boolValue = flag;
        else property.floatValue = (float)value;
    }

    private static void BuildAttackClips(EnemyAIController enemy, AnimatorController controller)
    {
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState idle = machine.states.Select(value => value.state).FirstOrDefault(value => value.name == "Idle");
        AnimationClip idleClip = idle != null ? idle.motion as AnimationClip : null;
        if (idleClip == null) return;
        EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(idleClip);
        EditorCurveBinding spriteBinding = bindings.FirstOrDefault(binding => binding.type == typeof(SpriteRenderer));
        ObjectReferenceKeyframe[] idleKeys = AnimationUtility.GetObjectReferenceCurve(idleClip, spriteBinding);
        Sprite firstIdle = idleKeys != null ? idleKeys.Select(key => key.value as Sprite).FirstOrDefault(sprite => sprite != null) : null;
        if (firstIdle == null) return;
        string controllerFolder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(controller)).Replace('\\', '/');
        AnimationClip windup = Clip(controllerFolder + "/Windup.anim", new[] { firstIdle }, .35f, spriteBinding);
        AnimatorState windupState = machine.states.Select(value => value.state).FirstOrDefault(state => state.name == "Windup")
            ?? machine.AddState("Windup", new Vector3(480f, -100f));
        windupState.motion = windup; windupState.transitions = Array.Empty<AnimatorStateTransition>();

        string identity = controller.name + "/" + enemy.name;
        string rawFolder = identity.Contains("Monster3") ? "Assets/ArtRes/Monster/Melee/Monster3/MAttack" :
            identity.Contains("Monster5") ? "Assets/ArtRes/Monster/Melee/Monster5/Attack" : null;
        // Legacy MouseMonster has no dedicated attack frames. Its existing Attack motion remains, with lunge feedback.
        if (rawFolder != null && Directory.Exists(rawFolder))
        {
            Sprite[] frames = Directory.GetFiles(rawFolder, "*.png")
                .OrderBy(path => Regex.Replace(Path.GetFileName(path), @"\d+", match => match.Value.PadLeft(6, '0')))
                .Select(path => NormalizeFrame(path.Replace('\\', '/'), firstIdle.bounds.size.x)).ToArray();
            AnimationClip attack = Clip(controllerFolder + "/Attack.anim", frames, frames.Length / 12f, spriteBinding);
            AnimatorState attackState = machine.states.Select(value => value.state).FirstOrDefault(state => state.name == "Attack")
                ?? machine.AddState("Attack", new Vector3(480f, 0f));
            attackState.motion = attack; attackState.transitions = Array.Empty<AnimatorStateTransition>();
            EditorUtility.SetDirty(attackState);
        }
        EditorUtility.SetDirty(windupState); EditorUtility.SetDirty(machine); EditorUtility.SetDirty(controller);
    }

    private static Sprite NormalizeFrame(string path, float width)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) throw new InvalidOperationException("Missing melee attack sprite: " + path);
        if (Mathf.Abs(sprite.bounds.size.x - width) <= .001f) return sprite;
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        importer.spritePixelsPerUnit = sprite.rect.width / width; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static AnimationClip Clip(string path, Sprite[] sprites, float duration, EditorCurveBinding binding)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
        clip.ClearCurves(); clip.name = Path.GetFileNameWithoutExtension(path); clip.frameRate = 12f;
        var keys = new ObjectReferenceKeyframe[sprites.Length + 1];
        for (int i = 0; i < sprites.Length; i++) keys[i] = new ObjectReferenceKeyframe { time = i / 12f, value = sprites[i] };
        keys[sprites.Length] = new ObjectReferenceKeyframe { time = duration, value = sprites[sprites.Length - 1] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false; settings.startTime = 0f; settings.stopTime = duration;
        AnimationUtility.SetAnimationClipSettings(clip, settings); EditorUtility.SetDirty(clip); return clip;
    }
}
#endif
