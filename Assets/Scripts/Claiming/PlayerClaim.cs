using UnityEngine;

public class PlayerClaim : EntityClaim {

    [Header("Claim")]
    private Claimable claimable;
    private Color claimColor;
    private EffectType effectType;
    private float multiplierAddition;
    private int ownerId;

    public void Claim(Claimable claimable, Color claimColor, EffectType effectType, int ownerId) {

        this.claimable = claimable;
        this.claimColor = claimColor;
        this.effectType = effectType;
        this.ownerId = ownerId;
        this.multiplierAddition = claimable.GetMultiplierAddition();
        gameManager.AddClaim(this);

    }

    private void OnDisable() {

        GetComponent<Claimable>().OnClaimDestroy(this); // trigger destroy event
        gameManager.RemoveClaim(this); // remove claim

    }

    public Claimable GetClaimable() => claimable;

    public Color GetColor() => claimColor;

    public EffectType GetEffectType() => effectType;

    public bool IsOwner(int playerId) => ownerId == playerId;

    public int GetOwnerId() => ownerId;

    public float GetMultiplierAddition() => multiplierAddition;

}
