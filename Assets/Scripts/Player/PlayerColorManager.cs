using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PlayerController))]
public class PlayerColorManager : MonoBehaviourPun {

    [Header("References")]
    private PlayerController playerController;
    private UIController uiController;

    [Header("Color Cycling")]
    [SerializeField] private PlayerColor[] playerColors;
    [SerializeField] private float colorCycleCooldown;
    private int currColorIndex;
    private SpriteRenderer spriteRenderer;
    private bool canColorCycle;

    [Header("Keybinds")]
    [SerializeField] private KeyCode colorSwitchKey;

    private void Start() {

        playerController = GetComponent<PlayerController>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        currColorIndex = 0;
        spriteRenderer.color = playerColors[currColorIndex].GetSpriteColor();

        // only update UI for the local player
        if (photonView.IsMine) {

            uiController = FindFirstObjectByType<UIController>();
            uiController.UpdateEffectText(playerColors[currColorIndex]); // update effect text
            uiController.UpdateClaimablesHUD(); // update claimables HUD (for selected indicator)

        }

        canColorCycle = true;

    }

    private void Update() {

        if (!photonView.IsMine) return; // only local player can switch their own color

        // color switching
        if (playerController.IsMechanicEnabled(MechanicType.ColorCycling)) // don't return if false to allow for more code to be added to this method later
            if (Input.GetKeyDown(colorSwitchKey))
                CycleColor();

    }

    private void CycleColor() {

        if (!canColorCycle) return;

        currColorIndex++;

        // loop sprite colors
        if (currColorIndex >= playerColors.Length)
            currColorIndex = 0;

        // sync color change to all other clients via RPC
        photonView.RPC(nameof(RPC_SyncColorIndex), RpcTarget.AllBuffered, currColorIndex);

        // start cooldown
        canColorCycle = false;
        Invoke(nameof(ColorCycleCooldownComplete), colorCycleCooldown);

    }

    // RPC: syncs color index change to all clients (AllBuffered so late joiners also get it)
    [PunRPC]
    private void RPC_SyncColorIndex(int colorIndex) {

        currColorIndex = colorIndex;
        spriteRenderer.color = playerColors[currColorIndex].GetSpriteColor();

        // only update UI on the local player's screen
        if (photonView.IsMine) {

            uiController.UpdateEffectText(playerColors[currColorIndex]); // update effect text
            uiController.UpdateClaimablesHUD(); // update claimables HUD (for selected indicator)

        }
    }

    private void ColorCycleCooldownComplete() => canColorCycle = true;

    public PlayerColor[] GetPlayerColors() => playerColors;

    public PlayerColor GetCurrentPlayerColor() => playerColors[currColorIndex];

}