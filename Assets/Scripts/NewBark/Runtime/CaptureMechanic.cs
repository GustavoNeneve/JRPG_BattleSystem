using UnityEngine;
using NewBark.Data;

namespace NewBark.Runtime
{
    public static class CaptureMechanic
    {
        /// <summary>
        /// Attempts to catch a target Pokemon using a specific ball.
        /// Consumes the ball if specified.
        /// </summary>
        /// <returns>True if caught, False otherwise.</returns>
        public static bool TryCatchPokemon(PokemonInstance target, string ballItemId, bool consumeBall = true)
        {
            // 1. Consume Ball
            if (consumeBall)
            {
                if (!InventoryManager.Instance.RemoveItem(ballItemId, 1))
                {
                    Debug.Log("[Capture] No ball found!");
                    return false;
                }
            }

            // 2. Get Data
            ItemData ballData = GameDatabase.Instance.Items.ContainsKey(ballItemId) ? GameDatabase.Instance.Items[ballItemId] : null;
            if (ballData == null)
            {
                Debug.LogError($"[Capture] Invalid Ball ID: {ballItemId}");
                return false;
            }
            
            // Note: ItemData.catchRate seems to be the multiplier. 
            // In Gen 3/4, PokeBall = 1x, GreatBall = 1.5x, UltraBall = 2x.
            // Our JSON for PokeBall has catchRate: 1. Let's assume it works as multiplier.
            float ballMod = ballData.catchRate > 0 ? ballData.catchRate : 1f;

            // 3. Calculate Catch Rate
            // Formula: a = (((3 * MaxHP - 2 * HP) * Rate * BallMod) / (3 * MaxHP)) * StatusMod
            
            float maxHP = target.MaxHP;
            float currentHP = target.CurrentHP;
            // Specie catch rate (0-255)
            float speciesRate = target.BaseData != null ? target.BaseData.forms[0].catchRate : 45; 
            
            // Status Mod: Sleep/Freeze = 2.5, Paralyze/Poison/Burn = 1.5. None = 1.
            // We don't have Status implemented yet in Instance, assume 1.
            float statusMod = 1f; 

            float numerator = ((3 * maxHP) - (2 * currentHP)) * speciesRate * ballMod;
            float denominator = (3 * maxHP);
            
            float a = (numerator / denominator) * statusMod;

            Debug.Log($"[Capture] Chance Calculation: MaxHP={maxHP}, CurHP={currentHP}, Rate={speciesRate}, BallMod={ballMod}. Final 'a' value: {a}");

            // 4. Determine Success
            // If a >= 255, guaranteed.
            if (a >= 255)
            {
                OnCaptureSuccess(target);
                return true;
            }

            // Otherwise, calculate 'b'. b = 1048560 / sqrt(sqrt(16711680 / a)) roughly.
            // Shake check: 4 random numbers must be < b.
            // Simplified: Percentage chance ~ (a / 255).
            // Let's use the Gen 3/4 Shake Check logic for authenticity or simple probability.
            // Probability P approx = (a/255)^0.75 ?? 
            // Actually, let's use the exact Shake check if possible, or a simpler approximation.
            // Simpler: Random(0, 255) < a ?
            // The formula 'a' is modified catch rate. 
            // If we strictly follow Bulbapedia:
            // if a >= 255, catch.
            // Else, b = 65535 * sqrt(sqrt(a/255)).
            // Perform 4 checks: Random(0, 65535) < b.
            
            int b = Mathf.FloorToInt(65535f * Mathf.Pow(a / 255f, 0.25f)); // x^0.25 is sqrt(sqrt(x))
            
            for (int i = 0; i < 4; i++)
            {
                if (Random.Range(0, 65536) >= b)
                {
                     // Shake failed
                     Debug.Log($"[Capture] Shake {i+1} failed.");
                     return false;
                }
            }

            OnCaptureSuccess(target);
            return true;
        }

        private static void OnCaptureSuccess(PokemonInstance target)
        {
            Debug.Log($"[Capture] Success! Captured {target.Nickname} ({target.SpeciesID})!");
            
            // Add to Party or Box
            bool addedToParty = PlayerParty.Instance.AddMember(target);
            
            // If specific logic for "just caught" is needed (e.g. rename prompt), trigger events here.
        }
    }
}
