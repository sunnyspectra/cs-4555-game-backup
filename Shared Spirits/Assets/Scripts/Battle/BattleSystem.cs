using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public enum BattleState { Start, ActionSelection, MoveSelection, TargetSelection, RunningTurn, Busy, PartyScreen, AboutToUse, MoveToForget, BattleOver }

public class BattleSystem : MonoBehaviour
{
    [SerializeField] BattleUnit player1Unit;
    [SerializeField] BattleUnit player2Unit;
    [SerializeField] List<BattleUnit> enemyUnitsMulti;
    [SerializeField] BattleDialogBox dialogBox;
    [SerializeField] PartyScreen partyScreen;
    //[SerializeField] GameObject shardSprite;
    [SerializeField] MoveSelectionUI moveSelectionUI;
    [SerializeField] InventoryUI inventoryUI;
    [SerializeField] GameObject multiBattleElements;
    [SerializeField] Camera worldCamera;
    private List<BattleUnit> originalEnemyUnits;

    List<BattleUnit> player1Units;
    List<BattleUnit> player2Units;
    List<BattleUnit> enemyUnits;
    List<BattleAction> actions;

    int unitCount = 1;
    int actionIndex = 0;
    BattleUnit currentUnit;

    public event Action<bool> OnBattleOver;

    BattleState state;
    int currentAction;
    int currentMove;
    int currentTarget;

    SpiritParty playerParty;
    SpiritParty playerParty2;
    SpiritParty handlerParty;
    Spirit wildSpirit;

    bool isHandlerBattle = false;
    PlayerController player;
    PlayerController player2;
    HandlerController handler;

    MoveBase moveToLearn;
    BattleUnit unitTryingToLearn;
    BattleUnit unitToSwitch;


    public void StartBattle(SpiritParty playerParty, SpiritParty playerParty2, Spirit wildSpirit)
    {
        this.playerParty = playerParty;
        this.wildSpirit = wildSpirit;
        player = playerParty.GetComponent<PlayerController>();
        isHandlerBattle = false;
        unitCount = 1;

        StartCoroutine(SetupBattle());
    }

    public void StartHandlerBattle(SpiritParty playerParty, SpiritParty playerParty2, SpiritParty handlerParty)
    {
        this.playerParty = playerParty;
        this.playerParty2 = playerParty2;
        this.handlerParty = handlerParty;

        isHandlerBattle = true;
        player = playerParty.GetComponent<PlayerController>();
        player2 = playerParty2.GetComponent<PlayerController>();
        handler = handlerParty.GetComponent<HandlerController>();
        StartCoroutine(SetupBattle());
    }

    public IEnumerator SetupBattle()
    {

        player1Unit.Hud.ClearData();
        player2Unit.Hud.ClearData();
        if (enemyUnits != null)
        {
            foreach (var enemyUnit in enemyUnits)
            {
                enemyUnit.Hud.ClearData();
                enemyUnit.gameObject.SetActive(false);
            }
        }

        if (!isHandlerBattle)
        {
            yield return dialogBox.TypeDialog($"A wild {enemyUnits[0].Spirit.Base.Name} appeared.");
        }

        yield return dialogBox.TypeDialog($"{handler.Name} wants to battle");
        unitCount = 2;
        multiBattleElements.SetActive(true);
        player1Units = new List<BattleUnit>() { player1Unit };
        player2Units = new List<BattleUnit>() { player2Unit };
        enemyUnits = new List<BattleUnit>(enemyUnitsMulti);

        var enemySpirits = handlerParty.GetHealthySpirit(2);
        for (int i = 0; i < enemySpirits.Count; i++)
        {
            enemyUnits[i].Hud.gameObject.SetActive(true);
            enemyUnits[i].gameObject.SetActive(true);
            enemyUnits[i].Setup(enemySpirits[i]);
        }

        string names = String.Join(" and ", enemySpirits.Select(p => p.Base.Name));
        yield return dialogBox.TypeDialog($"{handler.Name} send out {names}");

        player1Unit.gameObject.SetActive(true);
        player2Unit.gameObject.SetActive(true);
        player1Units[0].Setup(playerParty.GetHealthySpirit());
        player2Units[0].Setup(playerParty2.GetHealthySpirit());

        var playerSpirits = playerParty.GetHealthySpirit(1);
        var playerSpirits2 = playerParty2.GetHealthySpirit(1);

        names = String.Join(" and ", playerSpirits.Select(p => p.Base.Name));
        yield return dialogBox.TypeDialog($"Go {names}!");
        names = String.Join(" and ", playerSpirits2.Select(p => p.Base.Name));
        yield return dialogBox.TypeDialog($"Go {names}!");

        partyScreen.Init(playerParty, KeyCode.W, KeyCode.S, KeyCode.Z, KeyCode.X);
        actions = new List<BattleAction>();
        ActionSelection(0);
    }

