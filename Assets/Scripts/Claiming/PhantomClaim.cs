public class PhantomClaim : EntityClaim {

    public void Claim() => gameManager.AddClaim(this);

    private void OnDisable() {

        GetComponent<Claimable>().OnClaimDestroy(this); // trigger destroy event
        gameManager.RemoveClaim(this); // remove claim

    }
}
