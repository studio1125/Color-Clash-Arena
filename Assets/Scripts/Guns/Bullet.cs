using Photon.Pun;
using UnityEngine;

public class Bullet : MonoBehaviourPun {

    [Header("References")]
    private Rigidbody2D rb;

    [Header("Movement")]
    [SerializeField] private float speed;
    private float damage;

    [Header("Impact")]
    [SerializeField] private GameObject impactEffect;

    [Header("Range")]
    private float maxRange;
    private Vector3 spawnPos;

    // start function
    public void Initialize(float damage, Vector3 spawnPos, float maxRange, Collider2D shooterCollider) {

        rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = transform.right * speed;

        this.damage = damage;
        this.spawnPos = spawnPos;
        this.maxRange = maxRange;

        Physics2D.IgnoreCollision(GetComponent<Collider2D>(), shooterCollider); // ignore collision with shooter

    }

    private void Update() {

        if (Vector3.Distance(spawnPos, transform.position) > maxRange) // check if bullet reached max range
            SelfDestruct();

    }

    private void OnCollisionEnter2D(Collision2D collision) {

        // routes damage through the correct RequestTakeDamage method depending on target type:
        // - PlayerHealthManager routes damage to the player's owner via RPC
        // - PhantomHealthManager routes damage to MasterClient via RPC
        // both avoid the bug of double-processing damage on both clients

        if (collision.transform.TryGetComponent(out HealthManager healthManager))
            healthManager.RequestTakeDamage(damage);

        SelfDestruct();

    }

    private void SelfDestruct() {

        Instantiate(impactEffect, transform.position, transform.rotation);
        PhotonNetwork.Destroy(gameObject); // use PhotonNetwork.Destroy so the bullet is removed on all clients

    }
}