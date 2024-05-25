using UnityEngine;

public class CustomCameraController : MonoBehaviour
{
    // The speed at which the camera rotates
    public float rotationSpeed = 5.0f;

    // The speed at which the camera zooms
    public float zoomSpeed = 5.0f;

    // The minimum and maximum angles for the camera rotation
    public float minAngle = -90.0f;
    public float maxAngle = 90.0f;

    public float flySpeed = 5.0f;
    public float keyboardRotationSpeed = 5.0f;


    // Update is called once per frame
    void Update()
    {
        if (MakeScriptable.isInputFieldFocused == false)
        {
            // Rotate the camera using the right button
            if (Input.GetMouseButton(1))
            {
                float rotationX = Input.GetAxis("Mouse X") * rotationSpeed;
                float rotationY = Input.GetAxis("Mouse Y") * rotationSpeed;
                transform.localEulerAngles += new Vector3(-rotationY, rotationX, 0);

                // Clamp the camera rotation
                float x = transform.localEulerAngles.x;
                if (x > minAngle && x < maxAngle)
                {
                    transform.localEulerAngles = new Vector3(x, transform.localEulerAngles.y, 0);
                }

            }
            if (Input.GetKey(KeyCode.E))
            {
                float rotationY = keyboardRotationSpeed * Time.deltaTime;
                transform.localEulerAngles += new Vector3(0, rotationY, 0);
            }
            if (Input.GetKey(KeyCode.Q))
            {
                float rotationY = keyboardRotationSpeed * Time.deltaTime;
                transform.localEulerAngles += new Vector3(0, -rotationY, 0);
            }
            if (Input.GetKey(KeyCode.F))
            {
                float rotationX = keyboardRotationSpeed * Time.deltaTime;
                transform.localEulerAngles += new Vector3(rotationX, 0, 0);
            }
            if (Input.GetKey(KeyCode.R))
            {
                float rotationX = keyboardRotationSpeed * Time.deltaTime;
                transform.localEulerAngles += new Vector3(-rotationX, 0, 0);
            }
            if (Input.GetKey(KeyCode.Space))
            {
                float moveY = flySpeed * Time.deltaTime;
                transform.position += new Vector3(0, moveY, 0);
            }
            if (Input.GetKey(KeyCode.LeftShift))
            {
                float moveY = flySpeed * Time.deltaTime;
                transform.position += new Vector3(0, -moveY, 0);
            }
            if (Input.GetKey(KeyCode.W))
            {
                Vector3 moveZ = flySpeed * Time.deltaTime * transform.forward;
                transform.position += moveZ;
            }
            if (Input.GetKey(KeyCode.S))
            {
                Vector3 moveZ = -flySpeed * Time.deltaTime * transform.forward;
                transform.position += moveZ;
            }
            if (Input.GetKey(KeyCode.D))
            {
                Vector3 moveX = flySpeed * Time.deltaTime * transform.right;
                transform.position += moveX;
            }
            if (Input.GetKey(KeyCode.A))
            {
                Vector3 moveX = -flySpeed * Time.deltaTime * transform.right;
                transform.position += moveX;
            }

            // Zoom the camera using the mouse scroll wheel
            float zoom = Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
            transform.localPosition += transform.forward * zoom;
        }
    }
}

