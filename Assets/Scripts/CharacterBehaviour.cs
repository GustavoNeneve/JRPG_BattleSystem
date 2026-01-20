using DG.Tweening;
using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Base class for all characters in battle (Players and Enemies). Handles stats, movement, and battle logic.
/// Classe base para todos os personagens em batalha (Jogadores e Inimigos). Gerencia status, movimento e lógica de batalha.
/// </summary>
public class CharacterBehaviour : NetworkBehaviour
{
    [field: SerializeField] public Transform GetAttackedPos { get; private set; }
    [SerializeField] protected SpriteEffects combatEffects;
    
    /// <summary>
    /// The current state of the character in the Battle State Machine.
    /// O estado atual do personagem na Máquina de Estados da Batalha.
    /// </summary>
    [field: SerializeField] public BattleState CurrentBattlePhase { get; set; }

    [Header("Character ID")]
    [SerializeField] int id;

    [Header("STATS")]
    [SerializeField] protected CharacterStats myStats;

    [Header("Anim Controller")]
    [SerializeField] protected CharacterAnimationController myAnimController;

    [Space(20)]

    [SerializeField] protected CombatActionSO[] characterActions; // 0 = recharging, 1 = normal attack, 2-8 = skills, 9 = useItem

    [Space(20)]
    [Header("INVENTORY")]
    [SerializeField] CharacterInventory inventory;

    [SerializeField] protected CharacterUIController characterUIController;
    [SerializeField] private TrailEffect trailEffect;

    int currentConsumableItemIndex;
    float currentCooldown;
    protected int currentHP;
    protected int currentMP;
    protected Vector2 originalPosition;
    CharacterBehaviour currentTarget = null;
    bool isBusy = false;
    protected int syncedCalculatedValue;
    protected CombatActionSO currentPreAction;
    protected CombatActionSO currentExecutingAction;
    protected bool isDoingCritDamageAction;
    protected MainBattlePanel mainBattlePanel;

    public CharacterUIController CharacterUIController => characterUIController;
    public CombatActionSO[] CharacterActions => characterActions;
    public CharacterStats MyStats => myStats;
    public int ID => id;
    public int CurrentHP => currentHP;
    public int CurrentMP => currentMP;
    public CombatActionSO CurrentPreAction => currentPreAction;
    public bool IsBusy() => isBusy;
    public bool IsDoingCritDamageAction => isDoingCritDamageAction;

    public CharacterBehaviour CurrentTarget
    {
        get => currentTarget;
        set => currentTarget = value;
    }

    public static Action<string> OnSkillUsed;
    public static Action OnSkillEnded;

    protected virtual void OnEnable()
    {
        GameManager.OnGameWon += GameOver_Win;
    }

    protected virtual void OnDisable()
    {
        GameManager.OnGameWon -= GameOver_Win;
    }

    public virtual void Start()
    {
        Initialize();
    }

    protected virtual void Initialize()
    {
        currentPreAction = ScriptableObject.CreateInstance<CombatActionSO>();
        currentExecutingAction = ScriptableObject.CreateInstance<CombatActionSO>();

        CombatManager.instance.AddPlayerOnField(this);

        originalPosition = transform.localPosition;


        ChangeBattleState(BattleState.RECHARGING);

        currentHP = myStats.baseHP;
        currentMP = myStats.baseMP;

        if (!GameManager.IsOnline())
            transform.SetParent(CombatManager.instance.PlayersParent);

        mainBattlePanel = characterUIController.FindMainBattlePanel();
    }

    public virtual void Update()
    {
        if (CurrentBattlePhase == BattleState.DEAD || CombatManager.instance.CurrentActivePlayer != this)
            return;

        mainBattlePanel.ShowHideSwapCharsIndicator(CombatManager.instance.ReadyPlayersAmount() > 1);
    }

    /// <summary>
    /// Increases current HP.
    /// Aumenta o HP atual.
    /// </summary>
    /// <param name="amount">Amount to heal.</param>
    public void IncreaseHP(int amount)
    {
        currentHP += amount;

        if (currentHP > myStats.baseHP)
            currentHP = myStats.baseHP;

        characterUIController.RefreshHPMP();
    }

    public void IncreaseMP(int amount)
    {
        currentMP += amount;

        if (currentMP > myStats.baseMP) 
            currentMP = myStats.baseMP;

        characterUIController.RefreshHPMP();
    }

    private void DecreaseMP(int amount)
    {
        currentMP -= amount;
        characterUIController.RefreshHPMP();
    }

