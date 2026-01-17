using System.Collections.Generic;
using UnityEngine;
using System;

namespace NewBark.Data
{
    [CreateAssetMenu(fileName = "NewPokemon", menuName = "NewBark/Pokemon Data", order = 0)]
    public class PokemonData : ScriptableObject
    {
        public string klass;
        public int id;
        public string dbSymbol;
        public PokemonForm[] forms;
    }

    [Serializable]
    public class PokemonForm
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
        public Evolution[] evolutions;
        public int experienceType;
        public int baseExperience;
        public int baseLoyalty;
        public int catchRate;
        public int femaleRate; // -1 for genderless? Need to check json but usually int 0-100 or specific codes
        public int[] breedGroups;
        public int hatchSteps;
        public string babyDbSymbol;
        public int babyForm;
        public ItemHeld[] itemHeld;
        public string[] abilities;
        public float frontOffsetY;
        public Resources resources;
        public MoveEntry[] moveSet;
        public FormTextId formTextId;
    }

    [Serializable]
    public class Evolution
    {
        public int form;
        public EvolutionCondition[] conditions;
    }

    [Serializable]
    public class EvolutionCondition
    {
        public string type;
        public string value;
    }

    [Serializable]
    public class ItemHeld
    {
        public string dbSymbol;
        public int chance;
    }

    [Serializable]
    public class Resources
    {
        public string icon;
        public string iconF;
        public string iconShiny;
        public string iconShinyF;
        public string front;
        public string frontF;
        public string frontShiny;
        public string frontShinyF;
        public string back;
        public string backF;
        public string backShiny;
        public string backShinyF;
        public string footprint;
        public string character;
        public string characterF;
        public string characterShiny;
        public string characterShinyF;
        public string cry;
        public bool hasFemale;
        public string egg;
        public string iconEgg;
    }

    [Serializable]
    public class MoveEntry
    {
        public string move;
        public string klass;
        public int level;
    }

    [Serializable]
    public class FormTextId
    {
        public int name;
        public int description;
    }
}
