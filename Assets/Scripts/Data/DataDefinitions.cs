using System;
using System.Collections.Generic;

namespace NewBark.Data
{
    [Serializable]
    public class SpecieData
    {
        public string klass;
        public int id;
        public string dbSymbol;
        public List<SpecieForm> forms;
    }

    [Serializable]
    public class SpecieForm
    {
        public int form;
        public float height;
        public float weight;
        public string type1;
        public string type2;
        public int baseHp;
        public int baseAtk;
        public int baseDfe;
        public int baseSpd;
        public int baseAts;
        public int baseDfs;
        public int evHp;
        public int evAtk;
        public int evDfe;
        public int evSpd;
        public int evAts;
        public int evDfs;
        public int experienceType;
        public int baseExperience;
        public int baseLoyalty;
        public int catchRate;
        public int femaleRate;
        public int hatchSteps;
        public string babyDbSymbol;
        public int babyForm;
        public List<string> abilities;
        public SpecieResources resources;
        public List<LearnableMove> moveSet;
    }

    [Serializable]
    public class SpecieResources
    {
        public string icon;
        public string front;
        public string back;
        public string cry;
    }

    [Serializable]
    public class LearnableMove
    {
        public string move;
        public string klass; // LevelLearnableMove, etc
        public int level;
    }

    [Serializable]
    public class MoveData
    {
        public string klass;
        public int id;
        public string dbSymbol;
        public string type;
        public int power;
        public int accuracy;
        public int pp;
        public string category; // physical, special, status
        public int priority;
        public int effectChance;
        public bool isHeal;
        public string battleEngineAimedTarget;
    }

    [Serializable]
    public class TrainerData
    {
        public string klass;
        public int id;
        public string dbSymbol;
        public int baseMoney;
        public int ai; // Difficulty Level (1 to 5?)
        public List<TrainerPartyMember> party;
        public TrainerResources resources;
    }

    [Serializable]
    public class TrainerResources
    {
        public string sprite;
        public string artworkFull;
    }

    [Serializable]
    public class TrainerPartyMember
    {
        public string specie;
        public int form;
        public int level; // Flattened for convenience, real JSON has levelSetup
        public TrainerPokemonLevelSetup levelSetup;
        public TrainerPokemonExpandSetup[] expandPokemonSetup;
    }

    [Serializable]
    public class TrainerPokemonLevelSetup
    {
        public string kind;
        public int level;
    }

    [Serializable]
    public class TrainerPokemonExpandSetup
    {
        public string type; // "moves", "evs", "ivs", etc
        public object value; // This is tricky in strict JSON parsers, might need custom deserializer or string
        // For simplicity in Unity JsonUtility, we might need a workaround if structure varies too wildy.
        // Or we assume specific fields.
    }
    
    // For manual parsing of the dynamic attributes in Trainer
    // We might just load them as dictionaries if we use Newtonsoft, but sticking to simple classes for now.
    
    [Serializable]
    public class AbilityData
    {
        public string klass;
        public int id;
        public string dbSymbol;
        public int textId;
    }

    [Serializable]
    public class NatureData
    {
        public string klass;
        public int id;
        public string dbSymbol;
        public NatureStats stats;
    }

    [Serializable]
    public class NatureStats
    {
        public int atk;
        public int dfe;
        public int spd;
        public int ats;
        public int dfs;
    }

    [Serializable]
    public class TypeData
    {
        public string klass;
        public int id;
        public string dbSymbol;
        public string color;
        public List<TypeDamageFactor> damageTo;
    }

    [Serializable]
    public class TypeDamageFactor
    {
        public string defensiveType;
        public float factor;
    }

    [Serializable]
    public class ItemData
    {
        public string klass;
        public int id;
        public string dbSymbol;
        public string icon;
        public int price;
        public string description; // Often in a separate text file, but keeping placeholder
        public bool isBattleUsable;
        public bool isMapUsable;
        public bool isHoldable;
        public bool isLimited;
        
        // Specifics
        public int hpCount; // For Potions
        public int catchRate; // For Balls
        public string spriteFilename; // For Balls
        public ItemColor color; // For Balls
    }

    [Serializable]
    public class ItemColor
    {
        public int red;
        public int green;
        public int blue;
        public int alpha;
    }
}
