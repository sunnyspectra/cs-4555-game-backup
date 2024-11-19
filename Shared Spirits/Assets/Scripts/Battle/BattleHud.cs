using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleHud : MonoBehaviour
{
    [SerializeField] Text nameText;
    [SerializeField] Text levelText;
    [SerializeField] Text statusText;
    [SerializeField] HPBar hpBar;
    [SerializeField] GameObject expBar;

    [SerializeField] Color psnColor;
    [SerializeField] Color brnColor;
    [SerializeField] Color slpColor;
    [SerializeField] Color parColor;
    [SerializeField] Color frzColor;

    Spirit _spirit;
    Dictionary<ConditionID, Color> statusColors;

    public void SetData(Spirit spirit)
    {
        if (_spirit != null)
        {
            _spirit.OnHPChanged -= UpdateHP;
            _spirit.OnStatusChanged -= SetStatusText;
        }

        _spirit = spirit;

        nameText.text = spirit.Base.Name;
        SetLevel();
        hpBar.SetHP((float) spirit.HP / spirit.MaxHp);
        SetExp();

        statusColors = new Dictionary<ConditionID, Color>()
        {
            {ConditionID.psn, psnColor },
            {ConditionID.brn, brnColor },
            {ConditionID.slp, slpColor },
            {ConditionID.par, parColor },
            {ConditionID.frz, frzColor },
        };

        SetStatusText();
        _spirit.OnStatusChanged += SetStatusText;
        _spirit.OnHPChanged += UpdateHP;
    }

    void SetStatusText()
    {
        if (_spirit.Status == null)
        {
            statusText.text = "";
        }
        else
        {
            statusText.text = _spirit.Status.Id.ToString().ToUpper();
            statusText.color = statusColors[_spirit.Status.Id];
        }
    }

    public void SetLevel()
    {
        levelText.text = "Lvl " + _spirit.Level;
    }

    public void SetExp()
    {
        if (expBar == null) return;

        float normalizedExp = GetNormalizedExp();
        expBar.transform.localScale = new Vector3(normalizedExp, 1, 1);
    }

    public IEnumerator SetExpSmooth(bool reset = false)
    {
        if (expBar == null) yield break;

        if (reset)
            expBar.transform.localScale = new Vector3(0, 1, 1);

        float normalizedExp = GetNormalizedExp();
        //yield return expBar.transform.DOScaleX(normalizedExp, 1.5f).WaitForCompletion();
    }

    float GetNormalizedExp()
    {
        int currLevelExp = _spirit.Base.GetExpForLevel(_spirit.Level);
        int nextLevelExp = _spirit.Base.GetExpForLevel(_spirit.Level + 1);

        float normalizedExp = (float)(_spirit.Exp - currLevelExp) / (nextLevelExp - currLevelExp);
        return Mathf.Clamp01(normalizedExp);
    }

    public void UpdateHP()
    {
        StartCoroutine(UpdateHPAsync());
    }

    public IEnumerator UpdateHPAsync()
    {
        yield return hpBar.SetHPSmooth((float)_spirit.HP / _spirit.MaxHp);
    }

    public IEnumerator WaitForHPUpdate()
    {
        yield return new WaitUntil(() => hpBar.IsUpdating == false);
    }

    public void ClearData()
    {
        if (_spirit != null)
        {
            _spirit.OnHPChanged -= UpdateHP;
            _spirit.OnStatusChanged -= SetStatusText;
        }
    }
}
