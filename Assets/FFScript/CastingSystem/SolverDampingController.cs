using UnityEngine;
using Obi;

public class SolverDampingController : MonoBehaviour
{
    private ObiSolver solver;

    [System.Serializable]
    public class DampingSetting
    {
        [Tooltip("The rope length at which this damping setting should be applied")]
        public float length;

        [Tooltip("The air resistance/drag (damping) value to apply at this rope length")]
        public float damping;

        [Tooltip("The downward pull (gravity) value to apply at this rope length")]
        public float gravityY;
    }

    [Header("Length-Based Damping Settings")]
    public DampingSetting firstDamping;
    public DampingSetting secondDamping;
    public DampingSetting thirdDamping;
    public DampingSetting fourthDamping;
    public DampingSetting fifthDamping;

    [Header("Highest Priority Setting (IfCastingDamping&Gravity)")]
    [Tooltip("The damping value to apply when casting (spacebar not held)")]
    public float castingDamping;

    [Tooltip("The downward pull (gravity) value to apply when casting (spacebar not held)")]
    public float castingGravityY;

    void Start()
    {
        solver = GetComponent<ObiSolver>();

        if (solver == null)
        {
            Debug.LogError("ObiSolver component not found on this GameObject.");
        }
    }

    void Update()
    {
        if (solver == null) return;

        bool isSpacePressed = Input.GetKey(KeyCode.Space);

        if (!isSpacePressed)
        {
            ApplyCastingDampingAndGravity();
        }
        else
        {

            ObiRope rope = GetFirstRope();
            if (rope == null) return;

            float currentLength = rope.restLength;

            UpdateDampingAndGravityBasedOnLength(currentLength);
        }
    }

    private ObiRope GetFirstRope()
    {
        foreach (var actor in solver.actors)
        {
            if (actor is ObiRope rope)
            {
                return rope;
            }
        }
        return null;
    }

    void UpdateDampingAndGravityBasedOnLength(float currentLength)
    {
        if (currentLength >= firstDamping.length && currentLength < secondDamping.length)
        {
            ApplyDampingAndGravity(firstDamping, "firstDamping");
        }
        else if (currentLength >= secondDamping.length && currentLength < thirdDamping.length)
        {
            ApplyDampingAndGravity(secondDamping, "secondDamping");
        }
        else if (currentLength >= thirdDamping.length && currentLength < fourthDamping.length)
        {
            ApplyDampingAndGravity(thirdDamping, "thirdDamping");
        }
        else if (currentLength >= fourthDamping.length && currentLength < fifthDamping.length)
        {
            ApplyDampingAndGravity(fourthDamping, "fourthDamping");
        }
        else if (currentLength >= fifthDamping.length)
        {
            ApplyDampingAndGravity(fifthDamping, "fifthDamping");
        }
    }

    void ApplyCastingDampingAndGravity()
    {
        solver.parameters.damping = castingDamping;
        solver.gravity = new Vector3(solver.gravity.x, castingGravityY, solver.gravity.z);
        solver.PushSolverParameters();
    }

    void ApplyDampingAndGravity(DampingSetting setting, string settingName)
    {
        solver.parameters.damping = setting.damping;
        solver.gravity = new Vector3(solver.gravity.x, setting.gravityY, solver.gravity.z);
        solver.PushSolverParameters();
    }
}
