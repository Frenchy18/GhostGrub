using System.Collections.Generic;
using UnityEngine;

public class SandwichObject : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform layerRoot;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private BoxCollider sandwichCollider;

    [Header("Collider")]
    [SerializeField] private float colliderVerticalPadding = 0.01f;

    private readonly List<IngredientType> ingredients =
        new List<IngredientType>();

    private float currentHeight;

    private Transform holdAnchor;
    private Vector3 holdLocalPosition;
    private Quaternion holdLocalRotation;

    private bool isHeld;

    private Vector3 originalColliderCenter;

    public bool HasIngredients => ingredients.Count > 0;
    public int IngredientCount => ingredients.Count;
    public float CurrentHeight => currentHeight;

    public IReadOnlyList<IngredientType> Ingredients => ingredients;

    public IngredientType LastIngredient =>
        ingredients.Count > 0
            ? ingredients[ingredients.Count - 1]
            : default;

    public bool IsClosedSandwich =>
        ingredients.Count > 1 &&
        LastIngredient == IngredientType.Bread;

    public Vector3 PickupPosition
    {
        get
        {
            if (sandwichCollider != null &&
                sandwichCollider.enabled)
            {
                return sandwichCollider.bounds.center;
            }

            return transform.position;
        }
    }

    public Vector3 NextLayerPosition =>
        transform.TransformPoint(
            new Vector3(0f, currentHeight, 0f)
        );

    private void Awake()
    {
        if (layerRoot == null)
            layerRoot = transform;

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (sandwichCollider == null)
            sandwichCollider = GetComponent<BoxCollider>();

        if (sandwichCollider != null)
        {
            originalColliderCenter =
                sandwichCollider.center;
        }
    }

    public void PrepareForStation()
    {
        isHeld = false;
        holdAnchor = null;

        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        if (sandwichCollider != null)
            sandwichCollider.enabled = false;
    }

    public bool AddIngredient(
        IngredientItem ingredient,
        float extraSpacing)
    {
        if (ingredient == null ||
            ingredient.PlacedVisualPrefab == null)
        {
            return false;
        }

        float height = ingredient.StackHeight;

        if (height <= 0f)
            return false;

        GameObject placedVisual =
            Instantiate(
                ingredient.PlacedVisualPrefab,
                layerRoot
            );

        Vector3 offset =
            ingredient.PlacedLocalOffset;

        placedVisual.transform.localPosition =
            new Vector3(
                offset.x,
                currentHeight + height * 0.5f + offset.y,
                offset.z
            );

        placedVisual.transform.localRotation =
            Quaternion.Euler(
                ingredient.PlacedLocalEulerAngles
            );

        ingredients.Add(ingredient.Type);

        currentHeight += height + extraSpacing;

        UpdateCollider(extraSpacing);

        return true;
    }

    private void UpdateCollider(float extraSpacing)
    {
        if (sandwichCollider == null)
            return;

        float physicalHeight =
            Mathf.Max(
                0.01f,
                currentHeight - extraSpacing
            );

        Vector3 size =
            sandwichCollider.size;

        size.y =
            physicalHeight +
            colliderVerticalPadding;

        sandwichCollider.size = size;

        Vector3 center =
            originalColliderCenter;

        center.y = physicalHeight * 0.5f;

        sandwichCollider.center = center;
        sandwichCollider.enabled = true;
    }

    public void BeginHold(
        Transform controllerAnchor,
        Vector3 localPosition,
        Quaternion localRotation)
    {
        if (controllerAnchor == null)
            return;

        holdAnchor = controllerAnchor;
        holdLocalPosition = localPosition;
        holdLocalRotation = localRotation;

        isHeld = true;

        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        // Prevent the sandwich from physically fighting the hand.
        if (sandwichCollider != null)
            sandwichCollider.enabled = false;

        UpdateHeldPose();
    }

    private void LateUpdate()
    {
        if (isHeld)
            UpdateHeldPose();
    }

    private void UpdateHeldPose()
    {
        if (holdAnchor == null)
            return;

        Vector3 targetPosition =
            holdAnchor.TransformPoint(
                holdLocalPosition
            );

        Quaternion targetRotation =
            holdAnchor.rotation *
            holdLocalRotation;

        transform.SetPositionAndRotation(
            targetPosition,
            targetRotation
        );
    }

    public void Launch(
        Vector3 linearVelocity,
        Vector3 angularVelocity)
    {
        if (!isHeld)
            return;

        UpdateHeldPose();

        isHeld = false;
        holdAnchor = null;

        if (sandwichCollider != null)
            sandwichCollider.enabled = true;

        if (rb == null)
            return;

        rb.isKinematic = false;
        rb.useGravity = true;

        rb.linearVelocity = linearVelocity;
        rb.angularVelocity = angularVelocity;
    }

    public void Discard()
    {
        Destroy(gameObject);
    }
}