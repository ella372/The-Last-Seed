using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the isometric 8x8 grid from LevelData, plus the forest around it.
/// Grid coordinates: x goes to the lower-right, y goes to the lower-left, (0,0) is the top corner.
/// Handles grid <-> world conversion, clicks and highlights.
/// </summary>
public class GridManager : MonoBehaviour
{
    [Header("Tile types - assign your own Sprite on each")]
    public TileDefinition[] definitions;

    [Header("Camera")]
    public float viewSize = 3.9f;
    public float viewOffsetY = 0.3f;
    public Color skyColor = new Color(0.13f, 0.24f, 0.16f);

    public int Width { get; private set; }
    public int Height { get; private set; }

    /// <summary>Raised when the player clicks a tile.</summary>
    public event Action<Tile> TileClicked;

    static readonly Vector2Int[] Dirs = { new(0, 1), new(1, 0), new(0, -1), new(-1, 0) };

    Tile[,] tiles;
    Transform root, selector;
    Camera cam;

    void Reset() { definitions = TileDefinition.CreateDefaults(); }
    void Awake() { EnsureDefinitions(); }

    void EnsureDefinitions()
    {
        if (definitions == null || definitions.Length == 0) definitions = TileDefinition.CreateDefaults();
    }

    TileDefinition GetDefinition(TileType t)
    {
        foreach (var d in definitions) if (d.type == t) return d;
        foreach (var d in TileDefinition.CreateDefaults()) if (d.type == t) return d;
        return definitions[0];
    }

    public void Build(LevelData level)
    {
        EnsureDefinitions();
        if (root != null) Destroy(root.gameObject);
        root = new GameObject("Grid").transform;
        root.SetParent(transform, false);

        Width = level.Width;
        Height = level.Height;
        tiles = new Tile[Width, Height];

        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                var go = new GameObject($"Tile {x},{y}");
                go.transform.SetParent(root, false);
                go.transform.position = GridToWorld(x, y);
                var tile = go.AddComponent<Tile>();
                tile.Init(x, y, GetDefinition(level.GetTileType(x, y)));
                tiles[x, y] = tile;
            }

        BuildScenery();

        // Yellow diamond frame that shows the selected tile.
        selector = new GameObject("Selector").transform;
        selector.SetParent(root, false);
        var ssr = selector.gameObject.AddComponent<SpriteRenderer>();
        ssr.sprite = IsoArt.DiamondFrame;
        ssr.color = new Color(1f, 0.85f, 0.2f);
        ssr.sortingOrder = 5000;
        selector.gameObject.SetActive(false);

        SetupCamera();
    }

    // Dark forest floor around the map + trees (back), bushes, rocks and flowers (front).
    void BuildScenery()
    {
        const int R = 4;
        var scn = new GameObject("Scenery").transform;
        scn.SetParent(root, false);
        for (int y = -R; y < Height + R; y++)
            for (int x = -R; x < Width + R; x++)
            {
                if (InBounds(x, y)) continue;
                var g = new GameObject($"Ground {x},{y}");
                g.transform.SetParent(scn, false);
                g.transform.position = GridToWorld(x, y);
                var sr = g.AddComponent<SpriteRenderer>();
                sr.sprite = IsoArt.Ground(TileType.Grass, Mathf.Abs(x * 5 + y * 11) % 3, true);
                sr.sortingOrder = (x + y) * 10;

                float h = IsoArt.Hash(x, y, 42);
                int v = Mathf.Abs(x * 3 + y * 5) % 3;
                bool back = x < 0 || y < 0;      // tall trees only behind the map, never in front of it
                Sprite prop = null;
                if (back)
                {
                    if (h < .45f) prop = IsoArt.Tree(v);
                    else if (h < .60f) prop = IsoArt.Bush(v);
                    else if (h < .68f) prop = IsoArt.Rock(v);
                }
                else
                {
                    if (h < .20f) prop = IsoArt.Bush(v);
                    else if (h < .30f) prop = IsoArt.Rock(v);
                    else if (h < .50f) prop = IsoArt.Flowers(v);
                }
                if (prop == null) continue;

                var pg = new GameObject("Prop");
                pg.transform.SetParent(g.transform, false);
                var psr = pg.AddComponent<SpriteRenderer>();
                psr.sprite = prop;
                psr.sortingOrder = (x + y) * 10 + 3;
            }
    }

    void SetupCamera()
    {
        cam = Camera.main;
        if (cam == null) cam = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = viewSize;
        cam.transform.position = new Vector3(0, viewOffsetY, -10);
        cam.transform.rotation = Quaternion.identity;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = skyColor;
    }

    // ---------- Coordinates (isometric) ----------
    public Vector3 GridToWorld(int x, int y)
    {
        float dx = x - (Width - 1) / 2f, dy = y - (Height - 1) / 2f;
        return new Vector3((dx - dy) * 0.5f, -(dx + dy) * 0.25f, 0f);
    }

    public Vector3 GridToWorld(Vector2Int c) => GridToWorld(c.x, c.y);

    public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

    public Tile GetTile(int x, int y) => InBounds(x, y) ? tiles[x, y] : null;
    public Tile GetTile(Vector2Int c) => GetTile(c.x, c.y);

    /// <summary>The 4 orthogonal neighbours that exist on the map.</summary>
    public IEnumerable<Tile> Neighbors(Tile t)
    {
        foreach (var d in Dirs)
        {
            var n = GetTile(t.X + d.x, t.Y + d.y);
            if (n != null) yield return n;
        }
    }

    // ---------- Selection & highlights ----------
    public void SetSelected(Tile t)
    {
        if (selector == null) return;
        selector.gameObject.SetActive(t != null);
        if (t != null) selector.position = GridToWorld(t.X, t.Y);
    }

    public void ClearHighlights()
    {
        foreach (var t in tiles) t.ClearHighlight();
    }

    public void Highlight(IEnumerable<Tile> list, Color color)
    {
        foreach (var t in list) t.SetHighlight(color);
    }

    // ---------- Click ----------
    void Update()
    {
        if (tiles == null || cam == null || !InputHelper.PrimaryDown()) return;
        if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

        Vector2 p = InputHelper.PointerPosition();
        Vector3 w = cam.ScreenToWorldPoint(new Vector3(p.x, p.y, Mathf.Abs(cam.transform.position.z)));
        float dx = w.x - 2f * w.y, dy = -2f * w.y - w.x;   // inverse of GridToWorld
        int x = Mathf.RoundToInt(dx + (Width - 1) / 2f);
        int y = Mathf.RoundToInt(dy + (Height - 1) / 2f);

        var t = GetTile(x, y);
        if (t != null) TileClicked?.Invoke(t);
    }
}
