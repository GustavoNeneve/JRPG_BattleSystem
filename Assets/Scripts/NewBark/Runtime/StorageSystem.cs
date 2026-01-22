using System.Collections.Generic;
using UnityEngine;

namespace NewBark.Runtime
{
    public class StorageSystem : MonoBehaviour
    {
        public static StorageSystem Instance;

        public List<PokemonInstance> StoredPokemon = new List<PokemonInstance>();

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

        public void Deposit(PokemonInstance pokemon)
        {
            if (pokemon == null) return;
            
            // Auto Heal when deposited "se os player capturar mais de 6 monstros eles vão para o lab, eles vão se curados"
            pokemon.Heal();
            
            StoredPokemon.Add(pokemon);
            Debug.Log($"[Storage] Deposited {pokemon.Nickname} ({pokemon.SpeciesID}) to Box. Total Stored: {StoredPokemon.Count}");
        }

        public bool Withdraw(PokemonInstance pokemon)
        {
            if (StoredPokemon.Contains(pokemon))
            {
                StoredPokemon.Remove(pokemon);
                return true;
            }
            return false;
        }
        
        // Save/Load logic would go here
    }
}
