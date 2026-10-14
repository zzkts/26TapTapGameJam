using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class EnemyCombatPolishTests
{
    [TestCase("Monster3")]
    [TestCase("Monster5")]
    public void OrdinaryMeleePrefabUsesTelegraphedTwoHpAttacks(string name)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefab/Enemy/Melee/" + name + ".prefab");
        var settings = new SerializedObject(prefab.GetComponent<EnemyAIController>());
        Assert.That(settings.FindProperty("contactDamage").boolValue, Is.False);
        Assert.That(settings.FindProperty("meleeDamage").floatValue * settings.FindProperty("damageMultiplier").floatValue, Is.EqualTo(2f));
        Assert.That(settings.FindProperty("attackWindup").floatValue, Is.GreaterThanOrEqualTo(.35f));
        Assert.That(settings.FindProperty("meleeStrikeDelay").floatValue, Is.GreaterThanOrEqualTo(.1f));
        Assert.That(settings.FindProperty("attackCooldown").floatValue, Is.GreaterThanOrEqualTo(.9f),
            "Every strike must leave enough recovery time to move away.");
        Assert.That(prefab.GetComponent<EnemyCombatPresentation>(), Is.Not.Null);
    }

    [TestCase("Monster3")]
    [TestCase("Monster5")]
    public void WindupAndAttackAreSeparateNonLoopingAnimations(string name)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/ArtRes/Animation/Gameplay/" + name + "/" + name + ".controller");
        var states = controller.layers[0].stateMachine.states.Select(child => child.state).ToArray();
        AnimationClip windup = states.Single(state => state.name == "Windup").motion as AnimationClip;
        AnimationClip attack = states.Single(state => state.name == "Attack").motion as AnimationClip;
        Assert.That(windup, Is.Not.Null); Assert.That(attack, Is.Not.Null);
        Assert.That(windup, Is.Not.EqualTo(attack));
        Assert.That(AnimationUtility.GetAnimationClipSettings(windup).loopTime, Is.False);
        Assert.That(AnimationUtility.GetAnimationClipSettings(attack).loopTime, Is.False);
        Assert.That(states.Single(state => state.name == "Windup").transitions, Is.Empty);
        Assert.That(states.Single(state => state.name == "Attack").transitions, Is.Empty);
        Sprite[] frames = AnimationUtility.GetObjectReferenceCurveBindings(attack).SelectMany(binding =>
            AnimationUtility.GetObjectReferenceCurve(attack, binding).Select(key => key.value as Sprite))
            .Where(sprite => sprite != null).Distinct().ToArray();
        Assert.That(frames.Length, Is.EqualTo(8));
        foreach (Sprite frame in frames)
            Assert.That(AssetDatabase.GetAssetPath(frame), Does.Contain(name == "Monster3" ? "/MAttack/" : "/Attack/"));
    }
}
