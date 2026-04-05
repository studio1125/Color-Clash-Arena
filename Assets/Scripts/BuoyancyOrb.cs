using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BuoyancyOrb : MonoBehaviour {

    [Header("References")]
    private SpriteRenderer spriteRenderer;
    private new Collider2D collider;
    private GameCore gameCore;
    private Coroutine fadeCoroutine;

    [Header("Buoyancy")]
    [SerializeField] private float buoyancyMultiplier;
    [SerializeField] private float destroyFadeDuration;

    private void Start() {

        spriteRenderer = GetComponent<SpriteRenderer>();
        collider = GetComponent<Collider2D>();
        gameCore = FindFirstObjectByType<GameCore>();

    }

    private void OnTriggerEnter2D(Collider2D collision) {

        if (collision.CompareTag("Player") && fadeCoroutine == null) { // collider is player and not already fading

            gameCore.ModifyGravity(1 / buoyancyMultiplier);
            collider.enabled = false; // disable collider

            // no need to stop existing fade coroutines since we check for null before starting a new one
            fadeCoroutine = StartCoroutine(FadeSprite(spriteRenderer, 0f, destroyFadeDuration)); // fade out and destroy the object

        }
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

        // destroy the sprite renderer's game object if faded out
        if (targetOpacity == 0f)
            Destroy(spriteRenderer.gameObject);

        fadeCoroutine = null;

    }
}
