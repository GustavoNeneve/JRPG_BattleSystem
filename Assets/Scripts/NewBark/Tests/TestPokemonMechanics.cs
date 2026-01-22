using UnityEngine;
using NewBark.Runtime;

namespace NewBark.Tests
{
    public class TestPokemonMechanics : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log("--- Starting Pokemon Mechanics Tests ---");

            TestStats();
            TestRarity();
            TestCapture();
            TestTypeChart();
        }

        private void TestStats()
        {
            Debug.Log(">>> Testing Stats");
            // Assuming Bulbasaur(1) exists. Base: 45, 49, 49, 65, 65, 45
            // Create Lv 50
            var pkm = new PokemonInstance("bulbasaur", 50);
            
            Debug.Log($"Generated {pkm.Nickname} (Lv {pkm.Level})");
            Debug.Log($"IVs: {pkm.IV_HP}/{pkm.IV_Atk}/{pkm.IV_Def}/{pkm.IV_SpA}/{pkm.IV_SpD}/{pkm.IV_Spe}");
            Debug.Log($"Stats: HP:{pkm.MaxHP} Atk:{pkm.Attack} Def:{pkm.Defense} SpA:{pkm.SpAttack} SpD:{pkm.SpDefense} Spe:{pkm.Speed}");
            Debug.Log($"Nature: {pkm.NatureID}");
            Debug.Log($"Rarity: {pkm.Rarity}");

            // Verify HP Formula manually for a specific case if we controlled IVs
            // But just logging proves it doesn't crash calculations.
        }

        private void TestRarity()
        {
            Debug.Log(">>> Testing Rarity Pity");
            // Force 4999 encounters
            RaritySystem.LoadState(4999);
            var rarity = RaritySystem.GenerateRarity();
            
            if (rarity != PokemonRarity.Normal)
            {
                Debug.Log($"SUCCESS: Encounter 5000 triggered Rare! Result: {rarity}");
            }
            else
            {
                Debug.LogError("FAILURE: Encounter 5000 did not trigger Rare.");
            }
            
            if (RaritySystem.GetEncountersCount() == 0)
            {
                 Debug.Log("SUCCESS: Encounter counter reset after rare.");
            }
            else
            {
                 Debug.LogError($"FAILURE: Encounter counter not reset. Value: {RaritySystem.GetEncountersCount()}");
            }
        }

        private void TestCapture()
        {
            Debug.Log(">>> Testing Capture");
            var pkm = new PokemonInstance("bulbasaur", 5);
            // Low level, high HP catch rate (45).
            
            // Add PokeBall (id: poke_ball)
            InventoryManager.Instance.AddItem("poke_ball", 10);
            
            // Try Catch
            bool caught = CaptureMechanic.TryCatchPokemon(pkm, "poke_ball");
            Debug.Log($"Capture Result: {caught}. Party Size: {PlayerParty.Instance.ActiveParty.Count}");
            
            // Add 6 more to fill party
            for(int i=0; i<6; i++)
            {
                PlayerParty.Instance.AddMember(new PokemonInstance("bulbasaur", 1));
            }
            
            // Try Catch 7th
            var extra = new PokemonInstance("charmander", 5);
            bool caughtExtra = CaptureMechanic.TryCatchPokemon(extra, "poke_ball", false); // Don't consume ball to save logs
             if (caughtExtra)
             {
                 Debug.Log("Caught extra! Checking Storage...");
                 Debug.Log($"Storage Count: {StorageSystem.Instance.StoredPokemon.Count}");
                 if (StorageSystem.Instance.StoredPokemon.Count > 0)
                 {
                     Debug.Log("SUCCESS: Pokemon sent to Storage.");
                 }
                 else
                 {
                     Debug.LogError("FAILURE: Pokemon not found in Storage.");
                 }
             }
        }

        private void TestTypeChart()
        {
            Debug.Log(">>> Testing Type Chart");
            // Water vs Fire (2.0)
            float waterVsFire = TypeChart.GetEffectiveness("water", "fire");
            Debug.Log($"Water vs Fire: {waterVsFire} (Expected 2)");

            // Fighting vs Normal (2.0)
            float fightVsNorm = TypeChart.GetEffectiveness("fighting", "normal");
             Debug.Log($"Fighting vs Normal: {fightVsNorm} (Expected 2)");
             
            // Fighting vs Ghost (0.0)
             float fightVsGhost = TypeChart.GetEffectiveness("fighting", "ghost");
             Debug.Log($"Fighting vs Ghost: {fightVsGhost} (Expected 0)");
        }
    }
}
