using System.Collections.Generic;
using UnityEngine;
using NewBark.Data;
using NewBark.State;

namespace NewBark.Runtime
{
    public class PlayerPartyController : MonoBehaviour
    {
        public const int MaxPartySize = 6;

        public List<PokemonInstance> Party => GameManager.Data.party;

        public bool AddPokemon(string speciesId, int level)
        {
            if (Party.Count >= MaxPartySize)
            {
                Debug.Log("Party is Full!");
                return false; // Send to PC later
            }

            PokemonInstance newMon = new PokemonInstance(speciesId, level);
            Party.Add(newMon);
            Debug.Log($"Added {speciesId} (Lvl {level}) to party.");
            return true;
        }

        public bool AddPokemon(PokemonInstance existing)
        {
            if (Party.Count >= MaxPartySize)
            {
                Debug.Log("Party is Full!");
                // Implementation for PC Storage would go here
                return false;
            }

            Party.Add(existing);
            return true;
        }

        public void HealTeam()
        {
            foreach (var mon in Party)
            {
                mon.CurrentHP = mon.MaxHP;
                // Restore PP logic if needed
            }
            Debug.Log("Team Healed!");
        }

        public PokemonInstance GetFirstHealthyPokemon()
        {
            foreach (var mon in Party)
            {
                if (mon.CurrentHP > 0) return mon;
            }
            return null;
        }

        // Debug Helpers
        [ContextMenu("Give Starter")]
        public void GiveStarter()
        {
            AddPokemon("bulbasaur", 5);
            AddPokemon("charmander", 5);
        }
    }
}
