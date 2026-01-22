using System.Collections.Generic;
using UnityEngine;
using NewBark.Data;

namespace NewBark.Runtime
{
    [System.Serializable]
    public class PokemonInstance
    {
        public string UniqueID;
        public string SpeciesID;
        public string Nickname;
        public int Level;
        public int CurrentHP;
        public int CurrentMP; // Added for JRPG compat if needed
        public string Gender; // "M", "F", "N"
        public PokemonRarity Rarity;

        // Stats
        public int MaxHP;
        public int Attack;
        public int Defense;
        public int SpAttack;
        public int SpDefense;
        public int Speed;

        // Dynamic Data
        public int IV_HP, IV_Atk, IV_Def, IV_SpA, IV_SpD, IV_Spe;
        public int EV_HP, EV_Atk, EV_Def, EV_SpA, EV_SpD, EV_Spe;
        
        public string NatureID;
        public string AbilityID;
        public string ItemID; // Held Item

        public List<CombatActionSO> Moves = new List<CombatActionSO>();

        public SpecieData BaseData => GameDatabase.Instance.GetSpecie(SpeciesID);

        public PokemonInstance(string speciesId, int level)
        {
            SpeciesID = speciesId;
            Level = level;
            UniqueID = System.Guid.NewGuid().ToString();

            // Rarity Generation using the new System
            Rarity = RaritySystem.GenerateRarity();

            // Default random IVs (0-31)
            IV_HP = Random.Range(0, 32);
            IV_Atk = Random.Range(0, 32);
            IV_Def = Random.Range(0, 32);
            IV_SpA = Random.Range(0, 32);
            IV_SpD = Random.Range(0, 32);
            IV_Spe = Random.Range(0, 32);

            // EVs start at 0
            EV_HP = 0; EV_Atk = 0; EV_Def = 0; EV_SpA = 0; EV_SpD = 0; EV_Spe = 0;

            // Set Nature randomly if not set? Or should be passed in?
            // For now random nature if null logic inside CalculateStats or Init.
            // Let's pick a random nature here.
            NatureID = Utilities.GetRandomNature(); 

            CalculateStats();
            Heal(); // Set HP to Max
            
            // Generate Moves based on Level
            GenerateMoves();
        }

        // Helper if NatureID is not set strictly
        public static class Utilities {
            public static string GetRandomNature() {
                var keys = new List<string>(GameDatabase.Instance.Natures.Keys);
                if (keys.Count > 0) return keys[Random.Range(0, keys.Count)];
                return "hardy";
            }
        }

        public void CalculateStats()
        {
            var data = BaseData;
            if (data == null || data.forms == null || data.forms.Count == 0) return;
            
            var form = data.forms[0]; // Default to form 0 for now.

            // Nature Multipliers
            float natureAtk = 1f, natureDef = 1f, natureSpA = 1f, natureSpD = 1f, natureSpe = 1f;
            
            if (GameDatabase.Instance.Natures.TryGetValue(NatureID ?? "hardy", out var nature))
            {
                if (nature.stats != null)
                {
                    // JSON format: 110 means +10%, 90 means -10%, 100 means neutral
                    natureAtk = nature.stats.atk / 100f;
                    natureDef = nature.stats.dfe / 100f; // Note: dfe in JSON
                    natureSpA = nature.stats.ats / 100f; // Note: ats in JSON
                    natureSpD = nature.stats.dfs / 100f; // Note: dfs in JSON
                    natureSpe = nature.stats.spd / 100f;
                }
            }

            // HP Formula: ((2 * Base + IV + (EV/4)) * Level / 100) + Level + 10
            MaxHP = Mathf.FloorToInt(((2 * form.baseHp + IV_HP + (EV_HP / 4)) * Level) / 100f) + Level + 10;

            // Other Stats Formula: (((2 * Base + IV + (EV/4)) * Level / 100) + 5) * Nature
            Attack = Mathf.FloorToInt((Mathf.FloorToInt(((2 * form.baseAtk + IV_Atk + (EV_Atk / 4)) * Level) / 100f) + 5) * natureAtk);
            Defense = Mathf.FloorToInt((Mathf.FloorToInt(((2 * form.baseDfe + IV_Def + (EV_Def / 4)) * Level) / 100f) + 5) * natureDef);
            SpAttack = Mathf.FloorToInt((Mathf.FloorToInt(((2 * form.baseAts + IV_SpA + (EV_SpA / 4)) * Level) / 100f) + 5) * natureSpA);
            SpDefense = Mathf.FloorToInt((Mathf.FloorToInt(((2 * form.baseDfs + IV_SpD + (EV_SpD / 4)) * Level) / 100f) + 5) * natureSpD);
            Speed = Mathf.FloorToInt((Mathf.FloorToInt(((2 * form.baseSpd + IV_Spe + (EV_Spe / 4)) * Level) / 100f) + 5) * natureSpe);
        }

        public void Heal()
        {
            CurrentHP = MaxHP;
            // Restore PP for moves if we track it
            // Status = None
        }

        public void GainEVs(int hp, int atk, int def, int spa, int spd, int spe)
        {
            // Cap total EVs at 510, and individual at 252
            int total = EV_HP + EV_Atk + EV_Def + EV_SpA + EV_SpD + EV_Spe;
            int remaining = 510 - total;
            if (remaining <= 0) return;

            EV_HP = Mathf.Min(EV_HP + hp, 252);
            EV_Atk = Mathf.Min(EV_Atk + atk, 252);
            EV_Def = Mathf.Min(EV_Def + def, 252);
            EV_SpA = Mathf.Min(EV_SpA + spa, 252);
            EV_SpD = Mathf.Min(EV_SpD + spd, 252);
            EV_Spe = Mathf.Min(EV_Spe + spe, 252);

            CalculateStats();
        }

        public void GenerateMoves()
        {
            Moves.Clear();
            var data = BaseData;
            if (data == null || data.forms == null || data.forms.Count == 0 || data.forms[0].moveSet == null) return;

            // Simple logic: Get 4 latest moves for this level
            List<LearnableMove> eligibleMoves = new List<LearnableMove>();
            foreach(var m in data.forms[0].moveSet)
            {
                if (m.klass == "LevelLearnableMove" && m.level <= Level)
                {
                    eligibleMoves.Add(m);
                }
            }
            
            // Take last 4
            int start = Mathf.Max(0, eligibleMoves.Count - 4);
            for(int i = start; i < eligibleMoves.Count; i++)
            {
                string moveId = eligibleMoves[i].move;
                var moveInstance = CreateCombatAction(moveId);
                if (moveInstance != null) Moves.Add(moveInstance);
            }
        }

        public static CombatActionSO CreateCombatAction(string moveId)
        {
            var moveData = GameDatabase.Instance.GetMove(moveId);
            if (moveData == null) return null;

            // Create Runtime SO
            CombatActionSO action = ScriptableObject.CreateInstance<CombatActionSO>();
            action.InitializeFromData(moveData);
            return action;
        }
    }
}
