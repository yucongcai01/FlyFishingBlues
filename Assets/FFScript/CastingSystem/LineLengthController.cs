using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Obi;

public class LineLengthController : MonoBehaviour
{
    private ObiRopeCursor ropeCursor;
    private ObiRope rope;

    [Header("Growth Settings")]
    public float initialLength = 5f;

    public float maxLength = 10f;
    public float growthAmount = 1f; // The amount by which the rope length increases when growing
    public float growthSpeed = 1f; // The speed at which the rope length changes when growing

    [Header("Retrieval Settings")]
    public float RetrieveSpeed = 1f; // The speed at which the rope length changes when retrieving
    public float RetrieveAmount = 1f; // The amount by which the rope length decreases when retrieving
    public float MinLength = 2f; // The minimum length to which the rope can be retrieved

    [Header("Landing Settings")]
    public float landingSpeed = 2f; // The speed at which the rope length changes when landing
    public float landingAmount = 2f; // The amount by which the rope length decreases when landing

    private bool isGrowing = false;
    private bool isRetrieving = false;
    private bool isLanding = false;
    private float targetLength;

    public Animator animator;

    private bool wasLiftRodPlaying = false;

    public FishStaminaBar fishStaminaBar;

    void Start()
    {
        ropeCursor = GetComponent<ObiRopeCursor>();
        rope = GetComponent<ObiRope>();
        ropeCursor.ChangeLength(initialLength);

        Debug.Log($"Initial Rope Length: {rope.restLength}");

        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError("Animator component not found on the GameObject.");
            }
        }

        if (fishStaminaBar == null)
        {
            fishStaminaBar = FindObjectOfType<FishStaminaBar>();
            if (fishStaminaBar == null)
            {
                Debug.LogError("FishStaminaBar component not found in the scene.");
            }
        }
    }

    void Update()
    {
        // Press D to grow the rope
        if (Input.GetKeyDown(KeyCode.D) && !isGrowing && !isRetrieving && !isLanding)
        {
            targetLength = Mathf.Min(rope.restLength + growthAmount, maxLength);
            if (targetLength > rope.restLength)
            {
                isGrowing = true;
            }
        }

        // Press S to retrieve the rope
        if (Input.GetKeyDown(KeyCode.S) && !isRetrieving && !isGrowing && !isLanding)
        {
            targetLength = Mathf.Max(rope.restLength - RetrieveAmount, MinLength);
            if (targetLength < rope.restLength)
            {
                isRetrieving = true;
            }
        }

        // Check if the "LiftRod" animation is playing to trigger landing
        if (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            bool isLiftRodPlaying = stateInfo.IsName("LiftRod");

            if (isLiftRodPlaying && !wasLiftRodPlaying && !isGrowing && !isRetrieving && !isLanding)
            {
                if (fishStaminaBar != null && fishStaminaBar.currentStamina <= 0)
                {
                    targetLength = Mathf.Max(rope.restLength - landingAmount, MinLength);
                    if (targetLength < rope.restLength)
                    {
                        isLanding = true;
                    }
                }
            }

            wasLiftRodPlaying = isLiftRodPlaying; // Update the previous state for the next frame
        }

        // Handle rope length changes for growth
        if (isGrowing)
        {
            if (rope.restLength < targetLength)
            {
                float changeAmount = growthSpeed * Time.deltaTime;
                ropeCursor.ChangeLength(Mathf.Min(changeAmount, targetLength - rope.restLength));
            }
            else
            {
                isGrowing = false;
                Debug.Log($"Rope Length after growth: {rope.restLength}");
            }
        }

        // Handle rope length changes for retrieval
        if (isRetrieving)
        {
            if (rope.restLength > targetLength)
            {
                float changeAmount = RetrieveSpeed * Time.deltaTime;
                ropeCursor.ChangeLength(-Mathf.Min(changeAmount, rope.restLength - targetLength));
            }
            else
            {
                isRetrieving = false;
                Debug.Log($"Rope Length after retrieval: {rope.restLength}");
            }
        }

        // Handle rope length changes for landing
        if (isLanding)
        {
            if (rope.restLength > targetLength)
            {
                float changeAmount = landingSpeed * Time.deltaTime;
                ropeCursor.ChangeLength(-Mathf.Min(changeAmount, rope.restLength - targetLength));
            }
            else
            {
                isLanding = false;
                Debug.Log($"Rope Length after landing: {rope.restLength}");
            }
        }
    }
}
