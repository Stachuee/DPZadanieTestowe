using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovment : MonoBehaviour
{
    [SerializeField] float normalSpeed;
    [SerializeField] float fasterSpeed;


    //[SerializeField] float minFov = 15f;
    //[SerializeField] float maxFov = 90f;
    //[SerializeField] float sensitivity = 10f;

    void Update()
    {
        float verticalSpeed = Input.GetAxis("Vertical");
        float horizontalSpeed = Input.GetAxis("Horizontal");

        bool faster = Input.GetKey(KeyCode.LeftShift); // change later for new input system
        transform.position += transform.forward * verticalSpeed * (faster ? fasterSpeed : normalSpeed) * Time.deltaTime;
        transform.position += transform.right * horizontalSpeed * normalSpeed * Time.deltaTime;

    }
}
