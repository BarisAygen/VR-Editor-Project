using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PeasantGirlController : MonoBehaviour
{
    Animator animator;
    public GameObject peasantPanel;
    public Button waveButton; // dancebutton
    public Button singButton; // guitarbutton
    private float checkValue;

    
    //then drag and drop the Username_field

    // Start is called before the first frame update
    void Start()
    {
        checkValue = -1000;
        animator = gameObject.GetComponent<Animator>();


    }

    public void SetButtonOnClick()
    {
        waveButton.onClick.AddListener(delegate () { WaveControl(); });
        singButton.onClick.AddListener(delegate () { SingControl(); });
    }





    private void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(1)) // Check if the right mouse button is pressed
        {
            checkValue = gameObject.transform.position.x;
            Debug.Log(gameObject.transform.position.x);
            peasantPanel.SetActive(true); // Show the panel



        }
    }

    public void WaveControl()
    {
        if (checkValue == gameObject.transform.position.x)
        {
            animator.SetBool("isWaving", true);
            
            StartCoroutine(ChangeBooleanAfterDelay());
        }

    }

    public void SingControl()
    {
        if (checkValue == gameObject.transform.position.x)
        {
            Debug.Log(gameObject.transform.position.x);
            Debug.Log(checkValue);
            animator.SetBool("isSinging", true);
            StartCoroutine(ChangeBooleanAfterDelay());
        }

    }


    IEnumerator ChangeBooleanAfterDelay()
    {
        if (checkValue == gameObject.transform.position.x)
        {
            var input = peasantPanel.transform.GetChild(0).GetComponent<TMP_InputField>().text;
            var int_input = float.Parse(input) ;
            Debug.Log("entered here");
            peasantPanel.SetActive(false);
            if (int_input > 5)
            {
                int_input -= 3;
            }
            yield return new WaitForSeconds(int_input);
            animator.SetBool("isSinging", false);
            animator.SetBool("isWaving", false);
            checkValue = -1000;
            Debug.Log("waited for 10");
        }

        // Add your own custom behavior here
    }

}