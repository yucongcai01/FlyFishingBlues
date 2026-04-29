using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Obi;

public class SlackLengthController : MonoBehaviour
{
    ObiRopeCursor cursor;
    ObiRope rope;

    [Header("Slack Settings")]
    public float speed = 1f; // The speed at which the rope length changes when swinging or retrieving
    public float smallSlack = 2f; // Target rope length when swinging right (Tight)
    public float largeSlack = 5f; // Target rope length when swinging left (Loose)
    public float maxLength = 15f; // Maximum rope length

    [Header("Retrieve Settings")]
    public float retrieveAmount = 1f;
    public string retrieveAnimationStateName = "Retrieve";

    public GameObject character;
    private Animator animator;

    private string swingLeftAnimationStateName = "SwingLeft";
    private string swingRightAnimationStateName = "SwingRight";

    private bool hasRetrieved = false;
    void Start()
    {
        cursor = GetComponentInChildren<ObiRopeCursor>();
        rope = cursor.GetComponent<ObiRope>();

        float initialChange = largeSlack - rope.restLength;
        cursor.ChangeLength(initialChange);

        if (character != null)
        {
            animator = character.GetComponent<Animator>();
        }
        else
        {
            Debug.LogError("Character GameObject is not assigned.");
        }
    }

    void Update()
    {
        if (animator == null) return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        // Case 1: Swinging right (Tightening the rope)
        if (stateInfo.IsName(swingRightAnimationStateName))
        {
            if (rope.restLength > smallSlack)
            {
                float changeAmount = -speed * Time.deltaTime;
                cursor.ChangeLength(Mathf.Max(changeAmount, smallSlack - rope.restLength));
            }
        }
        // Case 2: Swinging left (Loosening the rope)
        else if (stateInfo.IsName(swingLeftAnimationStateName))
        {
            if (rope.restLength < largeSlack)
            {
                float changeAmount = speed * Time.deltaTime;
                cursor.ChangeLength(Mathf.Min(changeAmount, largeSlack - rope.restLength));
            }
        }
        // Case 3: Retrieving the rope
        else if (stateInfo.IsName(retrieveAnimationStateName))
        {
            if (!hasRetrieved)
            {
                float newLength = Mathf.Min(rope.restLength + retrieveAmount, maxLength);
                float actualChange = newLength - rope.restLength;
                if (actualChange > 0)
                {
                    cursor.ChangeLength(actualChange);
                    Debug.Log($"Rope Length after Retrieve: {rope.restLength}");
                }
                hasRetrieved = true;
            }
        }
        // Case 4: No relevant animation playing, reset retrieval flag
        else
        {
            if (hasRetrieved)
            {
                hasRetrieved = false;
            }
        }
    }
}
