using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MultiplierInfo : MonoBehaviour {

    [Header("References")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text multiplierText;
    [SerializeField] private float transitionDuration;
    private RectTransform rectTransform;
    private float currMultiplier;
    private Coroutine textLerpCoroutine;

    private void Start() => rectTransform = GetComponent<RectTransform>();

    private void OnEnable() {

        if (textLerpCoroutine != null) StopCoroutine(textLerpCoroutine); // stop any existing text lerp coroutines
        textLerpCoroutine = StartCoroutine(LerpText(multiplierText, currMultiplier, currMultiplier, transitionDuration)); // start a new text lerp coroutine to update the multiplier text to the current multiplier value when the multiplier info is enabled

    }

    public void UpdateInfo(Sprite icon, float multiplier) {

        this.icon.sprite = icon;

        if (gameObject.activeInHierarchy) { // only update multiplier text if the multiplier info is currently visible

            if (textLerpCoroutine != null) StopCoroutine(textLerpCoroutine); // stop any existing text lerp coroutines
            textLerpCoroutine = StartCoroutine(LerpText(multiplierText, currMultiplier, multiplier, transitionDuration)); // start a new text lerp coroutine

        }

        currMultiplier = multiplier; // update current multiplier to the new value for the next update

    }

    private IEnumerator LerpText(TMP_Text text, float startValue, float targetValue, float duration) {

        float currentTime = 0f;

        while (currentTime < duration) {

            float currentValue = Mathf.Lerp(startValue, targetValue, currentTime / duration);
            text.text = (Mathf.Round(currentValue * 100f) / 100f) + "x"; // round multiplier to 2 decimal places
            RefreshLayout(rectTransform); // refresh the layout to update multiplier text width
            currentTime += Time.deltaTime;
            yield return null;

        }

        text.text = (Mathf.Round(targetValue * 100f) / 100f) + "x"; // ensure final value is set and rounded to 2 decimal places
        RefreshLayout(rectTransform); // refresh the layout to update multiplier text width
        textLerpCoroutine = null;

    }

    // IMPORTANT: this only works when the root is visible
    protected void RefreshLayout(RectTransform root) {

        foreach (LayoutGroup layoutGroup in root.GetComponentsInChildren<LayoutGroup>())
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup.GetComponent<RectTransform>());

        LayoutRebuilder.ForceRebuildLayoutImmediate(root); // force a rebuild of the root layout at the end

    }
}
