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
        var playerSpirits2 = playerParty2.GetHealthySpirit(1);

        names = String.Join(" and ", playerSpirits.Select(p => p.Base.Name));
        yield return dialogBox.TypeDialog($"Go {names}!");
        names = String.Join(" and ", playerSpirits2.Select(p => p.Base.Name));
        yield return dialogBox.TypeDialog($"Go {names}!");

        partyScreen.Init();
        actions = new List<BattleAction>();
        ActionSelection(0);
    }

    void ActionSelection(int index)
    {
        state = BattleState.ActionSelection;

        if (index == 0)
        {
            currentUnit = player1Units[0];
            dialogBox.SetMoveNames(currentUnit.Spirit.Moves);
            dialogBox.SetDialog($"Choose an action for {currentUnit.Spirit.Base.Name}");
            dialogBox.EnableActionSelector(true);
        }
        else if (index == 1)
        {
            currentUnit = player2Units[0];
            dialogBox.SetMoveNames(currentUnit.Spirit.Moves);
            dialogBox.SetDialog($"Choose an action for {currentUnit.Spirit.Base.Name}");
            dialogBox.EnableActionSelector(true);
        }
    }

    void AddBattleAction(BattleAction action)
    {
        actions.Add(action);
        Debug.Log($"Action added for {action.User.Spirit.Base.Name}, Total Actions: {actions.Count}");

        if (actionIndex == 0)
        {
            actionIndex = 1;
        }
        else if (actionIndex == 1)
        {
            actionIndex = 2;
        }

        Debug.Log($"Next actionIndex: {actionIndex}");

        
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
        if (actionIndex == 0)
        {
            HandlePlayer1ActionInput();
        }
        else if (actionIndex == 1)
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
            else if (currentAction == 1) OpenPartyScreen();
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
            else if (currentAction == 1) OpenPartyScreen();
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
        /*else if (state == BattleState.MoveToForget)
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
        }*/
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
            // Skip the turn for dead units (HP <= 0)
            if (action.User.Spirit.HP <= 0)
            {
                Debug.Log($"Skipping {action.User.name}'s turn because they are dead.");
                continue; // Skip dead units' actions
            }

            if (action.IsInvalid)
                continue;

            // Handle the action for the living unit
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
            Debug.Log("Enemy's turn begins.");
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
        yield return dialogBox.TypeDialog($"{faintedUnit.Spirit.Base.Name} Fainted");
        yield return new WaitForSeconds(2f);
        //yield return HandleExpGain(faintedUnit);
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

    void NextStepsAfterFainting(BattleUnit faintedUnit)
    {
        var actionToRemove = actions.FirstOrDefault(a => a.User == faintedUnit);
        if (actionToRemove != null)
            actionToRemove.IsInvalid = true;

        if (faintedUnit.IsPlayerUnit)
        {
            // Get all active spirits (those that are alive)
            var activeSpirits = player1Units.Select(u => u.Spirit).Where(p => p.HP > 0).ToList();

            if (activeSpirits.Count == 0)
            {
                BattleOver(false); // Player loses if no active spirits
                return;
            }

            // Get the next healthy spirit from the party
            var nextSpirit = playerParty.GetHealthySpirits(activeSpirits);
            if (nextSpirit != null)
            {
                unitToSwitch = faintedUnit;
                OpenPartyScreen(); // Show party screen for switching
            }
            else
            {
                player1Units.Remove(faintedUnit); // Remove fainted unit from active list
                faintedUnit.Hud.gameObject.SetActive(false); // Hide its HUD
            }
        }
        else
        {
            // Handle for enemy
            if (enemyUnits.Count == 0)
            {
                BattleOver(true); // Enemy loses if no active enemies
                return;
            }

            var activeSpirits = enemyUnits.Select(u => u.Spirit).Where(p => p.HP > 0).ToList();
            var nextSpirit = handlerParty.GetHealthySpirits(activeSpirits);

            if (nextSpirit != null)
            {
                unitToSwitch = faintedUnit;
                StartCoroutine(SendNextHandlerSpirit()); // Send the next healthy enemy spirit
            }
            else
            {
                enemyUnits.Remove(faintedUnit);
                faintedUnit.Hud.gameObject.SetActive(false);
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
            yield return dialogBox.TypeDialog($"Come back {unitToSwitch.Spirit.Base.Name}");
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


    /*IEnumerator SendNextHandlerSpirit()
    {
        state = BattleState.Busy;
        var faintedUnit = enemyUnits.First(u => u.Spirit.HP == 0);
        var activeSpirits = enemyUnits.Select(u => u.Spirit).Where(p => p.HP > 0).ToList();
        var nextSpirit = handlerParty.GetHealthySpirits(activeSpirits);
        faintedUnit.Setup(nextSpirit);
        yield return dialogBox.TypeDialog($"{handler.Name} send out {nextSpirit.Base.Name}!");

        state = BattleState.RunningTurn;
    }*/

    IEnumerator SendNextHandlerSpirit()
    {
        state = BattleState.Busy;

        // Find the fainted unit (if any)
        var faintedUnit = enemyUnits.FirstOrDefault(u => u.Spirit.HP == 0);

        if (faintedUnit != null)
        {
            // Get the list of active spirits (those with HP > 0)
            var activeSpirits = enemyUnits.Select(u => u.Spirit).Where(p => p.HP > 0).ToList();

            // Get the next healthy spirit from the handler's party
            var nextSpirit = handlerParty.GetHealthySpirits(activeSpirits);

            // Set up the fainted unit with the next healthy spirit
            faintedUnit.Setup(nextSpirit);
            yield return dialogBox.TypeDialog($"{handler.Name} sends out {nextSpirit.Base.Name}!");
        }

        state = BattleState.RunningTurn;
    }


    /*IEnumerator ChooseMoveToForget(Spirit Spirit, MoveBase newMove)
    {
        state = BattleState.Busy;
        yield return dialogBox.TypeDialog($"Choose a move you wan't to forget");
        moveSelectionUI.gameObject.SetActive(true);
        moveSelectionUI.SetMoveData(Spirit.Moves.Select(x => x.Base).ToList(), newMove);
        moveToLearn = newMove;

        state = BattleState.MoveToForget;
    }*/

    /*IEnumerator HandleExpGain(BattleUnit faintedUnit)
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
    }*/

}