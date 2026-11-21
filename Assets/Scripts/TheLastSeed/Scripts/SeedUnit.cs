using UnityEngine;

/// <summary>The Last Seed: cannot move or attack. Grows one stage per turn if no enemy is adjacent.</summary>
public class SeedUnit : Unit
{
    public int Stage { get; private set; }
    public int MaxStage { get; private set; } = 5;
    Bar growBar;

    protected override Sprite DefaultSprite() => IsoArt.SeedPlant(0);
    protected override float BarY => 1.1f;
    protected override bool RemoveOnDeath => false;

    protected override void OnInit()
    {
        growBar = new Bar(transform, new Vector3(0f, BarY + .14f, 0f), .7f, new Color(.95f, .8f, .25f));
        growBar.Set(0f);
    }

    protected override void ApplySorting(int tileOrder)
    {
        base.ApplySorting(tileOrder);
        growBar.SetSorting(tileOrder + 8);
    }

    public void SetMaxStage(int max) { MaxStage = Mathf.Max(1, max); Refresh(); }

    public void Grow()
    {
        if (Stage >= MaxStage) return;
        Stage++;
        Refresh();
        FloatingText.Spawn(transform.position, "Growth +1", new Color(1f, .9f, .4f));
        NotifyChanged();
    }

   void Refresh()
{
    var sr = GetComponent<SpriteRenderer>();
    if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
    if (sr != null)
    {
        float scale = 1f + (Stage * 0.15f);
        transform.localScale = new Vector3(scale, scale, 1f);
    }
}

    public override string InfoLine() => $"{DisplayName}: HP {HP}/{MaxHP} · Growth {Stage}/{MaxStage}";
}
