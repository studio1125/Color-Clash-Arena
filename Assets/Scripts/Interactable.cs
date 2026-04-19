using System.Collections;
using UnityEngine;

public abstract class Interactable : MonoBehaviour {

    [Header("References")]
    private Coroutine fadeCoroutine;

    [Header("Icon")]
    [SerializeField] protected SpriteRenderer interactKeyIcon;
    [SerializeField] protected float iconFadeDuration;
    private bool isVisible;

    protected void Awake() => interactKeyIcon.gameObject.SetActive(false);

    public abstract void Interact();

    public void ShowInteractKeyIcon() {

        if (isVisible) return;

        isVisible = true;
        interactKeyIcon.gameObject.SetActive(true); // enable the icon's game object before fading in

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine); // stop any existing fade coroutines
        fadeCoroutine = StartCoroutine(FadeSprite(interactKeyIcon, 1f, iconFadeDuration)); // fade in icon

    }

    public void HideInteractKeyIcon() {

        if (!isVisible) return;

        isVisible = false;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine); // stop any existing fade coroutines
        fadeCoroutine = StartCoroutine(FadeSprite(interactKeyIcon, 0f, iconFadeDuration)); // fade out icon

    }

    private IEnumerator FadeSprite(SpriteRenderer spriteRenderer, float targetOpacity, float fadeDuration) {

        float currentTime = 0f;
        Color initialColor = spriteRenderer.color;

        while (currentTime < fadeDuration) {

            spriteRenderer.color = Color.Lerp(initialColor, new Color(initialColor.r, initialColor.g, initialColor.b, targetOpacity), currentTime / fadeDuration);
            currentTime += Time.deltaTime;
            yield return null;

        }

        spriteRenderer.color = new Color(initialColor.r, initialColor.g, initialColor.b, targetOpacity); // make sure the final opacity is set to the target opacity

        // disable the sprite renderer's game object if faded out
        if (targetOpacity == 0f)
            spriteRenderer.gameObject.SetActive(false);

        fadeCoroutine = null;

    }
}
