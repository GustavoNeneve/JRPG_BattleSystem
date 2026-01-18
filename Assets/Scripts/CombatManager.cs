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
                var audioCtrl = FindObjectOfType<NewBark.Audio.AudioController>();
                if (audioCtrl)
                {
                    audioCtrl.PlayBgmTransition(EncounterManager.instance.CurrentBattleMusic);
                }
            }
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
            Instantiate(enemyPrefab, enemySpawnSpots[i].position, Quaternion.identity, enemiesParent);
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
