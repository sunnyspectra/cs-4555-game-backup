using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandlerController : MonoBehaviour, Interactable
{
    [SerializeField] string name;
    [SerializeField] Sprite sprite;
    [SerializeField] Dialog dialog;
    [SerializeField] Dialog dialogAfterBattle;

    bool battleLost = false;

    private void Awake()
    {

    }

    private void Start()
    {

    }

    private void Update()
    {

    }

    public IEnumerator Interact(Transform initiator)
    {
        if (!battleLost)
        {
            yield return DialogManager.Instance.ShowDialog(dialog);
            GameController.Instance.StartHandlerBattle(this);
        }
        else
        {
            yield return DialogManager.Instance.ShowDialog(dialogAfterBattle);
        }
    }

    public IEnumerator TriggerHandlerBattle(PlayerController player)
    {
        yield return DialogManager.Instance.ShowDialog(dialog);
        GameController.Instance.StartHandlerBattle(this);
    }

    public void BattleLost()
    {
        battleLost = true;
    }

    public string Name {
        get => name;
    }

    public Sprite Sprite {
        get => sprite;
    }
}
