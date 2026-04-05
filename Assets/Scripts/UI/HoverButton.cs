using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoverButton : CustomButton {

    [Header("Animations")]
    [SerializeField] protected Color hoverColor;
    [SerializeField] protected float hoverFadeDuration;
    [SerializeField][Tooltip("If other screens/HUDs fade over it")] private bool hasOverlays;
    protected Color startColor;
    private Button button;
    private Coroutine textColorCoroutine;

    private void Start() {

        button = GetComponent<Button>();
        startColor = text.color;

    }

    private void OnDisable() {

        // remove hover effects if this has overlays
        if (hasOverlays)
            text.color = startColor;

    }

    protected override void OnPointerEnter(PointerEventData eventData) {

        if (!button.interactable) return; // don't start hover animation if button isn't interactable

        if (textColorCoroutine != null) StopCoroutine(textColorCoroutine); // stop any existing text color coroutines
        textColorCoroutine = StartCoroutine(LerpTextColor(text, hoverColor, hoverFadeDuration));

    }

    protected override void OnPointerExit(PointerEventData eventData) {

        // don't do the interactable check here because we want the text to fade back to the start color even if the button becomes non-interactable

        if (textColorCoroutine != null) StopCoroutine(textColorCoroutine); // stop any existing text color coroutines
        textColorCoroutine = StartCoroutine(LerpTextColor(text, startColor, hoverFadeDuration));

    }


    private IEnumerator LerpTextColor(TMP_Text text, Color targetColor, float fadeDuration) {

        float currentTime = 0f;
        Color initialColor = text.color;

        while (currentTime < fadeDuration) {

            text.color = Color.Lerp(initialColor, targetColor, currentTime / fadeDuration);
            currentTime += Time.unscaledDeltaTime; // use unscaled delta time to ignore timescale
            yield return null;

        }

        text.color = targetColor; // make sure the final color is set to the target color
        textColorCoroutine = null;

    }
}
