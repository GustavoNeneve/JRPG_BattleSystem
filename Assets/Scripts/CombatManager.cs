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

    public static CombatManager instance;

    private void Awake()
    {
        instance = this;
    }

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
            gameOverScreen.ShowGameOverScreen();
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
