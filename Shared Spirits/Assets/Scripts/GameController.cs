using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState { FreeRoam, Battle, Dialog, Menu, PartyScreen, Bag, Cutscene, Paused }

public class GameController : MonoBehaviour
{
    [SerializeField] PlayerController playerController;
    [SerializeField] PlayerController2 playerController2;
    [SerializeField] Camera worldCamera;
    [SerializeField] BattleSystem battleSystem;
    [SerializeField] PartyScreen partyScreen;
    [SerializeField] InventoryUI inventoryUI;

    GameState state;
    GameState prevState;
    GameState stateBeforeEvolution;

    public SceneDetails CurrentScene { get; private set; }
    public SceneDetails PrevScene { get; private set; }

    MenuController menuController;

    public static GameController Instance { get; private set; }
    private void Awake()
    {
        Instance = this;

        menuController = GetComponent<MenuController>();

        SpiritDB.Init();
        MoveDB.Init();
        ConditionsDB.Init();
        ItemDB.Init();
    }

    private void Start()
    {
        battleSystem.OnBattleOver += EndBattle;

        partyScreen.Init();

        DialogManager.Instance.OnShowDialog += () =>
        {
            prevState = state;
            state = GameState.Dialog;
        };

        DialogManager.Instance.OnDialogFinished += () =>
        {
            if (state == GameState.Dialog)
                state = prevState;
        };

        menuController.onBack += () =>
        {
            state = GameState.FreeRoam;
        };

        menuController.onMenuSelected += OnMenuSelected;
    }

    public void PauseGame(bool pause)
    {
        if (pause)
        {
            prevState = state;
            state = GameState.Paused;
        }
        else
        {
            state = prevState;
        }
    }

    
    public void StartBattle()
    {
        state = GameState.Battle;
        battleSystem.gameObject.SetActive(true);
        //Player.MainCamera.gameObject.SetActive(false);

        //var playerParty = playerController.GetComponent<SpiritParty>();
        //var wildSpirit = CurrentScene.GetComponent<MapArea>().GetRandomWildSpirit();

        //var wildSpiritCopy = new Spirit(wildSpirit.Base, wildSpirit.Level);

        //battleSystem.StartBattle(playerParty, wildSpiritCopy);
    }

    HandlerController handler;
    public void StartHandlerBattle(HandlerController handler)
    {

        Debug.Log($"Interaction runs here");
        state = GameState.Battle;
        battleSystem.gameObject.SetActive(true);
        worldCamera.gameObject.SetActive(false);

        this.handler = handler;
        var playerParty = playerController.GetComponent<SpiritParty>();
        var playerParty2 = playerController2.GetComponent<SpiritParty>();
        var handlerParty = handler.GetComponent<SpiritParty>();

        battleSystem.StartHandlerBattle(playerParty, playerParty2, handlerParty);
    }

    public void OnEnterHandlersView(HandlerController handler)
    {
        state = GameState.Cutscene;
        StartCoroutine(handler.TriggerHandlerBattle(playerController));
    }

    private void Update()
    {
        if (state == GameState.FreeRoam)
        {
            playerController.HandleUpdate();
            playerController2.HandleUpdate();

            if (Input.GetKeyDown(KeyCode.Return))
            {
                menuController.OpenMenu();
                state = GameState.Menu;
            }
        }
        else if (state == GameState.Battle)
        {
            battleSystem.HandleUpdate();
        }
        else if (state == GameState.Dialog)
        {
            DialogManager.Instance.HandleUpdate();
        }
        else if (state == GameState.Menu)
        {
            menuController.HandleUpdate();
        }
        else if (state == GameState.PartyScreen)
        {
            Action onSelected = () =>
            {
                // TODO: Go to Summary Screen
            };

            Action onBack = () =>
            {
                partyScreen.gameObject.SetActive(false);
                state = GameState.FreeRoam;
            };

            partyScreen.HandleUpdate(onSelected, onBack);
        }
        else if (state == GameState.Bag)
        {
            Action onBack = () =>
            {
                inventoryUI.gameObject.SetActive(false);
                state = GameState.FreeRoam;
            };

            inventoryUI.HandleUpdate(onBack);
        }
    }

    public void SetCurrentScene(SceneDetails currScene)
    {
        PrevScene = CurrentScene;
        CurrentScene = currScene;
    }

    void OnMenuSelected(int selectedItem)
    {
        if (selectedItem == 0)
        {
            // Pokemon
            partyScreen.gameObject.SetActive(true);
            state = GameState.PartyScreen;
        }
        else if (selectedItem == 1)
        {
            // Bag
            inventoryUI.gameObject.SetActive(true);
            state = GameState.Bag;
        }
    }

    void EndBattle(bool won)
    {
        if (handler != null && won == true)
        {
            handler.BattleLost();
            handler = null;
        }

        partyScreen.SetPartyData();

        state = GameState.FreeRoam;
        battleSystem.gameObject.SetActive(false);
        //worldCamera.gameObject.SetActive(true);
    }

    public GameState State => state;
}