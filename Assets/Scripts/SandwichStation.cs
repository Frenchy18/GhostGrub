using System;
using System.Collections.Generic;
using UnityEngine;

public class SandwichStation : MonoBehaviour
{
    [Header("Sandwich")]
    [SerializeField] private SandwichObject sandwichPrefab;
    [SerializeField] private Transform sandwichSpawnPoint;

    [Header("Stacking")]
    [SerializeField] private float extraSpacing = 0.01f;

    [Header("Ghost")]
    [SerializeField] private Transform ghostRoot;

    [SerializeField] private IngredientItem[] ingredientCatalog;

    private readonly List<IngredientType> assemblySteps = new List<IngredientType>();
    private GameObject nextLayerPreview;

    public event Action AssemblyChanged;
    public int CompletedSteps
    {
        get
        {
            if (currentSandwich == null)
                return 0;

            int count = Mathf.Min(currentSandwich.IngredientCount, assemblySteps.Count);
            int completed = 0;
            while (completed < count &&
                currentSandwich.Ingredients[completed] == assemblySteps[completed])
                completed++;
            return completed;
        }
    }
    public bool HasAssemblyMistake => currentSandwich != null &&
        currentSandwich.IngredientCount > CompletedSteps;
    public bool AssemblyComplete => assemblySteps.Count > 0 &&
        CompletedSteps == assemblySteps.Count && !HasAssemblyMistake;

    private SandwichObject currentSandwich;
    private IngredientItem heldIngredient;

    public bool CanTakeSandwich =>
        currentSandwich != null &&
        currentSandwich.HasIngredients;

    public void SetRecipe(List<IngredientType> ingredients)
    {
        assemblySteps.Clear();
        if (ingredients != null)
            assemblySteps.AddRange(ingredients);

        if (currentSandwich != null)
            currentSandwich.Discard();
        SpawnFreshSandwich();
        AssemblyChanged?.Invoke();
    }

    public Vector3 SandwichPickupPosition =>
        currentSandwich != null
            ? currentSandwich.PickupPosition
            : sandwichSpawnPoint.position;

    private void Start()
    {
        if (ghostRoot != null)
            ghostRoot.gameObject.SetActive(false);

        SpawnFreshSandwich();
    }

    private void SpawnFreshSandwich()
    {
        if (sandwichPrefab == null ||
            sandwichSpawnPoint == null)
        {
            Debug.LogError(
                "SandwichStation is missing its prefab or spawn point."
            );

            return;
        }

        currentSandwich =
            Instantiate(
                sandwichPrefab,
                sandwichSpawnPoint.position,
                sandwichSpawnPoint.rotation
            );

        currentSandwich.PrepareForStation();

        UpdateGhost();
        AssemblyChanged?.Invoke();
    }

    private void OnTriggerEnter(Collider other)
    {
        IngredientItem ingredient = null;

        if (other.attachedRigidbody != null)
        {
            ingredient =
                other.attachedRigidbody
                    .GetComponent<IngredientItem>();
        }

        if (ingredient == null)
        {
            ingredient =
                other.GetComponentInParent<IngredientItem>();
        }

        if (ingredient == null ||
            ingredient.IsConsumed)
        {
            return;
        }

        TryPlaceIngredient(ingredient);
    }

    private void TryPlaceIngredient(
        IngredientItem ingredient)
    {
        if (currentSandwich == null)
            return;

        if (ingredient.PlacedVisualPrefab == null ||
            ingredient.StackHeight <= 0f)
        {
            Debug.LogWarning(
                $"{ingredient.name} needs a Placed Visual Prefab and positive Stack Height."
            );

            return;
        }

        if (!ingredient.TryConsume())
            return;

        bool placed =
            currentSandwich.AddIngredient(
                ingredient,
                extraSpacing
            );

        if (!placed)
            return;

        if (heldIngredient == ingredient)
            heldIngredient = null;

        // Destroy the loose Meta-interactable version.
        // The sandwich now contains only its visual replacement.
        Destroy(ingredient.gameObject);

        UpdateGhost();
        AssemblyChanged?.Invoke();
    }

    public void IngredientGrabbed(
        IngredientItem ingredient)
    {
        if (ingredient == null ||
            ingredient.IsConsumed)
        {
            return;
        }

        heldIngredient = ingredient;

        UpdateGhost();
    }

    public void IngredientReleased(
        IngredientItem ingredient)
    {
        if (heldIngredient == ingredient)
            heldIngredient = null;

        UpdateGhost();
    }

    private void UpdateGhost()
    {
        UpdateNextLayerPreview();
    }

    private void UpdateNextLayerPreview()
    {
        if (nextLayerPreview != null)
            Destroy(nextLayerPreview);

        // Replace the generic held-part marker with the actual next ingredient,
        // visible before grabbing so its final position is always indicated.
        if (ghostRoot != null)
            ghostRoot.gameObject.SetActive(false);

        if (currentSandwich == null || AssemblyComplete || HasAssemblyMistake ||
            assemblySteps.Count == 0 || ingredientCatalog == null)
            return;

        foreach (IngredientItem ingredient in ingredientCatalog)
        {
            if (ingredient == null || ingredient.Type != assemblySteps[CompletedSteps] ||
                ingredient.PlacedVisualPrefab == null)
                continue;

            nextLayerPreview = Instantiate(ingredient.PlacedVisualPrefab,
                currentSandwich.transform);
            nextLayerPreview.name = "Next ingredient placement preview";
            Vector3 offset = ingredient.PlacedLocalOffset;
            nextLayerPreview.transform.localPosition = new Vector3(offset.x,
                currentSandwich.CurrentHeight + ingredient.StackHeight * 0.5f + offset.y,
                offset.z);
            nextLayerPreview.transform.localRotation = Quaternion.Euler(ingredient.PlacedLocalEulerAngles);

            foreach (Collider collider in nextLayerPreview.GetComponentsInChildren<Collider>())
                collider.enabled = false;

            Renderer ghostRenderer = ghostRoot != null ? ghostRoot.GetComponent<Renderer>() : null;
            if (ghostRenderer != null)
            {
                foreach (Renderer renderer in nextLayerPreview.GetComponentsInChildren<Renderer>())
                {
                    Material[] materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                        materials[i] = ghostRenderer.sharedMaterial;
                    renderer.sharedMaterials = materials;
                }
            }
            break;
        }
    }

    public bool TryTakeSandwich(
        Transform controllerAnchor,
        Vector3 holdLocalPosition,
        Quaternion holdLocalRotation,
        out SandwichObject sandwich)
    {
        sandwich = null;

        if (!CanTakeSandwich ||
            controllerAnchor == null)
        {
            return false;
        }

        sandwich = currentSandwich;

        sandwich.BeginHold(
            controllerAnchor,
            holdLocalPosition,
            holdLocalRotation
        );

        // The important part:
        // immediately make a fresh sandwich on the board.
        SpawnFreshSandwich();

        heldIngredient = null;

        UpdateGhost();

        return true;
    }
}
