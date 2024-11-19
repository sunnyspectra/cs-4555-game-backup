using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandlerController : MonoBehaviour, Interactable
{
    [SerializeField] string name;
    [SerializeField] Sprite sprite;
    [SerializeField] Dialog dialog;
    [SerializeField] Dialog dialogAfterBattle;
    [SerializeField] GameObject fov;

    bool battleLost = false;

    //Character character;
    private void Awake()
    {
        //character = GetComponent<Character>();
    }

    private void Start()
    {
        //SetFovRotation(character.Animator.DefaultDirection);
    }

    private void Update()
    {
        //character.HandleUpdate();
    }

    public IEnumerator Interact(Transform initiator)
    {
        Debug.Log($"Interaction1");
        if (!battleLost)
        {
            yield return DialogManager.Instance.ShowDialog(dialog);

            Debug.Log($"Interaction2");
            GameController.Instance.StartHandlerBattle(this);

            Debug.Log($"Interaction finished");
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

    /*public void SetFovRotation(FacingDirection dir)
    {
        float angle = 0f;
        if (dir == FacingDirection.Right)
            angle = 90f;
        else if (dir == FacingDirection.Up)
            angle = 180f;
        else if (dir == FacingDirection.Left)
            angle = 270;

        fov.transform.eulerAngles = new Vector3(0f, 0f, angle);
    }*/

    public void BattleLost()
    {
        battleLost = true;
        //fov.gameObject.SetActive(false);
    }

    public string Name {
        get => name;
    }

    public Sprite Sprite {
        get => sprite;
    }
}