    protected IEnumerator ApplyDamageOrHeal(CharacterBehaviour target)
    {
        if (IsServer || !GameManager.IsOnline())
        {
            syncedCalculatedValue = CalculatedValue();

            if (IsServer)
            {
                SyncCalculatedValueClientRpc(syncedCalculatedValue);
            }
        }

        yield return new WaitUntil(() => syncedCalculatedValue != 0);

        if (!currentExecutingAction.isAreaOfEffect)
        {
            target.TakeDamageOrHeal(syncedCalculatedValue, currentExecutingAction.damageType, isDoingCritDamageAction);
        }
        else
        {
            DamageAllEnemies();
        }

        syncedCalculatedValue = 0;
    }

    private void DamageAllEnemies()
    {
        for (int i = 0; i < CombatManager.instance.EnemiesOnField.Count; i++)
        {
            CombatManager.instance.EnemiesOnField[i].TakeDamageOrHeal(syncedCalculatedValue, currentExecutingAction.damageType, isDoingCritDamageAction);
        }
    }

    /// <summary>
    /// Receives damage or healing.
    /// Recebe dano ou cura.
    /// </summary>
    /// <param name="amount">Amount value.</param>
    /// <param name="dmgType">Type of damage (Harmful, Healing, Mana)</param>
    /// <param name="isCrit">Is it a critical hit?</param>
    public virtual void TakeDamageOrHeal(int amount, DamageType dmgType, bool isCrit)
    {
        if (dmgType == DamageType.HARMFUL)
        {
            currentHP -= amount;
            characterUIController.ShowFloatingDamageText(amount, dmgType, isCrit);

            if (currentHP <= 0)
            {
                ChangeBattleState(BattleState.DEAD);
            }
        }

        else if (dmgType == DamageType.HEALING)
        {
            IncreaseHP(amount);

        }
        else if (dmgType == DamageType.MANA)
        {
            IncreaseMP(amount);
        }

        characterUIController.ShowFloatingDamageText(amount, dmgType, isCrit);
        characterUIController.RefreshHPMP();
    }

    private int CalculatedValue()
    {
        int _rawDamage = myStats.baseDamage();


        if (currentExecutingAction.actionType != ActionType.ITEM)
        {
            return Mathf.RoundToInt(_rawDamage * CritDamageMultiplier() * currentExecutingAction.damageMultiplier);
        }

        return inventory.InventoryItems[currentConsumableItemIndex].itemData.effectAmount;
    }

    public int CritDamageMultiplier()
    {
        if (currentExecutingAction.actionType == ActionType.SKILL)
            return 1;

        return isDoingCritDamageAction ? 2 : 1;
    }

    public void MoveToTarget(CharacterBehaviour enemy)
    {
        transform.DOMove(enemy.GetAttackedPos.position, myAnimController.SecondsToReachTarget).SetEase(Ease.InOutExpo);
    }

    public void GoBackToStartingPosition()
    {
        transform.DOLocalMove(originalPosition, myAnimController.SecondsToGoBack).SetEase(Ease.OutExpo);
    }

    public void GoBackToStartingPositionAndSetToIdle()
    {
        if (currentExecutingAction.goToTarget)
        {
            transform.DOLocalMove(originalPosition, myAnimController.SecondsToGoBack).SetEase(Ease.OutExpo).OnComplete(SetToIdle);
        }
    }

    protected void SetToIdle()
    {
        StartCoroutine(SetToIdle_Coroutine());
    }

    IEnumerator SetToIdle_Coroutine()
    {
        currentTarget = null;
        characterUIController.ShowUI();
        myAnimController.PlayAnimation(myAnimController.IdleAnimationName);
        yield return new WaitForSeconds(0.001f);
        isBusy = false;
    }

    protected void SetToBusy()
    {
        isBusy = true;
        characterUIController.HideMainBattlePanel();
        characterUIController.HideUI();
    }

    /// <summary>
    /// Returns the CombatActionSO for a specific ActionType.
    /// Retorna o CombatActionSO para um ActionType específico.
    /// </summary>
    /// <param name="type">Type of action to search for.</param>
    /// <returns>The CombatActionSO found or null.</returns>
    public CombatActionSO SelectAction(ActionType type)
    {

        for (int i = 0; i < characterActions.Length; i++)
        {
            if (characterActions[i].actionType == type)
                return characterActions[i];
        }

        return null;
    }

