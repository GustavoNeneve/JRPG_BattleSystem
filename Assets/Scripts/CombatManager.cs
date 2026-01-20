using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public enum BattleState
{
    RECHARGING,
    READY,
    PICKING_TARGET,
    EXECUTING_ACTION,
    SELECTING_TECH,
    SELECTING_ITEM,
    WAITING,
    DEAD,
    GAMEWIN,
    NULL
}

/// <summary>
/// Manages the turn-based battle system, including turn order, victory conditions, and network synchronization.
/// Gerencia o sistema de batalha por turnos, incluindo ordem de turnos, condições de vitória e sincronização de rede.
/// </summary>
public class CombatManager : NetworkBehaviour
{
    [SerializeField] Transform playersParent;
    [SerializeField] Transform enemiesParent;
    [SerializeField] GameOverScreen gameOverScreen;
    [SerializeField] List<CharacterBehaviour> combatQueue = new List<CharacterBehaviour>();
    [Tooltip("Delay when removing character from queue")][SerializeField] float queueDelay = 1.25f;
    [SerializeField] List<EnemyBehaviour> enemiesOnField = new List<EnemyBehaviour>();
    [SerializeField] List<CharacterBehaviour> playersOnField = new List<CharacterBehaviour>();

    int totalXPEarned = 0;
    int currentTargetEnemyIndex = 0;
    int currentFriendlyTargetIndex = 0;

    CharacterBehaviour currentActivePlayer = null;

    public List<CharacterBehaviour> PlayersOnField => playersOnField;
    public List<EnemyBehaviour> EnemiesOnField => enemiesOnField;
    public Transform PlayersParent => playersParent;
    public Transform EnemiesParent => enemiesParent;
    public int CurrentTargetEnemyIndex => currentTargetEnemyIndex;
    public int CurrentFriendlyTargetIndex => currentFriendlyTargetIndex;

    public CharacterBehaviour CurrentActivePlayer
    {
        get => currentActivePlayer;
        set => currentActivePlayer = value;
    }

    public List<CharacterBehaviour> CombatQueue
    {
        get => combatQueue;
        set => combatQueue = value;
    }

    /// <summary>
    /// Singleton instance of the CombatManager.
    /// Instância Singleton do CombatManager.
    /// </summary>
    public static CombatManager instance;

    private void Awake()
    {
        instance = this;
    }
    private void Start()
    {
        // If EncounterManager exists and has enemies, spawn them!
        if (EncounterManager.instance != null && EncounterManager.instance.EnemiesToSpawn.Count > 0)
        {
            SetupEncounter(EncounterManager.instance.EnemiesToSpawn);

            // Set Background
            if (EncounterManager.instance.CurrentBackground != null)
            {
                // Assuming there is a background object or Camera Stack
                // For now, we just log it as we need a reference to the background sprite renderer
                Debug.Log("Should set background to: " + EncounterManager.instance.CurrentBackground.name);
                var bgObj = GameObject.Find("BattleBackground"); // Example name, or just log for now
                if (bgObj && bgObj.GetComponent<SpriteRenderer>())
                    bgObj.GetComponent<SpriteRenderer>().sprite = EncounterManager.instance.CurrentBackground;
            }

            // Play Music
            if (EncounterManager.instance.CurrentBattleMusic != null)
            {
                var audioCtrl = FindFirstObjectByType<NewBark.Audio.AudioController>();
                if (audioCtrl)
                {
                    audioCtrl.PlayBgmTransition(EncounterManager.instance.CurrentBattleMusic);
                }
            }

            // Spawn Player Party
            SetupPlayerParty();
        }
    }

    [SerializeField] List<Transform> playerSpawnSpots;

