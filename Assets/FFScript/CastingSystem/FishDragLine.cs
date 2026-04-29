using UnityEngine;
using Obi;

public class FishDragLine : MonoBehaviour
{
    [Header("Speed Settings")]
    public float dragSpeed = 1f; // Speed when the fish pulls the line
    public float retrieveSpeed = 1f; // Speed when the player retrieves the line
    public float struggleSpeed = 1f; // Speed when the fish struggles
    public float pullSpeed = 1f; // Speed when the player pulls the line

    private ObiRope rope;
    private ObiRopeCursor ropeCursor;
    [Header("Current States")]
    public bool isDragging = false;
    public bool isRetrieving = false;
    public bool isStruggling = false;
    public bool isPulling = false;
    private Animator characterAnimator;

    void Start()
    {
        rope = GetComponent<ObiRope>();
        ropeCursor = GetComponent<ObiRopeCursor>();

        if (rope == null || ropeCursor == null)
        {
            Debug.LogError("FishDragLine: ObiRope or ObiRopeCursor component not found on the GameObject.");
        }
    }

    void Update()
    {
        // If dragging, retrieving, struggling, or pulling, adjust the rope length accordingly
        if (isDragging && rope != null && ropeCursor != null)
        {
            ExtendRope(dragSpeed);
        }
        if (isRetrieving && rope != null && ropeCursor != null)
        {
            ExtendRope(-retrieveSpeed);
        }
        if (isStruggling && rope != null && ropeCursor != null)
        {
            ExtendRope(struggleSpeed);
        }
        if (isPulling && rope != null && ropeCursor != null)
        {
            ExtendRope(-pullSpeed);
        }

        if (characterAnimator != null && characterAnimator.GetCurrentAnimatorStateInfo(0).IsName("SetTheHook"))
        {
            StopAllActions();
        }
    }

    public void StartDragging()
    {
        isDragging = true;
    }

    public void StopDragging()
    {
        isDragging = false;
    }

    public void StartRetrieving()
    {
        isRetrieving = true;
    }

    public void StopRetrieving()
    {
        isRetrieving = false;
    }

    public void StartStruggling()
    {
        isStruggling = true;
    }

    public void StopStruggling()
    {
        isStruggling = false;
    }

    public void StartPulling()
    {
        isPulling = true;
    }

    public void StopPulling()
    {
        isPulling = false;
    }

    public void StopAllActions()
    {
        isDragging = false;
        isRetrieving = false;
        isStruggling = false;
        isPulling = false;
    }

    private void ExtendRope(float speed)
    {
        // Rope length is changed by the specified speed, multiplied by Time.deltaTime for frame rate independence
        ropeCursor.ChangeLength(speed * Time.deltaTime);
        Debug.Log("Rope length changed by: " + speed * Time.deltaTime);
    }
}
