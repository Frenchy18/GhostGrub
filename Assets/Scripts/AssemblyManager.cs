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
    [SerializeField] private float sandwichColliderHorizontalPadding = 0.01f;

    private float currentTopY;
    private int ingredientCount;

    private readonly List<IngredientItem> heldIngredients =
        new List<IngredientItem>();

    private Material ghostMaterial;
    private float ghostVisibleAlpha;
    private Coroutine ghostFadeRoutine;
    private int ghostColorProperty = -1;

    private Vector3 snapOriginalLocalPosition;
    private Vector3 stackBaseWorldPosition;
    private IngredientType lastIngredientType;

    private void Start()
    {
        NormalizeBoardStackRoot();

        if (snapPoint != null)
        {
            snapOriginalLocalPosition = snapPoint.localPosition;
            stackBaseWorldPosition = snapPoint.position;
            currentTopY = snapPoint.position.y;
        }

        SetBoardSandwichPhysicsAvailable(false);
        UpdateTriggerPosition();
        SetupGhost();
    }

    private void NormalizeBoardStackRoot()
    {
        if (stackRoot == null)
            return;

        // AssemblyArea is non-uniformly scaled in this scene. A movable
        // Rigidbody should not live under that hierarchy.
        Vector3 worldPosition = stackRoot.position;
        Quaternion worldRotation = stackRoot.rotation;

        stackRoot.SetParent(null, true);
        stackRoot.SetPositionAndRotation(worldPosition, worldRotation);
        stackRoot.localScale = Vector3.one;

        if (sandwichRigidbody == null)
            sandwichRigidbody = stackRoot.GetComponent<Rigidbody>();

        if (sandwichCollider == null)
            sandwichCollider = stackRoot.GetComponent<BoxCollider>();
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

        // Every sandwich must begin with bread.
        if (ingredientCount == 0 &&
            ingredient.Type != IngredientType.Bread)
        {
            return;
        }

        AddToStack(ingredient);
    }

    private void AddToStack(IngredientItem ingredient)
    {
        if (snapPoint == null || stackRoot == null)
            return;

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
        targetPosition.y = currentTopY + halfHeight;

        heldIngredients.Remove(ingredient);

        ingredient.PlaceAt(
            targetPosition,
            snapPoint.rotation,
            stackRoot
        );

        ingredientCount++;
        lastIngredientType = ingredient.Type;

        currentTopY += height + extraSpacing;

        Vector3 newSnapPosition = snapPoint.position;
        newSnapPosition.y = currentTopY;
        snapPoint.position = newSnapPosition;

        FitColliderToContents(
            sandwichCollider,
            stackRoot,
            sandwichColliderHorizontalPadding,
            sandwichColliderPaddingY
        );

        if (ingredientCount == 1)
            SetBoardSandwichPhysicsAvailable(true);

        UpdateTriggerPosition();
        RefreshGhostVisibility();
    }

    public void IngredientGrabbed(IngredientItem ingredient)
    {
        if (ingredient == null || ingredient.IsPlaced)
            return;

        if (!heldIngredients.Contains(ingredient))
            heldIngredients.Add(ingredient);

        RefreshGhostVisibility();
    }

    public void IngredientReleased(IngredientItem ingredient)
    {
        if (ingredient == null)
            return;

        heldIngredients.Remove(ingredient);
        RefreshGhostVisibility();
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
            if (sandwichCollider != null &&
                sandwichCollider.enabled)
            {
                return sandwichCollider.bounds.center;
            }

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
            stackRoot == null)
        {
            return false;
        }

        IngredientItem[] ingredients =
            stackRoot.GetComponentsInChildren<IngredientItem>(true);

        if (ingredients.Length == 0)
            return false;

        heldIngredients.Clear();
        SetGhostVisible(false);

        // Create a NEW, unscaled world-space object for the sandwich that
        // leaves the board. The persistent StackRoot stays on the board.
        GameObject sandwichObject =
            new GameObject("Sandwich");

        sandwichObject.layer = stackRoot.gameObject.layer;

        Transform sandwichTransform = sandwichObject.transform;
        sandwichTransform.SetPositionAndRotation(
            stackRoot.position,
            stackRoot.rotation
        );
        sandwichTransform.localScale = Vector3.one;

        foreach (IngredientItem ingredient in ingredients)
        {
            if (ingredient != null)
            {
                ingredient.transform.SetParent(
                    sandwichTransform,
                    true
                );
            }
        }

        Rigidbody rb =
            sandwichObject.AddComponent<Rigidbody>();

        rb.mass = sandwichRigidbody != null
            ? sandwichRigidbody.mass
            : 0.25f;

        rb.linearDamping = 0f;
        rb.angularDamping = 0.05f;
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        BoxCollider collider =
            sandwichObject.AddComponent<BoxCollider>();

        collider.isTrigger = false;

        FitColliderToContents(
            collider,
            sandwichTransform,
            sandwichColliderHorizontalPadding,
            sandwichColliderPaddingY
        );

        SandwichObject runtimeSandwich =
            sandwichObject.AddComponent<SandwichObject>();

        runtimeSandwich.Initialize(rb, collider);
        runtimeSandwich.BeginHold(
            controllerAnchor,
            holdLocalPosition,
            holdLocalRotation
        );

        takenSandwich = runtimeSandwich;

        ResetBoardForNextSandwich();

        return true;
    }

    private void ResetBoardForNextSandwich()
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

        if (sandwichCollider != null)
        {
            sandwichCollider.center = Vector3.zero;
            sandwichCollider.size =
                new Vector3(0.01f, 0.01f, 0.01f);
        }

        SetBoardSandwichPhysicsAvailable(false);

        if (assemblyTrigger != null)
            assemblyTrigger.enabled = true;

        UpdateTriggerPosition();
        RefreshGhostVisibility();
    }

    private void SetBoardSandwichPhysicsAvailable(bool available)
    {
        if (sandwichCollider != null)
            sandwichCollider.enabled = available;

        if (sandwichRigidbody != null)
        {
            if (!sandwichRigidbody.isKinematic)
            {
                sandwichRigidbody.linearVelocity = Vector3.zero;
                sandwichRigidbody.angularVelocity = Vector3.zero;
            }

            sandwichRigidbody.useGravity = false;
            sandwichRigidbody.isKinematic = true;
        }
    }

    private static void FitColliderToContents(
        BoxCollider targetCollider,
        Transform root,
        float horizontalPadding,
        float verticalPadding)
    {
        if (targetCollider == null || root == null)
            return;

        Renderer[] renderers =
            root.GetComponentsInChildren<Renderer>(true);

        bool hasBounds = false;
        Bounds localBounds = new Bounds();

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled)
                continue;

            Bounds worldBounds = renderer.bounds;
            Vector3 min = worldBounds.min;
            Vector3 max = worldBounds.max;

            for (int x = 0; x <= 1; x++)
            {
                for (int y = 0; y <= 1; y++)
                {
                    for (int z = 0; z <= 1; z++)
                    {
                        Vector3 worldCorner = new Vector3(
                            x == 0 ? min.x : max.x,
                            y == 0 ? min.y : max.y,
                            z == 0 ? min.z : max.z
                        );

                        Vector3 localCorner =
                            root.InverseTransformPoint(worldCorner);

                        if (!hasBounds)
                        {
                            localBounds =
                                new Bounds(
                                    localCorner,
                                    Vector3.zero
                                );

                            hasBounds = true;
                        }
                        else
                        {
                            localBounds.Encapsulate(localCorner);
                        }
                    }
                }
            }
        }

        if (!hasBounds)
            return;

        targetCollider.center = localBounds.center;
        targetCollider.size =
            localBounds.size +
            new Vector3(
                horizontalPadding,
                verticalPadding,
                horizontalPadding
            );
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
                if (ingredient.Type == IngredientType.Bread)
                {
                    shouldShowGhost = true;
                    break;
                }
            }
            else
            {
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

        if (ghostVisibleAlpha <= 0.01f)
            ghostVisibleAlpha = 0.35f;

        color.a = 0f;
        ghostMaterial.SetColor(ghostColorProperty, color);
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
            StopCoroutine(ghostFadeRoutine);

        ghostFadeRoutine =
            StartCoroutine(FadeGhost(visible));
    }

    private IEnumerator FadeGhost(bool visible)
    {
        if (visible)
            ghostRenderer.enabled = true;

        Color color =
            ghostMaterial.GetColor(ghostColorProperty);

        float startAlpha = color.a;
        float targetAlpha =
            visible ? ghostVisibleAlpha : 0f;

        float elapsed = 0f;

        while (elapsed < ghostFadeDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / ghostFadeDuration
            );

            t = Mathf.SmoothStep(0f, 1f, t);

            color.a = Mathf.Lerp(
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
            ghostRenderer.enabled = false;

        ghostFadeRoutine = null;
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
