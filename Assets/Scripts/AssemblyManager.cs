using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssemblyManager : MonoBehaviour
{
    [Header("Stack")]
    [SerializeField] private Transform stackRoot;
    [SerializeField] private Transform snapPoint;
    [SerializeField] private BoxCollider assemblyTrigger;

    [Header("Spacing")]
    [SerializeField] private float extraSpacing = 0f;

    [Header("Ghost Indicator")]
    [SerializeField] private Renderer ghostRenderer;
    [SerializeField] private float ghostFadeDuration = 0.12f;

    [Header("Sandwich Pickup")]
    [SerializeField] private Rigidbody sandwichRigidbody;
    [SerializeField] private BoxCollider sandwichCollider;
    [SerializeField] private float sandwichColliderPaddingY = 0.01f;

    private float currentTopY;
    private int ingredientCount = 0;

    private readonly List<IngredientItem> heldIngredients =
        new List<IngredientItem>();

    private Material ghostMaterial;
    private float ghostVisibleAlpha;
    private Coroutine ghostFadeRoutine;

    private int ghostColorProperty = -1;

    private Transform stackOriginalParent;
    private Vector3 stackOriginalLocalPosition;
    private Quaternion stackOriginalLocalRotation;
    private Vector3 stackOriginalLocalScale;
    private Vector3 snapOriginalLocalPosition;
    private Vector3 stackBaseWorldPosition;

    private IngredientType lastIngredientType;

    // Template values used to create a fresh empty StackRoot every time
    // the player takes a sandwich away from the cutting board.
    private string stackRootName;
    private int stackRootLayer;

    private Vector3 baseColliderCenter;
    private Vector3 baseColliderSize;

    private float baseRigidbodyMass;
    private float baseLinearDamping;
    private float baseAngularDamping;
    private RigidbodyInterpolation baseInterpolation;
    private CollisionDetectionMode baseCollisionDetection;
    private RigidbodyConstraints baseConstraints;

    private void Start()
    {
        if (stackRoot != null)
        {
            stackOriginalParent = stackRoot.parent;
            stackOriginalLocalPosition = stackRoot.localPosition;
            stackOriginalLocalRotation = stackRoot.localRotation;
            stackOriginalLocalScale = stackRoot.localScale;

            stackRootName = stackRoot.name;
            stackRootLayer = stackRoot.gameObject.layer;
        }

        if (sandwichCollider != null)
        {
            baseColliderCenter = sandwichCollider.center;
            baseColliderSize = sandwichCollider.size;
        }

        if (sandwichRigidbody != null)
        {
            baseRigidbodyMass = sandwichRigidbody.mass;
            baseLinearDamping = sandwichRigidbody.linearDamping;
            baseAngularDamping = sandwichRigidbody.angularDamping;
            baseInterpolation = sandwichRigidbody.interpolation;
            baseCollisionDetection = sandwichRigidbody.collisionDetectionMode;
            baseConstraints = sandwichRigidbody.constraints;
        }

        if (snapPoint != null)
        {
            snapOriginalLocalPosition = snapPoint.localPosition;
            stackBaseWorldPosition = snapPoint.position;
            currentTopY = snapPoint.position.y;
        }

        SetSandwichPhysicsAvailable(false);
        UpdateTriggerPosition();
        SetupGhost();
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

        // Bottom layer must be bread.
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

        targetPosition.y =
            currentTopY + halfHeight;

        // It is no longer considered held once it snaps.
        heldIngredients.Remove(ingredient);

        ingredient.PlaceAt(
            targetPosition,
            snapPoint.rotation,
            stackRoot
        );

        ingredientCount++;
        lastIngredientType = ingredient.Type;

        // Move directly to the top of the placed ingredient.
        currentTopY += height + extraSpacing;

        Vector3 newSnapPosition = snapPoint.position;
        newSnapPosition.y = currentTopY;

        snapPoint.position = newSnapPosition;

        UpdateSandwichCollider();

        // The whole sandwich becomes eligible for trigger pickup as soon
        // as the first bread is placed.
        if (ingredientCount == 1)
        {
            SetSandwichPhysicsAvailable(true);
        }

        UpdateTriggerPosition();
        RefreshGhostVisibility();
    }

    public void IngredientGrabbed(IngredientItem ingredient)
    {
        if (ingredient == null || ingredient.IsPlaced)
            return;

        if (!heldIngredients.Contains(ingredient))
        {
            heldIngredients.Add(ingredient);
        }

        RefreshGhostVisibility();
    }

    public void IngredientReleased(IngredientItem ingredient)
    {
        if (ingredient == null)
            return;

        heldIngredients.Remove(ingredient);

        RefreshGhostVisibility();
    }

    private void RefreshGhostVisibility()
    {
        heldIngredients.RemoveAll(
            item => item == null || item.IsPlaced
        );

        bool shouldShowGhost = false;

        foreach (IngredientItem ingredient in heldIngredients)
        {
            if (ingredientCount == 0)
            {
                // Empty stack:
                // only Bread is a valid placement.
                if (ingredient.Type == IngredientType.Bread)
                {
                    shouldShowGhost = true;
                    break;
                }
            }
            else
            {
                // Once Bread is down,
                // any ingredient is allowed.
                shouldShowGhost = true;
                break;
            }
        }

        SetGhostVisible(shouldShowGhost);
    }

    private void SetupGhost()
    {
        if (ghostRenderer == null)
            return;

        ghostRenderer.gameObject.SetActive(true);

        // Creates a unique runtime material for this ghost.
        ghostMaterial = ghostRenderer.material;

        if (ghostMaterial.HasProperty("_BaseColor"))
        {
            ghostColorProperty =
                Shader.PropertyToID("_BaseColor");
        }
        else if (ghostMaterial.HasProperty("_Color"))
        {
            ghostColorProperty =
                Shader.PropertyToID("_Color");
        }

        if (ghostColorProperty == -1)
        {
            Debug.LogWarning(
                "Ghost material has no supported color property."
            );

            ghostRenderer.enabled = false;
            return;
        }

        Color color =
            ghostMaterial.GetColor(ghostColorProperty);

        ghostVisibleAlpha = color.a;

        // Fallback in case material alpha was accidentally zero.
        if (ghostVisibleAlpha <= 0.01f)
        {
            ghostVisibleAlpha = 0.35f;
        }

        color.a = 0f;

        ghostMaterial.SetColor(
            ghostColorProperty,
            color
        );

        ghostRenderer.enabled = false;
    }

    private void SetGhostVisible(bool visible)
    {
        if (ghostRenderer == null ||
            ghostMaterial == null ||
            ghostColorProperty == -1)
        {
            return;
        }

        if (ghostFadeRoutine != null)
        {
            StopCoroutine(ghostFadeRoutine);
        }

        ghostFadeRoutine =
            StartCoroutine(FadeGhost(visible));
    }

    private IEnumerator FadeGhost(bool visible)
    {
        if (visible)
        {
            ghostRenderer.enabled = true;
        }

        Color color =
            ghostMaterial.GetColor(ghostColorProperty);

        float startAlpha = color.a;
        float targetAlpha =
            visible ? ghostVisibleAlpha : 0f;

        float elapsed = 0f;

        while (elapsed < ghostFadeDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / ghostFadeDuration
                );

            // Slightly smoother than a straight linear fade.
            t = Mathf.SmoothStep(0f, 1f, t);

            color.a =
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    t
                );

            ghostMaterial.SetColor(
                ghostColorProperty,
                color
            );

            yield return null;
        }

        color.a = targetAlpha;

        ghostMaterial.SetColor(
            ghostColorProperty,
            color
        );

        if (!visible)
        {
            ghostRenderer.enabled = false;
        }

        ghostFadeRoutine = null;
    }


    public bool IsClosedSandwich =>
        ingredientCount > 1 &&
        lastIngredientType == IngredientType.Bread;

    public bool CanTakeSandwich =>
        ingredientCount > 0;

    public bool HasSandwich =>
        ingredientCount > 0;

    public Vector3 SandwichPickupPosition
    {
        get
        {
            if (sandwichCollider != null && sandwichCollider.enabled)
                return sandwichCollider.bounds.center;

            return stackRoot != null
                ? stackRoot.position
                : transform.position;
        }
    }

    public bool TryTakeSandwich(
        Transform controllerAnchor,
        Vector3 holdLocalPosition,
        Quaternion holdLocalRotation,
        out SandwichObject takenSandwich)
    {
        takenSandwich = null;

        if (!CanTakeSandwich ||
            controllerAnchor == null ||
            stackRoot == null ||
            sandwichRigidbody == null)
        {
            return false;
        }

        heldIngredients.Clear();
        SetGhostVisible(false);

        // This StackRoot becomes the sandwich that leaves the board.
        GameObject takenObject = stackRoot.gameObject;
        takenObject.name = "Sandwich";

        SandwichObject sandwichObject =
            takenObject.GetComponent<SandwichObject>();

        if (sandwichObject == null)
            sandwichObject = takenObject.AddComponent<SandwichObject>();

        sandwichObject.Initialize(sandwichRigidbody);

        // Snap the sandwich to a predictable position near the controller
        // instead of preserving its old cutting-board offset.
        sandwichObject.AttachTo(
            controllerAnchor,
            holdLocalPosition,
            holdLocalRotation
        );

        takenSandwich = sandwichObject;

        // Immediately give the cutting board a brand-new empty StackRoot.
        CreateFreshStackRoot();
        ResetAssemblyForNextSandwich();

        return true;
    }

    private void CreateFreshStackRoot()
    {
        GameObject freshRoot =
            new GameObject(
                string.IsNullOrEmpty(stackRootName)
                    ? "StackRoot"
                    : stackRootName
            );

        freshRoot.layer = stackRootLayer;

        Transform freshTransform = freshRoot.transform;
        freshTransform.SetParent(stackOriginalParent, false);
        freshTransform.localPosition = stackOriginalLocalPosition;
        freshTransform.localRotation = stackOriginalLocalRotation;
        freshTransform.localScale = stackOriginalLocalScale;

        Rigidbody freshRigidbody =
            freshRoot.AddComponent<Rigidbody>();

        freshRigidbody.mass =
            baseRigidbodyMass > 0f
                ? baseRigidbodyMass
                : 0.25f;

        freshRigidbody.linearDamping = baseLinearDamping;
        freshRigidbody.angularDamping = baseAngularDamping;
        freshRigidbody.interpolation = baseInterpolation;
        freshRigidbody.collisionDetectionMode = baseCollisionDetection;
        freshRigidbody.constraints = baseConstraints;
        freshRigidbody.useGravity = false;
        freshRigidbody.isKinematic = true;

        BoxCollider freshCollider =
            freshRoot.AddComponent<BoxCollider>();

        freshCollider.center = baseColliderCenter;
        freshCollider.size = baseColliderSize;
        freshCollider.isTrigger = false;
        freshCollider.enabled = false;

        stackRoot = freshTransform;
        sandwichRigidbody = freshRigidbody;
        sandwichCollider = freshCollider;
    }

    private void ResetAssemblyForNextSandwich()
    {
        ingredientCount = 0;
        lastIngredientType = default;
        heldIngredients.Clear();

        if (snapPoint != null)
        {
            snapPoint.localPosition = snapOriginalLocalPosition;
            stackBaseWorldPosition = snapPoint.position;
            currentTopY = snapPoint.position.y;
        }

        SetSandwichPhysicsAvailable(false);

        if (assemblyTrigger != null)
            assemblyTrigger.enabled = true;

        UpdateTriggerPosition();
        RefreshGhostVisibility();
    }

    private void SetSandwichPhysicsAvailable(bool available)
    {
        if (sandwichCollider != null)
            sandwichCollider.enabled = available;

        if (sandwichRigidbody != null)
        {
            sandwichRigidbody.linearVelocity = Vector3.zero;
            sandwichRigidbody.angularVelocity = Vector3.zero;
            sandwichRigidbody.useGravity = false;
            sandwichRigidbody.isKinematic = true;
        }
    }

    private void UpdateSandwichCollider()
    {
        if (stackRoot == null ||
            sandwichCollider == null ||
            snapPoint == null)
        {
            return;
        }

        Vector3 topWorldPosition = stackBaseWorldPosition;
        topWorldPosition.y =
            Mathf.Max(stackBaseWorldPosition.y, currentTopY - extraSpacing);

        Vector3 localBottom =
            stackRoot.InverseTransformPoint(stackBaseWorldPosition);

        Vector3 localTop =
            stackRoot.InverseTransformPoint(topWorldPosition);

        float minY = Mathf.Min(localBottom.y, localTop.y);
        float maxY = Mathf.Max(localBottom.y, localTop.y);

        Vector3 center = sandwichCollider.center;
        center.y = (minY + maxY) * 0.5f;
        sandwichCollider.center = center;

        Vector3 size = sandwichCollider.size;
        size.y = Mathf.Max(
            0.01f,
            (maxY - minY) + sandwichColliderPaddingY
        );
        sandwichCollider.size = size;
    }

    private void UpdateTriggerPosition()
    {
        if (assemblyTrigger == null ||
            snapPoint == null)
        {
            return;
        }

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