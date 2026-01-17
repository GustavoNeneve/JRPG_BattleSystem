using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using NewBark.Support;

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

    public void EndEncounter(bool playerWon)
    {
        if (playerWon)
        {
            // Return to world
            SceneManager.LoadScene(lastWorldSceneName);
            // We need a way to set position after load. 
            // We can subscribe to scene loaded event or use a "PlayerSpawner" in the world scene.
            SceneManager.sceneLoaded += OnSceneLoadedVictory;
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
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        GameObject mainCamera = GameObject.Find("MainCameraWorld");


        // If not found, maybe it's disabled? Try finding by type (slower but works for disabled if using Resources/etc, but FindObjectOfType doesn't working on inactive).
        // If the player was destroyed and reloaded, it should be found.
        // If it was persistent and disabled, we have an issue.
        // Assuming naive reload for now.

        if (player)
        {
            player.transform.position = lastWorldPosition;
            player.SetActive(true); // Ensure player is re-enabled
        }

        if (preservedCamera != null)
        {
            preservedCamera.SetActive(true);
            mainCamera = preservedCamera;
        }
        else if (mainCamera)
        {
            mainCamera.SetActive(true);
        }

        // Cleanup multiple AudioListeners/Cameras
        var listeners = FindObjectsOfType<AudioListener>();
        if (listeners.Length > 1)
        {
            Debug.LogWarning($"[EncounterManager] Found {listeners.Length} AudioListeners. Cleaning up duplicates...");
            foreach (var l in listeners)
            {
                // If we have a preferred 'mainCamera', destroy others.
                // If mainCamera is the one we hold, keep it.
                if (mainCamera != null && l.gameObject != mainCamera)
                {
                    Destroy(l.gameObject); // Destroy the duplicate camera object entirely? Or just component? Usually object is duplicate.
                }
            }
        }

        // Restore Music
        if (lastWorldMusic != null)
        {
            var audioCtrl = FindObjectOfType<NewBark.Audio.AudioController>();
            if (audioCtrl)
                audioCtrl.PlayBgmTransition(lastWorldMusic);
        }
    }

    private void OnSceneLoadedDefeat(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoadedDefeat;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        GameObject mainCamera = GameObject.Find("MainCameraWorld");
        if (player)
        {
            player.transform.position = hospitalPosition;
            player.SetActive(true); // Ensure player is re-enabled
        }
        if (preservedCamera != null)
        {
            preservedCamera.SetActive(true);
            mainCamera = preservedCamera;
        }
        else if (mainCamera)
        {
            mainCamera.SetActive(true);
        }

        // Cleanup multiple AudioListeners
        var listeners = FindObjectsOfType<AudioListener>();
        if (listeners.Length > 1)
        {
            foreach (var l in listeners)
            {
                if (mainCamera != null && l.gameObject != mainCamera)
                {
                    Destroy(l.gameObject);
                }
            }
        }


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
