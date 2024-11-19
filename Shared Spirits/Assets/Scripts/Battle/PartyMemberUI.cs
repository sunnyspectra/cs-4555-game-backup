using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PartyMemberUI : MonoBehaviour
{
    [SerializeField] Text nameText;
    [SerializeField] Text levelText;
    [SerializeField] HPBar hpBar;
    [SerializeField] Text messageText;

    Spirit _spirit;

    public void Init(Spirit spirit)
    {
        _spirit = spirit;
        UpdateData();
        SetMessage("");

        _spirit.OnHPChanged += UpdateData;
    }

    void UpdateData()
    {
        nameText.text = _spirit.Base.Name;
        levelText.text = "Lvl " + _spirit.Level;
        hpBar.SetHP((float)_spirit.HP / _spirit.MaxHp);
    }

    public void SetSelected(bool selected)
    {
        /*if (selected)
            nameText.color = GlobalSettings.i.HighlightedColor;
        else
            nameText.color = Color.black;*/
    }

    public void SetMessage(string message)
    {
        messageText.text = message;
    }
}
