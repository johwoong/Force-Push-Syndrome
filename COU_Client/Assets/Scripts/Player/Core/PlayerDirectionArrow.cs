using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerDirectionArrow : MonoBehaviour
{
    [SerializeField] private Transform arrow;
    [SerializeField] private float radius = 0.95f;

    private PlayerMovement playerMovement;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void LateUpdate()
    {
        Vector2 direction = playerMovement.FacingDirection;
        arrow.localPosition = direction * radius;
        arrow.right = direction;
    }
}
