using System;
using UnityEngine;

public enum TileType { Grass, Water, Stone, Moss }

/// <summary>
/// Describes one kind of tile. Shown in the GridManager Inspector:
/// drag your own Sprite into the 'sprite' field to replace the placeholder.
/// </summary>
[Serializable]
public class TileDefinition
{
    public TileType type;
    public string displayName;
    [TextArea(2, 3)] public string description;

    [Header("Art (leave sprite empty to use the colored placeholder)")]
    public Sprite sprite;
    public Color tint = Color.white;

    [Header("Rules")]
    public bool walkable = true;
    [Min(1)] public int moveCost = 1;
    [Tooltip("HP healed at the start of the player's turn")]
    public int healPerTurn = 0;

    public static TileDefinition[] CreateDefaults()
    {
        return new[]
        {
            new TileDefinition { type = TileType.Grass, displayName = "Grass",
                description = "Easy to cross. No effect.",
                tint = new Color(0.55f, 0.76f, 0.40f), moveCost = 1 },
            new TileDefinition { type = TileType.Water, displayName = "Water",
                description = "Slow to wade through. Costs 2 movement.",
                tint = new Color(0.40f, 0.65f, 0.85f), moveCost = 2 },
            new TileDefinition { type = TileType.Stone, displayName = "Stone",
                description = "Solid rock. Nobody can enter.",
                tint = new Color(0.55f, 0.55f, 0.57f), walkable = false },
            new TileDefinition { type = TileType.Moss, displayName = "Moss",
                description = "Soft and healing. Restores 2 HP at the start of your turn.",
                tint = new Color(0.30f, 0.55f, 0.38f), moveCost = 1, healPerTurn = 2 },
        };
    }
}
