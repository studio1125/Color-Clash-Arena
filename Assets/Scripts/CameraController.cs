using System.Collections;
using UnityEngine;

public class CameraController : MonoBehaviour {

    [Header("References")]
    private GameCore gameCore;
    private new Camera camera;
    private Coroutine rotationCoroutine;

    [Header("Follow")]
    [SerializeField] private float followSmoothing;
    private Transform target;
    private float zOffset;

    [Header("Bounds")]
    [SerializeField] private BoxCollider2D mapBounds;
    private float xMin, yMin, xMax, yMax;
    private float camSize;
    private float camRatio;

    private void Start() {

        gameCore = FindFirstObjectByType<GameCore>();
        target = FindFirstObjectByType<PlayerController>().transform;
        camera = GetComponent<Camera>();

        zOffset = transform.position.z - target.position.z;

        xMin = mapBounds.bounds.min.x;
        yMin = mapBounds.bounds.min.y;
        xMax = mapBounds.bounds.max.x;
        yMax = mapBounds.bounds.max.y;

        camSize = camera.orthographicSize;
        camRatio = ((float) Screen.width / Screen.height) * camSize;

    }

    private void LateUpdate() {

        transform.position = Vector3.Lerp(transform.position, new Vector3(Mathf.Clamp(target.position.x, xMin + camRatio, xMax - camRatio), Mathf.Clamp(target.position.y, yMin + camSize, yMax - camSize), zOffset), followSmoothing * Time.deltaTime); // z value of vector3 should be zero because offset is being added after

    }

    public void RotateCamera(float duration, bool isRotated) {

        if (isRotated) {

            if (rotationCoroutine != null) StopCoroutine(rotationCoroutine); // stop any existing rotation coroutines
            rotationCoroutine = StartCoroutine(LerpRotation(Quaternion.identity, duration)); // rotate back to default orientation

        } else {

            if (rotationCoroutine != null) StopCoroutine(rotationCoroutine); // stop any existing rotation coroutines
            rotationCoroutine = StartCoroutine(LerpRotation(Quaternion.Euler(0f, 0f, 180f), duration)); // rotate 180 degrees

        }
    }

    public void ResetCamera() {

        // stop any existing rotation coroutines
        if (rotationCoroutine != null) {

            StopCoroutine(rotationCoroutine);
            rotationCoroutine = null;

        }

        gameCore.ResetGravity();
        camera.transform.rotation = Quaternion.identity;

    }

    private IEnumerator LerpRotation(Quaternion targetRotation, float duration) {

        Quaternion initialRotation = camera.transform.rotation;
        float currentTime = 0f;

        while (currentTime < duration) {

            camera.transform.rotation = Quaternion.Lerp(initialRotation, targetRotation, currentTime / duration);
            currentTime += Time.deltaTime;
            yield return null;

        }

        camera.transform.rotation = targetRotation; // make sure the final rotation is set
        rotationCoroutine = null;

    }

    public void SetTarget(Transform target) { this.target = target; }

}
