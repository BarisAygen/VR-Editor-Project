using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class ChangeColor : MonoBehaviour, IPointerEvents
{

    public void Blue()
    {
        GetComponent<Renderer>().material.color = Color.blue;
    }

    public void Red()
    {
        GetComponent<Renderer>().material.color = Color.red;
    }

    public void White()
    {
        GetComponent<Renderer>().material.color = Color.white;
    }

    public void OnPointerEnter()
    {
        Blue();
    }

    public void OnPointerExit()
    {
        White();
    }

    public void OnPointerClick()
    {
        Red();
    }
}
