using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [SerializeField]
    private Camera cam;
    public GameObject target;
    [SerializeField]
    private float offset = 5;
    private float yOffset = 0.3f;
    private float scale = 0.2f;

    private float minOffset = 2.5f;
    private float maxOffset = 6;

    private Vector3 previousPosition;
    private Vector3 currentPosition;

    private float res = 0; // residual speed
    private float resY = 0;
    private bool mouseUp = false;
    private bool yRot = true;

    public bool canRotate;

    [SerializeField]
    private float limitSpeed = 0.15f;
    [SerializeField]
    private float brakeSpeed = 1.015f;
    [SerializeField]
    private float minSpeed = 0.75f;

    void Start()
    {
        canRotate = true;
    }

    // Update is called once per frame
    void Update()
    {
        // implement mouse scrolling - zoom out and in
        float offsetDiff = offset - Input.mouseScrollDelta.y * scale;
        if(offsetDiff > minOffset && offsetDiff < maxOffset)
        {
            offset = offsetDiff;
        }

        // make the camera point towards the car
        cam.transform.position = target.transform.GetChild(0).GetComponent<Renderer>().bounds.center;
        cam.transform.Translate(new Vector3(0, -yOffset, -offset));

        // check if the camera can rotate
        if (canRotate)
        {
            if (Input.GetMouseButtonDown(0))
            {
                // user presses the button -> compute the first position
                previousPosition = cam.ScreenToViewportPoint(Input.mousePosition);
                //transform.position = target.transform.position - new Vector3(0f, -offset, offset);
                res = 0;
            }

            else if (Input.GetMouseButton(0))
            {
                // compute the current position of the mouse
                Vector3 currentPosition = cam.ScreenToViewportPoint(Input.mousePosition);
                Vector3 direction = previousPosition - currentPosition; // direction of the rotation

                float rotationAroundYAxis = -direction.x * 180; // camera moves horizontally
                float rotationAroundXAxis = direction.y * 180; // camera moves vertically

                // update residual speed
                if (Mathf.Abs(rotationAroundYAxis) > 0)
                {
                    res = rotationAroundYAxis;
                }
                if (Mathf.Abs(rotationAroundXAxis) > 0)
                {
                    resY = rotationAroundXAxis;
                }

                cam.transform.position = target.transform.GetChild(0).GetComponent<Renderer>().bounds.center;

                // perform the rotation around the Y axis
                cam.transform.Rotate(new Vector3(0, 1, 0), rotationAroundYAxis, Space.World);

                // instantiate bounds
                if ((cam.transform.eulerAngles.x <= 5 && cam.transform.eulerAngles.x < 90 && resY < 0) || (cam.transform.eulerAngles.x >= 80 && resY > 0))
                {
                    yRot = false;
                }
                else
                {
                    yRot = true;
                }

                // perform the rotation around the X axis
                if (yRot) cam.transform.Rotate(new Vector3(1, 0, 0), rotationAroundXAxis);

                // keep the camera at a certain distance
                cam.transform.Translate(new Vector3(0, -yOffset, -offset));

                // update previous position
                previousPosition = currentPosition;
            }

            // as soon as the user releases the button, if the speed of the rotation is above a certain threshold,
            // let the camera continue its rotation
            else if (Input.GetMouseButtonUp(0) && Mathf.Abs(res) > minSpeed)
            {
                mouseUp = true;
            }
            // check if the residual speed is below a certain threshold, in that case stop further rotations
            if (Mathf.Abs(res) < limitSpeed)
            {
                mouseUp = false;
            }
            
            if (mouseUp) //residual rotation
            {
                cam.transform.Rotate(new Vector3(0, 1, 0), res, Space.World);
                res = res / brakeSpeed; // divide in half the rotation (eventually will reach 0)
            }
        }

    }
}
