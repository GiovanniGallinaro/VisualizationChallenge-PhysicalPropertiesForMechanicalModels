using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Slider : MonoBehaviour
{

    private bool canRotate;

    void Start() {
        canRotate = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0) && isMouseOverUI())
        {
            canRotate = false;       // start of holding down - NO rotation!
        }
        else if (Input.GetMouseButtonUp(0))
        {
            canRotate = true;      // stop of mouse holding down - OK rotation
        }

        // update the boolean variable of the camera
        if (Input.GetMouseButton(0) && canRotate)
        {
            GameObject.Find("Main Camera").GetComponent<CameraManager>().canRotate = true;
        }
        else if(!canRotate)
        {
            GameObject.Find("Main Camera").GetComponent<CameraManager>().canRotate = false;
        }
    }

    // check if mouse is over the slider
    private bool isMouseOverUI()
    {
        return EventSystem.current.IsPointerOverGameObject();
    }
    
}
