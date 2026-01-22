using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using NewBark.Support;
using UnityEngine.Rendering.Universal;

public class EncounterManager : MonoBehaviour
{
    public enum BattleMode
    {
        Single, // 1v1
        Double, // 2v2
        Horde   // 1v3 or rarely 2v3
    }

    public static EncounterManager instance;

    [Header("Runtime Info")]
    [SerializeField] EncounterData currentEncounterData;
    [SerializeField] List<GameObject> enemiesToSpawn = new List<GameObject>();
    [SerializeField] Vector3 lastWorldPosition;
    [SerializeField] string lastWorldSceneName;
    [SerializeField] AudioClip lastWorldMusic;

    // Battle Rules
    public BattleMode CurrentBattleMode = BattleMode.Single;
    public int AllowedPlayerSlots = 1; // Limit of active pokemon for player (1 or 2)

    // Keep reference to the camera we disabled
    private GameObject preservedCamera;

    // Keep reference to the player we disabled
    private GameObject preservedPlayer;

    // Async loading operation
    private AsyncOperation preloadOperation;

    [Header("Defaults")]
    [Tooltip("Where to go if defeated")]
    public string hospitalSceneName = "World";
    public Vector3 hospitalPosition;

    public List<GameObject> EnemiesToSpawn => enemiesToSpawn;
    public Sprite CurrentBackground => currentEncounterData != null ? currentEncounterData.backgroundImage : null;
    public AudioClip CurrentBattleMusic => currentEncounterData != null ? currentEncounterData.battleMusic : null;

    // Data Transfer for Battle
    public List<NewBark.Runtime.PokemonInstance> CurrentEnemyParty = new List<NewBark.Runtime.PokemonInstance>();
    public List<NewBark.Runtime.PokemonInstance> CurrentPlayerParty = new List<NewBark.Runtime.PokemonInstance>();
    public int CurrentEncounterAILevel = 1;
    private NewBark.Data.TrainerData currentTrainer; // Track who we are fighting

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(this.gameObject);

        var uniquePersistent = GetComponent<UniquePersistent>();
        if (uniquePersistent == null)
            uniquePersistent = gameObject.AddComponent<UniquePersistent>();