    void ActionSelection(int index)
    {
        var activeSpirits = player1Units.Select(u => u.Spirit).Where(p => p.HP > 0).ToList();
        var activeSpirits2 = player2Units.Select(u => u.Spirit).Where(p => p.HP > 0).ToList();
        state = BattleState.ActionSelection;

        if (index == 0 && activeSpirits.Count > 0)
        {
            currentUnit = player1Units[0];
            dialogBox.SetMoveNames(currentUnit.Spirit.Moves);
            dialogBox.SetDialog($"Choose an action for {currentUnit.Spirit.Base.Name}...");
            dialogBox.EnableActionSelector(true);
        }
        else if (index == 1 && activeSpirits2.Count > 0)
        {
            currentUnit = player2Units[0];
            dialogBox.SetMoveNames(currentUnit.Spirit.Moves);
            dialogBox.SetDialog($"Choose an action for {currentUnit.Spirit.Base.Name}...");
            dialogBox.EnableActionSelector(true);
        }
    }

    void AddBattleAction(BattleAction action)
    {
        actions.Add(action);
        if (actionIndex == 0)
        {
            actionIndex = 1;
        }
        else if (actionIndex == 1)
        {
            actionIndex = 2;
        }
        
        if (actions.Count == unitCount)
        {
            StartCoroutine(RunTurns());
        }
        else
        {
            ActionSelection(actionIndex);
        }
    }

    void HandleActionSelection()
    {
        var activeSpirits = player1Units.Select(u => u.Spirit).Where(p => p.HP > 0).ToList();
        var activeSpirits2 = player2Units.Select(u => u.Spirit).Where(p => p.HP > 0).ToList();

        if (actionIndex == 0 && activeSpirits.Count > 0)
        {
            HandlePlayer1ActionInput();
        }
        else if (actionIndex == 1 && activeSpirits2.Count > 0)
        {
            HandlePlayer2ActionInput();
        }
    }

