using UnityEngine;

public class IngredientItem : MonoBehaviour
{
    [SerializeField] private IngredientType ingredientType;
    [SerializeField] private Rigidbody rb;

    [Header("Assembly")]
    [SerializeField] private float stackHeight = 0.015f;

    [Header("Disable After Placement")]
    [SerializeField] private Behaviour[] disableWhenPlaced;

    public IngredientType Type => ingredientType;
    public bool IsPlaced { get; private set; }
    public float StackHeight => stackHeight;

    private AssemblyManager assemblyManager;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        assemblyManager =
            FindFirstObjectByType<AssemblyManager>();
    }

    private void Reset()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void PlaceAt(
        Vector3 position,
        Quaternion rotation,
        Transform parent)
    {
        if (IsPlaced)
            return;

        IsPlaced = true;

        // Disable normal grab/interactable behaviour.
        foreach (Behaviour behaviour in disableWhenPlaced)
        {
            if (behaviour != null)
                behaviour.enabled = false;
        }

        if (rb != null)
        {
            // Meta may already have made this Rigidbody kinematic.
            // Only assign velocity if Unity currently allows it.
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            rb.useGravity = false;
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        transform.SetPositionAndRotation(
            position,
            rotation
        );

        if (parent != null)
            transform.SetParent(parent, true);

        // The sandwich root collider handles collision from this point on.
        Collider[] colliders =
            GetComponentsInChildren<Collider>(true);

        foreach (Collider itemCollider in colliders)
        {
            if (itemCollider != null)
                itemCollider.enabled = false;
        }

        // IMPORTANT:
        // Do NOT destroy the Rigidbody.
        // Meta's RigidbodyKinematicLocker depends on it.
    }

    public void NotifyGrabbed()
    {
        if (IsPlaced)
            return;

        assemblyManager?.IngredientGrabbed(this);
    }

    public void NotifyReleased()
    {
        assemblyManager?.IngredientReleased(this);
    }
}