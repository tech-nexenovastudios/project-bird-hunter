using System;
using UnityEngine;


// This class tracks only up and down inputs; it does not track inputs like mouse position.
// For example: Input.GetMouseDown, Input.GetMouseUp.
// Create a static Action and invoke it inside a condition. Remember, the Action should be static.

public class InputManager : MonoBehaviour
{
    public static Action mouseUp;
    public static Action mouseDown;
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
          
            mouseDown?.Invoke();
           
        }
        else if (Input.GetMouseButtonUp(0))
        {
           
            mouseUp?.Invoke();
            
        }

    }
}
