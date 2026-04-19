using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerHealthManager : HealthManager {

    [Header("References")]
    private PlayerColorManager colorManager;
    private PlayerEffectManager effectManager;
    private PlayerGunManager gunManager;
    private GameCore gameCore;
    private GameManager gameManager;
    private UIController uiController;
    private PlayerController playerController;
    private Rigidbody2D rb;

    [Header("Regeneration")]
    [SerializeField] private int regenAmount;
    [SerializeField] private float regenWaitDuration;

    [Header("Respawn")]
    [SerializeField] private float respawnTime;

    [Header("Deferred Updates")]
    private bool pendingHealthUpdate; // set true when UpdateHealth fires before uiController is ready

    private void Awake() {

        // get UI controller reference for the local player; doing it here in case uiController reference is needed before it is initialized
        if (photonView.IsMine)
            uiController = FindFirstObjectByType<UIController>();

    }

    private new void Start() {

        base.Start(); // sets health and calls UpdateHealth; may set pendingHealthUpdate if uiController isn't ready yet

        colorManager = GetComponent<PlayerColorManager>();
        effectManager = GetComponent<PlayerEffectManager>();
        gunManager = GetComponent<PlayerGunManager>();
        gameCore = FindFirstObjectByType<GameCore>();
        gameManager = FindFirstObjectByType<GameManager>();
        playerController = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();

        // only the local player runs their own regeneration
        if (photonView.IsMine)
            StartCoroutine(HandleRegeneration());

    }

    // called by UIController.Initialize() after it has fully set itself up; flushes any deferred updates
    public void OnUIControllerInitialized(UIController ui) {

        if (!photonView.IsMine) return; // only local player needs to set uiController reference

        uiController = ui;

        if (pendingHealthUpdate) {

            pendingHealthUpdate = false;
            uiController.UpdateHealth(); // flush the health update that was missed before ui was ready

        }
    }

    private void OnTriggerExit2D(Collider2D collision) {

        if (!photonView.IsMine) return; // only local player handles their own death triggers

        if (gameCore.IsQuitting()) return; // to prevent errors

        // player falls out of map
        if (collision.CompareTag("Map"))
            StartCoroutine(Die());

    }

    // routes damage to the player's owner via RPC; use this instead of TakeDamage directly
    public override void RequestTakeDamage(float damage) => SendDamageToOwner(damage);

    // only runs on the player who owns this object (they apply damage and die if needed)
    public override bool TakeDamage(float damage) {

        if (!photonView.IsMine) return false; // only the owner processes damage

        RemoveHealth(damage * (colorManager.GetCurrentPlayerColor().GetEffectType() == EffectType.Defense ? (1f / effectManager.GetEffectMultiplier(EffectType.Defense)) : 1f)); // if player has the defense color equipped, add multiplier

        if (health <= 0f) {

            StartCoroutine(Die());
            return true;

        } else {

            return false;

        }
    }

    private IEnumerator Die() {

        isDead = true;

        health = 0f;

        playerController.ResetPlayer(); // reset player & camera (mainly for if player is rotated)

        // clear all player claims
        List<PlayerClaim> playerClaims = gameManager.GetPlayerClaims();

        foreach (PlayerClaim claim in playerClaims.ToList()) // use ToList() to avoid InvalidOperationException
            Destroy(claim);

        // reload all weapons
        foreach (Gun gun in gunManager.GetGuns().ToList()) // use ToList() to avoid InvalidOperationException
            gun.InstantReload();

        rb.linearVelocity = Vector2.zero; // reset velocity

        ParticleSystem.MainModule pm = Instantiate(deathEffect, transform.position, Quaternion.identity).main; // instantiate death effect where player died
        pm.startColor = colorManager.GetCurrentPlayerColor().GetSpriteColor(); // change particle color based on player color

        yield return new WaitForSeconds(respawnTime);

        SetHealth(maxHealth); // restore health
        transform.position = gameManager.GetPlayerSpawn(); // respawn at level spawn

        if (photonView.IsMine) // should already be true, but just in case, only update claimables HUD for local player
            uiController.UpdateClaimablesHUD();

        isDead = false;

    }

    public override void UpdateHealth(float health) {

        if (!photonView.IsMine) return;

        if (!uiController.IsInitialized()) {

            pendingHealthUpdate = true; // defer; will be flushed when OnUIControllerInitialized is called
            return;

        }

        pendingHealthUpdate = false; // clear flag; we're executing successfully
        uiController.UpdateHealth();

    }

    private IEnumerator HandleRegeneration() {

        while (true) {

            if (health < maxHealth) {

                AddHealth(regenAmount * (colorManager.GetCurrentPlayerColor().GetEffectType() == EffectType.Regeneration ? effectManager.GetEffectMultiplier(EffectType.Regeneration) : 1f)); // if player has the regeneration color equipped, add multiplier
                yield return new WaitForSeconds(regenWaitDuration);

            }

            yield return null;

        }
    }
}
