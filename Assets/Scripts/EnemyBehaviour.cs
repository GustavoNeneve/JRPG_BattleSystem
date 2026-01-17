using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Controls enemy AI logic, including target selection and random attacks.
/// Controla a lógica de IA do inimigo, incluindo seleção de alvo e ataques aleatórios.
/// </summary>
public class EnemyBehaviour : CharacterBehaviour
{
    [Header("ENEMY SPECIFIC PARAMETERS")]
    [SerializeField] float minAttackRate = 6;
    [SerializeField] float maxattackRate = 8;
    [SerializeField] int xpRewarded;
    [SerializeField] float chanceToUseSkill;

    int defaultSortingOrder;
    float waitTime;
    float randomizedInitialDelay;
    CharacterBehaviour currentPlayerTarget;
    SpriteRenderer spriteRenderer;

    public float ChanceToUseSkill => chanceToUseSkill;
    public CharacterBehaviour CurrentPlayerTarget => currentPlayerTarget;

    public static Action<string> OnEnemyUsedSkill;

    public override void Start()
    {
        Initialize();
    }

    protected override void Initialize()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        defaultSortingOrder = spriteRenderer.sortingOrder;

        currentPreAction = ScriptableObject.CreateInstance<CombatActionSO>();
        currentExecutingAction = ScriptableObject.CreateInstance<CombatActionSO>();

        characterUIController = GetComponentInChildren<CharacterUIController>();
        CombatManager.instance.EnemiesOnField.Add(this);
        originalPosition = transform.localPosition;
        currentHP = myStats.baseHP;

        transform.SetParent(CombatManager.instance.EnemiesParent);

        if (GameManager.instance.EnemiesWontAttack)
            return;

