using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class HelixController : MonoBehaviour
{

    public float rotationSpeed = 10f;
    private float currentRotation = 0f;

    private void Update()
    {
        if (Pointer.current != null && Pointer.current.press.isPressed)
        {
            Vector2 delta =  Pointer.current.delta.ReadValue();

            if (delta.x != 0)
            {
                
                float rotationInput = (delta.x / Screen.width) * rotationSpeed * 1000f;

                currentRotation -= rotationInput * Time.deltaTime;
                transform.rotation = Quaternion.Euler(0,currentRotation,0);
            }
        }
    }
}
