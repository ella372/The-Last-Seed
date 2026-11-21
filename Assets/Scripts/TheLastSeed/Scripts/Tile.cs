using UnityEngine;

/// <summary>One square of the map (an isometric block). Created by GridManager.</summary>
public class Tile : MonoBehaviour
{
    public int X { get; private set; }
    public int Y { get; private set; }
    public Vector2Int Coord => new Vector2Int(X, Y);
    public TileDefinition Def { get; private set; }

    public bool Walkable => Def.walkable;
    public int MoveCost => Def.moveCost;

    /// <summary>Sorting order of this tile. Units standing on it use SortOrder + 5.</summary>
    public int SortOrder => (X + Y) * 10;

    public Sprite PreviewSprite => body != null ? body.sprite : null;

    /// <summary>The unit standing on this tile (null if empty).</summary>
    public Unit Occupant { get; set; }

    SpriteRenderer body, overlay;

    public void Init(int x, int y, TileDefinition def)
    {
        X = x; Y = y; Def = def;

        body = gameObject.AddComponent<SpriteRenderer>();
        bool custom = def.sprite != null;
        body.sprite = custom ? def.sprite : IsoArt.Ground(def.type, Mathf.Abs(x * 7 + y * 13) % 3);
        body.sortingOrder = SortOrder;

        // Fit any sprite to exactly 1 unit wide (the width of one isometric tile).
        float w = Mathf.Max(0.01f, body.sprite.bounds.size.x);
        transform.localScale = Vector3.one * (1f / w);

        // Highlight overlay (blue = move, red = attack in later steps).
        var o = new GameObject("Highlight");
        o.transform.SetParent(transform, false);
        o.transform.localScale = Vector3.one * w;
        overlay = o.AddComponent<SpriteRenderer>();
        overlay.sprite = IsoArt.Diamond;
        overlay.sortingOrder = SortOrder + 1;
        overlay.color = Color.clear;

        // Stone is blocked: show a boulder on top (only if you did not assign your own sprite).
        if (def.type == TileType.Stone && !custom)
        {
            var b = new GameObject("Boulder");
            b.transform.SetParent(transform, false);
            b.transform.localScale = Vector3.one * w;
            var sr = b.AddComponent<SpriteRenderer>();
            sr.sprite = IsoArt.Boulder(Mathf.Abs(x + y) % 3);
            sr.sortingOrder = SortOrder + 3;
        }
    }

    public void SetHighlight(Color c) { overlay.color = c; }
    public void ClearHighlight() { overlay.color = Color.clear; }
}
