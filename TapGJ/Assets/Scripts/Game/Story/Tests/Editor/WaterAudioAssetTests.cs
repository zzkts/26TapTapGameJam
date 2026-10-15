using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed class WaterAudioAssetTests
{
    [Test]
    public void EveryNamedCueIsARealShortMonoAudioAsset()
    {
        Assert.That(GameAudioPresentation.CueNames.Distinct().Count(),Is.EqualTo(31));
        foreach(string name in GameAudioPresentation.CueNames)
        {
            var clip=Resources.Load<AudioClip>("Audio/WaterDrain/"+name);
            Assert.That(clip,Is.Not.Null,name);Assert.That(clip.channels,Is.EqualTo(1),name);
            Assert.That(clip.frequency,Is.EqualTo(44100),name);
            Assert.That(clip.length,Is.InRange(.07f,3f),name);
        }
    }
    [Test]
    public void AudioIsNonSilentAndHasSafePeaksAndSmoothEndpoints()
    {
        foreach(string name in GameAudioPresentation.CueNames)
        {
            var clip=Resources.Load<AudioClip>("Audio/WaterDrain/"+name);clip.LoadAudioData();
            var data=new float[clip.samples];Assert.That(clip.GetData(data,0),Is.True,name);
            Assert.That(data.Max(x=>Mathf.Abs(x)),Is.InRange(.01f,.8f),name);
            Assert.That(Mathf.Abs(data[0]),Is.LessThan(.002f),name);
            Assert.That(Mathf.Abs(data[data.Length-1]),Is.LessThan(.002f),name);
            Assert.That(Mathf.Abs(data.Average()),Is.LessThan(.002f),name);
        }
    }
}
