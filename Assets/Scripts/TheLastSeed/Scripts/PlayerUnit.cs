using UnityEngine;

/// <summary>Mira, the player. Tracks MP and what she already did this turn.</summary>
public class PlayerUnit : Unit
{
    public int MP { get; private set; }
    public int MaxMP => Stats.maxMP;

    public bool HasMoved { get; private set; }
    public bool HasAttacked { get; private set; }
    public bool ActionsDone { get; private set; }

    protected override Sprite DefaultSprite() => IsoArt.Mira();
    protected override float BarY => 1.15f;
    protected override bool RemoveOnDeath => false;
    protected override void OnInit() { MP = MaxMP; }

    public void BeginTurn() { HasMoved = false; HasAttacked = false; ActionsDone = false; NotifyChanged(); }
    public void MarkMoved() { HasMoved = true; NotifyChanged(); }
    public void MarkAttacked() { HasAttacked = true; ActionsDone = true; NotifyChanged(); }   // attack ends her actions
    public void EndActions() { ActionsDone = true; NotifyChanged(); }                          // skill / wait
    public void SpendMP(int n) { MP = Mathf.Max(0, MP - n); NotifyChanged(); }
}