    private void SetupPlayerParty()
    {
        if (EncounterManager.instance == null) return;
        var party = EncounterManager.instance.CurrentPlayerParty;
        if (party == null || party.Count == 0)
        {
            Debug.LogWarning("Player Party is Empty! Cannot spawn player.");
            return;
        }

        // Find first healthy
        NewBark.Runtime.PokemonInstance leader = null;
        foreach (var p in party)
        {
            if (p.CurrentHP > 0)
            {
                leader = p;
                break;
            }
        }

        if (leader == null)
        {
            Debug.LogWarning("All party data is dead! Sending out the first one anyway.");
            leader = party[0];
        }

        // Validate Spots
        if (playerSpawnSpots == null || playerSpawnSpots.Count == 0)
        {
            playerSpawnSpots = new List<Transform>();
            var s1 = playersParent.Find("Spot 1");
            if (s1) playerSpawnSpots.Add(s1);
        }

        // Resolve Prefab
        GameObject realPrefab = null;
        if (dex_prefab.instance != null)
        {
            realPrefab = dex_prefab.instance.GetEnemyPrefab(leader.SpeciesID);
        }

        if (realPrefab != null && playerSpawnSpots.Count > 0)
        {
            // Spawn
            GameObject spawned = Instantiate(realPrefab, playerSpawnSpots[0].position, Quaternion.identity, playersParent);
            spawned.name = "Player_" + leader.Nickname;

            // Ensure it has NetworkObject if online (skipping for now as we are offline mostly)

            // Strip EnemyBehaviour if present and replace? Or just disable AI?
            var enemyAI = spawned.GetComponent<EnemyBehaviour>();
            if (enemyAI)
            {
                // We CAN reuse EnemyBehaviour but we must disable its AI and set it as Player ownership
                // EnemyBehaviour inherits CharacterBehaviour.
                // We need to inject data.
                // EnemyBehaviour.Setup(data, aiLevel) exists.
                // If we call Setup, it sets up stats. 
                // But we need to ensure AI doesn't run.
                // We can set aiLevel to 0 or manual?
                // Let's look at EnemyBehaviour to see if we can disable AI.
                enemyAI.Setup(leader, 0); // AI 0 = Manual?

                // Force ownership?
                // CharacterBehaviour has 'IsOwner'. In offline, everything is owner locally.
                // CombatManager decides whose turn it is.

                // IMPORTANT: Players list needs to be populated.
                AddPlayerOnField(enemyAI);
            }
        }
    }

    [SerializeField] List<Transform> playerSpawnSpots;

    private void SetupPlayerParty()
    {
        if (EncounterManager.instance == null) return;
        var party = EncounterManager.instance.CurrentPlayerParty;
        if (party == null || party.Count == 0) return;

        NewBark.Runtime.PokemonInstance leader = null;
        foreach (var p in party) { if (p.CurrentHP > 0) { leader = p; break; } }
        if (leader == null) leader = party[0];

        if (playerSpawnSpots == null || playerSpawnSpots.Count == 0)
        {
            playerSpawnSpots = new List<Transform>();
            var s1 = playersParent.Find("Spot 1");
            if (s1) playerSpawnSpots.Add(s1);
        }

        GameObject realPrefab = null;
        if (dex_prefab.instance != null) realPrefab = dex_prefab.instance.GetEnemyPrefab(leader.SpeciesID);

        if (realPrefab != null && playerSpawnSpots.Count > 0)
        {
            GameObject spawned = Instantiate(realPrefab, playerSpawnSpots[0].position, Quaternion.identity, playersParent);
            spawned.name = "Player_" + leader.Nickname;

            var enemyAI = spawned.GetComponent<EnemyBehaviour>();
            if (enemyAI)
            {
                enemyAI.Setup(leader, 0);
                AddPlayerOnField(enemyAI);
            }
        }
    }

    public void AttemptCapture(CharacterBehaviour target, int catchRateBonus)
    {
        StartCoroutine(CaptureCoroutine(target, catchRateBonus));
    }

    IEnumerator CaptureCoroutine(CharacterBehaviour target, int catchRateBonus)
    {
        int maxHP = target.MyStats.baseHP;
        int curHP = target.CurrentHP;
        int specieRate = 45;

        float catchVal = ((3f * maxHP - 2f * curHP) * specieRate * catchRateBonus) / (3f * maxHP);
        if (curHP == 1) catchVal *= 1.5f;

        Debug.Log($"[Capture] Chance: {catchVal} (Bonus: {catchRateBonus})");
        yield return new WaitForSeconds(1f);

        bool caught = (UnityEngine.Random.Range(0, 255) < catchVal) || catchRateBonus >= 255;

        if (caught)
        {
            Debug.Log("Caught!");
            int enemyIndex = enemiesOnField.IndexOf(target as EnemyBehaviour);
            if (enemyIndex != -1 && EncounterManager.instance != null && EncounterManager.instance.CurrentEnemyParty.Count > enemyIndex)
            {
                var caughtMon = EncounterManager.instance.CurrentEnemyParty[enemyIndex];
                caughtMon.CurrentHP = curHP;
                if (NewBark.GameManager.Data.party.Count < 6)
                {
                    NewBark.GameManager.Data.party.Add(caughtMon);
                }
                GameManager.instance.EndGame();
                if (EncounterManager.instance != null) EncounterManager.instance.EndEncounter(true);
            }
        }
        else
        {
            Debug.Log("Broke free!");
        }
    }