    public void UseNormalAttack()
    {

        if (CombatManager.instance.CurrentActivePlayer != this)
            return;

        currentPreAction = SelectAction(ActionType.NORMAL_ATTACK);
        ChangeBattleState(BattleState.PICKING_TARGET);
    }

    public void SelectTech(int skillIndex)
    {
        if (CombatManager.instance.CurrentActivePlayer != this)
            return;

        currentPreAction = characterActions[2 + skillIndex];

        if (currentMP < currentPreAction.mpCost)
            return;

        ChangeBattleState(BattleState.PICKING_TARGET);
    }

    public void SelectConsumableItem(int itemIndex, DamageType dmgType)
    {
        if (CombatManager.instance.CurrentActivePlayer != this)
            return;

        currentPreAction = SelectAction(ActionType.ITEM);
        currentPreAction.damageType = dmgType;
        currentConsumableItemIndex = itemIndex;

        if (GameManager.IsOnline() && IsOwner)
        {
            SyncUseConsumableItemServerRpc(itemIndex, dmgType);
        }

        ChangeBattleState(BattleState.PICKING_TARGET);
    }

    /// <summary>
    /// Executes the current pre-selected action on a target.
    /// Executa a ação pré-selecionada atual em um alvo.
    /// </summary>
    /// <param name="target">The target character.</param>
    public virtual void ExecuteActionOn(CharacterBehaviour target)
    {
        if (!GameManager.IsOnline() || IsOwner)
        {
            currentTarget = target;
            characterUIController.HideChatBubble();

            if (GameManager.IsOnline())
            {
                var _targetNetId = currentTarget.GetComponent<NetworkObject>().NetworkObjectId;

                int _actionIndex = -1;
                for (int i = 0; i < characterActions.Length; i++)
                {
                    if (currentPreAction == characterActions[i])
                    {
                        _actionIndex = i;
                        break;
                    }
                }

                SyncExecuteActionServerRpc(_targetNetId, _actionIndex);
            }
            else
            {
                CombatManager.instance.AddToCombatQueue(this);
                StartCoroutine(ExecuteActionCoroutine(currentTarget));
            }
        }
    }

    IEnumerator ExecuteActionCoroutine(CharacterBehaviour target)
    {
        ChangeBattleState(BattleState.WAITING);

        if (CombatManager.instance.CurrentActivePlayer == this)
        {
            CombatManager.instance.CurrentActivePlayer = null;
            CombatManager.instance.LookForReadyPlayer();
        }

        currentExecutingAction = currentPreAction;
        CombatManager.instance.HideAllEnemyPointers();
        target.characterUIController.HidePointer();

        yield return new WaitUntil(() => CombatManager.instance.IsFieldClear() &&
                                         CombatManager.instance.IsMyTurn(this));

        ChangeBattleState(BattleState.EXECUTING_ACTION);

        if (target.CurrentBattlePhase == BattleState.DEAD)
        {
            if (currentExecutingAction.IsHarmful)
                target = CombatManager.instance.EnemiesOnField[0];
            else target = CombatManager.instance.PlayersOnField[0];
        }


        if (currentExecutingAction.actionType == ActionType.SKILL)
        {
            OnSkillUsed?.Invoke(currentExecutingAction.actionName);
            DecreaseMP(currentExecutingAction.mpCost);
        }

        else if (currentExecutingAction.actionType == ActionType.NORMAL_ATTACK)
        {

            var _rndValue = UnityEngine.Random.value;

            if (!GameManager.IsOnline() || IsOwner)
            {
                isDoingCritDamageAction = _rndValue > myStats.critChance ? false : true;

                if (GameManager.IsOnline())
                    SyncIsDoingCritDamageServerRpc(isDoingCritDamageAction);
            }
        }

        if (currentExecutingAction.goToTarget)
        {
            if (trailEffect)
                trailEffect.ShowTrail();

            MoveToTarget(target);

            yield return new WaitForSeconds(myAnimController.SecondsToReachTarget);

            if (trailEffect)
                trailEffect.HideTrail();
        }

        myAnimController.PlayAnimation(currentExecutingAction.animationCycle.name);

        if (currentExecutingAction.projectile != null)
        {
            if (currentExecutingAction.isAreaOfEffect == false)
            {
                SpellBehaviour _instantiatedProjectile = Instantiate(currentExecutingAction.projectile.gameObject, myAnimController.ProjectileSpawnPoint.transform.position, Quaternion.identity).GetComponent<SpellBehaviour>();

                yield return new WaitForSeconds(0.25f);
                _instantiatedProjectile.Execute(myAnimController.ProjectileSpawnPoint.position, target);
            }
            else
            {
                yield return new WaitForSeconds(0.25f);
                for (int i = 0; i < CombatManager.instance.EnemiesOnField.Count; i++)
                {
                    SpellBehaviour _instantiatedProjectile = Instantiate(currentExecutingAction.projectile.gameObject, myAnimController.ProjectileSpawnPoint.position, Quaternion.identity).GetComponent<SpellBehaviour>();
                    _instantiatedProjectile.transform.SetParent(this.gameObject.transform);
                    _instantiatedProjectile.Execute(myAnimController.ProjectileSpawnPoint.position, CombatManager.instance.EnemiesOnField[i]);
                }
                yield return new WaitForSeconds(0.25f);
            }
        }

        CombatManager.instance.HideAllFriendlyTargetPointers();

        yield return new WaitForSeconds(currentExecutingAction.animationCycle.cycleTime - 0.25f);

        StartCoroutine(ApplyDamageOrHeal(target));

        if (currentExecutingAction.actionType == ActionType.ITEM)
        {
            inventory.ConsumeItem(currentConsumableItemIndex);
            
            if (currentExecutingAction.damageType == DamageType.CAPTURE)
            {
                  // Capture Logic
                  CombatManager.instance.AttemptCapture(target, currentExecutingAction.damageMultiplier > 0 ? (int)currentExecutingAction.damageMultiplier : 1);
            }
            else
            {
                target.myAnimController.PlayHealingEffect();
            }
        }

        yield return new WaitForSeconds(0.25f);

        GoBackToStartingPositionAndSetToIdle();
        OnSkillEnded?.Invoke();
        CombatManager.instance.RemoveFromCombatQueue(this);
        isDoingCritDamageAction = false;
        ChangeBattleState(BattleState.RECHARGING);
    }
    
