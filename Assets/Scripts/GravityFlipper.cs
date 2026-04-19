using UnityEngine;

public class GravityFlipper : MonoBehaviour {

    [Header("References")]
    private GameCore gameCore;

    [Header("Rotation")]
    [SerializeField] private float rotationDuration;
    [SerializeField] private float rotationCooldown;
    private bool canRotate;

    private void Start() {

        gameCore = FindFirstObjectByType<GameCore>();
        canRotate = true;

    }

    private void OnCollisionEnter2D(Collision2D collision) {

        if (collision.transform.CompareTag("Player"))
            Flip(collision.transform.GetComponent<PlayerController>());

    }

    private void Flip(PlayerController playerController) {

        if (!canRotate) return;

        gameCore.ModifyGravity(-1f);
        playerController.GravityFlip(rotationDuration);
        canRotate = false;

        Invoke(nameof(ResetRotateCooldown), rotationCooldown);

    }

    private void ResetRotateCooldown() => canRotate = true;

}
