using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Teleporter : Interactable {

    [Header("References")]
    private GameCore gameCore;
    private GameManager gameManager;
    private UIController uiController;
    private Coroutine sliderCoroutine;

    [Header("Progress")]
    [SerializeField] private ProgressType progressType;
    [SerializeField] private Slider teleporterProgressSlider;

    [Header("Usage")]
    [SerializeField] private float progressLerpDuration;
    [SerializeField] private float useLerpDuration;

    private void Start() {

        gameCore = FindFirstObjectByType<GameCore>();
        gameManager = FindFirstObjectByType<GameManager>();
        uiController = FindFirstObjectByType<UIController>();

        // set the teleporter progress slider max value based on the selected progress type
        if (progressType == ProgressType.Checkpoints)
            teleporterProgressSlider.maxValue = gameManager.GetLevelTotalCheckpoints();
        else if (progressType == ProgressType.Claimables)
            teleporterProgressSlider.maxValue = gameManager.GetLevelTotalClaimables();

        teleporterProgressSlider.value = 0f; // initialize the teleporter progress slider value to 0

    }

    public override void Interact() {

        if (teleporterProgressSlider.value == teleporterProgressSlider.maxValue) // make sure teleporter is full
            UseTeleporter();

    }

    public void UpdateTeleporter() {

        if (gameCore.IsQuitting()) return; // prevent updating the teleporter if the game is quitting

        if (progressType == ProgressType.Checkpoints) {

            if (sliderCoroutine != null) StopCoroutine(sliderCoroutine); // stop any existing slider coroutines
            sliderCoroutine = StartCoroutine(LerpSlider(teleporterProgressSlider, gameManager.GetLevelCurrentCheckpoints(), progressLerpDuration));

        } else if (progressType == ProgressType.Claimables) {

            if (sliderCoroutine != null) StopCoroutine(sliderCoroutine); // stop any existing slider coroutines
            sliderCoroutine = StartCoroutine(LerpSlider(teleporterProgressSlider, gameManager.GetLevelCurrentClaimables(), progressLerpDuration));

        }
    }

    private void UseTeleporter() {

        teleporterProgressSlider.value = teleporterProgressSlider.maxValue; // make sure progress slider is full
        gameManager.SetLevelCompleted(true); // set level completed to true (do this before calling OnLevelCleared() because some level completion UI checks if the level is completed)
        uiController.OnLevelCleared();

    }

    private IEnumerator LerpSlider(Slider slider, float targetValue, float fadeDuration) {

        float currentTime = 0f;
        float initialValue = slider.value;

        while (currentTime < fadeDuration) {

            slider.value = Mathf.Lerp(initialValue, targetValue, currentTime / fadeDuration);
            currentTime += Time.deltaTime;
            yield return null;

        }

        slider.value = targetValue; // make sure the final value is set to the target value
        sliderCoroutine = null;

    }
}
