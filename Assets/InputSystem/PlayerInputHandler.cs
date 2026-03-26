using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    private PlayerInput playerInput;
    public Vector2 MousePos { get; private set; }
    public Vector2 MoveInput { get; private set; }

    public bool LeftMouseButtonPressed { get; private set; }
    public bool RightMouseButtonPressed { get; private set; }

    public bool DashPressed { get; private set; }
    public bool JumpPressed { get; private set; }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        playerInput = new PlayerInput();

        playerInput.Player.MousePos.performed += ctx => MousePos = ctx.ReadValue<Vector2>();

        playerInput.Player.Move.performed += ctx => MoveInput = ctx.ReadValue<Vector2>();
        playerInput.Player.Move.canceled += ctx => MoveInput = Vector2.zero;

        playerInput.Player.LeftButton.performed += ctx => LeftMouseButtonPressed = true;
        playerInput.Player.LeftButton.canceled += ctx => LeftMouseButtonPressed = false;

        playerInput.Player.RightButton.performed += ctx => RightMouseButtonPressed = true;
        playerInput.Player.RightButton.canceled += ctx => RightMouseButtonPressed = false;

        playerInput.Player.Dash.performed += ctx => DashPressed = true;
        playerInput.Player.Jump.performed += ctx => JumpPressed = true;
    }

    private void OnEnable()
    {
        playerInput.Enable();
    }

    private void LateUpdate()
    {
        DashPressed = false;
        JumpPressed = false;
    }

    private void OnDisable()
    {
        playerInput.Disable();
    }
}
