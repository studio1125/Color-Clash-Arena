using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PlayerController))]
public class PlayerEffectManager : MonoBehaviourPun {

    [Header("References")]
    private UIController uiController;

    [Header("Effects")]
    [SerializeField] private List<EffectData> effectMultipliers; // declare all default effect multipliers

    private void Start() {

        // only get UI for the local player
        if (photonView.IsMine)
            uiController = FindFirstObjectByType<UIController>();

    }

    public float GetEffectMultiplier(EffectType effectType) {

        foreach (EffectData effectData in effectMultipliers)
            if (effectData.GetEffectType() == effectType)
                return effectData.GetEffectMultiplier();

        return 0f;

    }

    public void AddEffectMultiplier(EffectType effectType, float multiplier) {

        foreach (EffectData effectData in effectMultipliers) {

            if (effectData.GetEffectType() == effectType) {

                effectData.AddEffectMultiplier(multiplier);

                // update ui for local player only
                if (photonView.IsMine) // this should be true, but it's good practice to check before accessing the UI
                    uiController.UpdateClaimablesHUD();

                return;

            }
        }
    }

    public void RemoveEffectMultiplier(EffectType effectType, float multiplier) {

        foreach (EffectData effectData in effectMultipliers) {

            if (effectData.GetEffectType() == effectType) {

                effectData.RemoveEffectMultiplier(multiplier);

                // update ui for local player only
                if (photonView.IsMine)
                    uiController.UpdateClaimablesHUD();

                return;

            }
        }
    }

    public List<EffectData> GetEffectMultipliers() => effectMultipliers;

}
