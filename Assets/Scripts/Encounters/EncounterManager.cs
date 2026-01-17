using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EncounterManager : MonoBehaviour
{
    public static EncounterManager instance;

    [Header("Runtime Info")]
    [SerializeField] EncounterData currentEncounterData;
    [SerializeField] List<EnemyBehaviour> enemiesToSpawn = new List<EnemyBehaviour>();
    [SerializeField] Vector3 lastWorldPosition;
    [SerializeField] string lastWorldSceneName;
    [SerializeField] AudioClip lastWorldMusic;

    [Header("Defaults")]
    [Tooltip("Where to go if defeated")]
    public string hospitalSceneName = "World";
    public Vector3 hospitalPosition;

    public List<EnemyBehaviour> EnemiesToSpawn => enemiesToSpawn;
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
    }

    public void StartEncounter(EncounterData data)
    {
        currentEncounterData = data;

        // Save Position & Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player)
        {
            lastWorldPosition = player.transform.position;
            lastWorldSceneName = SceneManager.GetActiveScene().name;
            player.SetActive(false);
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
                    enemiesToSpawn.Add(enemy.enemyPrefab);
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

        // If not found, maybe it's disabled? Try finding by type (slower but works for disabled if using Resources/etc, but FindObjectOfType doesn't working on inactive).
        // If the player was destroyed and reloaded, it should be found.
        // If it was persistent and disabled, we have an issue.
        // Assuming naive reload for now.

        if (player)
        {
            player.transform.position = lastWorldPosition;
            player.SetActive(true); // Ensure player is re-enabled
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
        if (player)
        {
            player.transform.position = hospitalPosition;
            player.SetActive(true); // Ensure player is re-enabled
        }

        // Restore Music (optional, maybe hospital has its own music)
        // Generally hospital has its own BGM set by the scene.
    }
}
