using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PartyScreen : MonoBehaviour
{
    [SerializeField] Text messageText;

    PartyMemberUI[] memberSlots;
    List<Spirit> spirits;
    SpiritParty party;

    int selection = 0;
    KeyCode upKey = KeyCode.W;
    KeyCode downKey = KeyCode.S;
    KeyCode acceptKey = KeyCode.Z;
    KeyCode declineKey = KeyCode.X;

    public Spirit SelectedMember => spirits[selection];

    /// <summary>
    /// Party screen can be called from different states like ActionSelection, RunningTurn, AboutToUse
    /// </summary>
    public BattleState? CalledFrom { get; set; }

    // Initialize the party screen with the correct player's party
    public void Init(SpiritParty playerParty, KeyCode up, KeyCode down, KeyCode accept, KeyCode decline)
    {
        upKey = up;
        downKey = down;
        acceptKey = accept;
        declineKey = decline;

        memberSlots = GetComponentsInChildren<PartyMemberUI>(true);
        party = playerParty;
        SetPartyData();

        party.OnUpdated += SetPartyData;
    }

    public void SetPartyData()
    {
        spirits = party.Spirits;

        // Update the UI for the party members
        for (int i = 0; i < memberSlots.Length; i++)
        {
            if (i < spirits.Count)
            {
                memberSlots[i].gameObject.SetActive(true);
                memberSlots[i].Init(spirits[i]);
            }
            else
                memberSlots[i].gameObject.SetActive(false);
        }

        UpdateMemberSelection(selection);

        messageText.text = "Choose a Spirit";
    }

    public void HandleUpdate(Action onSelected, Action onBack)
    {
        var prevSelection = selection;

        // Navigate the selection
        if (Input.GetKeyDown(downKey))
            ++selection;
        else if (Input.GetKeyDown(upKey))
            --selection;

        // Clamp to prevent out-of-bounds selection
        selection = Mathf.Clamp(selection, 0, spirits.Count - 1);

        if (selection != prevSelection)
            UpdateMemberSelection(selection);

        // Select a spirit
        if (Input.GetKeyDown(acceptKey))
        {
            if (SelectedMember.HP <= 0)
            {
                SetMessageText("You can't send out a fainted Spirit");
                return;
            }
            onSelected?.Invoke();
        }
        // Go back
        else if (Input.GetKeyDown(declineKey)) // Shared cancel key
        {
            onBack?.Invoke();
        }
    }

    public void UpdateMemberSelection(int selectedMember)
    {
        for (int i = 0; i < spirits.Count; i++)
        {
            if (i == selectedMember)
                memberSlots[i].SetSelected(true);
            else
                memberSlots[i].SetSelected(false);
        }
    }

    public void ClearMemberSlotMessages()
    {
        for (int i = 0; i < spirits.Count; i++)
        {
            memberSlots[i].SetMessage("");
        }
    }

    public void SetMessageText(string message)
    {
        messageText.text = message;
    }
}