    [SerializeField] List<Transform> enemySpawnSpots;

    private void SetupEncounter(List<GameObject> enemies)
    {
        // 1. Validate Spots
        if (enemySpawnSpots == null || enemySpawnSpots.Count == 0)
        {
            enemySpawnSpots = new List<Transform>();
            // Try to find them by name if not assigned
            var s1 = enemiesParent.Find("Spot 1");
            var s2 = enemiesParent.Find("Spot 2");
            var s3 = enemiesParent.Find("Spot 3");
            if (s1) enemySpawnSpots.Add(s1);
            if (s2) enemySpawnSpots.Add(s2);
            if (s3) enemySpawnSpots.Add(s3);
        }

        // 2. Spawn Enemies
        for (int i = 0; i < enemies.Count; i++)
        {
            if (i >= enemySpawnSpots.Count)
            {
                Debug.LogWarning("Not enough spawn spots for all enemies!");
                break;
            }

            var enemyPrefab = enemies[i];

            // Instantiate at Spot Position, but keep in EnemiesParent
            GameObject spawned = Instantiate(enemyPrefab, enemySpawnSpots[i].position, Quaternion.identity, enemiesParent);

            // Setup Data if available
            if (EncounterManager.instance != null && EncounterManager.instance.CurrentEnemyParty.Count > i)
            {
                var behavior = spawned.GetComponent<EnemyBehaviour>();
                if (behavior != null)
                {
                    var data = EncounterManager.instance.CurrentEnemyParty[i];
                    int aiLevel = EncounterManager.instance.CurrentEncounterAILevel;
                    behavior.Setup(data, aiLevel);
                }
            }
        }
    }

    /// <summary>
    /// Registers a player character to the field.
    /// Registra um personagem jogador no campo.
    /// </summary>
    /// <param name="playerToAdd">The player character to add.</param>
    public void AddPlayerOnField(CharacterBehaviour playerToAdd)
    {
        playersOnField.Add(playerToAdd);
    }

    public void AddEnemyOnField(EnemyBehaviour enemyToAdd)
    {
        enemiesOnField.Add(enemyToAdd);
    }

    public bool IsFieldClear()
    {
        foreach (var p in playersOnField)
        {
            if (p.CurrentBattlePhase == BattleState.EXECUTING_ACTION)
                return false;
        }

        foreach (var e in enemiesOnField)
        {
            if (e.CurrentBattlePhase == BattleState.EXECUTING_ACTION)
                return false;
        }

        return true;
    }

    public bool IsAnyEnemyAttacking()
    {
        foreach (var e in enemiesOnField)
        {
            if (e.IsBusy())
                return true;
        }

        return false;
    }

    public EnemyBehaviour CurrentReadyEnemy()
    {
        if (enemiesOnField.Count == 1)
            return enemiesOnField[0];

        int _randomEnemyIndex = Random.Range(0, enemiesOnField.Count);

        while (enemiesOnField[_randomEnemyIndex].CurrentBattlePhase == BattleState.EXECUTING_ACTION)
        {
            _randomEnemyIndex = Random.Range(0, enemiesOnField.Count);
        }

        var _currentEnemy = enemiesOnField[_randomEnemyIndex];
        return _currentEnemy;
    }

    public CharacterBehaviour GetRandomPlayer()
    {
        int _randomPlayerIndex = Random.Range(0, playersOnField.Count);

        while (playersOnField[_randomPlayerIndex].CurrentBattlePhase == BattleState.DEAD)
        {
            _randomPlayerIndex = Random.Range(0, playersOnField.Count);
        }

        var _currentPlayerTarget = playersOnField[_randomPlayerIndex];
        return _currentPlayerTarget;
    }

    public bool IsMyTurn(CharacterBehaviour c)
    {
        if (combatQueue.Count == 0)
            return false;

        return combatQueue[0] == c;
    }

    /// <summary>
    /// Adds a character to the combat queue, indicating they are waiting to act.
    /// Adiciona um personagem à fila de combate, indicando que ele está esperando para agir.
    /// </summary>
    /// <param name="characterToAdd">The character to add.</param>
    public void AddToCombatQueue(CharacterBehaviour characterToAdd)
    {
        if (!GameManager.IsOnline())
        {
            combatQueue.Add(characterToAdd);
            return;
        }

        if (IsServer)
        {
            combatQueue.Add(characterToAdd);

            ulong[] _combatQueueIds = UpdateCombatQueue();

            SyncCombatQueueClientRpc(_combatQueueIds);
        }
    }

