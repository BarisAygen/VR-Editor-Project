using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ForBallBounce : MonoBehaviour
{
    public GameObject terrain;
    public GameObject ballPanel;
    public Button jumpButton;
    private float checkValue;
    public int maxBounces = 5;
    private int bounceCount = 0;
    public float jumpForce = 5.0f;
    private Rigidbody rb;

    void Start()
    {
        checkValue = -100;
        gameObject.transform.position += new Vector3(0f, 1f, 0f);

    }

    public void SetButtonOnClick()
    {
        jumpButton.onClick.AddListener(delegate () { JumpControl(); });
    }

    private void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(1)) // Check if the right mouse button is pressed
        {
            checkValue = gameObject.transform.position.x;
            ballPanel.SetActive(true); // Show the panel
        }
    }

    public void JumpControl()
    {
        if (checkValue == gameObject.transform.position.x )
        {
            bounceCount = 0;
            ballPanel.SetActive(false);
            Bounce();
            bounceCount++;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check if the collision occurred with the terrain
        if (collision.gameObject == terrain.gameObject && bounceCount < maxBounces)
        {
            Bounce();
            bounceCount++;
        }
    }

    void Bounce()
    {
        // Add an upward force to the ball to make it bounce
        GetComponent<Rigidbody>().AddForce(Vector3.up * 12f, ForceMode.Impulse);
        
    }
}



