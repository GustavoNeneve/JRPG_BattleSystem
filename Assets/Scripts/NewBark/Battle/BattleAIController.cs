using System.Collections.Generic;
using UnityEngine;
using NewBark.Data;
using NewBark.Runtime;

public enum AIDifficulty
{
    Beginner = 1,
    Easy = 2,
    Medium = 3,
    Hard = 4, 
    Inferno = 5
}

public class BattleAIController : MonoBehaviour
{
    // This component should be attached to the Enemy GameObject
    
    private EnemyBehaviour enemyBehaviour;
    private PokemonInstance myPokemon; // Requires integrating PokemonInstance into EnemyBehaviour
    
    public AIDifficulty Difficulty = AIDifficulty.Easy;
    
    public void Initialize(EnemyBehaviour behaviour, PokemonInstance pokemon, int aiLevel)
    {
        this.enemyBehaviour = behaviour;
        this.myPokemon = pokemon;
        this.Difficulty = (AIDifficulty)Mathf.Clamp(aiLevel, 1, 5);
    }
    
    public CharacterBehaviour SelectTarget(List<CharacterBehaviour> players)
    {
        // Filter out dead players
        List<CharacterBehaviour> alivePlayers = new List<CharacterBehaviour>();
        foreach(var p in players)
            if(p.CurrentBattlePhase != BattleState.DEAD) alivePlayers.Add(p);
            
        if (alivePlayers.Count == 0) return null;

        // Logic based on difficulty
        if (Difficulty <= AIDifficulty.Easy)
        {
            return alivePlayers[Random.Range(0, alivePlayers.Count)];
        }
        else
        {
            // Medium+: Find target with type weakness or lowest HP
            CharacterBehaviour bestTarget = alivePlayers[0];
            float highestScore = -1f;
            
            foreach(var p in alivePlayers)
            {
                float score = EvaluateTargetScore(p);
                if(score > highestScore)
                {
                    highestScore = score;
                    bestTarget = p;
                }
            }
            return bestTarget;
        }
    }
    
    public CombatActionSO SelectAction(PokemonInstance attacker, PokemonInstance defender)
    {
        if (attacker == null || attacker.Moves.Count == 0) return null;
        
        // If we don't have Defender info (e.g. initial state), return random
        if (defender == null && Difficulty < AIDifficulty.Medium)
             return attacker.Moves[Random.Range(0, attacker.Moves.Count)];

        // Retrieve defender types (Assumed available on PokemonInstance)
        string defType1 = defender != null ? defender.BaseData.forms[0].type1 : "normal";
        string defType2 = defender != null ? defender.BaseData.forms[0].type2 : null;

        CombatActionSO bestMove = attacker.Moves[0];
        float bestScore = -1f;

        List<CombatActionSO> legalMoves = attacker.Moves; // Filter by MP/PP if needed later

        foreach (var move in legalMoves)
        {
             // Temporary: Parse type from description string or map it properly
             // We need Type info on CombatActionSO. I added it to data init but CombatActionSO doesn't expose it cleanly yet.
             // We might need to access the MoveData from the Name or ID if stored.
             // For now, let's assume we can get basic info or use the "damageMultiplier" as Power.
             
             // To implement Type Effectiveness properly, CombatActionSO needs a 'Type' field.
             // I'll assume 'actionType' is somewhat indicative or I rely on my knowledge that I populated it from Data.
             
             float score = EvaluateMoveScore(move, defType1, defType2);
             
             // Add randomness for lower difficulties
             if (Difficulty <= AIDifficulty.Easy)
                 score += Random.Range(0, 50f);
                 
             if (score > bestScore)
             {
                 bestScore = score;
                 bestMove = move;
             }
        }
        
        return bestMove;
    }

    private float EvaluateTargetScore(CharacterBehaviour target)
    {
        // Need access to Target's PokemonInstance to know types/HP.
        // Assuming Target has a way to expose it.
        // For now, return random score to verify structure.
        return Random.value * 100f; 
    }

    private float EvaluateMoveScore(CombatActionSO move, string defType1, string defType2)
    {
        // Ideally we fetch the real MoveData using the name or ID
        // string type = move.moveType; // Not existing yet
        // float power = move.damageMultiplier;
        
        // Simulating score
        float score = move.damageMultiplier;
        if (move.damageType == DamageType.HEALING)
        {
            // Use heal if HP is low
             if (myPokemon.CurrentHP < myPokemon.MaxHP * 0.4f) score += 100f;
             else score = 0;
        }
        
        return score;
    }
}
