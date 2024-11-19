using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleUnit : MonoBehaviour
{
    [SerializeField] bool isPlayerUnit;
    [SerializeField] BattleHud hud;

    public bool IsPlayerUnit {
        get { return isPlayerUnit; }
    }

    public BattleHud Hud {
        get { return hud;  }
    }

    public Spirit Spirit { get; set; }

    Image image;
    Vector3 orginalPos;
    Color originalColor;
    private void Awake()
    {
        image = GetComponent<Image>();
        orginalPos = image.transform.localPosition;
        originalColor = image.color;
    }

    public void Setup(Spirit spirit)
    {
        Spirit = spirit;

        image.sprite = Spirit.Base.Sprite;

        hud.gameObject.SetActive(true);
        hud.SetData(spirit);

        transform.localScale = new Vector3(1, 1, 1);
        image.color = originalColor;
    }

    public void Clear()
    {
        hud.gameObject.SetActive(false);
    }

    public void SetSelected(bool selected)
    {
        //image.color = (selected) ? GlobalSettings.i.HighlightedColor : originalColor;
    }   
}
