using UnityEngine;
using UnityEngine.InputSystem;

public class DummyPlayer : MonoBehaviour
{
    [SerializeField] private float speed = 3f;
    [SerializeField] private HPBar hpBar;

    private InputAction moveAction;
    private float hp = 1f;

    private void Awake()
    {
        moveAction = InputSystem.actions.FindAction("Player/Move");
    }

    private void Update()
    {
        transform.Translate(moveAction.ReadValue<Vector2>() * (speed * Time.deltaTime));

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            hp = hp > 0.1f ? hp - 0.1f : 1f;
            hpBar.SetRatio(hp);
        }
    }
}