    private ulong[] UpdateCombatQueue()
    {
        CharacterBehaviour[] _combatQueueArray = CombatQueue.ToArray();

        ulong[] combatQueueIDs = new ulong[_combatQueueArray.Length];

        for (int i = 0; i < _combatQueueArray.Length; i++)
        {
            combatQueueIDs[i] = _combatQueueArray[i].GetComponent<NetworkBehaviour>().NetworkObjectId;
        }

        return combatQueueIDs;
    }

    public void RemoveFromCombatQueue(CharacterBehaviour characterToRemove)
    {
        StartCoroutine(RemoveFromCombatQueueCoroutine(characterToRemove));
    }

    IEnumerator RemoveFromCombatQueueCoroutine(CharacterBehaviour characterToRemove)
    {
        yield return new WaitForSeconds(queueDelay);

        if (!GameManager.IsOnline())
        {
            combatQueue.Remove(characterToRemove);
            yield break;
        }

        if (IsServer)
        {
            combatQueue.Remove(characterToRemove);
            ulong[] _combatQueueIDs = UpdateCombatQueue();
            SyncCombatQueueClientRpc(_combatQueueIDs);
        }
    }

    public void AddToTotalXP(int amount)
    {
        totalXPEarned += amount;
    }

    public int TotalXPEarned()
    {
        return totalXPEarned;
    }

    public int ReadyPlayersAmount()
    {
        int _playersReady = 0;

        foreach (CharacterBehaviour c in playersOnField)
        {
            if (c.CurrentBattlePhase == BattleState.READY && c.CurrentBattlePhase != BattleState.DEAD)
            {
                _playersReady++;
            }
        }

        return _playersReady;
    }

    public void LookForReadyPlayer()
    {
        StartCoroutine(LookForReadyPlayerCoroutine());
    }

    IEnumerator LookForReadyPlayerCoroutine()
    {
        yield return new WaitForSeconds(0.02f);

        if (currentActivePlayer != null && currentActivePlayer.CurrentBattlePhase != BattleState.DEAD)
        {
            Debug.LogWarning("breaking here");
            yield break;
        }

        foreach (CharacterBehaviour c in playersOnField)
        {
            if (c.CurrentBattlePhase == BattleState.READY)
            {
                SetCurrentActivePlayer(c);
                yield break;
            }
        }

        currentActivePlayer = null;
    }

    /// <summary>
    /// Sets the character who is currently taking their turn (UI input phase).
    /// Define o personagem que está atualmente jogando (fase de input de UI).
    /// </summary>
    /// <param name="c">The character to be active.</param>
    public void SetCurrentActivePlayer(CharacterBehaviour c)
    {
        currentActivePlayer = c;

        if (c != null && (!GameManager.IsOnline() || c.IsOwner))
        {
            c.CharacterUIController.ShowMainBattlePanel();
        }
    }

    public int GetCurrentActivePlayerIndex()
    {
        for (int i = 0; i < playersOnField.Count; i++)
        {
            if (currentActivePlayer == playersOnField[i])
                return i;
        }

        return -1;
    }

    public void PickNextReadyCharacter()
    {
        int _index = GetCurrentActivePlayerIndex();
        _index++;

        if (_index == playersOnField.Count)
            _index = 0;

        currentActivePlayer = null;
        SetCurrentActivePlayer(playersOnField[_index]);
    }

    public void SetTargetedEnemyByIndex(int index, bool isAreaOfEffect = false)
    {
        if (enemiesOnField.Count == 0)
            return;


        if (isAreaOfEffect)
        {
            currentTargetEnemyIndex = index;
            ShowAllEnemyPointers();
            return;
        }
        else
        {
            currentActivePlayer.CurrentTarget = enemiesOnField[index];
        }

        currentTargetEnemyIndex = index;

        for (int i = 0; i < enemiesOnField.Count; i++)
        {
            if (i == index)
            {
                enemiesOnField[i].CharacterUIController.ShowPointer();
            }
            else enemiesOnField[i].CharacterUIController.HidePointer();
        }
    }

    public void IncreaseTargetEnemyIndex()
    {
        currentTargetEnemyIndex++;

        if (currentTargetEnemyIndex > enemiesOnField.Count - 1)
            currentTargetEnemyIndex = 0;

        SetTargetedEnemyByIndex(currentTargetEnemyIndex);
    }

