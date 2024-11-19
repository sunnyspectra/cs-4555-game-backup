using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Items/Create new move")]
public class LearnableMoveItem : ItemBase
{
    [SerializeField] MoveBase move;

    public override string Name => base.Name + $": {move.Name}";

    public override bool Use(Spirit spirit)
    {
        return spirit.HasMove(move);
    }

    public bool CanBeTaught(Spirit spirit)
    {
        return spirit.Base.LearnableByItems.Contains(move);
    }


    public MoveBase Move => move;
}
