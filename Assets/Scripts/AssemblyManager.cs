using UnityEngine;

public class AssemblyManager : MonoBehaviour
{
    [Header("Stack")]
    [SerializeField] private Transform stackRoot;
    [SerializeField] private Transform snapPoint;

    [SerializeField] private BoxCollider assemblyTrigger;

    [Header("Spacing")]
    [SerializeField] private float extraSpacing = 0.005f;

    private float currentTopY;

    private void Start()
    {
        if (snapPoint != null)
            currentTopY = snapPoint.position.y;

            UpdateTriggerPosition();
    }

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;

        if (rb == null)
            return;

        IngredientItem ingredient =
            rb.GetComponent<IngredientItem>();

        if (ingredient == null || ingredient.IsPlaced)
            return;

        AddToStack(ingredient);
    }

    private void AddToStack(IngredientItem ingredient)
    {
        Collider ingredientCollider =
            ingredient.GetComponentInChildren<Collider>();

        if (ingredientCollider == null)
            return;

        // Get the object's approximate vertical thickness.
        float halfHeight = ingredientCollider.bounds.extents.y;

        Vector3 targetPosition = snapPoint.position;

        targetPosition.y =
            currentTopY + halfHeight;

        ingredient.PlaceAt(
            targetPosition,
            snapPoint.rotation,
            stackRoot
        );

        // Recalculate after snapping.
        ingredientCollider =
            ingredient.GetComponentInChildren<Collider>();

        currentTopY =
            ingredientCollider.bounds.max.y + extraSpacing;

        // Move the visual/trigger snap point to the top.
        Vector3 newSnapPosition = snapPoint.position;
        newSnapPosition.y = currentTopY;
        snapPoint.position = newSnapPosition;

        UpdateTriggerPosition();
    }

    private void UpdateTriggerPosition()
    {
        if (assemblyTrigger == null || snapPoint == null)
            return;

        Vector3 localSnapPosition =
            transform.InverseTransformPoint(snapPoint.position);

        Vector3 center = assemblyTrigger.center;

        center.x = localSnapPosition.x;
        center.y = localSnapPosition.y;
        center.z = localSnapPosition.z;

        assemblyTrigger.center = center;
    }
}