    public void DecreaseTargetEnemyIndex()
    {
        currentTargetEnemyIndex--;

        if (currentTargetEnemyIndex < 0)
            currentTargetEnemyIndex = enemiesOnField.Count - 1;

        SetTargetedEnemyByIndex(currentTargetEnemyIndex);
    }

    public void ShowAllEnemyPointers()
    {
        foreach (EnemyBehaviour enemy in enemiesOnField)
        {
            enemy.CharacterUIController.ShowPointer();
        }
    }

    public void HideAllEnemyPointers()
    {
        foreach (EnemyBehaviour enemy in enemiesOnField)
        {
            enemy.CharacterUIController.HidePointer();
        }
    }

    public void RemoveFromField(EnemyBehaviour enemyToRemove)
    {
        StartCoroutine(RemoveFromFieldCoroutine(enemyToRemove));
    }

    IEnumerator RemoveFromFieldCoroutine(EnemyBehaviour enemy)
    {
        yield return new WaitForSeconds(0.02f);

        enemiesOnField.Remove(enemy);
        CheckWinConditionCoroutine();
    }

    void CheckWinConditionCoroutine()
    {

        if (enemiesOnField.Count == 0)
        {
            GameManager.instance.EndGame();

            // if (EncounterManager.instance != null)
            //    EncounterManager.instance.EndEncounter(true);
        }
    }

    public bool AllPlayersDead()
    {
        foreach (CharacterBehaviour p in playersOnField)
        {
            if (p.CurrentBattlePhase != BattleState.DEAD)
            {
                return false;
            }
        }
        return true;
    }

    public IEnumerator ShowGameOverIfNeeded_Coroutine()
    {
        yield return new WaitForSeconds(0.1f);

        if (AllPlayersDead())
        {
            yield return new WaitForSeconds(1);

            // Always show Game Over screen. The UI will handle "Continue/Hospital".
            if (gameOverScreen != null)
                gameOverScreen.ShowGameOverScreen();

            // if (EncounterManager.instance != null)
            // {
            //     EncounterManager.instance.EndEncounter(false);
            // }
            // else
            // {
            //     gameOverScreen.ShowGameOverScreen();
            // }
        }
    }

    public void SetTargetedFriendlyTargetByIndex(int index, bool isAreaOfEffect = false)
    {
        if (isAreaOfEffect)
        {
            currentFriendlyTargetIndex = index;
            ShowAllFriendlyTargetPointers();
            return;
        }
        currentFriendlyTargetIndex = index;

        for (int i = 0; i < playersOnField.Count; i++)
        {
            if (i == index)
                playersOnField[i].CharacterUIController.ShowPointer();
            else playersOnField[i].CharacterUIController.HidePointer();
        }
    }

    public void IncreaseFriendlyTargetIndex()
    {
        currentFriendlyTargetIndex++;

        if (currentFriendlyTargetIndex > playersOnField.Count - 1)
            currentFriendlyTargetIndex = 0;

        SetTargetedFriendlyTargetByIndex(currentFriendlyTargetIndex);
    }

    public void DecreaseFriendlyTargetIndex()
    {
        currentFriendlyTargetIndex--;

        if (currentFriendlyTargetIndex < 0)
            currentFriendlyTargetIndex = playersOnField.Count - 1;

        SetTargetedFriendlyTargetByIndex(currentFriendlyTargetIndex);
    }

    public void ShowAllFriendlyTargetPointers()
    {
        foreach (CharacterBehaviour character in playersOnField)
            character.CharacterUIController.ShowPointer();
    }

    public void HideAllFriendlyTargetPointers()
    {
        foreach (CharacterBehaviour character in playersOnField)
            character.CharacterUIController.HidePointer();
    }

    #region ONLINE

    [ClientRpc]
    private void SyncCombatQueueClientRpc(ulong[] combatQueueIds)
    {
        CharacterBehaviour[] _combatQueueArray = new CharacterBehaviour[combatQueueIds.Length];

        for (int i = 0; i < combatQueueIds.Length; i++)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(combatQueueIds[i], out var networkObject))
            {
                _combatQueueArray[i] = networkObject.GetComponent<CharacterBehaviour>();
            }
            else
            {
                Debug.LogWarning($"[SyncCombatQueue] NetworkObjectId {combatQueueIds[i]} not found!");
            }
        }

        CombatQueue = _combatQueueArray.ToList();
    }

    #endregion
}
