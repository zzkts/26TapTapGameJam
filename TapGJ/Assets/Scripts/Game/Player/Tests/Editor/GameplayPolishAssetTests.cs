using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class GameplayPolishAssetTests
{
    [TestCase("Player")]
    [TestCase("Monster3")]
    [TestCase("Monster4")]
    [TestCase("Monster5")]
    [TestCase("Monster6")]
    [TestCase("Monster7")]
    public void AnimationFramesKeepAConsistentWorldWidth(string name)
    {
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/ArtRes/Animation/Gameplay/" + name + "/" + name + ".controller");
        Assert.That(controller, Is.Not.Null);
        Sprite[] frames = controller.animationClips.SelectMany(clip =>
            AnimationUtility.GetObjectReferenceCurveBindings(clip).SelectMany(binding =>
                AnimationUtility.GetObjectReferenceCurve(clip, binding).Select(key => key.value as Sprite)))
            .Where(sprite => sprite != null).ToArray();
        Assert.That(frames.Length, Is.GreaterThan(1));
        foreach (Sprite frame in frames)
            Assert.That(frame.bounds.size.x, Is.EqualTo(frames[0].bounds.size.x).Within(.003f), frame.name);
    }
    [Test]
    public void ElementShaderCompilesWithoutErrors()
    {
        Shader shader = Shader.Find("TapGJ/ElementSprite");
        Assert.That(shader, Is.Not.Null); Assert.That(ShaderUtil.ShaderHasError(shader), Is.False);
    }
    [Test]
    public void CombatLayersAndTerrainCollisionsExist()
    {
        foreach (string name in new[] {"PlayerInvulnerable","EnemyBullet","Pickup","OneWayPlatform"})
            Assert.That(LayerMask.NameToLayer(name), Is.GreaterThanOrEqualTo(8), name);
        Assert.That(Physics2D.GetIgnoreLayerCollision(LayerMask.NameToLayer("Enemy"), 0), Is.False);
        Assert.That(Physics2D.GetIgnoreLayerCollision(LayerMask.NameToLayer("PlayerInvulnerable"),LayerMask.NameToLayer("EnemyBullet")), Is.True);
    }
    [Test]
    public void MainMenuDoesNotStartTheDialogueTestUI()
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scene/BeginScene.unity");
        try
        {
            BeginPanel menu = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<BeginPanel>(true)).Single();
            Assert.That(menu.gameObject.activeInHierarchy, Is.True);
            Assert.That(menu.btnContinue, Is.Not.Null);
            Canvas canvas = menu.GetComponentInParent<Canvas>();
            Assert.That(canvas.GetComponentInChildren<DialogScripts.DialogKeyboard>(true), Is.Null);
            Assert.That(menu.GetComponent<BeginScenePresentation>(), Is.Not.Null);
            Assert.That(menu.GetComponent<BeginScenePresentation>().hero.sprite, Is.Not.Null);
            Assert.That(menu.transform.Find("Game Title"), Is.Not.Null);
            Assert.That(scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SettingPanel>(true)).Count(), Is.EqualTo(1));
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
    [TestCase("Assets/Scene/BeginScene.unity")]
    [TestCase("Assets/Scene/GameScene.unity")]
    [TestCase("Assets/Scene/SporeScene.unity")]
    [TestCase("Assets/Scene/CoreScene.unity")]
    public void SceneObjectsHaveNoBrokenReferences(string path)
    {
        var scene = EditorSceneManager.OpenPreviewScene(path);
        try { foreach (GameObject root in scene.GetRootGameObjects()) CheckHierarchyReferences(root, path); }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
    [Test]
    public void GameplayPrefabsHaveNoBrokenReferences()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab",new[] {"Assets/Resources/Prefab"}))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CheckHierarchyReferences(AssetDatabase.LoadAssetAtPath<GameObject>(path), path);
        }
    }
    private static void CheckHierarchyReferences(GameObject root, string path)
    {
        foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
        {
            Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject), Is.Zero, path + ":" + transform.name);
            foreach (Component component in transform.GetComponents<Component>())
            {
                if (component == null) continue;
                var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();
                while (property.Next(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null)
                        Assert.That(property.objectReferenceInstanceIDValue, Is.Zero, path + ":" + transform.name + "." + property.propertyPath);
            }
        }
    }
}
