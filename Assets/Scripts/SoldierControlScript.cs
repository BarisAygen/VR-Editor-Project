using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoldierControlScript : MonoBehaviour
{
    Animator animator;
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Debug.Log("The soldier object was right-clicked!");
            animator.SetBool("isJogging", true);
            Debug.Log(animator);
            StartCoroutine(ChangeBooleanAfterDelay());
            // Add your own custom behavior here
        }

    }

    IEnumerator ChangeBooleanAfterDelay()
    {
        Debug.Log("entered soldier here");
        yield return new WaitForSeconds(10.0f);
        animator.SetBool("isJogging", false);
        Debug.Log(animator);
        Debug.Log("waited for soldier 10");
        // Add your own custom behavior here
    }
}
