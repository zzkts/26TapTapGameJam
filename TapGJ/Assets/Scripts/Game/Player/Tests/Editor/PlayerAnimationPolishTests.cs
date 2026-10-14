using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class PlayerAnimationPolishTests
{
    private const string Folder = "Assets/ArtRes/Animation/Gameplay/Player";

    [Test]
    public void EveryPlayablePoseUsesTheApprovedCharacterAndFixedFeetPivot()
    {
        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Folder + "/Player.controller");
        Assert.That(controller, Is.Not.Null);
        Sprite[] frames = controller.animationClips.SelectMany(clip =>
            AnimationUtility.GetObjectReferenceCurveBindings(clip).SelectMany(binding =>
                AnimationUtility.GetObjectReferenceCurve(clip, binding).Select(key => key.value as Sprite)))
            .Where(sprite => sprite != null).Distinct().ToArray();
        Assert.That(frames.Length, Is.GreaterThanOrEqualTo(16));
        foreach (Sprite frame in frames)
        {
            Assert.That(AssetDatabase.GetAssetPath(frame), Does.StartWith("Assets/ArtRes/Animation/PlayerPolished/"), frame.name);
            Assert.That(frame.rect.size, Is.EqualTo(frames[0].rect.size), frame.name);
            Assert.That(frame.pivot, Is.EqualTo(frames[0].pivot), frame.name);
        }
    }

    [TestCase("Attack")]
    [TestCase("Hurt")]
    [TestCase("Roll")]
    [TestCase("Dead")]
    [TestCase("Jump")]
    [TestCase("Land")]
    public void ActionPosesDoNotLoopOrAutomaticallyReturnToIdle(string name)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + "/" + name + ".anim");
        Assert.That(clip, Is.Not.Null);
        Assert.That(AnimationUtility.GetAnimationClipSettings(clip).loopTime, Is.False);
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder + "/Player.controller");
        AnimatorState state = controller.layers[0].stateMachine.states.Select(child => child.state).Single(value => value.name == name);
        Assert.That(state.transitions, Is.Empty, "A finished death pose must never become a living Idle sprite.");
    }

    [Test]
    public void FallUsesItsOwnAirPoseInsteadOfRestartingTheLaunchSequence()
    {
        AnimationClip fall = AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + "/Fall.anim");
        Assert.That(fall, Is.Not.Null);
        Assert.That(AnimationUtility.GetAnimationClipSettings(fall).loopTime, Is.True);
        foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(fall))
            foreach (ObjectReferenceKeyframe key in AnimationUtility.GetObjectReferenceCurve(fall, binding))
                Assert.That(AssetDatabase.GetAssetPath(key.value), Does.Contain("/PlayerPolished/Fall/"));
    }

    [Test]
    public void PlayerPrefabAlignsArtworkFeetToTheUnchangedCollisionBody()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefab/Player/Player.prefab");
        Assert.That(prefab.GetComponent<PlayerPresentation>(), Is.Not.Null);
        SpriteRenderer visual = prefab.GetComponentInChildren<Animator>().GetComponent<SpriteRenderer>();
        BoxCollider2D body = prefab.GetComponent<BoxCollider2D>();
        Assert.That(visual.transform.localPosition.y, Is.EqualTo(body.offset.y - body.size.y * .5f).Within(.001f));
        Assert.That(body.size, Is.EqualTo(new Vector2(1f, 1.4f)));
        Assert.That(body.offset, Is.EqualTo(new Vector2(0f, -.2f)));
    }
}
