using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class SceneMusicAssetTests
{
    [TestCase(SceneMusicPlayer.MenuMusicPath, 50f, 60f)]
    [TestCase(SceneMusicPlayer.GameMusicPath, 250f, 260f)]
    public void LongMusicIsStereoAndStreamsWithoutPreloadingPCM(string resource, float min, float max)
    {
        var clip = Resources.Load<AudioClip>(resource);
        Assert.That(clip, Is.Not.Null, resource);
        Assert.That(clip.length, Is.InRange(min, max));
        Assert.That(clip.channels, Is.EqualTo(2));
        Assert.That(clip.loadType, Is.EqualTo(AudioClipLoadType.Streaming));
        var importer = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip));
        Assert.That(importer.defaultSampleSettings.preloadAudioData, Is.False);
        Assert.That(importer.loadInBackground, Is.True);
    }
}
