using UnityEngine;

public class IngredientItem : MonoBehaviour
{
    [SerializeField] private IngredientType ingredientType;
    [SerializeField] private Rigidbody rb;

    [Header("Disable After Placement")]
    [SerializeField] private Behaviour[] disableWhenPlaced;

    public IngredientType Type => ingredientType;
    public bool IsPlaced { get; private set; }

    private void Reset()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void PlaceAt(Transform snapPoint)
    {
        if (IsPlaced || snapPoint == null)
            return;

        IsPlaced = true;

        // Stop grab/interact components.
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

        transform.SetPositionAndRotation(
            snapPoint.position,
            snapPoint.rotation
        );

        transform.SetParent(snapPoint, true);
    }
}
