using System.Collections;
using UnityEngine;
using Photon.Pun;

public class Claimable : MonoBehaviourPun {

    [Header("References")]
    private GameCore gameCore;
    private GameManager gameManager;
    private SpriteRenderer spriteRenderer;
    private Color startColor;

    [Header("Claiming")]
    [SerializeField] private float addedMultiplier;
    private int currentClaimingActorId; // to track which player is currently claiming for the player claims, used to prevent overriding an ongoing claim with the same effect type; -1 means no ongoing claim
    private EffectType? currentClaimingEffectType; // to track which effect type is currently claiming for the player claims, used to prevent overriding an ongoing claim with the same effect type; null means no ongoing claim
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

        currentClaimingActorId = -1; // initialize to -1 to indicate no ongoing claim

    }

    private void Start() {

        spriteRenderer = GetComponent<SpriteRenderer>();
        startColor = spriteRenderer.color;

    }

    // called locally (player on their own client, phantom on MasterClient only)
    // RPCs the claim to all clients so the color lerp plays everywhere
    public void Claim(EntityType entityType, Color claimColor, EffectType? effectType = null) {

        PlayerClaim playerClaim = GetComponent<PlayerClaim>();
        PhantomClaim phantomClaim = GetComponent<PhantomClaim>();

        if (entityType == EntityType.Player) {

            // if there's an existing claim, only return if it is the local player's and the effect is the same
            if (playerClaim != null && playerClaim.GetOwnerId() == PhotonNetwork.LocalPlayer.ActorNumber && playerClaim.GetEffectType() == effectType)
                return;

            // also check if there's an ongoing lerp for the local player with the same effect type; if so, return to avoid overriding the existing lerp and registration
            if (playerColorCoroutine != null && currentClaimingActorId == PhotonNetwork.LocalPlayer.ActorNumber && currentClaimingEffectType == effectType)
                return;

        } else if (entityType == EntityType.Phantom) {

            // will only be here on the MasterClient

            // if there's an existing claim and there is already an ongoing lerp, return
            if (phantomClaim != null || phantomColorCoroutine != null)
                return;

        }

        // send claim to all clients so the lerp and registration happen everywhere
        // effectType is sent as int since Photon can't serialize nullable enums; -1 means null
        // also send the actor number of the claiming player
        photonView.RPC(nameof(RPC_Claim), RpcTarget.All, entityType, claimColor.r, claimColor.g, claimColor.b, effectType.HasValue ? (int) effectType.Value : -1, PhotonNetwork.LocalPlayer.ActorNumber);

    }

    // RPC: runs on all clients to start the claim lerp and register the claim when it finishes
    [PunRPC]
    private void RPC_Claim(EntityType entityType, float r, float g, float b, int effectTypeInt, int ownerId) {

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

        // set at the start of the claim so that if another claim with the same effect type is attempted by the same player while this claim is still lerping, it will be prevented
        currentClaimingActorId = ownerId;
        currentClaimingEffectType = effectType;

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

        if (entityClaim is PlayerClaim && playerColorCoroutine != null) {

            currentClaimingActorId = -1; // reset to -1 to indicate no ongoing claim
            currentClaimingEffectType = null; // reset to null to indicate no ongoing claim
            StopCoroutine(playerColorCoroutine);

        }

        if (entityClaim is PhantomClaim && phantomColorCoroutine != null)
            StopCoroutine(phantomColorCoroutine);

        if (resetCoroutine != null)
            StopCoroutine(resetCoroutine);

        photonView.RPC(nameof(RPC_ResetClaim), RpcTarget.All, startColor.r, startColor.g, startColor.b); // RPC the reset so the lerp back to start color plays on all clients

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
