using System;
using System.Collections.Generic;

/// <summary>
/// Dijkstra search on the grid. Each step costs the MoveCost of the tile entered.
/// Blocked (non-walkable) tiles are never entered. A tile that holds a unit is a dead end
/// (you may reach it, if canEnter allows, but never walk through it).
/// </summary>
public static class Pathfinding
{
    public class Result
    {
        public readonly Dictionary<Tile, int> Cost = new Dictionary<Tile, int>();
        public readonly Dictionary<Tile, Tile> Prev = new Dictionary<Tile, Tile>();

        /// <summary>Tiles to walk through, from the first step to the target (start not included).</summary>
        public List<Tile> PathTo(Tile target)
        {
            if (!Cost.ContainsKey(target)) return null;
            var path = new List<Tile>();
            var cur = target;
            while (Prev.ContainsKey(cur)) { path.Add(cur); cur = Prev[cur]; }
            path.Reverse();
            return path;
        }
    }

    public static Result Search(GridManager grid, Tile start, int maxCost, Func<Tile, bool> canEnter)
    {
        var res = new Result();
        res.Cost[start] = 0;
        var open = new List<Tile> { start };

        while (open.Count > 0)
        {
            Tile cur = open[0];
            foreach (var o in open) if (res.Cost[o] < res.Cost[cur]) cur = o;
            open.Remove(cur);

            if (cur != start && cur.Occupant != null) continue;   // dead end

            foreach (var n in grid.Neighbors(cur))
            {
                if (!n.Walkable || !canEnter(n)) continue;
                int nc = res.Cost[cur] + n.MoveCost;
                if (nc > maxCost) continue;
                if (!res.Cost.TryGetValue(n, out int old) || nc < old)
                {
                    res.Cost[n] = nc;
                    res.Prev[n] = cur;
                    if (!open.Contains(n)) open.Add(n);
                }
            }
        }
        return res;
    }
}