    /// <summary>
    /// Changes the character's battle state.
    /// Altera o estado de batalha do personagem.
    /// </summary>
    /// <param name="phase">The new BattleState.</param>
    public void ChangeBattleState(BattleState phase)
    {
        StartCoroutine(ChangeBattleStateCoroutine(phase));
    }

    IEnumerator ChangeBattleStateCoroutine(BattleState p)
    {
        yield return new WaitForSeconds(0.02f);
        CurrentBattlePhase = p;

        switch (CurrentBattlePhase)
        {
            case BattleState.RECHARGING:
                currentPreAction = SelectAction(ActionType.RECHARGING);
                currentExecutingAction = currentPreAction;
                StartCoroutine(RechargingCoroutine());
                SetToIdle();
                break;

            case BattleState.READY:

                if (characterUIController.FindMainBattlePanel())
                {
                    if (!GameManager.IsOnline())
                    {
                        if (CombatManager.instance.CurrentActivePlayer == null)
                            CombatManager.instance.SetCurrentActivePlayer(this);
                        else if (CombatManager.instance.CurrentActivePlayer == this)
                            CharacterUIController.ShowMainBattlePanel();
                        else CombatManager.instance.LookForReadyPlayer();
                    }
                    else
                    {
                        if (!IsOwner)
                            characterUIController.ShowChatBubble();
                        else
                        {
                            CombatManager.instance.SetCurrentActivePlayer(this);
                            characterUIController.HideChatBubble();
                        }
                    }
                }

                break;

            case BattleState.PICKING_TARGET:

                characterUIController.HideMainBattlePanel();
                if (currentPreAction.IsHarmful)
                    CombatManager.instance.SetTargetedEnemyByIndex(0, currentPreAction.isAreaOfEffect);
                else CombatManager.instance.SetTargetedFriendlyTargetByIndex(0, currentPreAction.isAreaOfEffect);

                break;

            case BattleState.EXECUTING_ACTION:
                SetToBusy();
                break;

            case BattleState.SELECTING_TECH:
                break;
            case BattleState.SELECTING_ITEM:
                break;
            case BattleState.WAITING:
                break;
            case BattleState.DEAD:
                CombatManager.instance.RemoveFromCombatQueue(this);

                if (!GetComponent<EnemyBehaviour>())
                {
                    myAnimController.PlayAnimation(myAnimController.DeadAnimationName);
                    Material _mat = GetComponentInChildren<SpriteRenderer>().material;
                    _mat.SetFloat("_GreyscaleBlend", 1);
                    characterUIController.HideCanvas(10, 0);

                    if (CombatManager.instance.CurrentActivePlayer == this)
                        CombatManager.instance.LookForReadyPlayer();

                    StartCoroutine(CombatManager.instance.ShowGameOverIfNeeded_Coroutine());

                }
                else
                {
                    myAnimController.DisableAnimator();
                    characterUIController.HideCanvas(5, .25f);
                    combatEffects.DieEffect();
                    CombatManager.instance.RemoveFromField(this.GetComponent<EnemyBehaviour>());

                    yield return new WaitForSeconds(.1f);

                    if (CombatManager.instance.CurrentActivePlayer == null)
                        break;

                    if (CombatManager.instance.CurrentActivePlayer.CurrentTarget == this &&
                        CombatManager.instance.CurrentActivePlayer.CurrentBattlePhase == BattleState.PICKING_TARGET)
                    {
                        Debug.LogWarning("I'm dead! Picking other");
                        CombatManager.instance.SetTargetedEnemyByIndex(0);
                    }
                }
                break;

            case BattleState.GAMEWIN:
                myAnimController.PlayAnimation(myAnimController.IdleAnimationName);
                characterUIController.HideCanvas();
                yield return new WaitForSeconds(.5f);
                StopAllCoroutines();
                break;

            case BattleState.NULL:
                break;
            default:
                break;
        }
    }

