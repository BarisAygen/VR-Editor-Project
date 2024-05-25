using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class BossAnimationController : MonoBehaviour
{
    Animator animator;
    public GameObject bossPanel;
    public Button stretchButton; // dancebutton
    public Button talkButton; // guitarbutton
    private float checkValue;
    // Start is called before the first frame update
    void Start()
    {
        checkValue = -1000;
        animator = gameObject.GetComponent<Animator>();


    }

    public void SetButtonOnClick()
    {
        stretchButton.onClick.AddListener(delegate () { StretchControl(); });
        talkButton.onClick.AddListener(delegate () { TalkControl(); });
    }





    private void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(1)) // Check if the right mouse button is pressed
        {
            checkValue = gameObject.transform.position.x;
            Debug.Log(gameObject.transform.position.x);
            bossPanel.SetActive(true); // Show the panel


        }
    }

    public void StretchControl()
    {
        if (checkValue == gameObject.transform.position.x)
        {
            
            animator.SetBool("isStretching", true);
            Debug.Log("stretching");
            StartCoroutine(ChangeBooleanAfterDelay());
        }

    }

    public void TalkControl()
    {
        if (checkValue == gameObject.transform.position.x)
        {
            Debug.Log(gameObject.transform.position.x);
            Debug.Log(checkValue);
            animator.SetBool("isTalking", true);
            StartCoroutine(ChangeBooleanAfterDelay());
        }

    }


    IEnumerator ChangeBooleanAfterDelay()
    {
        if (checkValue == gameObject.transform.position.x)
        {
            var input = bossPanel.transform.GetChild(0).GetComponent<TMP_InputField>().text;
            var int_input = float.Parse(input);
            Debug.Log("entered here");
            bossPanel.SetActive(false);
            if(int_input  > 5) {
                int_input -= 3;
            }
            yield return new WaitForSeconds(int_input);
            animator.SetBool("isTalking", false);
            animator.SetBool("isStretching", false);
            checkValue = -1000;
            Debug.Log("waited for 10");
        }

        // Add your own custom behavior here
    }

}