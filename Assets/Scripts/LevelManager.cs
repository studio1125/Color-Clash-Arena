using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : GameManager {

    [Header("Ready Check")]
    private int playersReady; // number of players who have reported ready

    [Header("Game Start")]
    [SerializeField] private float startDelay; // delay before level starts after all players have reported ready
    private double startTime; // synchronized start time for all players

    [Header("Score")]
    private Dictionary<int, int> remoteScores;

    private new void Awake() {

        base.Awake();
        playerController.SetKinematic(true); // enable kinematic to disable gravity until game fully starts

    }

    public override void Initialize() {

        remoteScores = new Dictionary<int, int>();

        // enable all UI
        uiController.EnableClaimablesInfoHUD();
        uiController.EnableGunCycleHUD();
        uiController.EnableHealthBarHUD();

        ReportReady(); // report ready to master client

    }

    public void ReportReady() {

        // set local player's score to 0 in custom properties so it can be accessed by all clients; this is done here to ensure the score is set before the MasterClient checks if all players are ready and starts the game
        ExitGames.Client.Photon.Hashtable scoreProps = new ExitGames.Client.Photon.Hashtable() { { "score", 0 } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(scoreProps);

        photonView.RPC(nameof(RPC_ReportReady), RpcTarget.MasterClient); // report ready to master client

    }

    [PunRPC]
    private void RPC_ReportReady() {

        if (!PhotonNetwork.IsMasterClient) return; // this RPC is only sent to the MasterClient, but check here just in case

        playersReady++;

        if (playersReady == PhotonNetwork.CurrentRoom.PlayerCount) // if all players have reported ready
            RequestStartGame();

    }

    private void RequestStartGame() {

        if (PhotonNetwork.IsMasterClient) {

            double timeToStart = PhotonNetwork.Time + startDelay;
            photonView.RPC(nameof(RPC_SyncStart), RpcTarget.AllBufferedViaServer, timeToStart);

        }
    }

    [PunRPC]
    private void RPC_SyncStart(double scheduledTime) {

        startTime = scheduledTime;
        StartCoroutine(WaitToStart());

    }

    private IEnumerator WaitToStart() {

        uiController.ShowCountdown(); // show countdown UI

        // update countdown every frame until start time is reached, using synchronized PhotonNetwork.Time to ensure all players see the same countdown and start at the same time
        while (PhotonNetwork.Time < startTime) {

            double timeLeft = startTime - PhotonNetwork.Time;
            uiController.UpdateCountdown(Mathf.CeilToInt((float) timeLeft));
            yield return null;

        }

        StartGame();

    }

    private void StartGame() {

        uiController.HideCountdown();
        playerController.EnableAllMechanics(); // enable all player controls
        playerController.SetKinematic(false); // disable kinematic to enable gravity

        // only MasterClient spawns enemies; they are networked objects so spawning on all clients would create duplicates
        if (PhotonNetwork.IsMasterClient)
            SpawnEnemies();

    }

    public override void AddClaim(EntityClaim claim) {

        if (claim is PlayerClaim playerClaim) {

            // only add claim if the local player is the owner of the claim
            if (playerController.photonView.OwnerActorNr == playerClaim.GetOwnerId()) {

                playerClaims.Add(playerClaim);
                claimManager.AddClaimable(playerClaim.GetColor(), playerClaim.GetEffectType(), playerClaim.GetMultiplierAddition());
                levelCurrClaimables++;

            }

            RequestUpdateScore(); // update score for all players

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

            RequestUpdateScore(); // update score for all players

            // update teleporter because some track claimables
            if (level.HasTeleporter() && levelClaimables.Contains(playerClaim.GetClaimable()))
                teleporter.UpdateTeleporter();

        } else if (claim is PhantomClaim phantomClaim) {

            enemyClaims.Remove(phantomClaim);

        }
    }

    private void RequestUpdateScore() {

        int score = claimManager.GetTotalClaims();
        uiController.UpdateLocalScore(score);
        photonView.RPC(nameof(RPC_UpdateRemoteScore), RpcTarget.Others, PhotonNetwork.LocalPlayer.ActorNumber, score);

    }

    [PunRPC]
    private void RPC_UpdateRemoteScore(int actorNumber, int remoteScore) {

        remoteScores[actorNumber] = remoteScore; // store the remote player's score in a dictionary using their actor number as the key (since this is run on all non-local clients, this client needs to keep track of the scores of all remote players to calculate the total score)

        int totalRemoteScore = 0;

        // calculate the sum of all remote scores
        foreach (int score in remoteScores.Values)
            totalRemoteScore += score;

        uiController.UpdateRemoteScore(totalRemoteScore);

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