        uniquePersistent.uniqueID = "encounter_manager";
    }

    public void StartEncounter(EncounterData data)
    {
        currentEncounterData = data;

        // Prepare Player Party
        CurrentPlayerParty.Clear();

        // Use live Party if available, otherwise fallback to Data
        if (NewBark.Runtime.PlayerParty.Instance != null && NewBark.Runtime.PlayerParty.Instance.ActiveParty != null)
        {
            foreach (var p in NewBark.Runtime.PlayerParty.Instance.ActiveParty)
            {
                if (p != null && !string.IsNullOrEmpty(p.SpeciesID))
                    CurrentPlayerParty.Add(p);
            }
        }
        else if (NewBark.GameManager.Data != null && NewBark.GameManager.Data.party != null)
        {
            foreach (var p in NewBark.GameManager.Data.party)
            {
                if (p != null && !string.IsNullOrEmpty(p.SpeciesID))
                    CurrentPlayerParty.Add(p);
            }
        }

        // Fallback: Kañyby 404
        if (CurrentPlayerParty.Count == 0)
        {
            CreateFallbackPokemon();
        }

        // Save Position & Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        // Find Camera explicitly by Name as requested
        GameObject mainCamera = GameObject.Find("MainCameraWorld");

        if (player)
        {
            preservedPlayer = player;
            lastWorldPosition = player.transform.position;
            lastWorldSceneName = SceneManager.GetActiveScene().name;
            player.SetActive(false);
        }
        if (mainCamera)
        {
            Debug.Log($"[EncounterManager] Disabling World Camera: {mainCamera.name}");
            preservedCamera = mainCamera;
            mainCamera.SetActive(false);

            // Force disable AudioListener to be sure
            var l = mainCamera.GetComponent<AudioListener>();
            if (l) l.enabled = false;
        }
        else
        {
            Debug.LogWarning("[EncounterManager] Could not find Main Camera to disable!");
        }

        // Save Music
        var audioCtrl = FindFirstObjectByType<NewBark.Audio.AudioController>();
        if (audioCtrl && audioCtrl.BgmChannel)
        {
            lastWorldMusic = audioCtrl.BgmChannel.clip;
        }

        // Calculate Enemies
        GenerateEnemyList();

        SceneManager.LoadScene("Main_Offline");
    }

    public void StartTrainerBattle(NewBark.Data.TrainerData trainer)
    {
        currentEncounterData = null; // Clear wild data
        currentTrainer = trainer; // Store reference
        if (trainer != null) CurrentEncounterAILevel = trainer.ai;
        else CurrentEncounterAILevel = 1;

        // Prepare Player Party
        CurrentPlayerParty.Clear();

        if (NewBark.Runtime.PlayerParty.Instance != null && NewBark.Runtime.PlayerParty.Instance.ActiveParty != null)
        {
            foreach (var p in NewBark.Runtime.PlayerParty.Instance.ActiveParty)
            {
                if (p != null && !string.IsNullOrEmpty(p.SpeciesID))
                    CurrentPlayerParty.Add(p);
            }
        }
        else if (NewBark.GameManager.Data != null && NewBark.GameManager.Data.party != null)
        {
            foreach (var p in NewBark.GameManager.Data.party)
            {
                if (p != null && !string.IsNullOrEmpty(p.SpeciesID))
                    CurrentPlayerParty.Add(p);
            }
        }

        // Fallback: Kañyby 404
        if (CurrentPlayerParty.Count == 0)
        {
            CreateFallbackPokemon();
        }

        // Save Position & Player (Reuse logic, maybe refactor later to 'SaveWorldState')
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        GameObject mainCamera = GameObject.Find("MainCameraWorld");

        if (player)
        {
            preservedPlayer = player;
            lastWorldPosition = player.transform.position;
            lastWorldSceneName = SceneManager.GetActiveScene().name;
            player.SetActive(false);
        }
        if (mainCamera)
        {
            preservedCamera = mainCamera;
            mainCamera.SetActive(false);
            var l = mainCamera.GetComponent<AudioListener>();
            if (l) l.enabled = false;
        }

        // Save Music
        var audioCtrl = FindFirstObjectByType<NewBark.Audio.AudioController>();
        if (audioCtrl && audioCtrl.BgmChannel)
        {
            lastWorldMusic = audioCtrl.BgmChannel.clip;
        }

        // Generate Enemy List from Trainer
        GenerateTrainerEnemyList(trainer);

        // Define Battle Rules for Trainer
        // TODO: Trainer Data could specify "Double Battle" or "Triple Battle"
        // For now:
        if (enemiesToSpawn.Count >= 2)
        {
            // If trainer has 2+ mons, maybe they want double? 
            // Standard pokemon logic: Trainer establishes the rule.
            // Let's assume Single unless specified. 
            // But user said: "se o oponente querer".
            // Implementation: We will default to Single unless we add a flag to TrainerData later.
            // But if we want to test multi, let's keep Single for safety or matching count?
            // "1x1 o limite é 1... ela pode ser 2x2 se o oponente querer"
            CurrentBattleMode = BattleMode.Single;
            AllowedPlayerSlots = 1;
        }
        else
        {
            CurrentBattleMode = BattleMode.Single;
            AllowedPlayerSlots = 1;
        }

        // Load Battle
        SceneManager.LoadScene("Main_Offline");
    }

    private void GenerateTrainerEnemyList(NewBark.Data.TrainerData trainer)
    {
        enemiesToSpawn.Clear();
        CurrentEnemyParty.Clear();

        if (trainer == null || trainer.party == null) return;

        foreach (var member in trainer.party)
        {
            // Create Instance
            var instance = new NewBark.Runtime.PokemonInstance(member.specie, member.levelSetup != null ? member.levelSetup.level : 5);
            CurrentEnemyParty.Add(instance);

            // Resolve Prefab
            string idName = member.specie;
            GameObject realPrefab = null;
            if (dex_prefab.instance != null)
            {
                realPrefab = dex_prefab.instance.GetEnemyPrefab(idName);
            }
            else
            {
                // Fallback or Log
                // Ideally we should have a default prefab if not found in Dex
            }

            if (realPrefab != null)
            {
                enemiesToSpawn.Add(realPrefab);
            }
            else
            {
                Debug.LogWarning($"Prefab for {idName} not found, skipping spawn visual but data is there.");
            }
        }
    }

    public void Update()
    {
        if (enemiesToSpawn.Count > 0)
        {
            Debug.Log("Enemies to spawn: " + enemiesToSpawn.Count);
        }
        else if (enemiesToSpawn.Count == 0)
        {
            //Debug.Log("No enemies to spawn");
        }
    }

    private void GenerateEnemyList()
    {
        enemiesToSpawn.Clear();
        if (currentEncounterData == null) return;

        // Determine Battle Mode & Count based on RNG and Rules
        // Default: Single 1v1
        CurrentBattleMode = BattleMode.Single;
        AllowedPlayerSlots = 1;

        int enemyCount = 1;

        // RNG for Battle Mode (Wild)
        float modeRoll = Random.Range(0f, 100f);

        // Example Rates:
        // 85% Single (1v1)
        // 10% Double (2v2) 
        // 5% Horde (1v3)
        // Note: Actual enemy count logic implies we loop.

        if (modeRoll < 5f) // 5% Horde
        {
            CurrentBattleMode = BattleMode.Horde;
            enemyCount = 3;
            // Horde rule: small chance 2v3, mostly 1v3
            if (Random.value < 0.2f) AllowedPlayerSlots = 2; // 20% chance of 2v3
            else AllowedPlayerSlots = 1;
        }
        else if (modeRoll < 15f) // 10% Double (5 to 15)
        {
            CurrentBattleMode = BattleMode.Double;
            enemyCount = 2;
            AllowedPlayerSlots = 2;
        }
        else // Single
        {
            CurrentBattleMode = BattleMode.Single;
            enemyCount = 1;
            AllowedPlayerSlots = 1;
        }

        // Clamp by EncounterData limits if they are strict (e.g. min/max enemies)
        // If data says max 1, we force 1.
        enemyCount = Mathf.Clamp(enemyCount, currentEncounterData.minEnemies, currentEncounterData.maxEnemies);
        if (enemyCount == 1)
        {
            CurrentBattleMode = BattleMode.Single;
            AllowedPlayerSlots = 1;
        }

        // Prepare weights
        int totalWeight = 0;
        foreach (var enemy in currentEncounterData.enemyList)
            totalWeight += enemy.spawnWeight;

        for (int i = 0; i < enemyCount; i++)
        {
            int randomValue = Random.Range(0, totalWeight);
            int currentSum = 0;

            foreach (var enemy in currentEncounterData.enemyList)
            {
                currentSum += enemy.spawnWeight;
                if (randomValue < currentSum)
                {
                    // Find the real prefab in the Dex to ensure we are using the "system" prefab
                    // Using the name of the assigned object in EncounterData as the key
                    if (enemy.enemyIdentifier == null)
                    {
                        Debug.LogError("[EncounterManager] Enemy Identifier in Data is missing or destroyed!");
                        continue;
                    }
                    string idName = enemy.enemyIdentifier.name;
                    Debug.Log($"[EncounterManager] Spawning Enemy: Requesting '{idName}' from Dex.");

                    GameObject realPrefab = null;
                    if (dex_prefab.instance != null)
                    {
                        realPrefab = dex_prefab.instance.GetEnemyPrefab(idName);
                        if (realPrefab == null) Debug.LogWarning($"[EncounterManager] '{idName}' not found in Dex!");
                    }
                    else
                    {
                        realPrefab = enemy.enemyIdentifier.gameObject; // Fallback
                        Debug.LogWarning("[EncounterManager] Dex instance is null! Using fallback.");
                    }

                    if (realPrefab)
                        enemiesToSpawn.Add(realPrefab);
                    else
                        Debug.LogError($"[EncounterManager] Failed to resolve prefab for '{idName}'");

                    break;
                }
            }
        }
    }

    public void PreloadWorldScene()
    {
        if (!string.IsNullOrEmpty(lastWorldSceneName))
        {
            StartCoroutine(PreloadCoroutine());
        }
    }

    private System.Collections.IEnumerator PreloadCoroutine()
    {
        preloadOperation = SceneManager.LoadSceneAsync(lastWorldSceneName);
        preloadOperation.allowSceneActivation = false;
        yield return null;
    }

    public void EndEncounter(bool playerWon)
    {
        if (playerWon)
        {
            // Save Trainer Victory
            if (currentTrainer != null && NewBark.GameManager.Data != null)
            {
                if (NewBark.GameManager.Data.beatenTrainers == null)
                {
                    NewBark.GameManager.Data.beatenTrainers = new System.Collections.Generic.List<string>();
                }

                if (!NewBark.GameManager.Data.beatenTrainers.Contains(currentTrainer.dbSymbol))
                {
                    NewBark.GameManager.Data.beatenTrainers.Add(currentTrainer.dbSymbol);
                    Debug.Log($"[EncounterManager] Victory against {currentTrainer.dbSymbol} recorded!");
                }
            }

            // If preloading is active, finish it.
            if (preloadOperation != null)
            {
                // Subscribe callback first to ensure it catches the load event (though it might have been safer to sub before starting async... 
                // but SceneLoaded event is global, so it fires when scene finishes loading).
                // Actually, ensure we don't sub multiple times.
                SceneManager.sceneLoaded -= OnSceneLoadedVictory;
                SceneManager.sceneLoaded += OnSceneLoadedVictory;

                preloadOperation.allowSceneActivation = true;
                preloadOperation = null;
            }
            else
            {
                // Return to world normal load
                SceneManager.LoadScene(lastWorldSceneName);
                SceneManager.sceneLoaded += OnSceneLoadedVictory;
            }
        }
        else
        {
            // Return to hospital/checkpoint
            SceneManager.LoadScene(hospitalSceneName);
            SceneManager.sceneLoaded += OnSceneLoadedDefeat;
        }

        currentEncounterData = null;
    }

    private void OnSceneLoadedVictory(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoadedVictory;

        // Restore Player
        GameObject player = preservedPlayer;
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");

        if (player)
        {
            player.transform.position = lastWorldPosition;
            player.SetActive(true); // Ensure player is re-enabled
        }

        GameObject mainCamera = GameObject.Find("MainCameraWorld");

        if (preservedCamera != null)
        {
            preservedCamera.SetActive(true);
            mainCamera = preservedCamera;
        }
        else if (mainCamera)
        {
            mainCamera.SetActive(true);
        }

        CleanupDuplicates(mainCamera);

        // Restore Music
        if (lastWorldMusic != null)
        {
            var audioCtrl = FindFirstObjectByType<NewBark.Audio.AudioController>();
            if (audioCtrl)
                audioCtrl.PlayBgmTransition(lastWorldMusic);
        }
    }

    private void CleanupDuplicates(GameObject mainCamera)
    {
        if (mainCamera == null) return;

        // 1. Cleanup multiple AudioListeners/Cameras
        var listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        if (listeners.Length > 1)
        {
            Debug.LogWarning($"[EncounterManager] Found {listeners.Length} AudioListeners. Cleaning up duplicates...");
            foreach (var l in listeners)
            {
                if (l.gameObject != mainCamera)
                {
                    // If the object is a Camera, likely we want to destroy the whole duplicate camera object.
                    if (l.GetComponent<Camera>() != null)
                    {
                        Debug.Log($"[EncounterManager] Destroying duplicate Camera object: {l.gameObject.name}");
                        Destroy(l.gameObject);
                    }
                    else
                    {
                        // It's likely a manager (GameController) with a stray AudioListener. 
                        // Just destroy the component, don't kill the manager!
                        Debug.Log($"[EncounterManager] Removing stray AudioListener component from: {l.gameObject.name}");
                        Destroy(l);
                    }
                }
            }
        }

        // 2. Cleanup multiple Global Lights (URP 2D)
        var lights = FindObjectsByType<Light2D>(FindObjectsSortMode.None);
        List<Light2D> globalLights = new List<Light2D>();
        foreach (var checkLight in lights)
        {
            if (checkLight.lightType == Light2D.LightType.Global)
                globalLights.Add(checkLight);
        }

        if (globalLights.Count > 1)
        {
            // If we have duplicates, we usually want to keep the ones in the current scene
            // and destroy survivors from DontDestroyOnLoad that aren't expected.
            foreach (var gl in globalLights)
            {
                // PROTECT THE PLAYER AND ESSENTIALS!
                // If this light belongs to the Player (or children), OR is a known essential light, DO NOT DESTROY IT.
                if (gl.CompareTag("Player")
                    || (preservedPlayer != null && gl.transform.IsChildOf(preservedPlayer.transform))
                    || gl.gameObject.name == "Player Light"
                    || gl.gameObject.name == "World Normal Light")
                {
                    continue;
                }

                // If it's a global light and it's in the DDOL scene, it's a candidate for removal 
                // UNLESS it's the only one (but we are in Count > 1).
                if (gl.gameObject.scene.name == "DontDestroyOnLoad")
                {
                    Debug.LogWarning($"[EncounterManager] Destroying persistent Global Light that might be causing conflicts: {gl.gameObject.name}");
                    Destroy(gl.gameObject);
                }
            }
        }
    }

    private void OnSceneLoadedDefeat(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoadedDefeat;
        // Restore Player
        GameObject player = preservedPlayer;
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");

        if (player)
        {
            player.transform.position = hospitalPosition;
            player.SetActive(true); // Ensure player is re-enabled
        }

        GameObject mainCamera = GameObject.Find("MainCameraWorld");
        if (preservedCamera != null)
        {
            preservedCamera.SetActive(true);
            mainCamera = preservedCamera;
        }
        else if (mainCamera)
        {
            mainCamera.SetActive(true);
        }

        CleanupDuplicates(mainCamera);

        // Restore Music (optional, maybe hospital has its own music)
        // Generally hospital has its own BGM set by the scene.
        if (lastWorldMusic != null)
        {
            var audioCtrl = FindFirstObjectByType<NewBark.Audio.AudioController>();
            if (audioCtrl)
                audioCtrl.PlayBgmTransition(lastWorldMusic);
        }
    }

    private void Start()
    {
        // Clean up listeners on start if any are lingering from previous scenes
        var camera = GameObject.Find("MainCameraWorld");
        CleanupDuplicates(camera);
    }

    private void CreateFallbackPokemon()
    {
        Debug.LogWarning("[EncounterManager] Player has no party! Spawning 'Kañyby 404'...");
        EnsureFallbackPokemonData();

        var kanyby = new NewBark.Runtime.PokemonInstance("000", 1);
        kanyby.Nickname = "Kañyby 404";

        // Force stats to 1 as requested
        kanyby.MaxHP = 1;
        kanyby.CurrentHP = 1;
        kanyby.Attack = 1;
        kanyby.Defense = 1;
        kanyby.SpAttack = 1;
        kanyby.SpDefense = 1;
        kanyby.Speed = 1;

        // Add to current battle party
        CurrentPlayerParty.Add(kanyby);

        // Add to persistent party to avoid this next time (or keep it as a punishment/feature)
        if (NewBark.Runtime.PlayerParty.Instance != null)
        {
            NewBark.Runtime.PlayerParty.Instance.AddMember(kanyby);
        }
    }

    private void EnsureFallbackPokemonData()
    {
        if (NewBark.Data.GameDatabase.Instance == null) return;

        if (!NewBark.Data.GameDatabase.Instance.Species.ContainsKey("000"))
        {
            var data = new NewBark.Data.SpecieData();
            data.dbSymbol = "000";
            data.id = 0;
            data.forms = new System.Collections.Generic.List<NewBark.Data.SpecieForm>();

            var form = new NewBark.Data.SpecieForm();
            form.baseHp = 1;
            form.baseAtk = 1;
            form.baseDfe = 1;
            form.baseAts = 1;
            form.baseDfs = 1;
            form.baseSpd = 1;
            form.type1 = "ghost";
            form.type2 = null;
            form.moveSet = new System.Collections.Generic.List<NewBark.Data.LearnableMove>();
            form.resources = new NewBark.Data.SpecieResources();
            form.resources.back = "000"; // Assuming filename is 000.png

            data.forms.Add(form);
            NewBark.Data.GameDatabase.Instance.Species.Add("000", data);
            Debug.Log("[EncounterManager] Injected '000' (Kañyby 404) data into GameDatabase.");
        }
    }
}