        ExecuteActionOn();
    }


    /// <summary>
    /// Selects a random player target from the list of alive players.
    /// Seleciona um alvo jogador aleatório da lista de jogadores vivos.
    /// </summary>
    public void SetRandomTarget()
    {
        if (CombatManager.instance.AllPlayersDead())
            return;

        int _randomPlayerIndex = UnityEngine.Random.Range(0, CombatManager.instance.PlayersOnField.Count);

        while (CombatManager.instance.PlayersOnField[_randomPlayerIndex].CurrentBattlePhase == BattleState.DEAD)
        {
            _randomPlayerIndex = UnityEngine.Random.Range(0, CombatManager.instance.PlayersOnField.Count);
        }

        
        SetTarget(CombatManager.instance.PlayersOnField[_randomPlayerIndex]);
    }

    /// <summary>
    /// Sets a specific target for this enemy.
    /// Define um alvo específico para este inimigo.
    /// </summary>
    /// <param name="target">The target character.</param>
    public void SetTarget(CharacterBehaviour target)
    {
        currentPlayerTarget = target;

        if (GameManager.IsOnline() && IsServer)
        {
            SyncTargetClientRpc(currentPlayerTarget.NetworkObjectId);
        }
    }

    public override void ExecuteActionOn(CharacterBehaviour target=null)
    {
        StartCoroutine(AttackRandomPlayerCoroutine(target));
    }

    IEnumerator AttackRandomPlayerCoroutine(CharacterBehaviour target)
    {
        yield return new WaitUntil(() => GameManager.instance.GameStarted);

        if (GameManager.IsOnline())
        {
            yield return new WaitUntil(() => NetworkManager.Singleton.ConnectedClients.Count == 2);
            Debug.LogWarning("Ok, there's at least one player connected!");
        }

        if (!GameManager.IsOnline() || IsServer)
        {
            randomizedInitialDelay = UnityEngine.Random.Range(4 - .1f, 4 + .1f);
            if (IsServer)
                SyncInitialDelayClientRpc(randomizedInitialDelay);
        }

        yield return new WaitForSeconds(.1f);
        yield return new WaitForSeconds(randomizedInitialDelay);

        while (CurrentBattlePhase != BattleState.DEAD)
        {
            if (!GameManager.IsOnline() || IsServer)
            {
                if (currentPlayerTarget == null)
                    SetRandomTarget();
                else SetTarget(target);
            }

            yield return new WaitForSeconds(.1f); // syncing

            CombatManager.instance.AddToCombatQueue(this);
            ChangeBattleState(BattleState.WAITING);

            yield return new WaitUntil(() => CombatManager.instance.IsFieldClear() &&
                                             CombatManager.instance.IsMyTurn(this) &&
                                             currentPlayerTarget != null);

            if (CombatManager.instance.AllPlayersDead())
            {
                ChangeBattleState(BattleState.WAITING);
                yield break;
            }

            ChangeBattleState(BattleState.EXECUTING_ACTION);

            SetRandomAction();
            yield return new WaitUntil(() => currentExecutingAction.actionType != ActionType.RECHARGING);

            if (!GameManager.IsOnline() || IsServer)
            {
                if (currentPlayerTarget.CurrentBattlePhase == BattleState.DEAD)
                    SetRandomTarget();
            }

            if (currentExecutingAction.goToTarget)
            {
                MoveToTarget(currentPlayerTarget);

                if (currentExecutingAction.actionType == ActionType.SKILL)
                {
                    OnEnemyUsedSkill?.Invoke(currentExecutingAction.actionName);
                }
                else if (currentExecutingAction.actionType == ActionType.NORMAL_ATTACK)
                {
                    if (!GameManager.IsOnline() || IsServer)
                    {
                        var _rndValue = UnityEngine.Random.value;
                        isDoingCritDamageAction = _rndValue > myStats.critChance ? false : true;

                        if (IsServer)
                            SyncIsDoingCritDamageActionClientRpc(isDoingCritDamageAction);
                    }
                }

                yield return new WaitForSeconds(myAnimController.SecondsToReachTarget);
                myAnimController.PlayAnimation(currentExecutingAction.animationCycle.name);
                spriteRenderer.sortingOrder = currentPlayerTarget.GetComponentInChildren<SpriteRenderer>().sortingOrder + 1;

                yield return new WaitForSeconds(currentExecutingAction.animationCycle.cycleTime - 0.25f);

                StartCoroutine(ApplyDamageOrHeal(currentPlayerTarget));

                yield return new WaitForSeconds(0.25f);

                if (currentExecutingAction.goToTarget)
                {
                    spriteRenderer.sortingOrder = defaultSortingOrder;
                    GoBackToStartingPosition();
                }

                OnSkillEnded?.Invoke();
                yield return new WaitForSeconds(.2f);
                myAnimController.PlayAnimation(myAnimController.IdleAnimationName);

                isDoingCritDamageAction = false;
                CombatManager.instance.RemoveFromCombatQueue(this);
                ChangeBattleState(BattleState.RECHARGING);
                currentPlayerTarget = null;
            }

            if (!GameManager.IsOnline() || IsServer)
            {
                waitTime = UnityEngine.Random.Range(minAttackRate, maxattackRate);
                if (IsServer)
                    SyncWaitTimeClientRpc(waitTime);
            }

            yield return new WaitForSeconds(.1f); // sync
            yield return new WaitForSeconds(waitTime);
        }
    }

    public override void TakeDamageOrHeal(int amount, DamageType dmgType, bool isCrit)
    {
        if (dmgType == DamageType.HARMFUL)
        {
            currentHP -= amount;
            characterUIController.ShowFloatingDamageText(amount, dmgType, isCrit);

            if (currentHP <= 0)
            {
                CombatManager.instance.AddToTotalXP(xpRewarded);
                ChangeBattleState(BattleState.DEAD);
            }
        }

        characterUIController.RefreshHP(currentHP, myStats.baseHP);
    }

    private void SetRandomAction(string s = null)
    {
        if (!GameManager.IsOnline() || IsServer)
        {
            float _randomValue = UnityEngine.Random.value;

            if (_randomValue > chanceToUseSkill)
                currentExecutingAction = SelectAction(ActionType.NORMAL_ATTACK);
            else
                currentExecutingAction = SelectAction(ActionType.SKILL);

            if (IsServer)
            {
                int _actionIndex = -1;

                if (currentExecutingAction.actionType == ActionType.NORMAL_ATTACK)
                    _actionIndex = 0;
                else _actionIndex = 1;

                SyncCurrentActionClientRpc(_actionIndex);
            }

        }
    }

    #region ONLINE

    [ClientRpc]
    private void SyncTargetClientRpc(ulong networkObjectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var networkObject))
        {
            currentPlayerTarget = networkObject.GetComponent<CharacterBehaviour>();
        }
        else
        {
            Debug.LogError($"Target with NetworkObjectId {networkObjectId} not found!");
            return;
        }
    }

    [ClientRpc]
    private void SyncInitialDelayClientRpc(float value)
    {
        randomizedInitialDelay = value;
    }

    [ClientRpc]
    private void SyncIsDoingCritDamageActionClientRpc(bool value)
    {
        isDoingCritDamageAction = value;
    }

    [ClientRpc]
    private void SyncWaitTimeClientRpc(float t)
    {
        waitTime = t;
    }

    [ClientRpc]
    private void SyncCurrentActionClientRpc(int actionIndex)
    {
        if (actionIndex == 0)
            currentExecutingAction = SelectAction(ActionType.NORMAL_ATTACK);
        else currentExecutingAction = SelectAction(ActionType.SKILL);
    }

    #endregion
}
