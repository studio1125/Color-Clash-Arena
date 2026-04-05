using System.Collections;
using UnityEngine;

public abstract class Checkpoint : MonoBehaviour {

    [Header("References")]
    [SerializeField] private SpriteRenderer arrowRenderer;
    private GameManager gameManager;
    private UIController uiController;
    private SpriteRenderer checkpointRenderer;
    private Coroutine checkpointFadeCoroutine;
    private Coroutine arrowFadeCoroutine;
    private Coroutine checkpointFlashCoroutine;

    [Header("Mechanics")]
    [SerializeField] private MechanicType mechanicToUnlock;

    [Header("Color")]
    [SerializeField] private Color errorColor;
    [SerializeField] private float errorDisplayDuration;
    private Color startCheckpointColor;
    private Color startArrowColor;

    [Header("Subtitles")]
    [SerializeField] private string[] subtitleTexts;
    [SerializeField] private float subtitleDisplayDuration;

    [Header("Destruction")]
    [SerializeField] private float destroyFadeDuration;

    protected void Start() {

        gameManager = FindFirstObjectByType<GameManager>();
        uiController = FindFirstObjectByType<UIController>();
        checkpointRenderer = GetComponent<SpriteRenderer>();

        startCheckpointColor = checkpointRenderer.color;
        startArrowColor = arrowRenderer.color;

    }

    private void OnTriggerEnter2D(Collider2D collision) {

        if (collision.CompareTag("Player")) { // collider is player

            if (CheckRequirements()) { // requirements are met to activate checkpoint

                if (subtitleTexts.Length > 1) // if there is more than one subtitle text, cycle through them
                    uiController.StartCycleSubtitleTexts(subtitleTexts, subtitleDisplayDuration); // start subtitle cycle
                else
                    uiController.SetSubtitleText(subtitleTexts[0]); // set subtitle text

                collision.GetComponent<PlayerController>().EnableMechanic(mechanicToUnlock); // unlock mechanic associated with checkpoint

                OnCheckpointDisabled(); // allow subclasses to do something when checkpoint is disabled

                if (checkpointFadeCoroutine != null) StopCoroutine(checkpointFadeCoroutine); // stop any existing fade coroutines
                checkpointFadeCoroutine = StartCoroutine(FadeSprite(checkpointRenderer, 0f, destroyFadeDuration, SpriteType.Checkpoint)); // fade out checkpoint

                if (arrowFadeCoroutine != null) StopCoroutine(arrowFadeCoroutine); // stop any existing fade coroutines
                arrowFadeCoroutine = StartCoroutine(FadeSprite(arrowRenderer, 0f, destroyFadeDuration, SpriteType.Arrow)); // fade out arrow

                gameManager.SetPlayerSpawn(transform.position); // update player spawn
                gameManager.UpdateCheckpoints(); // update checkpoints

            } else {

                // requirements to activate checkpoint are not met

                if (checkpointFlashCoroutine != null) StopCoroutine(checkpointFlashCoroutine); // stop any existing flash coroutines
                checkpointFlashCoroutine = StartCoroutine(FlashCheckpointError(errorDisplayDuration)); // start flash error coroutine

            }
        }
    }

    private IEnumerator FadeSprite(SpriteRenderer spriteRenderer, float targetOpacity, float fadeDuration, SpriteType spriteType) {

        float currentTime = 0f;
        Color initialColor = spriteRenderer.color;

        while (currentTime < fadeDuration) {

            spriteRenderer.color = Color.Lerp(initialColor, new Color(initialColor.r, initialColor.g, initialColor.b, targetOpacity), currentTime / fadeDuration);
            currentTime += Time.deltaTime;
            yield return null;

        }

        spriteRenderer.color = new Color(initialColor.r, initialColor.g, initialColor.b, targetOpacity); // make sure the final opacity is set

        // disable the sprite renderer's object if faded out
        if (targetOpacity == 0f)
            spriteRenderer.gameObject.SetActive(false);

        if (spriteType == SpriteType.Checkpoint)
            checkpointFadeCoroutine = null; // clear reference to checkpoint fade coroutine when done
        else if (spriteType == SpriteType.Arrow)
            arrowFadeCoroutine = null; // clear reference to arrow fade coroutine when done

    }

    private IEnumerator FlashCheckpointError(float flashDuration) {

        float currentTime = 0f;

        // lerp to error color over half of the duration
        while (currentTime < flashDuration / 2f) {

            checkpointRenderer.color = Color.Lerp(startCheckpointColor, errorColor, currentTime / (flashDuration / 2f));
            arrowRenderer.color = Color.Lerp(startArrowColor, errorColor, currentTime / (flashDuration / 2f));
            currentTime += Time.deltaTime;
            yield return null;

        }

        currentTime = 0f; // reset current time for lerp back

        // lerp back to start color over the other half of the duration
        while (currentTime < flashDuration / 2f) {

            checkpointRenderer.color = Color.Lerp(errorColor, startCheckpointColor, currentTime / (flashDuration / 2f));
            arrowRenderer.color = Color.Lerp(errorColor, startArrowColor, currentTime / (flashDuration / 2f));
            currentTime += Time.deltaTime;
            yield return null;

        }

        checkpointRenderer.color = startCheckpointColor; // reset to start color
        arrowRenderer.color = startArrowColor; // reset to start color

        checkpointFlashCoroutine = null;

    }

    protected abstract void OnCheckpointDisabled();

    protected abstract bool CheckRequirements();

    private enum SpriteType {

        Checkpoint,
        Arrow

    }
}
