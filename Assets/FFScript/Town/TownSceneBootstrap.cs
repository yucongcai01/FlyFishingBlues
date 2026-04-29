using UnityEngine;

[DefaultExecutionOrder(-200)]
public class TownSceneBootstrap : MonoBehaviour
{
    private const string AppearancePrefsKey = "MainCharacter.Appearance";
    private const string DefaultAppearanceId = "classic";
    private static readonly string[] FishingOnlyObjectNames =
    {
        "FlyLineSolver",
        "FlyLine",
        "SlackLineSolver",
        "SlackLine",
        "flyhook",
        "New FabrikSolver2D",
        "New FabrikSolver2D_Target",
        "bandIK"
    };

    [Header("Character")]
    [SerializeField] private UnityEngine.Object characterPrefab;
    [SerializeField] private RuntimeAnimatorController animatorController;
    [SerializeField] private AnimationClip idleClip;
    [SerializeField] private AnimationClip walkClip;
    [SerializeField] private Vector3 spawnPosition = new Vector3(0f, -0.8f, 4f);
    [SerializeField] private Vector3 spawnEulerAngles = Vector3.zero;
    [SerializeField] private float spawnScale = 0.7f;
    [SerializeField] private float moveSpeed = 2.25f;

    [Header("Animation")]
    [SerializeField] private string idleStatePath = "Base Layer.AnglerIdle";
    [SerializeField] private string walkStatePath = "Base Layer.Walk";
    [SerializeField] private float idlePlaybackSpeed = 1f;
    [SerializeField] private float walkPlaybackSpeed = 1f;
    [SerializeField] private bool mirrorFacingByMovement = true;

    [Header("Sprites")]
    [SerializeField] private Sprite classicAngler;
    [SerializeField] private Sprite forestAngler;
    [SerializeField] private Sprite retroAngler;
    [SerializeField] private Sprite femaleDarkAngler;
    [SerializeField] private Sprite classicHandRod;
    [SerializeField] private Sprite forestHandRod;
    [SerializeField] private Sprite retroHandRod;
    [SerializeField] private Sprite femaleDarkHandRod;

    private Camera sceneCamera;
    private Transform movementRoot;
    private Transform characterRoot;
    private Animator characterAnimator;
    private SpriteRenderer anglerRenderer;
    private SpriteRenderer handRodRenderer;
    private Quaternion facingRightRotation;
    private Quaternion facingLeftRotation;
    private Vector3 characterDefaultLocalPosition;
    private Vector3 characterDefaultLocalScale;
    private float idleNormalizedTime;
    private float walkNormalizedTime;
    private float walkDirection = 1f;
    private int idleStateHash;
    private int walkStateHash;

    private void Awake()
    {
        sceneCamera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
        idleStateHash = Animator.StringToHash(idleStatePath);
        walkStateHash = Animator.StringToHash(walkStatePath);
        SpawnCharacter();
    }

    private void Update()
    {
        if (characterRoot == null || characterAnimator == null)
        {
            return;
        }

        float moveInput = GetMoveInput();
        if (Mathf.Approximately(moveInput, 0f))
        {
            AdvanceIdle();
            return;
        }

        MoveCharacter(moveInput);
        AdvanceWalk();
    }

    private void SpawnCharacter()
    {
        if (characterPrefab == null)
        {
            Debug.LogError("TownSceneBootstrap is missing a character prefab reference.", this);
            return;
        }

        GameObject movementRootObject = new GameObject("TownCharacterMover");
        movementRoot = movementRootObject.transform;
        movementRoot.SetPositionAndRotation(spawnPosition, Quaternion.Euler(spawnEulerAngles));
        movementRoot.localScale = Vector3.one * spawnScale;
        facingRightRotation = movementRoot.rotation;
        facingLeftRotation = facingRightRotation * Quaternion.Euler(0f, 180f, 0f);

        Object instanceObject = Instantiate(characterPrefab);
        GameObject instance = ResolveInstanceObject(instanceObject);
        if (instance == null)
        {
            Debug.LogError(
                $"TownSceneBootstrap instantiated '{characterPrefab.GetType().Name}', but Unity returned '{instanceObject?.GetType().Name ?? "null"}' instead of a GameObject.",
                this);
            Destroy(movementRootObject);
            return;
        }

        instance.name = "TownCharacter";
        instance.transform.SetParent(movementRoot, false);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        DisableCopiedGameplayScripts(instance);
        DisableFishingOnlyObjects(instance);

        characterRoot = instance.transform;
        characterDefaultLocalPosition = characterRoot.localPosition;
        characterDefaultLocalScale = characterRoot.localScale;

        characterAnimator = instance.GetComponent<Animator>();
        if (characterAnimator == null)
        {
            characterAnimator = instance.AddComponent<Animator>();
        }

        if (animatorController != null)
        {
            characterAnimator.runtimeAnimatorController = animatorController;
        }

        characterAnimator.applyRootMotion = false;
        characterAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        characterAnimator.updateMode = AnimatorUpdateMode.Normal;
        characterAnimator.speed = 0f;
        characterAnimator.Rebind();
        characterAnimator.Update(0f);

        CacheSpriteRenderers(instance);
        ApplySelectedAppearance();
        SampleState(idleStateHash, 0f);
    }

    private static GameObject ResolveInstanceObject(Object instanceObject)
    {
        if (instanceObject is GameObject gameObjectAsset)
        {
            return gameObjectAsset;
        }

        if (instanceObject is Component componentAsset)
        {
            return componentAsset.gameObject;
        }

        return null;
    }

