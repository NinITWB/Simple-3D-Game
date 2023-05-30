using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterMovem : MonoBehaviour
{
    [SerializeField] private CharacterController controller;
    [SerializeField] private float speed;

    [Range(0f, 10f)]
    [SerializeField] private float powerJump;

    private InputAction action;

    private Animator anim;
    //private InputManager inputManager;  //Declare the class that refers to get the data from Input Action 
    private PlayerInput input;
    //private float cameraDelay = 1.0f;
    private Vector2 movement;
    private Vector3 move;
    private float pressureGravity = .5f;
    private float gravity;
    private bool isMove;
    private bool isRun;
    private Transform cam;
    private string currentState;

    //Jump's variables
    private bool isJump = false;
    private bool isGround;
    private float initialJump;
    private float maxJumpHeight = 3.0f;
    private float maxJumpTime = .75f;
    private bool isJumping;
    private int jumpCount = 0;
    private bool call = true;
    private Dictionary<int, float> keyJump = new Dictionary<int, float>();
    private Dictionary<int, float> keyGravity = new Dictionary<int, float>();
    Coroutine setUpCoroutine = null;

    //private AnimateStyle animate = AnimateStyle.Idle;
    

    public enum AnimateStyle
    {
        Idle,
        Walk,
        Run
    }
    
    
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        //anim = GetComponentInChildren<Animator>();
        anim = GetComponent<Animator>();
        //inputManager = InputManager.Instance;
        input = new PlayerInput();

        input.Land.Movement.started += OnMove;
        input.Land.Movement.performed += OnMove;
        input.Land.Movement.canceled += OnMove;

        input.Land.Run.started += OnRun;
        //input.Land.Run.performed += OnRun;
        input.Land.Run.canceled += OnRun;

        input.Land.Jump.started += OnJump;
        //input.Land.Jump.performed += OnJump;
        input.Land.Jump.canceled += OnJump;
       
    }

    private void Start()
    {
        SetJumpVariables();
        cam = Camera.main.transform;
        
    }

    private void SetJumpVariables()
    {
        float timeToApex = maxJumpTime / 2;
        gravity = (-2 * maxJumpHeight) / Mathf.Pow(timeToApex , 2);
        initialJump = (2 * maxJumpHeight) / timeToApex;

        float secondGravity = (-2 * (maxJumpHeight + 2)) / Mathf.Pow(timeToApex * 1.25f, 2);
        float thirdGravity = (-2 * (maxJumpHeight + 3)) / Mathf.Pow(timeToApex * 1.75f, 2);

        float secondInitialJump = (2 * (maxJumpHeight + 2)) / (timeToApex * 1.25f);
        float thirdInitialJump = (2 * (maxJumpHeight + 3)) / (timeToApex * 1.75f);

        keyJump.Add(1, initialJump);
        keyJump.Add(2, secondInitialJump);
        keyJump.Add(3, thirdInitialJump);

        keyGravity.Add(0, gravity);
        keyGravity.Add(1, gravity);
        keyGravity.Add(2, secondGravity);
        keyGravity.Add(3, thirdGravity);
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        isJump = ctx.ReadValueAsButton();
        
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        movement = ctx.ReadValue<Vector2>();
        //movement = inputManager.GetMove();
        move.x = movement.x;
        move.z = movement.y;
        isMove = movement.x != 0 || movement.y != 0;
    }

    

    private void OnRun(InputAction.CallbackContext ctx)
    {
        isRun = ctx.ReadValueAsButton();
    }

    private void HandleJump()
    {
        isGround = controller.isGrounded;
        if (!isJumping && isJump && isGround)
        {
            if (jumpCount < 3 && setUpCoroutine != null)
            {
                StopCoroutine(setUpCoroutine);
                call = true;
                
            }
            isJumping = true;
            if (jumpCount >= 3) jumpCount = 0;
            jumpCount += 1;
            move.y = keyJump[jumpCount] * .5f;
        }
        else if (isJumping && !isJump && isGround)
        {
            isJumping = false;
        }   
    }

    private void HandleMovement()
    {

        if (isRun) controller.Move(new Vector3(move.x * 3, move.y, move.z * 3) * Time.deltaTime * speed);
        else
        controller.Move(move * Time.deltaTime * speed);
    }

    private void HandleRotation()
    {
        Vector3 goalRotation = new Vector3(move.x, 0, move.z);
        Quaternion currentRotation = transform.rotation;

        if (goalRotation != Vector3.zero)
        {
            Quaternion targetPostion = Quaternion.LookRotation(goalRotation);
            //transform.rotation = Quaternion.Slerp(currentRotation, targetPostion, Time.deltaTime * cameraDelay);
            transform.rotation = Quaternion.RotateTowards(currentRotation, targetPostion, 720 * Time.deltaTime);
        }    
    }

    private void ChangeAnimationState(string animationState)
    {
        if (currentState == animationState) return;
        anim.Play(animationState);

        currentState = animationState;
    }

    private void HandleAnimation()
    {
        /*if (isRun && isMove) anim.Play("Run");
        else anim.Play("Idle");
        anim.SetBool("isWalk", isMove);*/
        if (isJump && !isGround)
        {
            if (jumpCount == 1) ChangeAnimationState("Male dynamic pose");
            if (jumpCount == 2) ChangeAnimationState("Female dynamic pose");
            if (jumpCount == 3) ChangeAnimationState("Stylish flip");
            return;
        }
        if (isRun && isMove)
        {
            ChangeAnimationState("Run");
            return;
        }
        if (isMove && !isRun) { ChangeAnimationState("Walk"); }
        else ChangeAnimationState("Idle");
    }

    private void GetGravity()
    {
        bool isFalling = move.y <= 0 || !isJump;
        float decelerator = 3f;

        if (controller.isGrounded)
        {
          
            if (call)
            {
                setUpCoroutine = StartCoroutine(ResetJumpPose());
                call = false;
            }
            
          
            move.y = -pressureGravity;
        }
        else if (isFalling)
        {
            float previousYVeclocity = move.y;
            float newYVelocity = move.y + keyGravity[jumpCount] * decelerator * Time.deltaTime;
            float lastY = (previousYVeclocity + newYVelocity) * .5f;
            move.y = lastY;
        }
        else
        {
            float previousYVeclocity = move.y;
            float newYVelocity = move.y + keyGravity[jumpCount] * Time.deltaTime;
            float lastY = (previousYVeclocity + newYVelocity) * .5f;
            move.y = lastY;

        }

        /*if (isFalling)
        {
            float previousYVeclocity = move.y;
            float newYVelocity = move.y + keyGravity[jumpCount] * decelerator * Time.deltaTime;
            float lastY = (previousYVeclocity + newYVelocity) * .5f;
            move.y = lastY;
        }
        else if (!controller.isGrounded)
        { 
            //if (isFalling) { gravity *= decelerator; }
            float previousYVeclocity = move.y;
            float newYVelocity = move.y + keyGravity[jumpCount] * Time.deltaTime;
            float lastY = (previousYVeclocity + newYVelocity) * .5f;
            move.y = lastY;
        }
        else
        {
            setUpCoroutine = StartCoroutine(ResetJumpPose());
            move.y = -pressureGravity;
        }*/
    }

    private IEnumerator ResetJumpPose()
    {
        yield return new WaitForSeconds(1.0f);
        jumpCount = 0;
        call = true;
       
    }

    private void OnEnable()
    {
        input.Enable();
    }

    private void Update()
    {
        HandleAnimation();
        HandleRotation();
        HandleMovement();
        GetGravity();
        HandleJump();
        
    }

    private void OnDisable()
    {
        input.Disable();
    }

}
