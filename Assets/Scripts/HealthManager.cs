using UnityEngine;
using Photon.Pun;

public abstract class HealthManager : MonoBehaviourPun {

    [Header("Health")]
    [SerializeField] protected int maxHealth;
    protected float health;

    [Header("Death")]
    [SerializeField] protected ParticleSystem deathEffect;
    protected bool isDead;

    protected void Start() => SetHealth(maxHealth);

    // external callers must use RequestTakeDamage rather than TakeDamage directly to ensure damage is routed through the correct RPC target (owner for players, MasterClient for phantoms)
    public abstract void RequestTakeDamage(float damage);

    // only ever called via RPC on the authoritative client
    public abstract bool TakeDamage(float damage);

    public abstract void UpdateHealth(float health);

    protected void SetHealth(float health) {

        this.health = health;
        UpdateHealth(this.health);

    }

    protected void AddHealth(float health) {

        this.health += health;
        UpdateHealth(this.health);

    }

    protected void RemoveHealth(float health) {

        this.health -= health;
        UpdateHealth(this.health);

    }

    // routes to the PhotonView owner (used by PlayerHealthManager)
    protected void SendDamageToOwner(float damage) => photonView.RPC(nameof(RPC_TakeDamageOwner), photonView.Owner, damage);

    // routes to MasterClient (used by PhantomHealthManager)
    protected void SendDamageToMasterClient(float damage) => photonView.RPC(nameof(RPC_TakeDamageMaster), RpcTarget.MasterClient, damage);

    // RPC landing pads (both call TakeDamage on whichever client receives the RPC)
    [PunRPC]
    protected void RPC_TakeDamageOwner(float damage) => TakeDamage(damage);

    [PunRPC]
    protected void RPC_TakeDamageMaster(float damage) => TakeDamage(damage);

    public float GetCurrentHealth() => health;

    public int GetMaxHealth() => maxHealth;

    public bool IsDead() => isDead;

}
