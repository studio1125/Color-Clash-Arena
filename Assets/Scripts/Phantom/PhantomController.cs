using TMPro;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhantomColorManager))]
[RequireComponent(typeof(PhantomGunManager))]
[RequireComponent(typeof(PhantomHealthManager))]
[RequireComponent(typeof(PhantomStateManager))]
public class PhantomController : MonoBehaviourPun {

    [Header("References")]
    private PhantomHealthManager healthManager;
    private PhantomGunManager gunManager;
    private PhantomColorManager colorManager;
    private PhantomStateManager stateManager;
    private Rigidbody2D rb;

    [Header("Label")]
    [SerializeField] private string enemyName;
    [SerializeField] private TMP_Text nameText;

    [Header("Spawn")]
    private PhantomSpawn phantomSpawn;

    [Header("Movement")]
    private bool isFacingRight;

    [Header("Ground Check")]
    [SerializeField] private Transform leftFoot;
    [SerializeField] private Transform rightFoot;
    [SerializeField] private LayerMask environmentMask;
    [SerializeField] private float groundCheckRadius;

    /*
    IMPORTANT:
        - ENEMY MUST START FACING RIGHT
        - VISION OBJECT CANNOT BE ON ENEMY LAYER
    */

    public void Initialize(PhantomSpawn phantomSpawn, Gun gun, bool isFlipped, Transform[] patrolPoints) {

        // only run on MasterClient to prevent double-initialization in multiplayer, though we can check again here just in case
        if (!PhotonNetwork.IsMasterClient) return;

        gunManager = GetComponent<PhantomGunManager>();
        stateManager = GetComponent<PhantomStateManager>();

        this.phantomSpawn = phantomSpawn;

        gunManager.SetGun(gun);

        isFacingRight = !isFlipped;

        stateManager.Initialize(patrolPoints, isFlipped);

    }

    private void Start() {

        healthManager = GetComponent<PhantomHealthManager>();
        colorManager = GetComponent<PhantomColorManager>();
        rb = GetComponent<Rigidbody2D>();

        nameText.text = enemyName;

    }

    private void Update() {

        // only MasterClient runs phantom AI and claiming logic (to prevent double-claiming in multiplayer)
        if (!PhotonNetwork.IsMasterClient) return;

        // if phantom is standing on something, claim it
        Collider2D leftCollider = Physics2D.OverlapCircle(leftFoot.position, groundCheckRadius, environmentMask);
        Collider2D rightCollider = Physics2D.OverlapCircle(rightFoot.position, groundCheckRadius, environmentMask);

        if (leftCollider != null)
            leftCollider.GetComponent<Claimable>()?.Claim(EntityType.Phantom, colorManager.GetCurrentPhantomColor().GetClaimColor());
        if (rightCollider != null)
            rightCollider.GetComponent<Claimable>()?.Claim(EntityType.Phantom, colorManager.GetCurrentPhantomColor().GetClaimColor());

    }

    public void CheckFlip() {

        if ((isFacingRight && rb.linearVelocity.x < 0f) // phantom is going left while facing right
            || (!isFacingRight && rb.linearVelocity.x > 0f)) // phantom is going right while facing left
            Flip();

    }

    public void Flip() {

        transform.Rotate(0f, 180f, 0f);
        healthManager.FlipCanvas();
        isFacingRight = !isFacingRight; // breaks when there are errors

    }

    public PhantomSpawn GetEnemySpawn() => phantomSpawn;

    public bool IsFacingRight() => isFacingRight;

}
