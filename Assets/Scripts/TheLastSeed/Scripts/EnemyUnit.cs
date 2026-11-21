using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyKind { Capcap, Sporeling }

/// <summary>A mushroom enemy. Its whole turn (move + attack) is in TakeTurn.</summary>
public class EnemyUnit : Unit
{
    public EnemyKind Kind;

    protected override Sprite DefaultSprite() => Kind == EnemyKind.Capcap ? IsoArt.Capcap() : IsoArt.Sporeling();
    protected override Color BarColor => new Color(.85f, .25f, .2f);
    protected override float BarY => Kind == EnemyKind.Capcap ? 1.08f : 0.88f;

    public IEnumerator TakeTurn(GridManager g, List<Unit> targets)
    {
        // 1) Not in range yet? Find the closest target by path cost and walk toward it.
        if (BestInRange(targets, Tile) == null)
        {
            var res = Pathfinding.Search(g, Tile, 99, tl => tl.Occupant == null || targets.Contains(tl.Occupant));
            Unit goal = null;
            int best = int.MaxValue;
            foreach (var tg in targets)
                if (tg.IsAlive && res.Cost.TryGetValue(tg.Tile, out int c) && c < best) { best = c; goal = tg; }

            if (goal != null)
            {
                var path = res.PathTo(goal.Tile);
                path.RemoveAt(path.Count - 1);                 // cannot stand on the target's tile
                var steps = new List<Tile>();
                int spent = 0;
                foreach (var step in path)
                {
                    if (spent + step.MoveCost > Stats.move) break;
                    spent += step.MoveCost;
                    steps.Add(step);
                    if (BestInRange(targets, step) != null) break;   // already close enough to attack
                }
                if (steps.Count > 0) yield return MoveAlong(steps);
            }
        }

        // 2) Attack the target in range with the lowest HP.
        var victim = BestInRange(targets, Tile);
        if (victim != null)
        {
            yield return Lunge(victim.Tile);
            victim.TakeDamage(Stats.attack);
            yield return new WaitForSeconds(.3f);
        }
    }

    Unit BestInRange(List<Unit> targets, Tile origin)
    {
        Unit best = null;
        foreach (var tg in targets)
        {
            if (!tg.IsAlive) continue;
            int d = Mathf.Abs(origin.X - tg.Tile.X) + Mathf.Abs(origin.Y - tg.Tile.Y);
            if (d <= Stats.range && (best == null || tg.HP < best.HP)) best = tg;
        }
        return best;
    }
}
