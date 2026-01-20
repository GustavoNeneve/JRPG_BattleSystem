using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json; // Assuming Newtonsoft is available or we use JsonUtility for simple stuff. Use UnityEngine.JsonUtility for now to avoid dependency hell if not installed.
// Actually, for complex nested JSONs like TrainerData with "expandPokemonSetup", JsonUtility is very weak.
// I will attempt to use a simple custom parser or Newtonsoft if likely present. 
// Given the user didn't mention Newtonsoft, I will try to use a robust approach or check if Newtonsoft is available.
// "expandPokemonSetup" has polymorphic values. 
// Let's assume for now we can read the specific fields we care about or use a cleaner DTO.

// NOTE: The JSONs provided seem to use standard JSON format.
// The trainer JSON has "value" field that changes type (int, string, object). This is BAD for JsonUtility.
// I'll use a specific DTO for Trainer Moves that is strictly typed, and handle the rest loosely.

namespace NewBark.Data
{
    public class GameDatabase : MonoBehaviour
    {
        public static GameDatabase Instance;

        public Dictionary<string, SpecieData> Species = new Dictionary<string, SpecieData>();
        public Dictionary<string, MoveData> Moves = new Dictionary<string, MoveData>();
        public Dictionary<string, TrainerData> Trainers = new Dictionary<string, TrainerData>();
        public Dictionary<string, AbilityData> Abilities = new Dictionary<string, AbilityData>();
        public Dictionary<string, NatureData> Natures = new Dictionary<string, NatureData>();
        public Dictionary<string, TypeData> Types = new Dictionary<string, TypeData>();

        // Audio Cache
        private Dictionary<string, AudioClip> moveAudioCache = new Dictionary<string, AudioClip>();

        private const string BASE_PATH = "Assets/novo projeto/Data/Studio/";
        private const string AUDIO_PATH = "Assets/novo projeto/audio/se/moves/";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadAllData();
        }

        public void LoadAllData()
        {
            LoadMoves();
            LoadSpecies();
            LoadAbilities();
            LoadNatures();
            LoadTypes();
            LoadTypes();
            LoadTrainers();
            LoadItems();
            
            Debug.Log($"[GameDatabase] Loaded {Moves.Count} Moves, {Species.Count} Species, {Trainers.Count} Trainers, {Items.Count} Items.");
        }

        public Dictionary<string, ItemData> Items = new Dictionary<string, ItemData>();

        private void LoadItems()
        {
            string path = Path.Combine(BASE_PATH, "items");
            if (!Directory.Exists(path)) return;

            foreach (var file in Directory.GetFiles(path, "*.json"))
            {
                try
                {
                    string json = File.ReadAllText(file);
                    ItemData data = JsonUtility.FromJson<ItemData>(json);
                    if (data != null && !string.IsNullOrEmpty(data.dbSymbol))
                    {
                        if (!Items.ContainsKey(data.dbSymbol))
                            Items.Add(data.dbSymbol, data);
                    }
                }
                catch (System.Exception e) { Debug.LogError($"Error loading item {file}: {e.Message}"); }
            }
        }

        private void LoadMoves()
        {
            string path = Path.Combine(BASE_PATH, "moves");
            if (!Directory.Exists(path)) return;

            foreach (var file in Directory.GetFiles(path, "*.json"))
            {
                string json = File.ReadAllText(file);
                // Using a simple wrapper because JsonUtility is strict.
                // If this fails, we might need a more robust parser.
                try
                {
                    MoveData data = JsonUtility.FromJson<MoveData>(json);
                    if (data != null && !string.IsNullOrEmpty(data.dbSymbol))
                    {
                        if (!Moves.ContainsKey(data.dbSymbol))
                            Moves.Add(data.dbSymbol, data);
                    }
                }
                catch (System.Exception e) { Debug.LogError($"Error loading move {file}: {e.Message}"); }
            }
        }

        private void LoadSpecies()
        {
            string path = Path.Combine(BASE_PATH, "pokemon");
            if (!Directory.Exists(path)) return;

            foreach (var file in Directory.GetFiles(path, "*.json"))
            {
                try
                {
                    string json = File.ReadAllText(file);
                    SpecieData data = JsonUtility.FromJson<SpecieData>(json);
                    if (data != null && !string.IsNullOrEmpty(data.dbSymbol))
                    {
                        if (!Species.ContainsKey(data.dbSymbol))
                            Species.Add(data.dbSymbol, data);
                    }
                }
                catch (System.Exception e) { Debug.LogError($"Error loading pokemon {file}: {e.Message}"); }
            }
        }

        private void LoadAbilities()
        {
            string path = Path.Combine(BASE_PATH, "abilities");
            if (!Directory.Exists(path)) return;

            foreach (var file in Directory.GetFiles(path, "*.json"))
            {
                try
                {
                    string json = File.ReadAllText(file);
                    AbilityData data = JsonUtility.FromJson<AbilityData>(json);
                    if (data != null && !string.IsNullOrEmpty(data.dbSymbol))
                    {
                        if (!Abilities.ContainsKey(data.dbSymbol))
                            Abilities.Add(data.dbSymbol, data);
                    }
                }
                catch { }
            }
        }