    void HandlePlayer1ActionInput()
    {
        if (Input.GetKeyDown(KeyCode.D))
            ++currentAction;
        else if (Input.GetKeyDown(KeyCode.A))
            --currentAction;
        else if (Input.GetKeyDown(KeyCode.S))
            currentAction += 2;
        else if (Input.GetKeyDown(KeyCode.W))
            currentAction -= 2;

        currentAction = Mathf.Clamp(currentAction, 0, 3);
        dialogBox.UpdateActionSelection(currentAction);

        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (currentAction == 0) MoveSelection();
            else if (currentAction == 1)
            {
                partyScreen.Init(playerParty, KeyCode.W, KeyCode.S, KeyCode.Z, KeyCode.X);
                OpenPartyScreen();
            }
        }
    }

    void HandlePlayer2ActionInput()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
            ++currentAction;
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            --currentAction;
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            currentAction += 2;
        else if (Input.GetKeyDown(KeyCode.UpArrow))
            currentAction -= 2;

        currentAction = Mathf.Clamp(currentAction, 0, 3);
        dialogBox.UpdateActionSelection(currentAction);

        if (Input.GetKeyDown(KeyCode.N))
        {
            if (currentAction == 0) MoveSelection();
            else if (currentAction == 1)
            {
                partyScreen.Init(playerParty2, KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.N, KeyCode.M);
                OpenPartyScreen();
            }
        }
    }

    void HandleMoveSelection()
    {
        if (currentUnit == player1Unit)
        {
            HandlePlayer1MoveInput();
        }
        else if (currentUnit == player2Unit)
        {
            HandlePlayer2MoveInput();
        }
    }

    void HandlePlayer1MoveInput()
    {
        if (Input.GetKeyDown(KeyCode.D)) ++currentMove;
        else if (Input.GetKeyDown(KeyCode.A)) --currentMove;
        else if (Input.GetKeyDown(KeyCode.S)) currentMove += 2;
        else if (Input.GetKeyDown(KeyCode.W)) currentMove -= 2;
        currentMove = Mathf.Clamp(currentMove, 0, currentUnit.Spirit.Moves.Count - 1);
        dialogBox.UpdateMoveSelection(currentMove, currentUnit.Spirit.Moves[currentMove]);

        if (Input.GetKeyDown(KeyCode.Z)) HandleMoveConfirmation();
        else if (Input.GetKeyDown(KeyCode.X)) ActionSelection(actionIndex);
    }

    void HandlePlayer2MoveInput()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow)) ++currentMove;
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) --currentMove;
        else if (Input.GetKeyDown(KeyCode.DownArrow)) currentMove += 2;
        else if (Input.GetKeyDown(KeyCode.UpArrow)) currentMove -= 2;
        currentMove = Mathf.Clamp(currentMove, 0, currentUnit.Spirit.Moves.Count - 1);
        dialogBox.UpdateMoveSelection(currentMove, currentUnit.Spirit.Moves[currentMove]);

        if (Input.GetKeyDown(KeyCode.N)) HandleMoveConfirmation();
        else if (Input.GetKeyDown(KeyCode.M)) ActionSelection(actionIndex);
    }

    void HandleMoveConfirmation()
    {
        var move = currentUnit.Spirit.Moves[currentMove];
        dialogBox.EnableMoveSelector(false);
        dialogBox.EnableDialogText(true);

        if (enemyUnits.Count > 1)
        {
            TargetSelection();
        }
        else
        {
            var action = new BattleAction()
            {
                Type = ActionType.Move,
                User = currentUnit,
                Target = enemyUnits[0],
                Move = move
            };
            AddBattleAction(action);
        }
    }

    void HandleTargetSelection()
    {
        if (currentUnit == player1Unit)
        {
            if (Input.GetKeyDown(KeyCode.D))
                ++currentTarget;
            else if (Input.GetKeyDown(KeyCode.A))
                --currentTarget;

            if (Input.GetKeyDown(KeyCode.Z))
            {
                ConfirmTargetSelection();
            }
            else if (Input.GetKeyDown(KeyCode.X))
            {
                dialogBox.EnableMoveSelector(true);
                TargetSelection();
            }
        }
        else if (currentUnit == player2Unit)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow))
                ++currentTarget;
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
                --currentTarget;

            if (Input.GetKeyDown(KeyCode.N))
            {
                ConfirmTargetSelection();
            }
            else if (Input.GetKeyDown(KeyCode.M))
            {
                dialogBox.EnableMoveSelector(true);
                TargetSelection();
            }
        }

        currentTarget = Mathf.Clamp(currentTarget, 0, enemyUnits.Count - 1);

        for (int i = 0; i < enemyUnits.Count; i++)
        {
            enemyUnits[i].SetSelected(i == currentTarget);
        }
    }

    void ConfirmTargetSelection()
    {
        enemyUnits[currentTarget].SetSelected(false);

        var action = new BattleAction()
        {
            Type = ActionType.Move,
            User = currentUnit,
            Target = enemyUnits[currentTarget],
            Move = currentUnit.Spirit.Moves[currentMove]
        };
        AddBattleAction(action);
    }

    public void HandleUpdate()
    {
        if (state == BattleState.ActionSelection)
        {
            HandleActionSelection();
        }
        else if (state == BattleState.MoveSelection)
        {
            HandleMoveSelection();
        }
        else if (state == BattleState.TargetSelection)
        {
            HandleTargetSelection();
        }
        else if (state == BattleState.PartyScreen)
        {
            HandlePartySelection();
        }

        else if (state == BattleState.AboutToUse)
        {
            StartCoroutine(SendNextHandlerSpirit());
        }
    }

    void MoveSelection()
    {
        state = BattleState.MoveSelection;
        dialogBox.EnableActionSelector(false);
        dialogBox.EnableDialogText(false);
        dialogBox.EnableMoveSelector(true);
    }

    void TargetSelection()
    {
        state = BattleState.TargetSelection;
        currentTarget = 0;
    }

    IEnumerator RunTurns()
    {
        state = BattleState.RunningTurn;

        foreach (var action in actions)
        {
            if (action.User.Spirit.HP <= 0)
            {
                Debug.Log($" {action.User.name}'");
                continue;
            }

            if (action.IsInvalid)
                continue;

            if (action.Type == ActionType.Move)
            {
                yield return RunMove(action.User, action.Target, action.Move);
                yield return RunAfterTurn(action.User);
                if (state == BattleState.BattleOver) yield break;
            }
            else if (action.Type == ActionType.SwitchSpirit)
            {
                state = BattleState.Busy;
                yield return SwitchSpirit(action.User, action.SelectedSpirit);
            }
            else if (action.Type == ActionType.UseItem)
            {
                dialogBox.EnableActionSelector(false);
            }

            if (state == BattleState.BattleOver) break;
        }

        if (actionIndex == 2)
        {
            yield return HandleEnemyActions();
        }

        if (state != BattleState.BattleOver)
        {
            actions.Clear();
            actionIndex = 0;
            ActionSelection(actionIndex);
        }
    }

    IEnumerator HandleEnemyActions()
    {
        foreach (var enemy in enemyUnits)
        {
            if (enemy.Spirit.HP <= 0)
            {
                continue;
            }
            int randomMove = UnityEngine.Random.Range(0, enemy.Spirit.Moves.Count);
            int randomTarget = 0;

            if (player1Units.Count != 0 && player2Units.Count != 0)
            {
                randomTarget = UnityEngine.Random.Range(0, 2);
            }
            else if (player1Units.Count != 0)
            {
                randomTarget = 0;
            }
            else if (player2Units.Count != 0)
            {
                randomTarget = 1;
            }

            var target = randomTarget == 0 ? player1Units[UnityEngine.Random.Range(0, player1Units.Count)] : player2Units[UnityEngine.Random.Range(0, player2Units.Count)];

            var action = new BattleAction()
            {
                Type = ActionType.Move,
                User = enemy,
                Target = target,
                Move = enemy.Spirit.Moves[randomMove]
            };
            yield return RunMove(action.User, action.Target, action.Move);
            yield return RunAfterTurn(action.User);
        }
        yield return null;
    }

    IEnumerator RunMove(BattleUnit sourceUnit, BattleUnit targetUnit, Move move)
    {
        bool canRunMove = sourceUnit.Spirit.OnBeforeMove();
        if (!canRunMove)
        {
            yield return ShowStatusChanges(sourceUnit.Spirit);
            yield return sourceUnit.Hud.WaitForHPUpdate();
            yield break;
        }
        yield return ShowStatusChanges(sourceUnit.Spirit);
        yield return dialogBox.TypeDialog($"{sourceUnit.Spirit.Base.Name} used {move.Base.Name}");

        if (CheckIfMoveHits(move, sourceUnit.Spirit, targetUnit.Spirit))
        {
            if (move.Base.Category == MoveCategory.Status)
            {
                yield return RunMoveEffects(move.Base.Effects, sourceUnit.Spirit, targetUnit.Spirit, move.Base.Target);
            }
            else
            {
                var damageDetails = targetUnit.Spirit.TakeDamage(move, sourceUnit.Spirit);
                yield return targetUnit.Hud.WaitForHPUpdate();
                yield return ShowDamageDetails(damageDetails);
            }

            if (move.Base.Secondaries != null && move.Base.Secondaries.Count > 0 && targetUnit.Spirit.HP > 0)
            {
                foreach (var secondary in move.Base.Secondaries)
                {
                    var rnd = UnityEngine.Random.Range(1, 101);
                    if (rnd <= secondary.Chance)
                        yield return RunMoveEffects(secondary, sourceUnit.Spirit, targetUnit.Spirit, secondary.Target);
                }
            }

            if (targetUnit.Spirit.HP <= 0)
            {
                yield return HandleSpiritFainted(targetUnit);
            }

        }
        else
        {
            yield return dialogBox.TypeDialog($"{sourceUnit.Spirit.Base.Name}'s attack missed");
        }
    }

    IEnumerator RunMoveEffects(MoveEffects effects, Spirit source, Spirit target, MoveTarget moveTarget)
    {
        if (effects.Boosts != null)
        {
            if (moveTarget == MoveTarget.Self)
                source.ApplyBoosts(effects.Boosts);
            else
                target.ApplyBoosts(effects.Boosts);
        }
        if (effects.Status != ConditionID.none)
        {
            target.SetStatus(effects.Status);
        }
        if (effects.VolatileStatus != ConditionID.none)
        {
            target.SetVolatileStatus(effects.VolatileStatus);
        }

        yield return ShowStatusChanges(source);
        yield return ShowStatusChanges(target);
    }

    void OpenPartyScreen()
    {
        partyScreen.CalledFrom = state;
        state = BattleState.PartyScreen;
        partyScreen.gameObject.SetActive(true);
    }

    IEnumerator RunAfterTurn(BattleUnit sourceUnit)
    {
        if (state == BattleState.BattleOver) yield break;
        yield return new WaitUntil(() => state == BattleState.RunningTurn);
        sourceUnit.Spirit.OnAfterTurn();
        yield return ShowStatusChanges(sourceUnit.Spirit);
        yield return sourceUnit.Hud.WaitForHPUpdate();
        if (sourceUnit.Spirit.HP <= 0)
        {
            yield return HandleSpiritFainted(sourceUnit);
            yield return new WaitUntil(() => state == BattleState.RunningTurn);
        }
    }

    bool CheckIfMoveHits(Move move, Spirit source, Spirit target)
    {
        if (move.Base.AlwaysHits)
            return true;

        float moveAccuracy = move.Base.Accuracy;

        int accuracy = source.StatBoosts[Stat.Accuracy];
        int evasion = target.StatBoosts[Stat.Evasion];

        var boostValues = new float[] { 1f, 4f / 3f, 5f / 3f, 2f, 7f / 3f, 8f / 3f, 3f };

        if (accuracy > 0)
            moveAccuracy *= boostValues[accuracy];
        else
            moveAccuracy /= boostValues[-accuracy];

        if (evasion > 0)
            moveAccuracy /= boostValues[evasion];
        else
            moveAccuracy *= boostValues[-evasion];

        return UnityEngine.Random.Range(1, 101) <= moveAccuracy;
    }

    IEnumerator ShowStatusChanges(Spirit Spirit)
    {
        while (Spirit.StatusChanges.Count > 0)
        {
            var message = Spirit.StatusChanges.Dequeue();
            yield return dialogBox.TypeDialog(message);
        }
    }

    IEnumerator HandleSpiritFainted(BattleUnit faintedUnit)
    {
        yield return dialogBox.TypeDialog($"{faintedUnit.Spirit.Base.Name} died.");
        yield return new WaitForSeconds(2f);
        NextStepsAfterFainting(faintedUnit);
    }

    void BattleOver(bool won)
    {
        state = BattleState.BattleOver;
        playerParty.Spirits.ForEach(p => p.OnBattleOver());
        playerParty2.Spirits.ForEach(p => p.OnBattleOver());
        player1Units.ForEach(u => u.Hud.ClearData());
        player2Units.ForEach(u => u.Hud.ClearData());
        enemyUnits.ForEach(u => u.Hud.ClearData());
        enemyUnits = new List<BattleUnit>(enemyUnitsMulti);

        foreach (var enemyUnit in enemyUnits)
        {
            enemyUnit.gameObject.SetActive(false);
        }

        actionIndex = 0;
        multiBattleElements.SetActive(false);
        worldCamera.gameObject.SetActive(true);

        OnBattleOver?.Invoke(won);
    }


    void NextStepsAfterFainting(BattleUnit faintedUnit)
    {
        var activeSpirits = player1Units.Select(u => u.Spirit).Where(p => p.HP > 0).ToList();
        var activeSpirits2 = player2Units.Select(u => u.Spirit).Where(p => p.HP > 0).ToList();

        var nextSpirit = playerParty.GetHealthySpirits(activeSpirits);
        var nextSpirit2 = playerParty2.GetHealthySpirits(activeSpirits2);

        if (player1Units.Contains(faintedUnit))
        {
            if ((activeSpirits.Count == 0 && nextSpirit == null) && (activeSpirits2.Count == 0 && nextSpirit2 == null))
            {
                Debug.Log($"P1 BO");
                BattleOver(false);
            }
            else if (nextSpirit != null && activeSpirits.Count == 0)
            {
                Debug.Log($"P1 next != null");
                partyScreen.Init(playerParty, KeyCode.W, KeyCode.S, KeyCode.Z, KeyCode.X);
                unitToSwitch = faintedUnit;
                OpenPartyScreen();
            }

            else if (nextSpirit == null)
            {
                Debug.Log($"P1 next = null");
                faintedUnit.Hud.gameObject.SetActive(false);
                faintedUnit.gameObject.SetActive(false);
                var actionsToChange = actions.Where(a => a.Target == faintedUnit).ToList();
                actionsToChange.ForEach(a => a.Target = player2Units.First());
            }
        }

        else if (player2Units.Contains(faintedUnit))
        {

            if (nextSpirit2 != null && activeSpirits2.Count == 0)
            {
                Debug.Log($"P2 next != null");
                partyScreen.Init(playerParty2, KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.N, KeyCode.M);
                unitToSwitch = faintedUnit;
                OpenPartyScreen();
            }
            else if (nextSpirit2 == null)
            {
                Debug.Log($"P2 next = null");
                faintedUnit.Hud.gameObject.SetActive(false);
                faintedUnit.gameObject.SetActive(false);
                var actionsToChange = actions.Where(a => a.Target == faintedUnit).ToList();
                actionsToChange.ForEach(a => a.Target = player1Units.First());
            }
        }

        else 
        {
            var activeSpirits3 = enemyUnits.Select(u => u.Spirit).Where(p => p.HP > 0).ToList();
            var nextSpirit3 = handlerParty.GetHealthySpirits(activeSpirits3);



            if (activeSpirits3.Count == 0 && nextSpirit3 == null)
            {
                faintedUnit.Hud.gameObject.SetActive(false);
                faintedUnit.gameObject.SetActive(false);
                BattleOver(true);
            }
            else if (nextSpirit3 != null)
            {
                unitToSwitch = faintedUnit;
                StartCoroutine(SendNextHandlerSpirit());
            }
            else if (nextSpirit3 == null && activeSpirits3.Count > 0)
            {
                faintedUnit.Hud.gameObject.SetActive(false);
                faintedUnit.gameObject.SetActive(false);
                var actionsToChange = actions.Where(a => a.Target == faintedUnit).ToList();
                actionsToChange.ForEach(a => a.Target = enemyUnits.First(e => e.gameObject.activeSelf));
            }
        }
    }

    IEnumerator ShowDamageDetails(DamageDetails damageDetails)
    {
        if (damageDetails.Critical > 1f)
            yield return dialogBox.TypeDialog("A critical hit!");

        if (damageDetails.TypeEffectiveness > 1f)
            yield return dialogBox.TypeDialog("It's super effective!");
        else if (damageDetails.TypeEffectiveness < 1f)
            yield return dialogBox.TypeDialog("It's not very effective!");
    }

    void HandlePartySelection()
    {
        Action onSelected = () =>
        {
            var selectedMember = partyScreen.SelectedMember;
            if (selectedMember.HP <= 0)
            {
                partyScreen.SetMessageText("Hes dead :'(");
                return;
            }

            if (player1Units.Any(p => p.Spirit == selectedMember))
            {
                partyScreen.SetMessageText("You can't switch with an active Spirit");
                return;
            }

            partyScreen.gameObject.SetActive(false);

            if (partyScreen.CalledFrom == BattleState.ActionSelection)
            {
                var action = new BattleAction()
                {
                    Type = ActionType.SwitchSpirit,
                    User = currentUnit,
                    SelectedSpirit = selectedMember
                };
                AddBattleAction(action);
            }
            else
            {
                state = BattleState.Busy;
                bool isHandlerAboutToUse = partyScreen.CalledFrom == BattleState.AboutToUse;
                StartCoroutine(SwitchSpirit(unitToSwitch, selectedMember, isHandlerAboutToUse));
                unitToSwitch = null;
            }
            partyScreen.CalledFrom = null;
        };

        Action onBack = () =>
        {
            if (player1Units.Any(u => u.Spirit.HP <= 0))
            {
                partyScreen.SetMessageText("You have to choose a Spirit to continue...");
                return;
            }
            partyScreen.gameObject.SetActive(false);
            if (partyScreen.CalledFrom == BattleState.AboutToUse)
            {
                StartCoroutine(SendNextHandlerSpirit());
            }
            else
            {
                ActionSelection(actionIndex);
            }

            partyScreen.CalledFrom = null;
        };
        partyScreen.HandleUpdate(onSelected, onBack);
    }

    IEnumerator SwitchSpirit(BattleUnit unitToSwitch, Spirit newSpirit, bool isHandlerAboutToUse = false)
    {
        if (unitToSwitch.Spirit.HP > 0)
        {
            yield return dialogBox.TypeDialog($"Return {unitToSwitch.Spirit.Base.Name}");
            yield return new WaitForSeconds(2f);
        }
        unitToSwitch.Setup(newSpirit);
        dialogBox.SetMoveNames(newSpirit.Moves);
        yield return dialogBox.TypeDialog($"Go {newSpirit.Base.Name}!");

        if (isHandlerAboutToUse)
        {
            StartCoroutine(SendNextHandlerSpirit());
        }
        else
        {
            state = BattleState.RunningTurn;
        }
    }

    IEnumerator SendNextHandlerSpirit()
    {
        state = BattleState.Busy;
        var faintedUnit = enemyUnits.FirstOrDefault(u => u.Spirit.HP == 0);

        if (faintedUnit != null)
        {
            var activeSpirits = enemyUnits.Select(u => u.Spirit).Where(p => p.HP > 0).ToList();
            var nextSpirit = handlerParty.GetHealthySpirits(activeSpirits);
            faintedUnit.Setup(nextSpirit);
            yield return dialogBox.TypeDialog($"{handler.Name} sends out {nextSpirit.Base.Name}.");
        }

        state = BattleState.RunningTurn;
    }
}