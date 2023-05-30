using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class InputManager : MonoBehaviour
{
    private PlayerInput input;
    private static InputManager _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject); 
        }
        else
        {
            _instance = this;
        }
        input = new PlayerInput();
    }

    public static InputManager Instance
    {
        get
        {
            return _instance;
        }
    }

    public Vector2 GetMove()
    {
        return input.Land.Movement.ReadValue<Vector2>();
    }

    public bool GetJump()
    {
        return input.Land.Jump.triggered;
    }

    public bool GetRun()
    {
        return input.Land.Run.triggered;
    }

    private void OnEnable()
    {
        input.Enable();
    }

    private void OnDisable()
    {
        input.Disable();
    }
}