        private void LoadNatures()
        {
            string path = Path.Combine(BASE_PATH, "natures");
            if (!Directory.Exists(path)) return;

            foreach (var file in Directory.GetFiles(path, "*.json"))
            {
                try
                {
                    string json = File.ReadAllText(file);
                    NatureData data = JsonUtility.FromJson<NatureData>(json);
                    if (data != null && !string.IsNullOrEmpty(data.dbSymbol))
                    {
                        if (!Natures.ContainsKey(data.dbSymbol))
                            Natures.Add(data.dbSymbol, data);
                    }
                }
                catch { }
            }
        }

        private void LoadTypes()
        {
            string path = Path.Combine(BASE_PATH, "types");
            if (!Directory.Exists(path)) return;

            foreach (var file in Directory.GetFiles(path, "*.json"))
            {
                try
                {
                    string json = File.ReadAllText(file);
                    // Standard JsonUtility handles list inside object fine if structure matches
                    TypeData data = JsonUtility.FromJson<TypeData>(json);
                    if (data != null && !string.IsNullOrEmpty(data.dbSymbol))
                    {
                        if (!Types.ContainsKey(data.dbSymbol))
                            Types.Add(data.dbSymbol, data);
                    }
                }
                catch { }
            }
        }

        private void LoadTrainers()
        {
            string path = Path.Combine(BASE_PATH, "trainers");
            if (!Directory.Exists(path)) return;
            
            foreach (var file in Directory.GetFiles(path, "*.json"))
            {
                try
                {
                    string json = File.ReadAllText(file);
                    // JsonUtility struggles with 'object' type in TrainerPokemonExpandSetup.
                    // However, it should parse the rest of the structure (party list, specie, etc) successfully.
                    // Polymorphic fields will simply be ignored/null/default.
                    TrainerData data = JsonUtility.FromJson<TrainerData>(json);
                    
                    if (data != null && !string.IsNullOrEmpty(data.dbSymbol))
                    {
                        if (!Trainers.ContainsKey(data.dbSymbol))
                        {
                            Trainers.Add(data.dbSymbol, data);
                            Debug.Log($"Loaded Trainer: {data.dbSymbol} with {data.party?.Count ?? 0} pokemons.");
                        }
                    }
                }
                catch (System.Exception e) { Debug.LogError($"Error loading trainer {file}: {e.Message}"); }
            }
        }
        
        // Simple Audio Loader
        public AudioClip GetMoveAudio(string moveName)
        {
            if (moveAudioCache.ContainsKey(moveName)) return moveAudioCache[moveName];

            // Try to find the file. Extensions found in dir list: .ogg, .mp3, .wav
            string[] extensions = new string[] { ".ogg", ".mp3", ".wav" };
            
            foreach (var ext in extensions)
            {
                string filePath = Path.Combine(AUDIO_PATH, moveName + ext);
                if (File.Exists(filePath))
                {
                    // Loading audio from file in Runtime is async normally (UnityWebRequest).
                    // Or since we are in Editor/Windows, we can use WWW or UnityWebRequestMultimedia.
                    // For IMMEDIATE blocking load, it is deprecated but WWW might work or we just return null and load async.
                    
                    // BETTER APPROACH: Use Resources if they were in Resources. They are not.
                    // So we must use UnityWebRequest.
                    return null; // Can't return async result in sync method.
                }
            }
            return null;
        }

        // Coroutine version
        public void LoadAudioAsync(string moveName, System.Action<AudioClip> callback)
        {
            if (moveAudioCache.ContainsKey(moveName))
            {
                callback?.Invoke(moveAudioCache[moveName]);
                return;
            }
            StartCoroutine(LoadAudioRoutine(moveName, callback));
        }

        private System.Collections.IEnumerator LoadAudioRoutine(string moveName, System.Action<AudioClip> callback)
        {
            string[] extensions = new string[] { ".ogg", ".mp3", ".wav" };
            string foundPath = null;
            
            foreach (var ext in extensions)
            {
                string filePath = Path.Combine(AUDIO_PATH, moveName + ext);
                if (File.Exists(filePath))
                {
                    foundPath = filePath;
                    break;
                }
            }

            if (foundPath != null)
            {
                string url = "file://" + Path.GetFullPath(foundPath);
                using (var uwr = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(url, UnityEngine.AudioType.UNKNOWN))
                {
                    yield return uwr.SendWebRequest();
                    if (uwr.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        var clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(uwr);
                        clip.name = moveName;
                        moveAudioCache[moveName] = clip;
                        callback?.Invoke(clip);
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to load audio for {moveName}: {uwr.error}");
                        callback?.Invoke(null);
                    }
                }
            }
            else
            {
                callback?.Invoke(null);
            }
        }

        public SpecieData GetSpecie(string id)
        {
            if (Species.TryGetValue(id, out var s)) return s;
            Debug.LogWarning($"Specie {id} not found!");
            return null;
        }
        
        public MoveData GetMove(string id)
        {
            if (Moves.TryGetValue(id, out var m)) return m;
            return null;
        }
    }
}
