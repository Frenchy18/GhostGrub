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
        {
            rb = GetComponent<Rigidbody>();
        }

        assemblyManager =
            FindFirstObjectByType<AssemblyManager>();
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

        foreach (Behaviour behaviour in disableWhenPlaced)
        {
            if (behaviour != null)
                behaviour.enabled = false;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        transform.SetPositionAndRotation(position, rotation);

        if (parent != null)
            transform.SetParent(parent, true);
    }
}