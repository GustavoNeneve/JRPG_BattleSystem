using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using NewBark.Support;
using UnityEngine.Rendering.Universal;

public class EncounterManager : MonoBehaviour
{
    public static EncounterManager instance;

    [Header("Runtime Info")]
    [SerializeField] EncounterData currentEncounterData;
    [SerializeField] List<GameObject> enemiesToSpawn = new List<GameObject>();
    [SerializeField] Vector3 lastWorldPosition;
    [SerializeField] string lastWorldSceneName;
    [SerializeField] AudioClip lastWorldMusic;

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

        // Save Position & Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        // Find Camera explicitly by Name as requested
        // Find Camera explicitly by Name as requested
        GameObject mainCamera = GameObject.Find("MainCameraWorld");
        // Ensure we handle AudioListener if present
        AudioListener listener = null;
        if (mainCamera) listener = mainCamera.GetComponent<AudioListener>();

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
        var audioCtrl = FindObjectOfType<NewBark.Audio.AudioController>();
        if (audioCtrl && audioCtrl.BgmChannel)
        {
            lastWorldMusic = audioCtrl.BgmChannel.clip;
        }

        // Calculate Enemies
        GenerateEnemyList();

        // Load Battle
        SceneManager.LoadScene("Main_Offline");
    }

    public void Update()
    {
        if (enemiesToSpawn.Count > 0)
        {
            Debug.Log("Enemies to spawn: " + enemiesToSpawn.Count);
        }
        else if (enemiesToSpawn.Count == 0)
        {
            Debug.Log("No enemies to spawn");
        }
    }

    private void GenerateEnemyList()
    {
        enemiesToSpawn.Clear();
        if (currentEncounterData == null) return;

        int enemyCount = Random.Range(currentEncounterData.minEnemies, currentEncounterData.maxEnemies + 1);

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
            var audioCtrl = FindObjectOfType<NewBark.Audio.AudioController>();
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
            var audioCtrl = FindObjectOfType<NewBark.Audio.AudioController>();
            if (audioCtrl)
                audioCtrl.PlayBgmTransition(lastWorldMusic);
        }
    }
}
