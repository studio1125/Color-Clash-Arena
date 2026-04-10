using System.Collections;
using UnityEngine;
using Photon.Pun;

public class Claimable : MonoBehaviourPun {

    [Header("References")]
    private PlayerHealthManager healthManager;
    private GameCore gameCore;
    private GameManager gameManager;
    private SpriteRenderer spriteRenderer;
    private Color startColor;

    [Header("Claiming")]
    [SerializeField] private float addedMultiplier;
    private EntityType claimer;

    [Header("Animations")]
    [SerializeField] private float claimDuration;
    // create separate coroutines for each entity to allow them to claim at the same time and show visual feedback (color alternates)
    private Coroutine playerColorCoroutine;
    private Coroutine phantomColorCoroutine;
    private Coroutine resetCoroutine;

    private void Awake() {

        // plain scene singletons safe to cache in Awake
        gameCore = FindFirstObjectByType<GameCore>();
        gameManager = FindFirstObjectByType<GameManager>();

    }

    private void Start() {

        healthManager = FindFirstObjectByType<PlayerHealthManager>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        startColor = spriteRenderer.color;

    }

    // called locally (player on their own client, phantom on MasterClient only)
    // RPCs the claim to all clients so the color lerp plays everywhere
    public void Claim(EntityType entityType, Color claimColor, EffectType? effectType = null) {

        PlayerClaim playerClaim = GetComponent<PlayerClaim>();
        PhantomClaim phantomClaim = GetComponent<PhantomClaim>();

        if ((entityType == EntityType.Player && ((playerClaim && playerClaim.GetEffectType() == effectType) || playerColorCoroutine != null || healthManager.IsDead())) || (entityType == EntityType.Phantom && (phantomClaim || phantomColorCoroutine != null))) // already claimed by entity (player done this way to make sure if effect types are different, they are still replaced)
            return;

        // send claim to all clients so the lerp and registration happen everywhere
        // effectType is sent as int since Photon can't serialize nullable enums; -1 means null
        // also send the actor number of the claiming player
        photonView.RPC(nameof(RPC_Claim), RpcTarget.All, (int) entityType, claimColor.r, claimColor.g, claimColor.b, effectType.HasValue ? (int) effectType.Value : -1, PhotonNetwork.LocalPlayer.ActorNumber);

    }

    // RPC: runs on all clients to start the claim lerp and register the claim when it finishes
    [PunRPC]
    private void RPC_Claim(int entityTypeInt, float r, float g, float b, int effectTypeInt, int ownerId) {

        EntityType entityType = (EntityType) entityTypeInt;
        Color claimColor = new Color(r, g, b);
        EffectType? effectType = effectTypeInt == -1 ? (EffectType?) null : (EffectType) effectTypeInt;

        if (playerColorCoroutine != null) {

            StopCoroutine(playerColorCoroutine);
            playerColorCoroutine = null;

        }

        if (phantomColorCoroutine != null) {

            StopCoroutine(phantomColorCoroutine);
            phantomColorCoroutine = null;

        }

        if (resetCoroutine != null) {

            StopCoroutine(resetCoroutine);
            resetCoroutine = null;

        }

        if (entityType == EntityType.Player)
            playerColorCoroutine = StartCoroutine(StartClaim(entityType, effectType, claimColor, claimDuration, ownerId));
        if (entityType == EntityType.Phantom)
            phantomColorCoroutine = StartCoroutine(StartClaim(entityType, effectType, claimColor, claimDuration, ownerId));

    }

    private IEnumerator StartClaim(EntityType entityType, EffectType? effectType, Color claimColor, float claimDuration, int ownerId) {

        float currentTime = 0f;
        Color startColor = spriteRenderer.color;

        while (currentTime < claimDuration) {

            currentTime += Time.deltaTime;
            spriteRenderer.color = Color.Lerp(startColor, claimColor, currentTime / claimDuration);
            yield return null;

        }

        spriteRenderer.color = claimColor;
        RegisterClaim(entityType, effectType, claimColor, ownerId);

        if (entityType == EntityType.Player)
            playerColorCoroutine = null;
        if (entityType == EntityType.Phantom)
            phantomColorCoroutine = null;

    }

    private void RegisterClaim(EntityType entityType, EffectType? effectType, Color claimColor, int ownerId) {

        if (entityType == EntityType.Player) {

            // destroy any existing claims
            foreach (PlayerClaim claim in GetComponents<PlayerClaim>())
                Destroy(claim);

            foreach (PhantomClaim claim in GetComponents<PhantomClaim>())
                Destroy(claim);

            gameObject.AddComponent<PlayerClaim>().Claim(this, claimColor, (EffectType) effectType, ownerId); // claim for player

        } else if (entityType == EntityType.Phantom) {

            // destroy any existing claims
            foreach (PlayerClaim claim in GetComponents<PlayerClaim>())
                Destroy(claim);

            foreach (PhantomClaim claim in GetComponents<PhantomClaim>())
                Destroy(claim);

            gameObject.AddComponent<PhantomClaim>().Claim(); // claim for phantom

        }

        claimer = entityType;

    }

    public void OnClaimDestroy(EntityClaim entityClaim) {

        if (gameManager is LevelManager levelManager && levelManager.IsLevelObjectiveCompleted()) return; // no resetting after level is cleared (make sure game manager is level manager)

        if (gameCore.IsQuitting() || playerColorCoroutine != null || phantomColorCoroutine != null) return;

        // check if there is another entity claim on the claimable
        foreach (EntityClaim claim in GetComponents<EntityClaim>())
            if (claim != entityClaim)
                return;

        if (entityClaim is PlayerClaim && playerColorCoroutine != null)
            StopCoroutine(playerColorCoroutine);
        if (entityClaim is PhantomClaim && phantomColorCoroutine != null)
            StopCoroutine(phantomColorCoroutine);

        if (resetCoroutine != null)
            StopCoroutine(resetCoroutine);

        // RPC the reset so the lerp back to start color plays on all clients
        photonView.RPC(nameof(RPC_ResetClaim), RpcTarget.All, startColor.r, startColor.g, startColor.b);

    }

    // RPC: runs on all clients to lerp the color back to the unclaimed start color
    [PunRPC]
    private void RPC_ResetClaim(float r, float g, float b) {

        Color resetColor = new Color(r, g, b);

        if (resetCoroutine != null)
            StopCoroutine(resetCoroutine);

        resetCoroutine = StartCoroutine(ResetClaim(resetColor, claimDuration));

    }

    private IEnumerator ResetClaim(Color resetColor, float claimDuration) {

        float currentTime = 0f;
        Color startColor = spriteRenderer.color;

        while (currentTime < claimDuration) {

            currentTime += Time.deltaTime;
            spriteRenderer.color = Color.Lerp(startColor, resetColor, currentTime / claimDuration);
            yield return null;

        }

        spriteRenderer.color = resetColor;
        claimer = EntityType.None;

    }

    public float GetMultiplierAddition() => addedMultiplier;

    public EntityType GetClaimer() => claimer;

}
