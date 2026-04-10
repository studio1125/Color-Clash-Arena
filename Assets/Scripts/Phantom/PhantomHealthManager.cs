using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

[RequireComponent(typeof(PhantomController))]
public class PhantomHealthManager : HealthManager, IPunObservable {

    [Header("References")]
    private PhantomController phantomController;
    private GameManager gameManager;
    private SpriteRenderer spriteRenderer;

    [Header("Health Bar")]
    [SerializeField] private Transform healthCanvas;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image sliderFill;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private float healthLerpDuration;
    [SerializeField] private Gradient healthGradient;
    private Coroutine healthLerpCoroutine;

    private new void Start() {

        base.Start(); // sets health and calls UpdateHealth

        phantomController = GetComponent<PhantomController>();
        gameManager = FindFirstObjectByType<GameManager>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // sync slider max with maxHealth set in base
        healthSlider.maxValue = maxHealth;
        healthSlider.value = healthSlider.maxValue;
        healthText.text = Mathf.CeilToInt(healthSlider.value) + ""; // health text is health rounded up

    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {

        if (stream.IsWriting) {

            // master client: send the current health value
            stream.SendNext(health);

        } else {

            // clients: receive the health value
            float networkHealth = (float) stream.ReceiveNext();

            // if the received health is different, update visuals
            if (networkHealth != health) {

                health = networkHealth;
                UpdateHealth(health);

            }
        }
    }

    // routes damage to MasterClient who owns all phantom logic; use this instead of TakeDamage directly
    public override void RequestTakeDamage(float damage) => SendDamageToMasterClient(damage);

    // only called on MasterClient via RPC_TakeDamageMaster in the base class
    public override bool TakeDamage(float damage) {

        RemoveHealth(damage);

        if (health <= 0f) {

            Die();
            return true;

        } else {

            return false;

        }
    }

    private void Die() {

        isDead = true;
        photonView.RPC(nameof(RPC_Die), RpcTarget.All); // sync death across all clients before destroying

    }

    // RPC: runs on all clients to play death effect and destroy phantom
    [PunRPC]
    private void RPC_Die() {

        ParticleSystem.MainModule pm = Instantiate(deathEffect, transform.position, Quaternion.identity).main;
        pm.startColor = spriteRenderer.color; // change particle color based on phantom color

        // clear all phantom claims
        List<PhantomClaim> phantomClaims = gameManager.GetEnemyClaims();

        foreach (PhantomClaim claim in phantomClaims.ToList()) // use ToList() to avoid InvalidOperationException
            Destroy(claim);

        // only MasterClient notifies the spawn about the death (to avoid double-respawn calls)
        if (PhotonNetwork.IsMasterClient)
            phantomController.GetEnemySpawn().OnEnemyDeath(); // tell phantom spawn to respawn phantom if enabled

        Destroy(gameObject);

    }

    public override void UpdateHealth(float health) {

        if (healthLerpCoroutine != null)
            StopCoroutine(healthLerpCoroutine);

        healthLerpCoroutine = StartCoroutine(LerpHealth(health, healthLerpDuration));

    }

    private IEnumerator LerpHealth(float targetHealth, float duration) {

        float currentTime = 0f;
        float startHealth = healthSlider.value;

        while (currentTime < duration) {

            currentTime += Time.deltaTime;
            healthSlider.value = Mathf.Lerp(startHealth, targetHealth, currentTime / duration);
            healthText.text = Mathf.CeilToInt(healthSlider.value) + ""; // health text is health rounded up
            sliderFill.color = healthGradient.Evaluate(healthSlider.normalizedValue); // normalizedValue returns the value between 0 and 1 (can't use DoTween here because of this line)
            yield return null;

        }

        healthSlider.value = targetHealth;
        healthLerpCoroutine = null;

    }

    public void FlipCanvas() => healthCanvas.Rotate(0f, 180f, 0f);

}
