using System.Collections;
using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.LowLevel.InputStateHistory;

public class TestPlayer : MonoBehaviour, IAttackable
{
    [SerializeField]
    private Stat _playerStat;
    public PlayerInput Pi { get; private set; }
    public Vector3 MoveDir { get; private set; }
    public InputActionAsset IAA;
    private List<InputAction> _actions = new List<InputAction>();
    private Rigidbody _rigidbody;

    [SerializeField]
    private float _sensitive = 0.5f;

    private int _maxX = 80;
    private int _minX = -80;

    private float _rotationX = 0;
    private float _rotationY = 0;

    [SerializeField]
    private Transform _cam;

    public void OnDamaged(int damage, Vector3 pos)
    {
        _playerStat.Hp -= damage;
    }

    public bool OnHealed(int heal)
    {
        return true;
    }

    void Awake()
    {
        MoveDir = Vector3.zero;
        Pi = Util.GetOrAddComponent<PlayerInput>(gameObject);
        _rigidbody = GetComponent<Rigidbody>();
        Pi.actions = IAA;

        _playerStat = new Stat(100, 100, 5, 0, 0, 0);
        InitInputSystem();
    }

    private void InitInputSystem()
    {
        _actions.Add(Pi.actions.FindAction("Move"));
        _actions.Add(Pi.actions.FindAction("Attack"));
        _actions.Add(Pi.actions.FindAction("Interaction"));
        _actions.Add(Pi.actions.FindAction("Reload"));
        _actions.Add(Pi.actions.FindAction("Aim"));
        _actions.Add(Pi.actions.FindAction("Inventory"));

        _actions[0].performed -= Move;
        _actions[0].performed += Move;

        _actions[0].canceled -= Idle;
        _actions[0].canceled += Idle;
    }

    private void Move(InputAction.CallbackContext ctx)
    {
        Vector2 input = ctx.ReadValue<Vector2>();
        MoveDir = new Vector3(input.x, 0, input.y);
    }

    private void Idle(InputAction.CallbackContext ctx)
    {
        MoveDir = Vector3.zero;
    }

    void FixedUpdate()
    {
        Vector3 move = (Quaternion.AngleAxis(transform.localEulerAngles.y, Vector3.up) * MoveDir.normalized * _playerStat.Speed);
        _rigidbody.velocity = move;
        RotateMouse();
    }

    private void RotateMouse()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * _sensitive;
        float mouseY = Input.GetAxisRaw("Mouse Y") * _sensitive;

        _rotationX += mouseX;
        _rotationY = Mathf.Clamp(_rotationY + mouseY, _minX, _maxX);

        _cam.eulerAngles = new Vector3(-_rotationY, _cam.eulerAngles.y, 0);
        transform.eulerAngles = new Vector3(0, _rotationX, 0);
    }
}
