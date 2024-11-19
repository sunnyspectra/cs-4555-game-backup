using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ActionType { Move, SwitchSpirit, UseItem, Run }

public class BattleAction
{
    public ActionType Type { get; set; }
    public BattleUnit User { get; set; }
    public BattleUnit Target { get; set; }

    public Move Move { get; set; }  // For Performing Moves
    public Spirit SelectedSpirit { get; set; }    // For Switching

    public bool IsInvalid { get; set; }

    public int Priority => (Type == ActionType.Move) ? Move.Base.Priority : 99;
}
