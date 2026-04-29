using UnityEngine;

public class AnimationSoundController : MonoBehaviour
{
    public Animator animator;
    public AudioSource audioSource;

    [System.Serializable]
    public class AnimationSoundPair
    {
        public string animationClipName;
        public AudioClip soundEffect;
    }

    public AnimationSoundPair[] animationSoundPairs;
    public AudioClip dragSoundEffect;
    public AudioSource dragAudioSource;
    public FishDragLine fishDragLine;

    private string currentClipName = "";
    private bool missingAnimatorLogged;
    private bool missingAudioSourceLogged;

    private void Update()
    {
        TryResolveAudioSources();

        if (!TryResolveAnimator())
        {
            return;
        }

        AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
        if (clipInfo.Length > 0)
        {
            string newClipName = clipInfo[0].clip.name;
            if (currentClipName != newClipName)
            {
                currentClipName = newClipName;

                if (animationSoundPairs != null)
                {
                    foreach (AnimationSoundPair pair in animationSoundPairs)
                    {
                        if (pair != null && pair.animationClipName == newClipName)
                        {
                            if (audioSource != null && pair.soundEffect != null)
                            {
                                audioSource.clip = pair.soundEffect;
                                audioSource.Play();
                            }

                            break;
                        }
                    }
                }
            }
        }

        if (fishDragLine != null && dragSoundEffect != null && dragAudioSource != null)
        {
            if (fishDragLine.isDragging || fishDragLine.isStruggling)
            {
                if (!dragAudioSource.isPlaying)
                {
                    dragAudioSource.clip = dragSoundEffect;
                    dragAudioSource.Play();
                }
            }
            else if (dragAudioSource.isPlaying)
            {
                dragAudioSource.Stop();
            }
        }
    }

    private void TryResolveAudioSources()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = GetComponentInChildren<AudioSource>(true);
            }
        }

        if (dragAudioSource == null)
        {
            dragAudioSource = audioSource;
        }

        if (audioSource != null)
        {
            missingAudioSourceLogged = false;
            return;
        }

        if (!missingAudioSourceLogged)
        {
            Debug.LogWarning("AnimationSoundController could not resolve an AudioSource. Animation audio is disabled.", this);
            missingAudioSourceLogged = true;
        }
    }

    private bool TryResolveAnimator()
    {
        if (animator != null)
        {
            missingAnimatorLogged = false;
            return true;
        }

        animator = GetComponentInParent<Animator>();
        if (animator == null && transform.root != null)
        {
            animator = transform.root.GetComponentInChildren<Animator>(true);
        }

        if (animator == null)
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
                animator = playerObject.GetComponentInChildren<Animator>(true);
            }
        }

        if (animator == null)
        {
            Animator[] animators = FindObjectsOfType<Animator>(true);
            foreach (Animator candidate in animators)
            {
                if (candidate != null && candidate.runtimeAnimatorController != null && candidate.isActiveAndEnabled)
                {
                    animator = candidate;
                    break;
                }
            }
        }

        if (animator != null)
        {
            missingAnimatorLogged = false;
            return true;
        }

        if (!missingAnimatorLogged)
        {
            Debug.LogWarning("AnimationSoundController could not resolve an Animator.", this);
            missingAnimatorLogged = true;
        }

        return false;
    }
}
