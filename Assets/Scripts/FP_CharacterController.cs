using UnityEngine;
using UnityEngine.InputSystem;

// This script implements a basic first-person character controller for Unity.
// It handles movement, sprinting, jumping, mouse look, and camera rotation.
// Attach this script to your player GameObject and assign the required components in the inspector.
[RequireComponent(typeof(CharacterController))]
public class FP_CharacterController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f; // Speed when walking
    public float sprintSpeed = 8f; // Speed when sprinting
    public float jumpHeight = 1.5f; // Height of the jump
    public float gravity = -9.81f; // Gravity applied to the character

    [Header("Look Settings")]
    public Transform cameraTransform; // Reference to the player's camera
    public float mouseSensitivity = 20f; // Mouse sensitivity for looking around
    public float upLimit = -80f; // Maximum upward look angle
    public float downLimit = 80f; // Maximum downward look angle

    // Private variables for internal logic
    private CharacterController _controller; // Reference to the CharacterController component
    private Vector2 _moveInput; // Stores movement input from the player
    private Vector2 _lookInput; // Stores mouse look input
    private Vector3 _velocity; // Stores current velocity (for gravity/jumping)
    private float _verticalRotation = 0f; // Tracks vertical camera rotation
    private bool _isSprinting; // Is the player currently sprinting?
    private bool _isGrounded; // Is the player on the ground?

    void Start()
    {
        // Get reference to the CharacterController component
        _controller = GetComponent<CharacterController>();
        // Lock the cursor to the game window for FPS control
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Handle camera rotation and player movement every frame
        HandleRotation();
        HandleMovement();
    }

    // Public Input Methods (called by Unity's Input System)

    // Called when movement input is received (WASD/left stick)
    public void OnMove(InputAction.CallbackContext context) => _moveInput = context.ReadValue<Vector2>();
    
    // Called when mouse look input is received (mouse movement/right stick)
    public void OnLook(InputAction.CallbackContext context) => _lookInput = context.ReadValue<Vector2>();

    // Called when jump input is pressed
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && _isGrounded)
        {
            // Calculate jump velocity using physics formula
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    // Called when sprint input is pressed or released
    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.performed) _isSprinting = true;
        else if (context.canceled) _isSprinting = false;
    }

    // Character Controller Logic

    // Handles all character movement, jumping, and gravity
    private void HandleMovement()
    {
        // Check if the character is on the ground
        _isGrounded = _controller.isGrounded;
        if (_isGrounded && _velocity.y < 0) _velocity.y = -2f; // Reset downward velocity when grounded

        // Determine movement speed (walk or sprint)
        float currentSpeed = _isSprinting ? sprintSpeed : walkSpeed;
        // Calculate movement direction based on input
        Vector3 move = transform.right * _moveInput.x + transform.forward * _moveInput.y;
        
        // Move the character
        _controller.Move(move * currentSpeed * Time.deltaTime);

        // Apply gravity
        _velocity.y += gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }

    // Handles camera and player rotation based on mouse input
    private void HandleRotation()
    {
        // Horizontal rotation (Player body)
        transform.Rotate(Vector3.up * _lookInput.x * mouseSensitivity * Time.deltaTime);

        // Vertical rotation (Camera)
        _verticalRotation -= _lookInput.y * mouseSensitivity * Time.deltaTime;
        _verticalRotation = Mathf.Clamp(_verticalRotation, upLimit, downLimit);
        cameraTransform.localRotation = Quaternion.Euler(_verticalRotation, 0, 0);
    }
}
