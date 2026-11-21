using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Draws the cozy forest art IN CODE (isometric tiles, trees, bushes, rocks, flowers, vignette).
/// These are only placeholders: any Sprite you assign in the Inspector replaces them.
/// All sprites use 128 pixels per unit, so one isometric tile is 1 unit wide and 0.5 unit tall.
/// </summary>
public static class IsoArt
{
    // ---------- tiny pixel canvas ----------
    class Pix
    {
        public readonly int W, H;
        readonly Color[] d;
        public Pix(int w, int h) { W = w; H = h; d = new Color[w * h]; }

        public void Blend(int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= W || y >= H || c.a <= 0f) return;
            Color o = d[y * W + x];
            float a = c.a + o.a * (1f - c.a);
            d[y * W + x] = new Color(
                (c.r * c.a + o.r * o.a * (1f - c.a)) / a,
                (c.g * c.a + o.g * o.a * (1f - c.a)) / a,
                (c.b * c.a + o.b * o.a * (1f - c.a)) / a, a);
        }

        public void Ellipse(float cx, float cy, float rx, float ry, Color c)
        {
            for (int y = Mathf.FloorToInt(cy - ry); y <= Mathf.CeilToInt(cy + ry); y++)
                for (int x = Mathf.FloorToInt(cx - rx); x <= Mathf.CeilToInt(cx + rx); x++)
                {
                    float u = (x + 0.5f - cx) / rx, v = (y + 0.5f - cy) / ry;
                    if (u * u + v * v <= 1f) Blend(x, y, c);
                }
        }

