using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{

    public void OnMenus_Confirm(InputValue value)
    {
        if (GameManager.instance.GameWon)
            return;

        CharacterBehaviour _activePlayer = CombatManager.instance.CurrentActivePlayer;

        if (_activePlayer == null)
            return;

        if (_activePlayer.CurrentBattlePhase == BattleState.PICKING_TARGET)
        {
            if (_activePlayer.CurrentPreAction.IsHarmful)
            {
                int idx = CombatManager.instance.CurrentTargetEnemyIndex;
                if (idx >= 0 && idx < CombatManager.instance.EnemiesOnField.Count)
                    _activePlayer.ExecuteActionOn(CombatManager.instance.EnemiesOnField[idx]);
            }
            else
            {
                int idx = CombatManager.instance.CurrentFriendlyTargetIndex;
                if (idx >= 0 && idx < CombatManager.instance.PlayersOnField.Count)
                    _activePlayer.ExecuteActionOn(CombatManager.instance.PlayersOnField[idx]);
            }
        }
    }

    public void OnMenus_Back(InputValue value)
    {
        CharacterBehaviour _activePlayer = CombatManager.instance.CurrentActivePlayer;

        if (_activePlayer == null)
            return;

        if (_activePlayer.CurrentBattlePhase == BattleState.SELECTING_TECH ||
            _activePlayer.CurrentBattlePhase == BattleState.SELECTING_ITEM ||
            _activePlayer.CurrentBattlePhase == BattleState.PICKING_TARGET)
        {
            CombatManager.instance.HideAllEnemyPointers();
            CombatManager.instance.HideAllFriendlyTargetPointers();
            _activePlayer.ChangeBattleState(BattleState.READY);
        }
    }

    public void OnSwapActiveCharacter(InputValue value)
    {
        if (GameManager.IsOnline())
            return;

        CharacterBehaviour _activePlayer = CombatManager.instance.CurrentActivePlayer;

        if (_activePlayer == null)
            return;


        CharacterUIController _uiController = _activePlayer.GetComponentInChildren<CharacterUIController>();


        if (_activePlayer.CurrentBattlePhase == BattleState.DEAD || CombatManager.instance.ReadyPlayersAmount() <= 1 || !_uiController)
            return;

        if (_uiController.FindMainBattlePanel() && _activePlayer.CurrentBattlePhase == BattleState.READY)
        {
            _activePlayer.SwapActiveCharacter();
        }
    }

    public void OnTargetNavigationUP(InputValue value)
    {
        CharacterBehaviour _activePlayer = CombatManager.instance.CurrentActivePlayer;

        if (_activePlayer == null)
            return;

        if (_activePlayer.CurrentBattlePhase == BattleState.PICKING_TARGET && !_activePlayer.CurrentPreAction.isAreaOfEffect)
        {
            if (_activePlayer.CurrentPreAction.IsHarmful)
                CombatManager.instance.IncreaseTargetEnemyIndex();
            else CombatManager.instance.IncreaseFriendlyTargetIndex();
        }
    }

    public void OnTargetNavigationDOWN(InputValue value)
    {
        CharacterBehaviour _activePlayer = CombatManager.instance.CurrentActivePlayer;

        if (_activePlayer == null)
            return;

        if (_activePlayer.CurrentBattlePhase == BattleState.PICKING_TARGET && !_activePlayer.CurrentPreAction.isAreaOfEffect)
        {
            if (_activePlayer.CurrentPreAction.IsHarmful)
                CombatManager.instance.DecreaseTargetEnemyIndex();
            else CombatManager.instance.DecreaseFriendlyTargetIndex();
        }
    }

    public void OnPauseGame(InputValue value)
    {
        if (GameManager.IsOnline() && NetworkManager.Singleton.ConnectedClients.Count < 2)
            return;

        if (!GameManager.IsOnline() || NetworkManager.Singleton.IsServer)
        {
            if (!GameManager.instance.IsPaused)
                GameManager.instance.PauseGame();
            else GameManager.instance.ResumeGame();
        }
    }
}
