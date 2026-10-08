using UnityEngine;

public class AssemblyManager : MonoBehaviour
{
    [Header("Stack")]
    [SerializeField] private Transform stackRoot;
    [SerializeField] private Transform snapPoint;
    [SerializeField] private BoxCollider assemblyTrigger;

    [Header("Spacing")]
    [SerializeField] private float extraSpacing = 0.001f;

    private float currentTopY;
    private int ingredientCount = 0;

    private void Start()
    {
        if (snapPoint != null)
        {
            currentTopY = snapPoint.position.y;
        }

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

        // The first ingredient must be bread.
        if (ingredientCount == 0 &&
            ingredient.Type != IngredientType.Bread)
        {
            return;
        }

        AddToStack(ingredient);
    }

    private void AddToStack(IngredientItem ingredient)
    {
        float height = ingredient.StackHeight;

        if (height <= 0f)
        {
            Debug.LogWarning(
                $"{ingredient.name} has an invalid StackHeight."
            );

            return;
        }

        float halfHeight = height * 0.5f;

        Vector3 targetPosition = snapPoint.position;

        // Position the ingredient so its bottom rests
        // directly on the current top of the sandwich.
        targetPosition.y =
            currentTopY + halfHeight;

        ingredient.PlaceAt(
            targetPosition,
            snapPoint.rotation,
            stackRoot
        );

        ingredientCount++;

        // New top of the sandwich.
        currentTopY += height + extraSpacing;

        // Move the placement indicator upward.
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
            transform.InverseTransformPoint(
                snapPoint.position
            );

        Vector3 center = assemblyTrigger.center;

        center.x = localSnapPosition.x;
        center.y = localSnapPosition.y;
        center.z = localSnapPosition.z;

        assemblyTrigger.center = center;
    }
}