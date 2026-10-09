using UnityEngine;

public class IngredientItem : MonoBehaviour
{
    [Header("Ingredient")]
    [SerializeField] private IngredientType ingredientType;

    [Header("Placed Version")]
    [SerializeField] private GameObject placedVisualPrefab;

    [SerializeField] private float stackHeight = 0.015f;

    [Header("Placed Visual Adjustment")]
    [SerializeField] private Vector3 placedLocalOffset = Vector3.zero;
    [SerializeField] private Vector3 placedLocalEulerAngles = Vector3.zero;

    public IngredientType Type => ingredientType;
    public GameObject PlacedVisualPrefab => placedVisualPrefab;
    public float StackHeight => stackHeight;
    public Vector3 PlacedLocalOffset => placedLocalOffset;
    public Vector3 PlacedLocalEulerAngles => placedLocalEulerAngles;

    public bool IsConsumed { get; private set; }

    private SandwichStation station;

    private void Awake()
    {
        station = FindFirstObjectByType<SandwichStation>();
    }

    public bool TryConsume()
    {
        if (IsConsumed)
            return false;

        IsConsumed = true;
        return true;
    }

    public void NotifyGrabbed()
    {
        if (IsConsumed)
            return;

        if (station == null)
            station = FindFirstObjectByType<SandwichStation>();

        station?.IngredientGrabbed(this);
    }

    public void NotifyReleased()
    {
        if (station == null)
            station = FindFirstObjectByType<SandwichStation>();

        station?.IngredientReleased(this);
    }
}