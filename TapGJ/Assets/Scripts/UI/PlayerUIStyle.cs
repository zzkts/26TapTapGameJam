using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Small reusable UI shapes and readable gene symbols, independent of the artwork atlas.</summary>
public static class PlayerUIStyle
{
    public static readonly Color Ink = new Color(0.12f, 0.11f, 0.20f);
    public static readonly Color Violet = new Color(0.29f, 0.23f, 0.43f);
    public static readonly Color Mint = new Color(0.43f, 0.94f, 0.77f);
    public static readonly Color Cream = new Color(1f, 0.96f, 0.85f);
    public static readonly Color Gold = new Color(1f, 0.80f, 0.36f);
    private static Sprite rounded, circle;
    private static readonly Dictionary<int, Sprite> icons = new Dictionary<int, Sprite>();

    public static Sprite Rounded
    {
        get
        {
            if (rounded == null)
            {
                Painter painter = new Painter(96);
                painter.RoundRect(new Vector2(48f, 48f), new Vector2(46f, 46f), 19f);
                rounded = painter.Sprite("Player UI Rounded", new Vector4(24f, 24f, 24f, 24f));
            }
            return rounded;
        }
    }

    public static Sprite Circle
    {
        get
        {
            if (circle == null)
            {
                Painter painter = new Painter(96);
                painter.Disc(new Vector2(48f, 48f), 46f);
                circle = painter.Sprite("Player UI Circle");
            }
            return circle;
        }
    }

    public static Color GeneColor(Modifier gene)
    {
        if (gene == null) return Cream;
        if (!gene.IsAttribute) return new Color(0.87f, 0.86f, 0.96f);
        if (gene.Element == E_ElementType.Fire) return new Color(1f, 0.57f, 0.33f);
        if (gene.Element == E_ElementType.Poison) return new Color(0.68f, 0.96f, 0.40f);
        return new Color(0.42f, 0.88f, 1f);
    }

    public static Sprite GeneIcon(Modifier gene)
    {
        int key = gene == null ? -1 : gene.IsAttribute ? 100 + (int)gene.Element + (gene.IsVortex ? 20 : 0) : (int)gene.ModifierType;
        if (icons.TryGetValue(key, out Sprite cached) && cached != null) return cached;
        Painter p = new Painter(96);
        Vector2 center = new Vector2(48f, 48f);

        if (gene != null && gene.IsAttribute)
        {
            if (gene.IsVortex)
            {
                Vector2 previous = center;
                for (int i = 1; i <= 64; i++)
                {
                    float angle = i * 0.18f;
                    float radius = 3f + i * 0.42f;
                    Vector2 next = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    p.Line(previous, next, 5f); previous = next;
                }
                p.Disc(previous, 7f);
            }
            else if (gene.Element == E_ElementType.Fire)
            {
                p.Polygon(new Vector2(49, 83), new Vector2(66, 58), new Vector2(71, 72), new Vector2(81, 46),
                    new Vector2(78, 29), new Vector2(64, 16), new Vector2(43, 13), new Vector2(26, 23),
                    new Vector2(19, 42), new Vector2(26, 60), new Vector2(33, 46));
                p.Line(new Vector2(44, 25), new Vector2(50, 46), 5f);
                p.Line(new Vector2(50, 46), new Vector2(58, 34), 5f);
            }
            else if (gene.Element == E_ElementType.Poison)
            {
                p.Polygon(new Vector2(23, 24), new Vector2(22, 46), new Vector2(37, 65), new Vector2(73, 80),
                    new Vector2(75, 48), new Vector2(65, 30), new Vector2(43, 20));
                p.Line(new Vector2(21, 17), new Vector2(67, 69), 4f);
                p.Disc(new Vector2(21, 76), 6f); p.Disc(new Vector2(12, 59), 4f);
            }
            else
            {
                p.Disc(new Vector2(48, 37), 25f);
                p.Polygon(new Vector2(23, 41), new Vector2(48, 83), new Vector2(73, 41));
                p.Disc(new Vector2(76, 75), 5f);
            }
        }
        else
        {
            E_ModifierType type = gene != null ? gene.ModifierType : E_ModifierType.None;
            switch (type)
            {
                case E_ModifierType.Atk:
                    p.Polygon(new Vector2(25, 14), new Vector2(39, 17), new Vector2(76, 68), new Vector2(74, 82), new Vector2(60, 79), new Vector2(22, 29));
                    p.Line(new Vector2(17, 33), new Vector2(43, 14), 5); break;
                case E_ModifierType.DamageMultiplier:
                    p.Polygon(new Vector2(48, 83), new Vector2(80, 48), new Vector2(48, 13), new Vector2(16, 48));
                    p.Disc(new Vector2(79, 78), 5); p.Disc(new Vector2(18, 17), 5); break;
                case E_ModifierType.ProjectileCount:
                    p.Disc(new Vector2(30, 28), 13); p.Disc(new Vector2(68, 28), 13); p.Disc(new Vector2(48, 66), 13); break;
                case E_ModifierType.ProjectileSize:
                    p.Disc(center, 19); p.Line(new Vector2(15, 58), new Vector2(15, 80), 4);
                    p.Line(new Vector2(15, 80), new Vector2(37, 80), 4); p.Line(new Vector2(81, 38), new Vector2(81, 16), 4);
                    p.Line(new Vector2(81, 16), new Vector2(59, 16), 4); break;
                case E_ModifierType.ProjectileLifetime:
                    p.Ring(center, 31, 5); p.Line(center, new Vector2(48, 72), 5); p.Line(center, new Vector2(67, 35), 5); break;
                case E_ModifierType.Pierce:
                    p.Line(new Vector2(34, 16), new Vector2(34, 80), 4); p.Line(new Vector2(57, 16), new Vector2(57, 80), 4);
                    p.Arrow(new Vector2(12, 48), new Vector2(83, 48), 5); break;
                case E_ModifierType.Rebound:
                case E_ModifierType.ReboundCoefficient:
                    p.Line(new Vector2(15, 22), new Vector2(42, 74), 5); p.Arrow(new Vector2(42, 74), new Vector2(77, 25), 5);
                    p.Line(new Vector2(24, 82), new Vector2(72, 82), 4); break;
                case E_ModifierType.ProjectileMove:
                    p.Ring(center, 27, 5); p.Disc(new Vector2(48, 77), 10); p.Disc(center, 5);
                    p.Line(new Vector2(18, 43), new Vector2(27, 34), 5); p.Line(new Vector2(27, 34), new Vector2(31, 48), 5); break;
                case E_ModifierType.Split:
                    p.Line(new Vector2(48, 15), new Vector2(48, 42), 5);
                    p.Line(new Vector2(48, 42), new Vector2(24, 69), 5); p.Line(new Vector2(48, 42), new Vector2(72, 69), 5);
                    p.Disc(new Vector2(22, 73), 9); p.Disc(new Vector2(74, 73), 9); break;
                case E_ModifierType.ProjectileSpeedMultiplier:
                    p.Arrow(new Vector2(14, 30), new Vector2(74, 30), 5); p.Arrow(new Vector2(14, 66), new Vector2(74, 66), 5); break;
                case E_ModifierType.ProjectileSpeed:
                    p.Arrow(new Vector2(29, 48), new Vector2(80, 48), 6); p.Line(new Vector2(13, 65), new Vector2(35, 65), 4);
                    p.Line(new Vector2(13, 31), new Vector2(35, 31), 4); break;
                default:
                    p.Ring(center, 24, 6); p.Line(new Vector2(48, 10), new Vector2(48, 29), 6);
                    p.Line(new Vector2(48, 67), new Vector2(48, 86), 6); p.Line(new Vector2(10, 48), new Vector2(29, 48), 6);
                    p.Line(new Vector2(67, 48), new Vector2(86, 48), 6); p.Disc(center, 7); break;
            }
        }
        Sprite result = p.Sprite("Player Gene Symbol " + key);
        icons[key] = result;
        return result;
    }

