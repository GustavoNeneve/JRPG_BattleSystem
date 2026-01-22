using UnityEngine;

namespace NewBark.Runtime
{
    public enum PokemonRarity
    {
        Normal,
        Albino,
        Melanistic
    }

    public static class RaritySystem
    {
        // Persistent counters - In a real save system these should be saved to disk
        private static int encountersSinceLastRare = 0;
        
        // Constants - User defined rules
        // "a cada 5000 monstros vistos 1 tem a chance de ser raro"
        // "se a pessoa ver 4999 monstros selvagens ou não o 5000 vai ser albino"
        private const int PITY_THRESHOLD = 5000;
        private const int RARE_CHANCE_DENOMINATOR = 5000; // 1 in 5000
        
        // "a cada 100 monstros albinos 6 deles podem ser Melanista" (6%)
        private const float MELANISTIC_CHANCE = 0.06f; 

        // Load/Save hooks (Placeholder for actual SaveSystem)
        public static void LoadState(int encounters)
        {
            encountersSinceLastRare = encounters;
        }

        public static int GetEncountersCount()
        {
            return encountersSinceLastRare;
        }

        public static PokemonRarity GenerateRarity()
        {
            encountersSinceLastRare++;
            
            bool isRare = false;

            // Check Pity
            if (encountersSinceLastRare >= PITY_THRESHOLD)
            {
                isRare = true;
                Debug.Log($"[RaritySystem] Pity Triggered! {encountersSinceLastRare} encounters.");
            }
            else
            {
                // Random Chance (1/5000)
                if (Random.Range(0, RARE_CHANCE_DENOMINATOR) == 0)
                {
                    isRare = true;
                    Debug.Log($"[RaritySystem] Lucky Rare! Encounter #{encountersSinceLastRare}");
                }
            }

            if (isRare)
            {
                // Reset counter ONLY if rare found
                encountersSinceLastRare = 0;

                // Determine type of Rare (Albino or Melanistic)
                // 6% chance for Melanistic
                if (Random.value < MELANISTIC_CHANCE)
                {
                     return PokemonRarity.Melanistic;
                }
                else
                {
                    return PokemonRarity.Albino;
                }
            }

            return PokemonRarity.Normal;
        }
    }
}
