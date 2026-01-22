using System.Collections.Generic;
using UnityEngine;

namespace NewBark.Runtime
{
    public class PlayerParty : MonoBehaviour
    {
        public static PlayerParty Instance;

        public List<PokemonInstance> ActiveParty = new List<PokemonInstance>();
        public const int MAX_PARTY_SIZE = 6;
        public const int MIN_PARTY_SIZE = 1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool AddMember(PokemonInstance pokemon)
        {
            if (ActiveParty.Count < MAX_PARTY_SIZE)
            {
                ActiveParty.Add(pokemon);
                Debug.Log($"[Party] Added {pokemon.Nickname} to party.");
                return true;
            }
            else
            {
                Debug.Log($"[Party] Party full! Sending {pokemon.Nickname} to Storage.");
                StorageSystem.Instance.Deposit(pokemon);
                return false; // Not added to party
            }
        }

        public bool RemoveMember(PokemonInstance pokemon)
        {
            if (ActiveParty.Count <= MIN_PARTY_SIZE)
            {
                Debug.LogWarning("[Party] Cannot remove member. Minimum party size reached.");
                return false;
            }

            if (ActiveParty.Contains(pokemon))
            {
                ActiveParty.Remove(pokemon);
                // Optionally send to storage or release? 
                // Usually "Remove" implies Deposit, but here we just remove from constraints perspective.
                // Assuming manual Swap logic handles deposit. 
                // If just releasing, then it's gone.
                return true;
            }
            return false;
        }

        public void SwapMember(int indexA, int indexB)
        {
            if (indexA >= 0 && indexA < ActiveParty.Count && indexB >= 0 && indexB < ActiveParty.Count)
            {
                var temp = ActiveParty[indexA];
                ActiveParty[indexA] = ActiveParty[indexB];
                ActiveParty[indexB] = temp;
            }
        }

        public PokemonInstance GetLeader()
        {
            if (ActiveParty.Count > 0) return ActiveParty[0];
            return null;
        }
        
        public bool IsDefeated()
        {
            foreach(var p in ActiveParty)
            {
                if (p.CurrentHP > 0) return false;
            }
            return true;
        }
    }
}
