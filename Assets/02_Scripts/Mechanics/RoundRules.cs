using System.Collections.Generic;
using Codice.Enemies;
using UnityEngine;

namespace Codice.Mechanics
{
    public static class RoundRules
    {
        public static bool IsBoss(int round) => round >= 5 && round % 5 == 0;
        public static EnemyKind[] Roster(int round)
        {
            if (round < 1 || IsBoss(round)) return new EnemyKind[0];
            int[] counts = round == 1 ? new[] { 3, 2, 0, 0 } : round == 2 ? new[] { 4, 2, 2, 0 } : round == 3 ? new[] { 5, 3, 3, 1 } : round == 4 ? new[] { 6, 4, 4, 2 } : null;
            if (counts == null)
            {
                int[] totals = { 20, 25, 30, 36, 42, 48, 55, 62, 70, 78, 86, 95 };
                int index = (round / 5 - 1) * 4 + round % 5 - 1;
                int total = index < totals.Length ? totals[index] : 95 + (index - 11) * 10;
                counts = new[] { 0, total / 4, total / 4, Mathf.Max(1, total / 8) };
                counts[0] = total - counts[1] - counts[2] - counts[3];
            }
            var result = new List<EnemyKind>();
            // Interleave archetypes so both halves contain a representative mixture.
            while (counts[0] + counts[1] + counts[2] + counts[3] > 0)
                for (int i = 0; i < 4; i++) if (counts[i] > 0) { result.Add((EnemyKind)i); counts[i]--; }
            return result.ToArray();
        }
    }
}
