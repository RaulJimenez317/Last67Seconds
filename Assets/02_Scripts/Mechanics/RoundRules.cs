using System;
using Codice.Enemies;
using UnityEngine;

namespace Codice.Mechanics
{
    public static class RoundRules
    {
        public const int FinalRound = 25;
        public static bool IsFinal(int round) => round == FinalRound;
        public static bool IsBoss(int round) => round >= 5 && round % 5 == 0;
        public static bool IsRegularBoss(int round) => IsBoss(round) && !IsFinal(round);
        public static bool IsColiseumRound(int round) => round >= 6 && round <= 10;
        public static string SceneForRound(int round)
        {
            if (IsFinal(round)) return "FinalArena";
            if (round >= 11 && round <= 15) return "Criptas";
            if (round >= 16 && round <= 20) return "Bastion";
            return IsColiseumRound(round) ? "Coliseo" : "Calabozo";
        }
        public static bool OpensPortalAfter(int round) => round > 0 && round < FinalRound && SceneForRound(round) != SceneForRound(round + 1);
        public static string MapTitle(int round)
        {
            switch(SceneForRound(round))
            {
                case "FinalArena": return "ARENA FINAL";
                case "Coliseo": return "COLISEO";
                case "Criptas": return "CRIPTAS DEL ECO";
                case "Bastion": return "BASTIÓN DE CENIZA";
                default: return "CALABOZO";
            }
        }

        // Total per normal wave, not the simultaneous population controlled by the encounter.
        public static int EnemyLimit(int round)
        {
            if (round < 1 || IsBoss(round)) return 0;
            return (int)Math.Min(int.MaxValue, (((long)round - 1) / 10 + 1) * 10);
        }

        // Keep the introductory five/eight enemies, then use the current block's ceiling.
        public static int EnemyCount(int round) => round == 1 ? 5 : round == 2 ? 8 : EnemyLimit(round);

        // x=swordsman, y=crossbowman, z=mage, w=heavy. Boss summons use their own rules.
        public static Vector4 SpawnProbabilities(int round)
        {
            if (round < 1 || IsBoss(round)) return Vector4.zero;
            if (round < 6) return new Vector4(1, 0, 0, 0);
            float mage = round < 11 ? 0 : .15f + .25f * (1 - Mathf.Exp(-(round - 11) / 30f));
            float heavy = round < 16 ? 0 : .10f + .40f * (1 - Mathf.Exp(-(round - 16) / 30f));
            float weak = 1 - mage - heavy;
            return new Vector4(weak * .65f, weak * .35f, mage, heavy);
        }

        public static EnemyKind SelectEnemy(Vector4 probabilities, float sample)
        {
            float cumulative = 0;
            EnemyKind lastEligible = EnemyKind.Swordsman;
            for (int i = 0; i < 4; i++)
            {
                if (probabilities[i] <= 0) continue;
                lastEligible = (EnemyKind)i;
                cumulative += probabilities[i];
                if (sample < cumulative) return lastEligible;
            }
            // Random.value can return exactly 1. Never fall through to a locked archetype.
            return lastEligible;
        }

        public static EnemyKind[] Roster(int round)
        {
            var result = new EnemyKind[EnemyCount(round)];
            var probabilities = SpawnProbabilities(round);
            for (int i = 0; i < result.Length; i++)
                result[i] = SelectEnemy(probabilities, UnityEngine.Random.value);
            return result;
        }
    }
}
