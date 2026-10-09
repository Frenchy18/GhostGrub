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

    private SandwichObject currentSandwich;
    private IngredientItem heldIngredient;

    public bool CanTakeSandwich =>
        currentSandwich != null &&
        currentSandwich.HasIngredients;

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

        // Every sandwich must start with Bread.
        if (!currentSandwich.HasIngredients &&
            ingredient.Type != IngredientType.Bread)
        {
            return;
        }

        if (ingredient.PlacedVisualPrefab == null)
        {
            Debug.LogWarning(
                $"{ingredient.name} has no Placed Visual Prefab."
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
        if (ghostRoot == null ||
            currentSandwich == null)
        {
            return;
        }

        bool validIngredient =
            heldIngredient != null &&
            !heldIngredient.IsConsumed;

        if (validIngredient &&
            !currentSandwich.HasIngredients)
        {
            validIngredient =
                heldIngredient.Type ==
                IngredientType.Bread;
        }

        if (!validIngredient)
        {
            ghostRoot.gameObject.SetActive(false);
            return;
        }

        float halfHeight =
            heldIngredient.StackHeight * 0.5f;

        ghostRoot.position =
            currentSandwich.NextLayerPosition +
            currentSandwich.transform.up *
            halfHeight;

        ghostRoot.rotation =
            currentSandwich.transform.rotation;

        ghostRoot.gameObject.SetActive(true);
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
