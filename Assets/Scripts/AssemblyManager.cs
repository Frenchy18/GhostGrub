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

    private float currentTopY;
    private int ingredientCount = 0;

    private readonly List<IngredientItem> heldIngredients =
        new List<IngredientItem>();

    private Material ghostMaterial;
    private float ghostVisibleAlpha;
    private Coroutine ghostFadeRoutine;

    private int ghostColorProperty = -1;

    private void Start()
    {
        if (snapPoint != null)
        {
            currentTopY = snapPoint.position.y;
        }

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

        // Move directly to the top of the placed ingredient.
        currentTopY += height + extraSpacing;

        Vector3 newSnapPosition = snapPoint.position;
        newSnapPosition.y = currentTopY;

        snapPoint.position = newSnapPosition;

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