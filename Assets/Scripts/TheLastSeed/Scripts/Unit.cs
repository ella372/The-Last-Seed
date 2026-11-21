using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Small world-space bar (HP, growth) drawn above a unit.</summary>
public class Bar
{
    readonly SpriteRenderer bg, fill;
    readonly Vector3 pos;
    readonly float width;

    public Bar(Transform parent, Vector3 localPos, float width, Color color)
    {
        this.width = width; pos = localPos;
        bg = Make(parent, "BarBG", new Color(.16f, .09f, .05f));
        bg.transform.localPosition = localPos;
        bg.transform.localScale = new Vector3(width + .05f, .13f, 1f);
        fill = Make(parent, "BarFill", color);
        Set(1f);
    }

    static SpriteRenderer Make(Transform parent, string name, Color c)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent, false);
        var r = g.AddComponent<SpriteRenderer>();
        r.sprite = PlaceholderSprites.Square;
        r.color = c;
        return r;
    }

    public void Set(float frac)
    {
        frac = Mathf.Clamp01(frac);
        fill.transform.localScale = new Vector3(Mathf.Max(.0001f, width * frac), .08f, 1f);
        fill.transform.localPosition = pos + new Vector3(-width / 2f + width * frac / 2f, 0f, 0f);
    }

    public void SetSorting(int order) { bg.sortingOrder = order; fill.sortingOrder = order + 1; }
}

/// <summary>
/// Base class of every unit: HP, position on a tile, smooth movement, damage/heal feedback.
/// </summary>
public abstract class Unit : MonoBehaviour
{
    public UnitStats Stats { get; private set; }
    public int HP { get; protected set; }
    public int MaxHP => Stats.maxHP;
    public Tile Tile { get; private set; }
    public bool IsAlive => HP > 0;
    public string DisplayName => Stats.unitName;

    public event System.Action<Unit> Died;
    public event System.Action Changed;

    protected GridManager grid;
    protected SpriteRenderer sr;
    protected Transform body;
    protected Bar hpBar;
    Color baseColor = Color.white;

    protected abstract Sprite DefaultSprite();
    protected virtual Color BarColor => new Color(.4f, .78f, .32f);
    protected virtual float BarY => 1f;
    protected virtual bool RemoveOnDeath => true;
    protected virtual void OnInit() { }
    protected void NotifyChanged() { Changed?.Invoke(); }

    public void Init(UnitStats stats, GridManager g, Tile tile)
    {
        Stats = stats; grid = g; HP = stats.maxHP;

        body = new GameObject("Body").transform;
        body.SetParent(transform, false);
        sr = body.gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = stats.sprite != null ? stats.sprite : DefaultSprite();
        if (stats.sprite != null) body.localScale = Vector3.one * (1f / Mathf.Max(.01f, sr.sprite.bounds.size.y));

        hpBar = new Bar(transform, new Vector3(0f, BarY, 0f), .7f, BarColor);
        OnInit();
        PlaceOn(tile);
        hpBar.Set(HP / (float)MaxHP);
    }

    protected virtual void ApplySorting(int tileOrder)
    {
        sr.sortingOrder = tileOrder + 5;
        hpBar.SetSorting(tileOrder + 8);
    }

    public int DistanceTo(Unit other) => Mathf.Abs(Tile.X - other.Tile.X) + Mathf.Abs(Tile.Y - other.Tile.Y);

    /// <summary>Put the unit on a tile instantly.</summary>
    public void PlaceOn(Tile t)
    {
        if (Tile != null && Tile.Occupant == this) Tile.Occupant = null;
        Tile = t;
        t.Occupant = this;
        transform.position = grid.GridToWorld(t.X, t.Y);
        ApplySorting(t.SortOrder);
    }

    /// <summary>Walk along a path (list of tiles, first step first) with a small hop animation.</summary>
    public IEnumerator MoveAlong(List<Tile> path)
    {
        if (path == null || path.Count == 0) yield break;
        Tile.Occupant = null;
        foreach (var next in path)
        {
            Vector3 a = transform.position, b = grid.GridToWorld(next.X, next.Y);
            ApplySorting(Mathf.Max(Tile.SortOrder, next.SortOrder));
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.16f;
                float k = Mathf.Clamp01(t);
                transform.position = Vector3.Lerp(a, b, k);
                body.localPosition = new Vector3(0f, Mathf.Sin(k * Mathf.PI) * .12f, 0f);
                yield return null;
            }
            Tile = next;
        }
        body.localPosition = Vector3.zero;
        Tile.Occupant = this;
        transform.position = grid.GridToWorld(Tile.X, Tile.Y);
        ApplySorting(Tile.SortOrder);
    }

    /// <summary>Quick forward lunge toward a tile (attack animation).</summary>
    public IEnumerator Lunge(Tile target)
    {
        Vector3 dir = (grid.GridToWorld(target.X, target.Y) - transform.position).normalized * .3f;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.14f;
            body.localPosition = dir * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
            yield return null;
        }
        body.localPosition = Vector3.zero;
    }

    public void TakeDamage(int amount)
    {
        amount = Mathf.Max(0, amount);
        HP = Mathf.Max(0, HP - amount);
        hpBar.Set(HP / (float)MaxHP);
        FloatingText.Spawn(transform.position, "-" + amount, new Color(1f, .35f, .3f));
        StartCoroutine(HitFeedback());
        NotifyChanged();
        if (HP <= 0) StartCoroutine(Die());
    }

    public void Heal(int amount)
    {
        int before = HP;
        HP = Mathf.Min(MaxHP, HP + amount);
        if (HP == before) return;
        hpBar.Set(HP / (float)MaxHP);
        FloatingText.Spawn(transform.position, "+" + (HP - before), new Color(.45f, 1f, .45f));
        NotifyChanged();
    }

    IEnumerator HitFeedback()
    {
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            sr.color = (int)(t * 20f) % 2 == 0 ? new Color(1f, .4f, .4f) : baseColor;
            body.localPosition = new Vector3(UnityEngine.Random.Range(-.05f, .05f), 0f, 0f);
            yield return null;
        }
        sr.color = baseColor;
        body.localPosition = Vector3.zero;
    }

    IEnumerator Die()
    {
        if (Tile.Occupant == this) Tile.Occupant = null;
        Died?.Invoke(this);
        if (!RemoveOnDeath)
        {
            baseColor = new Color(.5f, .5f, .5f);
            yield break;
        }
        float t = 0f;
        yield return new WaitForSeconds(.3f);
        while (t < 0.35f)
        {
            t += Time.deltaTime;
            sr.color = new Color(1f, 1f, 1f, 1f - t / .35f);
            yield return null;
        }
        Destroy(gameObject);
    }

    /// <summary>One line for the info box.</summary>
    public virtual string InfoLine()
    {
        string s = $"{DisplayName}: HP {HP}/{MaxHP}";
        if (Stats.attack > 0) s += $" · ATK {Stats.attack}";
        if (Stats.move > 0) s += $" · Move {Stats.move}";
        if (Stats.range > 0) s += $" · Range {Stats.range}";
        return s;
    }
}
