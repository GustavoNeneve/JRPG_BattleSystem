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
        public string Gender; // "M", "F", "N"
        
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
        public string ItemID;

        public List<CombatActionSO> Moves = new List<CombatActionSO>();

        public SpecieData BaseData => GameDatabase.Instance.GetSpecie(SpeciesID);

        public PokemonInstance(string speciesId, int level)
        {
            SpeciesID = speciesId;
            Level = level;
            UniqueID = System.Guid.NewGuid().ToString();

            // Default random IVs
            IV_HP = Random.Range(0, 32);
            IV_Atk = Random.Range(0, 32);
            IV_Def = Random.Range(0, 32);
            IV_SpA = Random.Range(0, 32);
            IV_SpD = Random.Range(0, 32);
            IV_Spe = Random.Range(0, 32);

            CalculateStats();
            CurrentHP = MaxHP;
            
            // Generate Moves based on Level
            GenerateMoves();
        }

        public void CalculateStats()
        {
            var data = BaseData;
            if (data == null || data.forms == null || data.forms.Count == 0) return;
            
            var form = data.forms[0]; // Default to form 0 for now. Handle forms later.

            // Nature Multipliers (Placeholder)
            float natureAtk = 1f, natureDef = 1f, natureSpA = 1f, natureSpD = 1f, natureSpe = 1f;
            if (GameDatabase.Instance.Natures.TryGetValue(NatureID ?? "hardy", out var nature))
            {
                // Nature stats logic: 
                // Usually Nature increases one by 10% and decreases one by 10%.
                // The JSON "stats" object has values like 110 (increase) and 90 (decrease) and 100 (neutral).
                if (nature.stats != null)
                {
                    natureAtk = nature.stats.atk / 100f;
                    natureDef = nature.stats.dfe / 100f;
                    natureSpA = nature.stats.ats / 100f;
                    natureSpD = nature.stats.dfs / 100f;
                    natureSpe = nature.stats.spd / 100f;
                }
            }

            MaxHP = Mathf.FloorToInt(((2 * form.baseHp + IV_HP + (EV_HP / 4)) * Level) / 100f) + Level + 10;
            Attack = Mathf.FloorToInt((Mathf.FloorToInt(((2 * form.baseAtk + IV_Atk + (EV_Atk / 4)) * Level) / 100f) + 5) * natureAtk);
            Defense = Mathf.FloorToInt((Mathf.FloorToInt(((2 * form.baseDfe + IV_Def + (EV_Def / 4)) * Level) / 100f) + 5) * natureDef);
            SpAttack = Mathf.FloorToInt((Mathf.FloorToInt(((2 * form.baseAts + IV_SpA + (EV_SpA / 4)) * Level) / 100f) + 5) * natureSpA);
            SpDefense = Mathf.FloorToInt((Mathf.FloorToInt(((2 * form.baseDfs + IV_SpD + (EV_SpD / 4)) * Level) / 100f) + 5) * natureSpD);
            Speed = Mathf.FloorToInt((Mathf.FloorToInt(((2 * form.baseSpd + IV_Spe + (EV_Spe / 4)) * Level) / 100f) + 5) * natureSpe);
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
