using System;
using UnityEngine;

/// <summary>
/// Stats + art of one kind of unit. Shown in the GameManager Inspector:
/// drag your own Sprite into 'sprite' to replace the placeholder drawing.
/// </summary>
[Serializable]
public class UnitStats
{
    public string unitName = "Unit";
    [Tooltip("Leave empty to use the placeholder drawing")] public Sprite sprite;
    public int maxHP = 10;
    public int move = 3;
    public int attack = 3;
    public int range = 1;
    public int maxMP = 0;

    public UnitStats() { }
    public UnitStats(string name, int hp, int move, int attack, int range, int mp)
    {
        unitName = name; maxHP = hp; this.move = move; this.attack = attack; this.range = range; maxMP = mp;
    }
}
