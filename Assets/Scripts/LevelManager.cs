using UnityEngine;

public class LevelManager : GameManager {

    public override void Initialize() {

        playerController.EnableAllMechanics(); // enable all player controls

        // enable all UI
        uiController.EnableClaimablesInfoHUD();
        uiController.EnableGunCycleHUD();
        uiController.EnableHealthBarHUD();

    }

    public override void AddClaim(EntityClaim claim) {

        if (claim is PlayerClaim playerClaim) {

            // only add claim if the local player is the owner of the claim
            if (playerController.photonView.OwnerActorNr == playerClaim.GetOwnerId()) {

                playerClaims.Add(playerClaim);
                claimManager.AddClaimable(playerClaim.GetColor(), playerClaim.GetEffectType(), playerClaim.GetMultiplierAddition());
                levelCurrClaimables++;

            }

            /* FOR ENDING GAME WHEN EVERYTHING IS CLAIMED
            CheckLevelClear(); // check if player has claimed all platforms
            */

            // update teleporter because some track claimables
            if (level.HasTeleporter() && levelClaimables.Contains(playerClaim.GetClaimable()))
                teleporter.UpdateTeleporter();

        } else if (claim is PhantomClaim phantomClaim) {

            enemyClaims.Add(phantomClaim);

        }
    }

    public override void RemoveClaim(EntityClaim claim) {

        if (claim is PlayerClaim playerClaim) {

            // only remove claim if player is the owner of the claim
            if (playerController.photonView.OwnerActorNr == playerClaim.GetOwnerId()) {

                playerClaims.Remove(playerClaim);
                claimManager.RemoveClaimable(playerClaim.GetColor(), playerClaim.GetEffectType(), playerClaim.GetMultiplierAddition());
                levelCurrClaimables--;

            }

            // update teleporter because some track claimables
            if (level.HasTeleporter() && levelClaimables.Contains(playerClaim.GetClaimable()))
                teleporter.UpdateTeleporter();

        } else if (claim is PhantomClaim phantomClaim) {

            enemyClaims.Remove(phantomClaim);

        }
    }

    public override bool IsLevelObjectiveCompleted() {

        // make sure player has all claimables claimed
        bool found;

        foreach (Claimable claimable in levelClaimables) {

            found = false;

            for (int i = 0; i < playerClaims.Count; i++) {

                if (playerClaims[i].GetClaimable() == claimable) {

                    found = true;
                    break;

                }
            }

            if (!found)
                return false;

        }

        // make sure all phantoms are dead
        if (FindObjectsByType<PhantomController>(FindObjectsSortMode.None).Length != 0)
            return false;

        levelCompleted = true;
        return true;

    }
}
