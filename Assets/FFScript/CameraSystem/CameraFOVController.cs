using System.Collections.Generic;
using UnityEngine;

public class CameraFOVController : MonoBehaviour
{
    [System.Serializable]
    public class AnimationFOVSettings
    {
        public string animationStateName;
        public float fovIncreaseAmount = 5f;
        public float fovSmoothTime = 0.5f;
        public float maxFOV = 90f;
    }

    public Animator characterAnimator;
    public List<AnimationFOVSettings> animationFOVSettingsList;

    private Camera targetCamera;
    private Dictionary<int, AnimationFOVSettings> animationSettingsDict;
    private float defaultFOV;
    private float targetFOV;
    private float fovVelocity;
    private int currentAnimationHash;
    private bool missingAnimatorLogged;
    private bool missingCameraLogged;

    private void Start()
    {
        TryResolveCamera();
        TryResolveAnimator();
        InitializeAnimationSettings();
    }

    private void Update()
    {
        if (!TryResolveCamera())
        {
            return;
        }

        if (animationSettingsDict == null)
        {
            InitializeAnimationSettings();
        }

        if (!TryResolveAnimator())
        {
            return;
        }

        AnimatorStateInfo stateInfo = characterAnimator.GetCurrentAnimatorStateInfo(0);
        int currentStateHash = stateInfo.shortNameHash;

        AnimationFOVSettings currentSettings = null;
        if (animationSettingsDict.TryGetValue(currentStateHash, out currentSettings))
        {
            if (currentAnimationHash != currentStateHash)
            {
                currentAnimationHash = currentStateHash;
                targetFOV = Mathf.Min(targetFOV + currentSettings.fovIncreaseAmount, currentSettings.maxFOV);
                fovVelocity = 0f;
            }
        }

        float smoothTime = currentSettings != null ? currentSettings.fovSmoothTime : 0.5f;
        targetCamera.fieldOfView = Mathf.SmoothDamp(targetCamera.fieldOfView, targetFOV, ref fovVelocity, smoothTime);
    }

    public void ResetCameraFOV()
    {
        targetFOV = defaultFOV;
    }

    private void InitializeAnimationSettings()
    {
        animationSettingsDict = new Dictionary<int, AnimationFOVSettings>();

        if (animationFOVSettingsList == null)
        {
            return;
        }

        foreach (AnimationFOVSettings settings in animationFOVSettingsList)
        {
            if (settings == null || string.IsNullOrWhiteSpace(settings.animationStateName))
            {
                continue;
            }

            int animationHash = Animator.StringToHash(settings.animationStateName);
            if (!animationSettingsDict.ContainsKey(animationHash))
            {
                animationSettingsDict.Add(animationHash, settings);
            }
            else
            {
                Debug.LogWarning($"Duplicate animation state '{settings.animationStateName}' was ignored.", this);
            }
        }
    }

    private bool TryResolveCamera()
    {
        if (targetCamera != null)
        {
            return true;
        }

        targetCamera = GetComponent<Camera>();
        if (targetCamera != null)
        {
            defaultFOV = targetCamera.fieldOfView;
            targetFOV = defaultFOV;
            missingCameraLogged = false;
            return true;
        }

        if (!missingCameraLogged)
        {
            Debug.LogError("CameraFOVController could not find a Camera on the same GameObject.", this);
            missingCameraLogged = true;
        }

        return false;
    }

    private bool TryResolveAnimator()
    {
        if (characterAnimator != null)
        {
            missingAnimatorLogged = false;
            return true;
        }

        characterAnimator = GetComponentInParent<Animator>();
        if (characterAnimator == null && transform.root != null)
        {
            characterAnimator = transform.root.GetComponentInChildren<Animator>(true);
        }

        if (characterAnimator == null)
        {
            GameObject playerObject = null;
            try
            {
                playerObject = GameObject.FindWithTag("Player");
            }
            catch (UnityException)
            {
            }

            if (playerObject != null)
            {
                characterAnimator = playerObject.GetComponentInChildren<Animator>(true);
            }
        }

        if (characterAnimator == null)
        {
            Animator[] animators = FindObjectsOfType<Animator>(true);
            foreach (Animator animator in animators)
            {
                if (animator != null && animator.runtimeAnimatorController != null && animator.isActiveAndEnabled)
                {
                    characterAnimator = animator;
                    break;
                }
            }

            if (characterAnimator == null)
            {
                foreach (Animator animator in animators)
                {
                    if (animator != null && animator.runtimeAnimatorController != null)
                    {
                        characterAnimator = animator;
                        break;
                    }
                }
            }
        }

        if (characterAnimator != null)
        {
            missingAnimatorLogged = false;
            return true;
        }

        if (!missingAnimatorLogged)
        {
            Debug.LogWarning("CameraFOVController could not resolve an Animator. FOV animation is temporarily disabled.", this);
            missingAnimatorLogged = true;
        }

        return false;
    }
}
