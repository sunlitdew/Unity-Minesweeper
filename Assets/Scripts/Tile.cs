using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Tile : MonoBehaviour, IPointerClickHandler
{
    [Header("Coordinates")]
    public int x;
    public int y;

    [Header("Logic")]
    public int logicValue; //0-8 are numbers (0 is empty tile), 9 is a mine
    public bool IsMine => logicValue == 9;
    public bool IsEmpty => logicValue == 0;
    
    public bool isRevealed = false;
    public bool isFlagged = false;
    
    
    

    [Header("Visual References")]
    public Image content;
    public Image cover;
    public Image flag;
    public Image background;
    
    [Header("Sprites")]
    public Sprite[] numberSprites;

    public Sprite mine;
    
    public void Init(int x, int y)
    {
        this.x = x;
        this.y = y;
        logicValue = 0;
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        switch (eventData.button)
        {
            case PointerEventData.InputButton.Left:
                Debug.Log("Left click at " + x + "," + y);
                GameManager.Instance.Reveal(x,y);
                break;
            case PointerEventData.InputButton.Right:
                Debug.Log("Right Click at " + x + "," + y);
                GameManager.Instance.ToggleFlag(x,y);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void Reveal()
    {
        isRevealed = true;
        cover.enabled = false;
        flag.enabled = false;
    }

    public void ToggleFlag()
    {
        isFlagged = !isFlagged;
        flag.enabled = !flag.enabled;
    }

    public void UpdateContent()
    {
        if (IsEmpty)
        {
            content.enabled = false;
        }
        else
        {
            content.enabled = true;
            content.sprite = IsMine ? mine : numberSprites[logicValue - 1];
        }
    }
}