    IEnumerator RechargingCoroutine()
    {
        if (!characterUIController.cooldownBar)
            yield break;

        currentCooldown = 0;

        yield return new WaitUntil(() => GameManager.instance.GameStarted);

        while (currentCooldown < myStats.rechargeRate)
        {
            currentCooldown += Time.deltaTime;
            characterUIController.cooldownBar.fillAmount = currentCooldown / myStats.rechargeRate;
            yield return null;
        }
        ChangeBattleState(BattleState.READY);
    }

    public void ShowDescription(int skillIndex)
    {
        CharacterUIController.ShowDescriptionTooltip(characterActions[2 + skillIndex].description);
    }

    public void SwapActiveCharacter()
    {
        StartCoroutine(SwapActiveCharacter_Coroutine());
    }

    IEnumerator SwapActiveCharacter_Coroutine()
    {
        yield return new WaitForSeconds(0.02f);
        CharacterUIController.HideMainBattlePanel();
        CombatManager.instance.PickNextReadyCharacter();
    }

    public void GameOver_Win()
    {
        ChangeBattleState(BattleState.GAMEWIN);
    }

    #region ONLINE

    [ClientRpc]
    private void SyncCalculatedValueClientRpc(int amount)
    {
        syncedCalculatedValue = amount;
    }

    [ServerRpc]
    private void SyncUseConsumableItemServerRpc(int itemIndex, DamageType dmgType)
    {
        currentPreAction = characterActions[characterActions.Length - 1];
        currentConsumableItemIndex = itemIndex;
        currentPreAction.damageType = dmgType;

        SyncUseConsumableItemClientRpc(itemIndex, dmgType);
    }

    [ClientRpc]
    private void SyncUseConsumableItemClientRpc(int itemIndex, DamageType dmgType)
    {
        currentPreAction = characterActions[characterActions.Length - 1];
        currentConsumableItemIndex = itemIndex;
        currentPreAction.damageType = dmgType;
    }

    [ServerRpc]
    private void SyncExecuteActionServerRpc(ulong targetNetId, int actionIndex)
    {
        SyncExecuteActionClientRpc(targetNetId, actionIndex);
    }

    [ClientRpc]
    private void SyncExecuteActionClientRpc(ulong targetNetId, int actionIndex)
    {
        currentPreAction = characterActions[actionIndex];

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetId, out var networkObject))
        {
            currentTarget = networkObject.GetComponent<CharacterBehaviour>();
        }
        else
        {
            Debug.LogError($"Target with NetworkObjectId {targetNetId} not found!");
            return;
        }

        CombatManager.instance.AddToCombatQueue(this);
        characterUIController.HideChatBubble();

        StartCoroutine(ExecuteActionCoroutine(currentTarget));
    }

    [ServerRpc]
    private void SyncIsDoingCritDamageServerRpc(bool isDoingCritDamageAction)
    {
        SyncIsDoingCritDamageClientRpc(isDoingCritDamageAction);
    }

    [ClientRpc]
    private void SyncIsDoingCritDamageClientRpc(bool result)
    {
        isDoingCritDamageAction = result;
    }

    #endregion
}