        public void Box(int x0, int y0, int x1, int y1, Color c)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++) Blend(x, y, c);
        }

        public Sprite ToSprite(Vector2 pivot, float ppu = 128f)
        {
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false)
            { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels(d);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, W, H), pivot, ppu);
        }
    }

    // ---------- helpers ----------
    static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);
    static Color Dk(Color c, float f) => new Color(c.r * f, c.g * f, c.b * f, 1f);

    public static float Hash(int x, int y, int seed)
    {
        unchecked
        {
            int n = x * 374761393 + y * 668265263 + seed * 362437;
            n = (n ^ (n >> 13)) * 1274126177;
            n ^= n >> 16;
            return (n & 0xFFFF) / 65535f;
        }
    }

    static float Noise(float x, float y, float cell, int seed)
    {
        float fx = x / cell, fy = y / cell;
        int ix = Mathf.FloorToInt(fx), iy = Mathf.FloorToInt(fy);
        float tx = fx - ix, ty = fy - iy;
        tx = tx * tx * (3f - 2f * tx); ty = ty * ty * (3f - 2f * ty);
        float a = Hash(ix, iy, seed), b = Hash(ix + 1, iy, seed), c = Hash(ix, iy + 1, seed), d = Hash(ix + 1, iy + 1, seed);
        return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
    }

    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
    static Sprite Cached(string key, Func<Sprite> make)
    {
        if (cache.TryGetValue(key, out var s) && s != null) return s;
        s = make();
        cache[key] = s;
        return s;
    }

    // ---------- ground tiles (isometric block: top face + two sides) ----------
    public static Sprite Ground(TileType type, int variant, bool dark = false) =>
        Cached($"g{(int)type}_{variant}_{dark}", () => MakeGround(type, variant, dark));

    static Sprite MakeGround(TileType type, int variant, bool dark)
    {
        const int W = 128, T = 26, H = 64 + T;
        var p = new Pix(W, H);
        int seed = variant * 97 + (int)type * 13 + (dark ? 5 : 0);

        Color a, b;
        switch (type)
        {
            case TileType.Water: a = C(.36f, .64f, .84f); b = C(.27f, .52f, .76f); break;
            case TileType.Stone: a = C(.62f, .58f, .50f); b = C(.50f, .47f, .41f); break;
            case TileType.Moss:  a = C(.30f, .55f, .36f); b = C(.20f, .43f, .31f); break;
            default:             a = C(.47f, .73f, .31f); b = C(.37f, .63f, .27f); break;
        }
        if (dark) { a = Dk(a, .78f); b = Dk(b, .78f); }

        float cx = 64f, cy = T + 32f;
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float dx = Mathf.Abs(x + 0.5f - cx);
                float dd = dx / 64f + Mathf.Abs(y + 0.5f - cy) / 32f;
                if (dd <= 1f)
                {
                    Color c = Color.Lerp(a, b, Noise(x, y, 12f, seed));
                    if (Hash(x, y, seed + 7) > .94f) c = Color.Lerp(c, b, .7f);
                    if (dd > .94f) c = Dk(c, .72f);
                    p.Blend(x, y, c);
                }
                else if (y >= dx / 2f && y < T + dx / 2f)
                {
                    bool left = x < 64;
                    Color earth = left ? C(.52f, .36f, .24f) : C(.41f, .28f, .19f);
                    earth = Color.Lerp(earth, Dk(earth, .8f), Noise(x, y, 6f, seed + 3));
                    if (y >= T + dx / 2f - 6f) earth = Dk(Color.Lerp(a, b, .5f), left ? .95f : .8f); // grassy lip
                    if (y < dx / 2f + 1.5f) earth = Dk(earth, .7f);
                    p.Blend(x, y, earth);
                }
            }

        // Small details on the top face
        for (int i = 0; i < 9; i++)
        {
            float u = Hash(i, 1, seed) * 2f - 1f, v = Hash(i, 2, seed) * 2f - 1f;
            if (Mathf.Abs(u) + Mathf.Abs(v) > .78f) continue;
            float px = cx + u * 64f, py = cy + v * 32f;
            switch (type)
            {
                case TileType.Grass:
                    if (i < 6)
                    {
                        p.Ellipse(px, py + 2, 1.2f, 3f, Dk(b, .75f));
                        p.Ellipse(px + 3, py + 1, 1.1f, 2.4f, Dk(b, .8f));
                    }
                    else
                    {
                        Color fl = i % 3 == 0 ? C(.98f, .98f, .95f) : i % 3 == 1 ? C(1f, .85f, .25f) : C(1f, .55f, .72f);
                        p.Ellipse(px, py, 2.4f, 1.7f, fl);
                        p.Ellipse(px, py, .9f, .6f, C(.95f, .75f, .15f));
                    }
                    break;
                case TileType.Water:
                    p.Box((int)px - 6, (int)py, (int)px + 6, (int)py, C(1, 1, 1, .55f));
                    p.Box((int)px - 3, (int)py - 3, (int)px + 3, (int)py - 3, C(1, 1, 1, .35f));
                    break;
                case TileType.Stone:
                    p.Ellipse(px, py - 1, 4.5f, 2.4f, Dk(b, .7f));
                    p.Ellipse(px, py, 4f, 2.2f, Color.Lerp(a, Color.white, .25f));
                    break;
                case TileType.Moss:
                    if (i < 5) p.Ellipse(px, py, 2.2f, 1.5f, C(.75f, .95f, .45f, .9f));
                    else
                    {
                        p.Box((int)px - 1, (int)py - 3, (int)px + 1, (int)py, C(.95f, .9f, .78f));
                        p.Ellipse(px, py + 1.5f, 4.8f, 3.2f, C(.85f, .25f, .22f));
                        p.Ellipse(px - 1.5f, py + 2.2f, .9f, .7f, Color.white);
                        p.Ellipse(px + 1.8f, py + 1.4f, .9f, .7f, Color.white);
                    }
                    break;
            }
        }
        return p.ToSprite(new Vector2(0.5f, (T + 32f) / H));
    }

    // ---------- diamond overlays (highlights / selector) ----------
    public static Sprite Diamond => Cached("diamond", () => MakeDiamond(false));
    public static Sprite DiamondFrame => Cached("diamondframe", () => MakeDiamond(true));

    static Sprite MakeDiamond(bool frame)
    {
        var p = new Pix(128, 64);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 128; x++)
            {
                float dd = Mathf.Abs(x + .5f - 64f) / 64f + Mathf.Abs(y + .5f - 32f) / 32f;
                if (dd <= 1f && (!frame || dd > .9f)) p.Blend(x, y, Color.white);
            }
        return p.ToSprite(new Vector2(.5f, .5f));
    }

    // ---------- props (pivot = base of the object) ----------
    public static Sprite Tree(int v) => Cached("tree" + v, () =>
    {
        var p = new Pix(128, 200);
        Color dark, mid, lite;
        if (v == 0) { dark = C(.16f, .38f, .22f); mid = C(.24f, .52f, .28f); lite = C(.42f, .70f, .34f); }
        else if (v == 1) { dark = C(.30f, .42f, .18f); mid = C(.46f, .58f, .22f); lite = C(.66f, .76f, .30f); }
        else { dark = C(.14f, .36f, .30f); mid = C(.22f, .50f, .38f); lite = C(.40f, .68f, .50f); }

        p.Ellipse(64, 18, 40, 11, C(0, 0, 0, .25f));
        p.Box(58, 16, 70, 76, C(.42f, .28f, .18f));
        p.Box(65, 16, 70, 76, C(.32f, .21f, .14f));
        float[,] blobs = { { 64, 120, 44 }, { 38, 102, 30 }, { 90, 104, 30 }, { 64, 156, 34 }, { 44, 138, 28 }, { 84, 140, 28 } };
        for (int k = 0; k < blobs.GetLength(0); k++) p.Ellipse(blobs[k, 0], blobs[k, 1], blobs[k, 2] + 2, blobs[k, 2] + 2, Dk(dark, .7f));
        for (int k = 0; k < blobs.GetLength(0); k++) p.Ellipse(blobs[k, 0], blobs[k, 1], blobs[k, 2], blobs[k, 2], dark);
        for (int k = 0; k < blobs.GetLength(0); k++) p.Ellipse(blobs[k, 0] - 5, blobs[k, 1] + 6, blobs[k, 2] * .75f, blobs[k, 2] * .7f, mid);
        for (int k = 0; k < blobs.GetLength(0); k++) p.Ellipse(blobs[k, 0] - 10, blobs[k, 1] + 12, blobs[k, 2] * .4f, blobs[k, 2] * .35f, lite);
        return p.ToSprite(new Vector2(.5f, 16f / 200f));
    });

    public static Sprite Bush(int v) => Cached("bush" + v, () =>
    {
        var p = new Pix(100, 64);
        Color dark = v == 1 ? C(.30f, .42f, .20f) : C(.18f, .42f, .25f);
        Color mid = v == 1 ? C(.46f, .58f, .26f) : C(.28f, .56f, .30f);
        p.Ellipse(50, 10, 36, 8, C(0, 0, 0, .22f));
        float[,] b = { { 30, 24, 18 }, { 54, 28, 22 }, { 76, 22, 16 } };
        for (int k = 0; k < 3; k++) p.Ellipse(b[k, 0], b[k, 1], b[k, 2] + 1.5f, b[k, 2] + 1.5f, Dk(dark, .7f));
        for (int k = 0; k < 3; k++) p.Ellipse(b[k, 0], b[k, 1], b[k, 2], b[k, 2], dark);
        for (int k = 0; k < 3; k++) p.Ellipse(b[k, 0] - 3, b[k, 1] + 4, b[k, 2] * .7f, b[k, 2] * .65f, mid);
        for (int i = 0; i < 7; i++)
        {
            float x = 16 + Hash(i, v, 11) * 66f, y = 14 + Hash(i, v, 12) * 26f;
            Color fl = i % 3 == 0 ? C(1f, .55f, .72f) : i % 3 == 1 ? C(1f, .85f, .25f) : C(.98f, .98f, .95f);
            p.Ellipse(x, y, 2.4f, 2.4f, fl);
        }
        return p.ToSprite(new Vector2(.5f, 8f / 64f));
    });

    public static Sprite Rock(int v) => Cached("rock" + v, () =>
    {
        var p = new Pix(90, 60);
        p.Ellipse(45, 10, 34, 8, C(0, 0, 0, .22f));
        p.Ellipse(45, 26, 33, 21, C(.30f, .30f, .33f));
        p.Ellipse(45, 28, 31, 19, C(.58f, .58f, .60f));
        p.Ellipse(36, 34, 17, 9, C(.74f, .74f, .76f));
        p.Ellipse(54, 40, 15, 5, C(.32f, .56f, .30f));
        return p.ToSprite(new Vector2(.5f, 8f / 60f));
    });

    public static Sprite Boulder(int v) => Cached("boulder" + v, () =>
    {
        var p = new Pix(128, 120);
        p.Ellipse(64, 16, 50, 13, C(0, 0, 0, .25f));
        p.Ellipse(50, 48, 42, 36, C(.28f, .28f, .31f));
        p.Ellipse(84, 40, 32, 28, C(.28f, .28f, .31f));
        p.Ellipse(50, 50, 40, 34, C(.56f, .56f, .59f));
        p.Ellipse(84, 42, 30, 26, C(.50f, .50f, .53f));
        p.Ellipse(38, 62, 20, 12, C(.74f, .74f, .77f));
        p.Ellipse(80, 54, 14, 8, C(.68f, .68f, .71f));
        p.Box(60, 30, 61, 52, C(.30f, .30f, .33f));
        p.Ellipse(52, 78, 24, 8, C(.32f, .56f, .30f));
        p.Ellipse(90, 62, 10, 4, C(.32f, .56f, .30f));
        return p.ToSprite(new Vector2(.5f, 14f / 120f));
    });

    public static Sprite Flowers(int v) => Cached("flowers" + v, () =>
    {
        var p = new Pix(70, 44);
        for (int i = 0; i < 7; i++)
        {
            int x = 8 + (int)(Hash(i, v, 21) * 54f), y = 8 + (int)(Hash(i, v, 22) * 20f);
            p.Box(x, 2, x, y, C(.28f, .55f, .25f));
            Color fl = i % 3 == 0 ? C(1f, .55f, .72f) : i % 3 == 1 ? C(1f, .85f, .25f) : C(.98f, .98f, .95f);
            p.Ellipse(x, y + 1, 3.2f, 3.2f, fl);
            p.Ellipse(x, y + 1, 1.1f, 1.1f, C(.95f, .7f, .15f));
        }
        return p.ToSprite(new Vector2(.5f, 2f / 44f));
    });

    // ---------- characters (pivot = feet, 90 pixels per unit) ----------
    public static Sprite Mira() => Cached("mira", () =>
    {
        var p = new Pix(64, 96);
        Color skin = C(1f, .84f, .72f), hair = C(.86f, .38f, .20f), hairD = C(.62f, .24f, .12f);
        Color coat = C(.28f, .52f, .34f), coatD = C(.16f, .36f, .22f), eye = C(.2f, .12f, .08f);
        p.Ellipse(32, 7, 20, 5.5f, C(0, 0, 0, .25f));
        p.Box(23, 7, 28, 22, C(.36f, .24f, .16f)); p.Box(36, 7, 41, 22, C(.36f, .24f, .16f));   // boots
        p.Box(17, 18, 46, 52, Dk(coat, .6f)); p.Box(18, 19, 45, 51, coat);                        // coat
        for (int x = 21; x <= 43; x += 6) p.Box(x, 19, x + 1, 51, coatD);                         // plaid
        for (int y = 24; y <= 48; y += 8) p.Box(18, y, 45, y + 1, coatD);
        p.Box(12, 26, 17, 48, coat); p.Box(46, 26, 51, 48, coat);                                 // arms
        p.Ellipse(14.5f, 24, 3.2f, 3.2f, skin); p.Ellipse(48.5f, 24, 3.2f, 3.2f, skin);           // hands
        p.Ellipse(32, 66, 17, 17, hairD);                                                         // hair back
        p.Box(15, 44, 21, 68, hair); p.Box(43, 44, 49, 68, hair);                                 // long hair
        p.Ellipse(32, 62, 13, 14, skin);                                                          // face
        p.Ellipse(32, 72, 15, 8, hair); p.Ellipse(24, 69, 7, 6, hair); p.Ellipse(40, 69, 7, 6, hair); // fringe
        p.Ellipse(27, 60, 1.8f, 2.4f, eye); p.Ellipse(37, 60, 1.8f, 2.4f, eye);                   // eyes
        p.Ellipse(24, 56, 3, 2, C(1f, .6f, .6f, .5f)); p.Ellipse(40, 56, 3, 2, C(1f, .6f, .6f, .5f)); // blush
        return p.ToSprite(new Vector2(.5f, 6f / 96f), 90f);
    });

    public static Sprite Capcap() => Cached("capcap", () =>
    {
        var p = new Pix(80, 90);
        Color cream = C(.97f, .91f, .78f), red = C(.86f, .20f, .20f), redD = C(.52f, .10f, .12f), ink = C(.15f, .08f, .06f);
        p.Ellipse(40, 7, 24, 6, C(0, 0, 0, .25f));
        p.Ellipse(40, 24, 17, 19, Dk(cream, .6f)); p.Ellipse(40, 24, 15.5f, 17.5f, cream);        // body
        p.Ellipse(40, 52, 36, 26, redD); p.Ellipse(40, 53, 34, 24, red);                           // cap
        p.Ellipse(30, 60, 14, 8, C(1f, .45f, .4f, .6f));
        p.Ellipse(24, 54, 6.5f, 5, Color.white); p.Ellipse(50, 62, 7.5f, 5.5f, Color.white);
        p.Ellipse(60, 48, 4.5f, 3.8f, Color.white); p.Ellipse(38, 44, 3.6f, 3f, Color.white);
        p.Ellipse(33, 25, 3f, 3.6f, ink); p.Ellipse(47, 25, 3f, 3.6f, ink);                        // eyes
        p.Ellipse(33.8f, 26.4f, 1f, 1.2f, Color.white); p.Ellipse(47.8f, 26.4f, 1f, 1.2f, Color.white);
        p.Box(28, 32, 31, 32, ink); p.Box(32, 31, 34, 31, ink); p.Box(35, 30, 36, 30, ink);        // angry brows
        p.Box(49, 32, 52, 32, ink); p.Box(46, 31, 48, 31, ink); p.Box(44, 30, 45, 30, ink);
        p.Box(37, 17, 43, 17, ink);                                                                // mouth
        return p.ToSprite(new Vector2(.5f, 6f / 90f), 90f);
    });

    public static Sprite Sporeling() => Cached("sporeling", () =>
    {
        var p = new Pix(64, 72);
        Color cream = C(.93f, .88f, .96f), cap = C(.58f, .34f, .74f), capD = C(.34f, .18f, .46f), ink = C(.15f, .08f, .2f);
        p.Ellipse(32, 6, 17, 4.5f, C(0, 0, 0, .25f));
        p.Ellipse(32, 17, 11.5f, 13, Dk(cream, .6f)); p.Ellipse(32, 17, 10, 11.5f, cream);
        p.Ellipse(32, 37, 25, 18, capD); p.Ellipse(32, 38, 23.5f, 16.5f, cap);
        p.Ellipse(24, 44, 9, 5, C(.85f, .65f, 1f, .5f));
        p.Ellipse(20, 38, 4, 3.2f, C(.95f, .9f, 1f)); p.Ellipse(40, 44, 4.5f, 3.4f, C(.95f, .9f, 1f)); p.Ellipse(45, 33, 3, 2.5f, C(.95f, .9f, 1f));
        p.Ellipse(28, 18, 2.4f, 3f, ink); p.Ellipse(36, 18, 2.4f, 3f, ink);
        for (int k = 0; k < 5; k++) p.Ellipse(8 + Hash(k, 3, 5) * 48f, 50 + Hash(k, 4, 5) * 18f, 2f, 2f, C(.85f, 1f, .5f, .8f)); // spores
        return p.ToSprite(new Vector2(.5f, 5f / 72f), 90f);
    });

    // The Last Seed: grows from a glowing bud (stage 0) to a flower (stage 6).
    public static Sprite SeedPlant(int stage)
    {
        stage = Mathf.Clamp(stage, 0, 6);
        return Cached("seed" + stage, () =>
        {
            var p = new Pix(64, 96);
            Color green = C(.34f, .68f, .30f), greenD = C(.18f, .42f, .20f), lime = C(.65f, 1f, .30f);
            p.Ellipse(32, 8, 24, 9, C(0, 0, 0, .22f));
            p.Ellipse(32, 10, 20, 8, C(.36f, .24f, .15f)); p.Ellipse(32, 12, 17, 6, C(.50f, .34f, .22f));   // mound
            int h = 6 + stage * 9;
            float top = 12 + h;
            if (stage > 0) { p.Box(31, 12, 33, 12 + h, greenD); p.Box(31, 12, 32, 12 + h, green); }
            if (stage >= 1) { p.Ellipse(24, 12 + h * .55f, 8, 3.6f, green); p.Ellipse(40, 12 + h * .4f, 8, 3.6f, green); }
            if (stage >= 3) { p.Ellipse(23, 12 + h * .85f, 9, 4f, green); p.Ellipse(41, 12 + h * .75f, 9, 4f, green); }
            if (stage >= 6)
            {
                p.Ellipse(32, top + 2, 18, 18, C(1f, .95f, .6f, .25f));
                for (int k = 0; k < 8; k++)
                {
                    float a = k * Mathf.PI / 4f;
                    p.Ellipse(32 + Mathf.Cos(a) * 9, top + 2 + Mathf.Sin(a) * 9, 5.5f, 5.5f, k % 2 == 0 ? C(1f, .75f, .85f) : Color.white);
                }
                p.Ellipse(32, top + 2, 6, 6, C(1f, .85f, .25f));
            }
            else
            {
                p.Ellipse(32, top + 4, 12, 12, C(.7f, 1f, .35f, .25f));
                p.Ellipse(32, top + 4, 5, 7, lime);
            }
            return p.ToSprite(new Vector2(.5f, 6f / 96f), 90f);
        });
    }

    // ---------- screen vignette (dark edges, like the reference) ----------
    public static Sprite Vignette() => Cached("vignette", () =>
    {
        const int S = 256;
        var p = new Pix(S, S);
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = (x + .5f) / S * 2f - 1f, w = (y + .5f) / S * 2f - 1f;
                float r = Mathf.Sqrt(u * u + w * w);
                float a = Mathf.Clamp01((r - .55f) / .85f) * .65f;
                p.Blend(x, y, new Color(.03f, .08f, .04f, a));
            }
        return p.ToSprite(new Vector2(.5f, .5f));
    });
}
