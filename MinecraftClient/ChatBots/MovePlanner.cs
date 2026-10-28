using System;
using System.Collections.Generic;
using System.Threading;
using MinecraftClient.Mapping;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// Path search for the agent bridge's movement (docs/agent-bridge.md, version 3): a weighted A* over block cells
    /// with a hard limit on the cells it expands, so a search never costs more than a few milliseconds of one core.
    /// When the goal is not reached (out of the loaded terrain, too far for the limit, walled off) it gives the path to
    /// the standing cell closest to the goal, if that brings the account at least <c>minProgress</c> nearer; the caller
    /// walks there and searches again.
    /// </summary>
    internal static class MovePlanner
    {
        /// <summary>The weight of the remaining distance: a little greedy, so long open walks expand few cells.</summary>
        private const double Weight = 1.5;

        /// <summary>A block position: the cell a player's feet are in.</summary>
        internal readonly record struct Cell(int X, int Y, int Z)
        {
            public static Cell Of(Location location) =>
                new((int)Math.Floor(location.X), (int)Math.Floor(location.Y), (int)Math.Floor(location.Z));

            /// <summary>The middle of the cell's floor, where a waypoint stands.</summary>
            public Location Center => new(X + 0.5, Y, Z + 0.5);

            public double DistanceTo(Cell other)
            {
                double dx = X - other.X, dy = Y - other.Y, dz = Z - other.Z;
                return Math.Sqrt(dx * dx + dy * dy + dz * dz);
            }
        }

        /// <summary>A path from the start (its first cell) to the goal or the best cell found; how many cells were expanded.</summary>
        internal sealed record Plan(IReadOnlyList<Cell> Path, bool Reached, int Expanded);

        /// <param name="start">Where the account stands.</param>
        /// <param name="isGoal">A cell that is the goal (it must also be one the walk may end on).</param>
        /// <param name="heuristic">The remaining distance from a cell to the goal, in blocks.</param>
        /// <param name="neighbors">The cells one move away that a player can move into.</param>
        /// <param name="canEnd">A cell a walk may end on: the player stands there.</param>
        /// <param name="maxNodes">At most this many cells are expanded.</param>
        /// <param name="minProgress">A path that does not reach the goal must bring the account this much nearer.</param>
        /// <param name="ct">Cancels the search (a newer operation replaced it, or it took too long).</param>
        /// <returns>The plan, or null when no cell brings the account nearer (or the search was cancelled).</returns>
        internal static Plan? Find(Cell start, Func<Cell, bool> isGoal, Func<Cell, double> heuristic,
            Func<Cell, IEnumerable<Cell>> neighbors, Func<Cell, bool> canEnd, int maxNodes, double minProgress,
            CancellationToken ct)
        {
            PriorityQueue<Cell, double> open = new();
            Dictionary<Cell, double> cost = new() { [start] = 0 };
            Dictionary<Cell, Cell> cameFrom = new();
            HashSet<Cell> closed = new();
            double startRemaining = heuristic(start);
            Cell? best = null;
            double bestRemaining = double.MaxValue;
            int expanded = 0;

            open.Enqueue(start, Weight * startRemaining);
            while (open.TryDequeue(out Cell cell, out _))
            {
                if (ct.IsCancellationRequested)
                    return null;
                if (!closed.Add(cell))
                    continue;

                if (isGoal(cell) && canEnd(cell))
                    return new Plan(Rebuild(cameFrom, cell), true, expanded);

                if (cell != start && canEnd(cell))
                {
                    double remaining = heuristic(cell);
                    if (remaining < bestRemaining)
                    {
                        bestRemaining = remaining;
                        best = cell;
                    }
                }

                if (++expanded > maxNodes)
                    break;

                double here = cost[cell];
                foreach (Cell next in neighbors(cell))
                {
                    if (closed.Contains(next))
                        continue;
                    double through = here + cell.DistanceTo(next);
                    if (cost.TryGetValue(next, out double known) && known <= through)
                        continue;
                    cost[next] = through;
                    cameFrom[next] = cell;
                    open.Enqueue(next, through + Weight * heuristic(next));
                }
            }

            if (best is { } found && startRemaining - bestRemaining >= minProgress)
                return new Plan(Rebuild(cameFrom, found), false, expanded);
            return null;
        }

        private static List<Cell> Rebuild(Dictionary<Cell, Cell> cameFrom, Cell end)
        {
            List<Cell> path = [end];
            Cell cell = end;
            while (cameFrom.TryGetValue(cell, out Cell previous))
            {
                path.Add(previous);
                cell = previous;
            }

            path.Reverse();
            return path;
        }
    }
}
