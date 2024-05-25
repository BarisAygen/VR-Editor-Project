using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class DanceAnimationController : MonoBehaviour
{
    Animator animator;
    public GameObject dancerPanel;
    public Button danceButton;
    public Button guitarButton;
    private float checkValue ;
    // Start is called before the first frame update
    void Start()
    {
        checkValue = -100;
        animator = gameObject.GetComponent<Animator>();


    }

    public void SetButtonOnClick()
    {
        danceButton.onClick.AddListener(delegate () { DanceControl(); });
        guitarButton.onClick.AddListener(delegate () { GuitarControl(); });
    }

    

    
    
    private void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(1)) // Check if the right mouse button is pressed
        {
            checkValue = gameObject.transform.position.x;
            Debug.Log(gameObject.transform.position.x);
            
            dancerPanel.SetActive(true); // Show the panel
            
           

        }
    }

    public void DanceControl()
    {
        if(checkValue == gameObject.transform.position.x) {
            animator.SetBool("isDancing", true);
            StartCoroutine(ChangeBooleanAfterDelay());
        }
        
    }
    
    public void GuitarControl()
    {
        if (checkValue == gameObject.transform.position.x) {
            Debug.Log(gameObject.transform.position.x);
            Debug.Log(checkValue);
            animator.SetBool("isGuitar", true);
            StartCoroutine(ChangeBooleanAfterDelay());
        }
            
    }


    IEnumerator ChangeBooleanAfterDelay()
    {
        if (checkValue == gameObject.transform.position.x) {
            var input = dancerPanel.transform.GetChild(0).GetComponent<TMP_InputField>().text;
            var int_input = float.Parse(input);
            Debug.Log("entered here");
            Debug.Log("entered here");
            dancerPanel.SetActive(false);
            yield return new WaitForSeconds(int_input);
            animator.SetBool("isDancing", false);
            animator.SetBool("isGuitar", false);
            checkValue = -100;
            Debug.Log("waited for 10");
        }
            
        // Add your own custom behavior here
    }

}