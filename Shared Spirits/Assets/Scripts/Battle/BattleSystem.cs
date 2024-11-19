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
    bool aboutToUseChoice = true;

    SpiritParty playerParty;
    SpiritParty playerParty2;
    SpiritParty handlerParty;
    Spirit wildSpirit;

    bool isHandlerBattle = false;
    PlayerController player;
    PlayerController2 player2;
    HandlerController handler;

    

    MoveBase moveToLearn;
    BattleUnit unitTryingToLearn;
    BattleUnit unitToSwitch;

    public void StartBattle(SpiritParty playerParty, Spirit wildSpirit)
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
        player2 = playerParty2.GetComponent<PlayerController2>();
        handler = handlerParty.GetComponent<HandlerController>();
        StartCoroutine(SetupBattle());
    }

    public IEnumerator SetupBattle()
    {
        if (!isHandlerBattle)
        {
            yield return dialogBox.TypeDialog($"A wild {enemyUnits[0].Spirit.Base.Name} appeared.");
        }

        yield return dialogBox.TypeDialog($"{handler.Name} wants to battle");
        unitCount = 2;
        multiBattleElements.SetActive(true);
        player1Units = new List<BattleUnit>() { player1Unit };
        player2Units = new List<BattleUnit>() { player2Unit };
        enemyUnits = enemyUnitsMulti;
        var enemySpirits = handlerParty.GetHealthySpirit(2);
        Debug.Log($"Spirits with {enemySpirits}");
        for (int i = 0; i < enemySpirits.Count; i++)
        {
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
        var playerSpirits2 =playerParty2.GetHealthySpirit(1);

        names = String.Join(" and ", playerSpirits.Select(p => p.Base.Name));
        yield return dialogBox.TypeDialog($"Go {names}!");
        names = String.Join(" and ", playerSpirits2.Select(p => p.Base.Name));
        yield return dialogBox.TypeDialog($"Go {names}!");

        partyScreen.Init();
        actions = new List<BattleAction>();
        //for (int i = 0; i < 2; i++)
        //{
            ActionSelection(0);
        //}
        
    }

    void ActionSelection(int actionIndex)
    {
        /*if (this.player == player1)
        {
            controls = off of wasd z, x

        }
        else if (this.player == player2) 
        { 
            controls  off of up arrow / down arrow etc, and n and m
        }*/

        state = BattleState.ActionSelection;

        this.actionIndex = actionIndex;
        currentUnit = player1Units[actionIndex];

        dialogBox.SetMoveNames(currentUnit.Spirit.Moves);
        
        dialogBox.SetDialog($"Choose an action for {currentUnit.Spirit.Base.Name}");
        dialogBox.EnableActionSelector(true);
    }

    void OpenPartyScreen()
    {
        partyScreen.CalledFrom = state;
        state = BattleState.PartyScreen;
        partyScreen.gameObject.SetActive(true);
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

    void AddBattleAction(BattleAction action)
    {
        actions.Add(action);

        // Check if all player actions are selected
        if (actions.Count == unitCount) // this would only let player 1 move because its not based for both players
        {
            // Choose enemy actions
            foreach (var enemyUnit in enemyUnits)
            {
                var randAction = new BattleAction()
                {
                    Type = ActionType.Move,
                    User = enemyUnit,
                    Target = player1Units[UnityEngine.Random.Range(0, player1Units.Count)],
                    Move = enemyUnit.Spirit.GetRandomMove()
                };
                actions.Add(randAction);
            }

            // Sort Actions
            actions = actions.OrderByDescending(a => a.Priority)
                .ThenByDescending(a => a.User.Spirit.Speed).ToList();

            StartCoroutine(RunTurns());
        }
        else
        {
            ActionSelection(actionIndex + 1);
        }
    }

    IEnumerator RunTurns()
    {
        state = BattleState.RunningTurn;

        foreach (var action in actions)
        {
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
                // This is handled from item screen, so do nothing and skip to enemy move
                dialogBox.EnableActionSelector(false);
            }


            if (state == BattleState.BattleOver) break;
        }

        if (state != BattleState.BattleOver)
        {
            actions.Clear();
            ActionSelection(0);
        }
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

        move.PP--;
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
        // Stat Boosting
        if (effects.Boosts != null)
        {
            if (moveTarget == MoveTarget.Self)
                source.ApplyBoosts(effects.Boosts);
            else
                target.ApplyBoosts(effects.Boosts);
        }

        // Status Condition
        if (effects.Status != ConditionID.none)
        {
            target.SetStatus(effects.Status);
        }

        // Volatile Status Condition
        if (effects.VolatileStatus != ConditionID.none)
        {
            target.SetVolatileStatus(effects.VolatileStatus);
        }

        yield return ShowStatusChanges(source);
        yield return ShowStatusChanges(target);
    }

    IEnumerator RunAfterTurn(BattleUnit sourceUnit)
    {
        if (state == BattleState.BattleOver) yield break;
        yield return new WaitUntil(() => state == BattleState.RunningTurn);

        // Statuses like burn or psn will hurt the Spirit after the turn
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
        yield return dialogBox.TypeDialog($"{faintedUnit.Spirit.Base.Name} Fainted");
        yield return new WaitForSeconds(2f);

        yield return HandleExpGain(faintedUnit);

        NextStepsAfterFainting(faintedUnit);
    }

    void BattleOver(bool won)
    {
        state = BattleState.BattleOver;
        playerParty.Spirits.ForEach(p => p.OnBattleOver());

        player1Units.ForEach(u => u.Hud.ClearData());
        enemyUnits.ForEach(u => u.Hud.ClearData());

        OnBattleOver(won);
    }

    IEnumerator ChooseMoveToForget(Spirit Spirit, MoveBase newMove)
    {
        state = BattleState.Busy;
        yield return dialogBox.TypeDialog($"Choose a move you wan't to forget");
        moveSelectionUI.gameObject.SetActive(true);
        moveSelectionUI.SetMoveData(Spirit.Moves.Select(x => x.Base).ToList(), newMove);
        moveToLearn = newMove;

        state = BattleState.MoveToForget;
    }

    IEnumerator HandleExpGain(BattleUnit faintedUnit)
    {
        if (!faintedUnit.IsPlayerUnit)
        {
            // Exp Gain
            int expYield = faintedUnit.Spirit.Base.ExpYield;
            int enemyLevel = faintedUnit.Spirit.Level;


            int expGain = Mathf.FloorToInt((expYield * enemyLevel) / (7 * unitCount));

            for (int i = 0; i < unitCount; i++)
            {
                var playerUnit = player1Units[i];

                playerUnit.Spirit.Exp += expGain;
                yield return dialogBox.TypeDialog($"{playerUnit.Spirit.Base.Name} gained {expGain} exp");
                yield return playerUnit.Hud.SetExpSmooth();

                // Check Level Up
                while (playerUnit.Spirit.CheckForLevelUp())
                {
                    playerUnit.Hud.SetLevel();
                    yield return dialogBox.TypeDialog($"{playerUnit.Spirit.Base.Name} grew to level {playerUnit.Spirit.Level}");

                    // Try to learn a new Move
                    var newMove = playerUnit.Spirit.GetLearnableMoveAtCurrLevel();
                    if (newMove != null)
                    {
                        if (playerUnit.Spirit.Moves.Count < SpiritBase.MaxNumOfMoves)
                        {
                            playerUnit.Spirit.LearnMove(newMove.Base);
                            yield return dialogBox.TypeDialog($"{playerUnit.Spirit.Base.Name} learned {newMove.Base.Name}");
                            dialogBox.SetMoveNames(playerUnit.Spirit.Moves);
                        }
                        else
                        {
                            unitTryingToLearn = playerUnit;
                            yield return dialogBox.TypeDialog($"{playerUnit.Spirit.Base.Name} trying to learn {newMove.Base.Name}");
                            yield return dialogBox.TypeDialog($"But it cannot learn more than {SpiritBase.MaxNumOfMoves} moves");
                            yield return ChooseMoveToForget(playerUnit.Spirit, newMove.Base);
                            yield return new WaitUntil(() => state != BattleState.MoveToForget);
                            yield return new WaitForSeconds(2f);
                        }
                    }

                    yield return playerUnit.Hud.SetExpSmooth(true);
                }
            }


            yield return new WaitForSeconds(1f);
        }
    }

    void NextStepsAfterFainting(BattleUnit faintedUnit)
    {
        // Remove the action of fainted Spirit
        var actionToRemove = actions.FirstOrDefault(a => a.User == faintedUnit);
        if (actionToRemove != null)
            actionToRemove.IsInvalid = true;

        if (faintedUnit.IsPlayerUnit)
        {
            var activeSpirits = player1Units.Select(u => u.Spirit).Where(p => p.HP > 0).ToList();
            var nextSpirit = playerParty.GetHealthySpirits(activeSpirits);

            if (activeSpirits.Count == 0 && nextSpirit == null)
            {
                BattleOver(false);
            }
            else if (nextSpirit != null)
            {
                // Send out next Spirit
                unitToSwitch = faintedUnit;
                OpenPartyScreen();
            }
            else if (nextSpirit == null && activeSpirits.Count > 0)
            {
                // No Spirit left to send out but we can stil continue the battle
                player1Units.Remove(faintedUnit);
                faintedUnit.Hud.gameObject.SetActive(false);

                // Attacks targeted at the removed unit should be changed
                var actionsToChange = actions.Where(a => a.Target == faintedUnit).ToList();
                actionsToChange.ForEach(a => a.Target = player1Units.First());
            }
        }
        else
        {
            if (!isHandlerBattle)
            {
                BattleOver(true);
                return;
            }

            var activeSpirits = enemyUnits.Select(u => u.Spirit).Where(p => p.HP > 0).ToList();
            var nextSpirit = handlerParty.GetHealthySpirits(activeSpirits);

            if (activeSpirits.Count == 0 && nextSpirit == null)
            {
                BattleOver(true);
            }
            else if (nextSpirit != null)
            {
                // Send out next Spirit
                unitToSwitch = faintedUnit;
                StartCoroutine(SendNextHandlerSpirit());
            }
            else if (nextSpirit == null && activeSpirits.Count > 0)
            {
                // No Spirit left to send out but we can stil continue the battle
                enemyUnits.Remove(faintedUnit);
                faintedUnit.Hud.gameObject.SetActive(false);

                // Attacks targeted at the removed unit should be changed
                var actionsToChange = actions.Where(a => a.Target == faintedUnit).ToList();
                actionsToChange.ForEach(a => a.Target = enemyUnits.First());
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
        else if (state == BattleState.MoveToForget)
        {
            Action<int> onMoveSelected = (moveIndex) =>
            {
                moveSelectionUI.gameObject.SetActive(false);
                if (moveIndex == SpiritBase.MaxNumOfMoves)
                {
                    // Don't learn the new move
                    StartCoroutine(dialogBox.TypeDialog($"{unitTryingToLearn.Spirit.Base.Name} did not learn {moveToLearn.Name}"));
                }
                else
                {
                    // Forget the selected move and learn new move
                    var selectedMove = unitTryingToLearn.Spirit.Moves[moveIndex].Base;
                    StartCoroutine(dialogBox.TypeDialog($"{unitTryingToLearn.Spirit.Base.Name} forgot {selectedMove.Name} and learned {moveToLearn.Name}"));

                    unitTryingToLearn.Spirit.Moves[moveIndex] = new Move(moveToLearn);
                }

                moveToLearn = null;
                unitTryingToLearn = null;
                state = BattleState.RunningTurn;
            };

            moveSelectionUI.HandleMoveSelection(onMoveSelected);
        }
    }

    void HandleActionSelection()
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

        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (currentAction == 0)
            {
                MoveSelection();
            }
            else if (currentAction == 1)
            {
                OpenPartyScreen();
            }
        }
        if (Input.GetKeyDown(KeyCode.N))
        {
            if (currentAction == 0)
            {
                MoveSelection();
            }
            else if (currentAction == 1)
            {
                OpenPartyScreen();
            }
        }
    }

    void HandleMoveSelection()
    {
        //needs player specific vers or to run 2 times
        if (Input.GetKeyDown(KeyCode.A))
            ++currentMove;
        else if (Input.GetKeyDown(KeyCode.D))
            --currentMove;
        else if (Input.GetKeyDown(KeyCode.S))
            currentMove += 2;
        else if (Input.GetKeyDown(KeyCode.W))
            currentMove -= 2;

        currentMove = Mathf.Clamp(currentMove, 0, currentUnit.Spirit.Moves.Count - 1);

        dialogBox.UpdateMoveSelection(currentMove, currentUnit.Spirit.Moves[currentMove]);

        if (Input.GetKeyDown(KeyCode.Z))
        {
            var move = currentUnit.Spirit.Moves[currentMove];
            if (move.PP == 0) return;

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
        else if (Input.GetKeyDown(KeyCode.X))
        {
            dialogBox.EnableMoveSelector(false);
            dialogBox.EnableDialogText(true);
            ActionSelection(actionIndex);
        }
    }

    void HandleTargetSelection()
    {
        if (Input.GetKeyDown(KeyCode.D))
            ++currentTarget;
        else if (Input.GetKeyDown(KeyCode.A))
            --currentTarget;

        currentTarget = Mathf.Clamp(currentTarget, 0, enemyUnits.Count - 1);

        for (int i = 0; i < enemyUnits.Count; i++)
        {
            enemyUnits[i].SetSelected(i == currentTarget);
        }

        if (Input.GetKeyDown(KeyCode.Z))
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
        else if (Input.GetKeyDown(KeyCode.X))
        {
            enemyUnits[currentTarget].SetSelected(false);
            MoveSelection();
        }
    }

    void HandlePartySelection()
    {
        Action onSelected = () =>
        {
            var selectedMember = partyScreen.SelectedMember;
            if (selectedMember.HP <= 0)
            {
                partyScreen.SetMessageText("You can't send out a fainted Spirit");
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
                bool isTrainerAboutToUse = partyScreen.CalledFrom == BattleState.AboutToUse;
                StartCoroutine(SwitchSpirit(unitToSwitch, selectedMember, isTrainerAboutToUse));
                unitToSwitch = null;
            }

            partyScreen.CalledFrom = null;
        };

        Action onBack = () =>
        {
            if (player1Units.Any(u => u.Spirit.HP <= 0))
            {
                partyScreen.SetMessageText("You have to choose a Spirit to continue");
                return;
            }

            partyScreen.gameObject.SetActive(false);

            if (partyScreen.CalledFrom == BattleState.AboutToUse)
            {
                StartCoroutine(SendNextHandlerSpirit());
            }
            else
                ActionSelection(actionIndex);

            partyScreen.CalledFrom = null;
        };

        partyScreen.HandleUpdate(onSelected, onBack);
    }

    IEnumerator SwitchSpirit(BattleUnit unitToSwitch, Spirit newSpirit, bool isHandlerAboutToUse = false)
    {
        if (unitToSwitch.Spirit.HP > 0)
        {
            yield return dialogBox.TypeDialog($"Come back {unitToSwitch.Spirit.Base.Name}");
            //unitToSwitch.PlayFaintAnimation();
            yield return new WaitForSeconds(2f);
        }

        unitToSwitch.Setup(newSpirit);
        dialogBox.SetMoveNames(newSpirit.Moves);
        yield return dialogBox.TypeDialog($"Go {newSpirit.Base.Name}!");

        if (isHandlerAboutToUse)
            StartCoroutine(SendNextHandlerSpirit());
        else
            state = BattleState.RunningTurn;
    }

    IEnumerator SendNextHandlerSpirit()
    {
        state = BattleState.Busy;

        var faintedUnit = enemyUnits.First(u => u.Spirit.HP == 0);

        var activeSpirits = enemyUnits.Select(u => u.Spirit).Where(p => p.HP > 0).ToList();
        var nextSpirit = handlerParty.GetHealthySpirits(activeSpirits);
        faintedUnit.Setup(nextSpirit);
        yield return dialogBox.TypeDialog($"{handler.Name} send out {nextSpirit.Base.Name}!");

        state = BattleState.RunningTurn;
    }
}