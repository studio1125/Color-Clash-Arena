using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UnderlineButton : CustomButton {

    [Header("References")]
    [SerializeField] private Slider underline;
    private Button button;
    private Coroutine sliderCoroutine;

    [Header("Animations")]
    [SerializeField] private float underlineDuration;

    private void Start() {

        button = GetComponent<Button>();
        underline.value = 0f; // reset underline value to 0 at start

    }

    private void OnDisable() => underline.value = 0f; // reset underline value to 0 when disabled

    protected override void OnPointerEnter(PointerEventData eventData) {

        if (!button.interactable) return; // don't start underline animation if button isn't interactable

        if (sliderCoroutine != null) StopCoroutine(sliderCoroutine); // stop any existing slider coroutines
        sliderCoroutine = StartCoroutine(LerpSlider(underline, underline.maxValue, underlineDuration)); // start a new coroutine to fade in the underline

    }

    protected override void OnPointerExit(PointerEventData eventData) {

        // don't do the interactable check here because we want the underline to fade out even if the button becomes non-interactable

        if (sliderCoroutine != null) StopCoroutine(sliderCoroutine); // stop any existing slider coroutines
        sliderCoroutine = StartCoroutine(LerpSlider(underline, 0f, underlineDuration)); // start a new coroutine to fade out the underline

    }

    private IEnumerator LerpSlider(Slider slider, float targetValue, float fadeDuration) {

        float currentTime = 0f;
        float initialValue = slider.value;

        while (currentTime < fadeDuration) {

            slider.value = Mathf.Lerp(initialValue, targetValue, currentTime / fadeDuration);
            currentTime += Time.unscaledDeltaTime; // use unscaled delta time to ignore timescale
            yield return null;

        }

        slider.value = targetValue; // make sure the final value is set to the target value
        sliderCoroutine = null;

    }
}