    private sealed class Painter
    {
        private readonly int size;
        private readonly float[] alpha;
        public Painter(int size) { this.size = size; alpha = new float[size * size]; }
        private void Draw(Func<Vector2, float> distance)
        {
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                int index = y * size + x;
                alpha[index] = Mathf.Max(alpha[index], Mathf.Clamp01(0.5f - distance(new Vector2(x + 0.5f, y + 0.5f))));
            }
        }
        public void Disc(Vector2 center, float radius) => Draw(point => Vector2.Distance(point, center) - radius);
        public void Ring(Vector2 center, float radius, float width) => Draw(point => Mathf.Abs(Vector2.Distance(point, center) - radius) - width * 0.5f);
        public void RoundRect(Vector2 center, Vector2 halfSize, float radius) => Draw(point =>
        {
            Vector2 q = new Vector2(Mathf.Abs(point.x - center.x), Mathf.Abs(point.y - center.y)) - halfSize + Vector2.one * radius;
            return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
        });
        private static float SegmentDistance(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 delta = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(point - a, delta) / Mathf.Max(delta.sqrMagnitude, 0.001f));
            return Vector2.Distance(point, a + delta * t);
        }
        public void Line(Vector2 a, Vector2 b, float width) => Draw(point => SegmentDistance(point, a, b) - width * 0.5f);
        public void Arrow(Vector2 a, Vector2 b, float width)
        {
            Line(a, b, width); Vector2 direction = (b - a).normalized;
            Vector2 side = new Vector2(-direction.y, direction.x);
            Line(b, b - direction * 15f + side * 13f, width);
            Line(b, b - direction * 15f - side * 13f, width);
        }
        public void Polygon(params Vector2[] points) => Draw(point =>
        {
            bool inside = false; float distance = float.MaxValue;
            for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
            {
                Vector2 a = points[j], b = points[i]; distance = Mathf.Min(distance, SegmentDistance(point, a, b));
                if ((a.y > point.y) != (b.y > point.y) && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
            }
            return inside ? -distance : distance;
        });
        public Sprite Sprite(string name, Vector4 border = default)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            { name = name, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            Color32[] pixels = new Color32[alpha.Length];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha[i] * 255f));
            texture.SetPixels32(pixels); texture.Apply(false, false);
            Sprite sprite = UnityEngine.Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, 100f, 0, SpriteMeshType.FullRect, border);
            sprite.name = name; sprite.hideFlags = HideFlags.HideAndDontSave; return sprite;
        }
    }
}
