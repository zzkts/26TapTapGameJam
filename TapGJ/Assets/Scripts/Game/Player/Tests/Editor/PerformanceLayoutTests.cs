using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public sealed class PerformanceLayoutTests
{
    [TestCase(448)]
    [TestCase(310)]
    public void LongPromptFitsInsideItsCard(float width)
    {
        var card=new GameObject("prompt",typeof(RectTransform));
        try
        {
            var label=GameplayUIFactory.Label("text",card.transform,Vector2.zero,Vector2.one,
                AssetDatabase.LoadAssetAtPath<Font>("Assets/ArtRes/Fonts/PlayerUI/NotoSansCJKsc-Medium.otf"),18,"",TextAnchor.MiddleLeft);
            InteractionPromptLayout.Apply((RectTransform)card.transform,label,"最终回流阀 · 查看胜利目标",
                "泵站输出清水 → 菌林与核心各接通两节点 → 击败守卫 → 选择水网的未来",width);
            var outer=((RectTransform)card.transform).rect;
            Assert.That(label.verticalOverflow,Is.EqualTo(VerticalWrapMode.Truncate));
            Assert.That(label.rectTransform.anchoredPosition.y,Is.GreaterThanOrEqualTo(16));
            Assert.That(label.rectTransform.anchoredPosition.x+label.rectTransform.rect.width,Is.LessThan(outer.width-20));
            Assert.That(label.rectTransform.rect.height,Is.GreaterThanOrEqualTo(label.preferredHeight-1));
        }
        finally{Object.DestroyImmediate(card);}
    }
    [Test]
    public void AtlasesPackAllThirtyGameplaySpritesWithoutProceduralTextures()
    {
        Assert.That(EditorSettings.spritePackerMode,Is.EqualTo(SpritePackerMode.SpriteAtlasV2));
        int count=0;
        foreach(string name in new[]{"World","Player","PlayerHUD"})
        {
            string path="Assets/ArtRes/Atlases/"+name+".spriteatlasv2";
            var atlas=AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            Assert.That(atlas,Is.Not.Null,name);
            var importer=(SpriteAtlasImporter)AssetImporter.GetAtPath(path);
            Assert.That(importer.packingSettings.enableRotation,Is.False);
            Assert.That(importer.packingSettings.enableTightPacking,Is.False);
            Assert.That(importer.textureSettings.readable,Is.False);
            Assert.That(importer.includeInBuild,Is.True);
            Assert.That(atlas.GetPackables().Any(p=>AssetDatabase.GetAssetPath(p).Contains("WaterProjectiles")||AssetDatabase.GetAssetPath(p).Contains("-source")),Is.False);
            var sprites=new Sprite[atlas.spriteCount];atlas.GetSprites(sprites);
            try
            {
                Assert.That(sprites.All(s=>s!=null&&s.packed),Is.True,name+" must have real packed render data");
                Assert.That(sprites.Select(s=>s.texture.GetInstanceID()).Distinct().Count(),Is.EqualTo(1),name);
                count+=sprites.Length;
            }
            finally{foreach(var sprite in sprites)if(sprite!=null)Object.DestroyImmediate(sprite);}
        }
        Assert.That(count,Is.EqualTo(30));
    }
}
