using UnityEngine;
using TMPro;
using UnityEngine.UI;

[ExecuteInEditMode]
public class MaxWidthLayout : MonoBehaviour {

    [Header("References")]
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private LayoutGroup layoutGroup;
    [SerializeField] private TextMeshProUGUI textChild;

    [Header("Settings")]
    [SerializeField] private float maxWidth;

    void LateUpdate() {

        if (textChild == null || layoutGroup == null) return;

        float preferredWidth = textChild.preferredWidth + layoutGroup.padding.left + layoutGroup.padding.right; // calculate the preferred width of the text plus padding
        float constrainedWidth = Mathf.Min(preferredWidth, maxWidth); // constrain the width to the maximum value
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, constrainedWidth); // set the width of the RectTransform to the constrained width

    }
}
