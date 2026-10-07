using UnityEngine;

public class IngredientDispenser : MonoBehaviour
{
    [Header("Ingredient")]
    [SerializeField] private GameObject ingredientPrefab;

    [Header("Spawn Location")]
    [SerializeField] private Transform spawnPoint;

    [Header("Settings")]
    [SerializeField] private float spawnCooldown = 0.25f;

    private float nextSpawnTime;

    public void Dispense()
    {
        if (ingredientPrefab == null)
        {
            Debug.LogWarning($"{name} has no ingredient prefab assigned.");
            return;
        }

        if (Time.time < nextSpawnTime)
            return;

        Transform point = spawnPoint != null ? spawnPoint : transform;

        Instantiate(
            ingredientPrefab,
            point.position,
            point.rotation
        );

        nextSpawnTime = Time.time + spawnCooldown;
    }
}