    private void DisableFishingOnlyObjects(GameObject instance)
    {
        for (int i = 0; i < FishingOnlyObjectNames.Length; i++)
        {
            DisableObjectsByName(instance.transform, FishingOnlyObjectNames[i]);
        }
    }

    private static void DisableObjectsByName(Transform root, string targetName)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            DisableObjectsByName(child, targetName);

            if (child.name != targetName)
            {
                continue;
            }

            child.gameObject.SetActive(false);
            MonoBehaviour[] behaviours = child.GetComponentsInChildren<MonoBehaviour>(true);
            for (int behaviourIndex = 0; behaviourIndex < behaviours.Length; behaviourIndex++)
            {
                if (behaviours[behaviourIndex] != null)
                {
                    behaviours[behaviourIndex].enabled = false;
                }
            }
        }
    }

    private void DisableCopiedGameplayScripts(GameObject instance)
    {
        MonoBehaviour[] behaviours = instance.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null)
            {
                continue;
            }

            if (behaviour.GetType().Assembly != typeof(TownSceneBootstrap).Assembly)
            {
                continue;
            }

            behaviour.enabled = false;
            Destroy(behaviour);
        }
    }

    private void CacheSpriteRenderers(GameObject instance)
    {
        SpriteRenderer[] renderers = instance.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (anglerRenderer == null && renderer.name == "Angler")
            {
                anglerRenderer = renderer;
            }
            else if (handRodRenderer == null && renderer.name == "HandRod")
            {
                handRodRenderer = renderer;
            }
        }
    }

    private void ApplySelectedAppearance()
    {
        string appearanceId = PlayerPrefs.GetString(AppearancePrefsKey, DefaultAppearanceId);
        Sprite anglerSprite = classicAngler;
        Sprite rodSprite = classicHandRod;

        switch (appearanceId)
        {
            case "forest-guide":
                anglerSprite = forestAngler != null ? forestAngler : classicAngler;
                rodSprite = forestHandRod != null ? forestHandRod : classicHandRod;
                break;
            case "retro-bucket":
                anglerSprite = retroAngler != null ? retroAngler : classicAngler;
                rodSprite = retroHandRod != null ? retroHandRod : classicHandRod;
                break;
            case "female-dark":
                anglerSprite = femaleDarkAngler != null ? femaleDarkAngler : classicAngler;
                rodSprite = femaleDarkHandRod != null ? femaleDarkHandRod : classicHandRod;
                break;
        }

        if (anglerRenderer != null && anglerSprite != null)
        {
            anglerRenderer.sprite = anglerSprite;
        }

        if (handRodRenderer != null && rodSprite != null)
        {
            handRodRenderer.sprite = rodSprite;
        }
    }

    private float GetMoveInput()
    {
        float input = 0f;
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            input -= 1f;
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            input += 1f;
        }

        return Mathf.Clamp(input, -1f, 1f);
    }

    private void MoveCharacter(float moveInput)
    {
        Vector3 horizontalAxis = ResolveScreenHorizontalAxis();
        if (movementRoot != null)
        {
            movementRoot.position += horizontalAxis * moveInput * moveSpeed * Time.deltaTime;
        }

        if (mirrorFacingByMovement)
        {
            Transform target = movementRoot != null ? movementRoot : characterRoot;
            if (target != null)
            {
                target.rotation = moveInput > 0f ? facingRightRotation : facingLeftRotation;
            }
        }
    }

    private Vector3 ResolveScreenHorizontalAxis()
    {
        if (sceneCamera == null)
        {
            return Vector3.right;
        }

        Vector3 horizontalAxis = Vector3.ProjectOnPlane(sceneCamera.transform.right, Vector3.up);
        if (horizontalAxis.sqrMagnitude < 0.0001f)
        {
            return Vector3.right;
        }

        return horizontalAxis.normalized;
    }

    private void AdvanceIdle()
    {
        float clipLength = GetClipLength(idleClip);
        idleNormalizedTime = Mathf.Repeat(
            idleNormalizedTime + (Time.deltaTime * idlePlaybackSpeed / clipLength),
            1f);
        SampleState(idleStateHash, idleNormalizedTime);
    }

    private void AdvanceWalk()
    {
        float clipLength = GetClipLength(walkClip);
        float delta = Time.deltaTime * walkPlaybackSpeed / clipLength;
        walkNormalizedTime += delta * walkDirection;

        if (walkNormalizedTime > 1f)
        {
            walkNormalizedTime = 1f - (walkNormalizedTime - 1f);
            walkDirection = -1f;
        }
        else if (walkNormalizedTime < 0f)
        {
            walkNormalizedTime = -walkNormalizedTime;
            walkDirection = 1f;
        }

        SampleState(walkStateHash, walkNormalizedTime);
    }

    private void SampleState(int stateHash, float normalizedTime)
    {
        if (characterAnimator == null || stateHash == 0)
        {
            return;
        }

        characterAnimator.Play(stateHash, 0, Mathf.Clamp01(normalizedTime));
        characterAnimator.Update(0f);
        StabilizeCharacterRoot();
    }

    private void StabilizeCharacterRoot()
    {
        if (characterRoot == null)
        {
            return;
        }

        characterRoot.localPosition = characterDefaultLocalPosition;
        characterRoot.localScale = characterDefaultLocalScale;
    }

    private static float GetClipLength(AnimationClip clip)
    {
        if (clip == null)
        {
            return 1f;
        }

        return Mathf.Max(clip.length, 0.01f);
    }
}
