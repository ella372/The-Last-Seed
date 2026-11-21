using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SpawnEntry
{
    public EnemyKind kind;
    public Vector2Int pos;
    public SpawnEntry() { }
    public SpawnEntry(EnemyKind k, int x, int y) { kind = k; pos = new Vector2Int(x, y); }
}

[Serializable]
public class Reinforcement
{
    [Tooltip("Appears at the start of this turn")] public int turn = 3;
    public EnemyKind kind;
    public Vector2Int pos;
    public Reinforcement() { }
    public Reinforcement(int turn, EnemyKind k, int x, int y) { this.turn = turn; kind = k; pos = new Vector2Int(x, y); }
}

/// <summary>
/// One level, described by data. Create your own: right click in Project > Create > The Last Seed > Level Data,
/// then drag it into GameManager > Levels. Map legend: G = Grass, W = Water, S = Stone, M = Moss.
/// Positions are (x, y) with (0,0) = top corner of the map.
/// </summary>
[CreateAssetMenu(menuName = "The Last Seed/Level Data", fileName = "Level")]
public class LevelData : ScriptableObject
{
    public string levelName = "Level 1";
    public string objective = "Protect the seed until it blooms";
    public string[] rows =
    {
        "GGGGGGGG", "GGGGGGGG", "GGGGGGGG", "GGGGGGGG",
        "GGGGGGGG", "GGGGGGGG", "GGGGGGGG", "GGGGGGGG",
    };
    public Vector2Int miraStart = new Vector2Int(3, 6);
    public Vector2Int seedPos = new Vector2Int(4, 5);
    public int seedStagesNeeded = 5;
    public List<SpawnEntry> enemies = new List<SpawnEntry>();
    public List<Reinforcement> reinforcements = new List<Reinforcement>();

    public int Height => rows.Length;
    public int Width => rows.Length == 0 ? 0 : rows[0].Length;

    public TileType GetTileType(int x, int y)
    {
        if (y < 0 || y >= rows.Length || x < 0 || x >= rows[y].Length) return TileType.Grass;
        switch (char.ToUpperInvariant(rows[y][x]))
        {
            case 'W': return TileType.Water;
            case 'S': return TileType.Stone;
            case 'M': return TileType.Moss;
            default: return TileType.Grass;
        }
    }

    // ---------- the 3 built-in levels (used when GameManager > Levels is empty) ----------
    public static LevelData[] CreateBuiltIn() => new[] { Level1(), Level2(), Level3() };

    static LevelData Make(string name, string[] rows, int mx, int my, int sx, int sy, int stages)
    {
        var l = CreateInstance<LevelData>();
        l.levelName = name; l.rows = rows;
        l.miraStart = new Vector2Int(mx, my); l.seedPos = new Vector2Int(sx, sy);
        l.seedStagesNeeded = stages;
        return l;
    }

    static LevelData Level1()
    {
        var l = Make("Level 1", new[]
        {
            "GGGGGGGG",
            "GGGGGGMG",
            "GGGGGGGG",
            "GMGGGGGG",
            "GGGGGGGG",
            "GGGMGGGG",
            "GGGGGGGG",
            "GGGGGGGG",
        }, 3, 6, 4, 5, 5);
        l.enemies.Add(new SpawnEntry(EnemyKind.Capcap, 0, 1));
        l.enemies.Add(new SpawnEntry(EnemyKind.Capcap, 6, 0));
        return l;
    }

    static LevelData Level2()
    {
        var l = Make("Level 2", new[]
        {
            "GGGSGGGG",
            "GGGGGWWG",
            "GMGGGWGG",
            "GGGSGGGG",
            "GWWGGGGG",
            "GGGGGSGG",
            "GGGGMGGG",
            "GGGGGGGG",
        }, 3, 6, 4, 5, 5);
        l.enemies.Add(new SpawnEntry(EnemyKind.Capcap, 0, 0));
        l.enemies.Add(new SpawnEntry(EnemyKind.Capcap, 7, 0));
        l.enemies.Add(new SpawnEntry(EnemyKind.Sporeling, 7, 3));
        return l;
    }

    static LevelData Level3()
    {
        var l = Make("Level 3", new[]
        {
            "GGSGGGSG",
            "GGGGWWGG",
            "GMGGWGGG",
            "GGGSGGMG",
            "GGGGGGGG",
            "GWGGSGGG",
            "GGGMGGGG",
            "GGGGGGGG",
        }, 3, 5, 3, 4, 6);
        l.enemies.Add(new SpawnEntry(EnemyKind.Capcap, 0, 0));
        l.enemies.Add(new SpawnEntry(EnemyKind.Capcap, 7, 0));
        l.enemies.Add(new SpawnEntry(EnemyKind.Capcap, 7, 7));
        l.enemies.Add(new SpawnEntry(EnemyKind.Sporeling, 0, 7));
        l.enemies.Add(new SpawnEntry(EnemyKind.Sporeling, 6, 1));
        l.reinforcements.Add(new Reinforcement(3, EnemyKind.Capcap, 7, 4));   // extra Capcap on turn 3, map edge
        return l;
    }
}
