using System;
using NewBark.Support;

namespace NewBark.State
{
    [Serializable]
    public class GameData
    {
        public static readonly int SchemaVersion = 2;
        public static readonly int MinCompatibleSchemaVersion = 2;

        public DateTime startDate = DateTime.Now;
        public DateTime saveDate = DateTime.Now;
        public float playTime;

        public string areaTitleTrigger;
        public SerializableVector2 playerPosition;
        public SerializableVector2 playerDirection;
        
        // Added list to track defeated trainers
        public System.Collections.Generic.List<string> beatenTrainers = new System.Collections.Generic.List<string>();

        // Player Party
        public System.Collections.Generic.List<NewBark.Runtime.PokemonInstance> party = new System.Collections.Generic.List<NewBark.Runtime.PokemonInstance>();
    }
}
