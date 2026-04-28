using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class KillFishSceneAutoReturn : MonoBehaviour
{
    [SerializeField] private string fishName = "TroutWithJawfbx";
    [SerializeField] private string bucketName = "bucket";
    [SerializeField] private string cameraName = "Main Camera";
    [SerializeField] private string nextSceneName = "FishingScene";
    [SerializeField] private float startDelay = 0.5f;
    [SerializeField] private float cameraMoveDuration = 1.4f;
    [SerializeField] private float fishMoveDuration = 1.6f;
    [SerializeField] private float holdDuration = 0.8f;

    private void Start()
    {
        StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        yield return new WaitForSeconds(startDelay);

        GameObject fish = GameObject.Find(fishName);
        GameObject bucket = GameObject.Find(bucketName);
        Camera sceneCamera = FindSceneCamera();

        if (fish == null)
        {
            Debug.LogError($"KillFishSceneAutoReturn could not find fish '{fishName}'.");
            yield break;
        }

        if (bucket == null)
        {
            Debug.LogError($"KillFishSceneAutoReturn could not find bucket '{bucketName}'.");
            yield break;
        }

        Rigidbody fishRigidbody = fish.GetComponent<Rigidbody>();
        if (fishRigidbody != null)
        {
            fishRigidbody.isKinematic = true;
            fishRigidbody.velocity = Vector3.zero;
            fishRigidbody.angularVelocity = Vector3.zero;
        }

        MonoBehaviour[] fishBehaviours = fish.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour behaviour in fishBehaviours)
        {
            if (behaviour != null && behaviour != this)
            {
                behaviour.enabled = false;
            }
        }

        Vector3 bucketCenter = GetRendererCenter(bucket);
        Vector3 fishStartPosition = fish.transform.position;
        Quaternion fishStartRotation = fish.transform.rotation;
        Vector3 fishEndPosition = bucketCenter + Vector3.up * 0.35f;
        Quaternion fishEndRotation = Quaternion.Euler(0f, fishStartRotation.eulerAngles.y, 90f);

        Coroutine cameraRoutine = null;
        if (sceneCamera != null)
        {
            Vector3 viewDirection = (sceneCamera.transform.position - bucketCenter).normalized;
            if (viewDirection == Vector3.zero)
            {
                viewDirection = new Vector3(0f, 0.35f, -1f).normalized;
            }

            Vector3 cameraEndPosition = bucketCenter + viewDirection * 2.6f + Vector3.up * 0.8f;
            Quaternion cameraEndRotation = Quaternion.LookRotation(bucketCenter - cameraEndPosition, Vector3.up);
            cameraRoutine = StartCoroutine(MoveTransform(sceneCamera.transform, cameraEndPosition, cameraEndRotation, cameraMoveDuration));
        }

        Vector3 liftPosition = Vector3.Lerp(fishStartPosition, fishEndPosition, 0.45f) + Vector3.up * 1.0f;
        yield return MoveTransform(fish.transform, liftPosition, fishStartRotation, fishMoveDuration * 0.4f);
        yield return MoveTransform(fish.transform, fishEndPosition, fishEndRotation, fishMoveDuration * 0.6f);

        if (cameraRoutine != null)
        {
            yield return cameraRoutine;
        }

        yield return new WaitForSeconds(holdDuration);
        SceneManager.LoadScene(nextSceneName);
    }

    private Camera FindSceneCamera()
    {
        GameObject cameraObject = GameObject.Find(cameraName);
        if (cameraObject != null && cameraObject.TryGetComponent(out Camera namedCamera))
        {
            return namedCamera;
        }

        return Camera.main;
    }

    private static Vector3 GetRendererCenter(GameObject target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return target.transform.position;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds.center;
    }

    private static IEnumerator MoveTransform(Transform target, Vector3 endPosition, Quaternion endRotation, float duration)
    {
        Vector3 startPosition = target.position;
        Quaternion startRotation = target.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            target.position = Vector3.Lerp(startPosition, endPosition, t);
            target.rotation = Quaternion.Slerp(startRotation, endRotation, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        target.position = endPosition;
        target.rotation = endRotation;
    }
}
