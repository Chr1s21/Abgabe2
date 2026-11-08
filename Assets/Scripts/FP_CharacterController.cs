using UnityEngine;
using UnityEngine.InputSystem;

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

    private CharacterController _controller; // Reference to the CharacterController component
    private Vector2 _moveInput; // Stores movement input from the player
    private Vector2 _lookInput; // Stores mouse look input
    private Vector3 _velocity; // Stores current velocity (for gravity/jumping)
    private float _verticalRotation = 0f; // Tracks vertical camera rotation
    private bool _isSprinting; // Is the player currently sprinting?
    private bool _isGrounded; // Is the player on the ground?

    void Start()
    {
        _controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HandleRotation();
        HandleMovement();
    }


    public void OnMove(InputAction.CallbackContext context) => _moveInput = context.ReadValue<Vector2>();

    public void OnLook(InputAction.CallbackContext context) => _lookInput = context.ReadValue<Vector2>();

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && _isGrounded)
        {
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.performed) _isSprinting = true;
        else if (context.canceled) _isSprinting = false;
    }


    private void HandleMovement()
    {
        _isGrounded = _controller.isGrounded;
        if (_isGrounded && _velocity.y < 0) _velocity.y = -2f; // Reset downward velocity when grounded

        float currentSpeed = _isSprinting ? sprintSpeed : walkSpeed;
        Vector3 move = transform.right * _moveInput.x + transform.forward * _moveInput.y;

        _controller.Move(move * currentSpeed * Time.deltaTime);

        _velocity.y += gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }

    private void HandleRotation()
    {
        transform.Rotate(Vector3.up * _lookInput.x * mouseSensitivity * Time.deltaTime);

        _verticalRotation -= _lookInput.y * mouseSensitivity * Time.deltaTime;
        _verticalRotation = Mathf.Clamp(_verticalRotation, upLimit, downLimit);
        cameraTransform.localRotation = Quaternion.Euler(_verticalRotation, 0, 0);
    }
}
