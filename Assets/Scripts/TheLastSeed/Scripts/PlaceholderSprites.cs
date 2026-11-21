using System;
using UnityEngine;

/// <summary>
/// Generates simple white sprites (square, circle, frame) in code.
/// They are used whenever you have not assigned your own Sprite in the Inspector.
/// All of them are exactly 1 world unit big.
/// </summary>
public static class PlaceholderSprites
{
    static Sprite square, circle, frame;

    public static Sprite Square =>
        square != null ? square : (square = Make(4, (x, y, s) => true, FilterMode.Point));

    public static Sprite Circle =>
        circle != null ? circle : (circle = Make(64, (x, y, s) =>
        {
            float dx = x - s / 2f + 0.5f, dy = y - s / 2f + 0.5f;
            return dx * dx + dy * dy <= (s / 2f) * (s / 2f);
        }, FilterMode.Bilinear));

    public static Sprite Frame =>
        frame != null ? frame : (frame = Make(32, (x, y, s) =>
            x < 3 || y < 3 || x >= s - 3 || y >= s - 3, FilterMode.Point));

    static Sprite Make(int size, Func<int, int, int, bool> inside, FilterMode filter)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = filter,
            wrapMode = TextureWrapMode.Clamp
        };
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                px[y * size + x] = inside(x, y, size) ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
