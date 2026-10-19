using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CustomerDeliveryZone : MonoBehaviour
{
    [Header("Required Recipe")]
    [SerializeField]
    private List<IngredientType> requiredIngredients =
        new List<IngredientType>();

    [Header("Delivery")]
    [SerializeField]
    private bool destroySandwichAfterDelivery = true;

    [Header("Reactions")]
    [SerializeField]
    private UnityEvent onCorrectOrder;

    [SerializeField]
    private UnityEvent onWrongOrder;

    private void OnTriggerEnter(Collider other)
    {
        SandwichObject sandwich =
            other.GetComponentInParent<SandwichObject>();

        if (sandwich == null)
            return;

        bool correct =
            MatchesRecipe(sandwich);

        if (correct)
        {
            Debug.Log("Correct sandwich delivered!");
            onCorrectOrder?.Invoke();
        }
        else
        {
            Debug.Log("Wrong sandwich delivered!");
            onWrongOrder?.Invoke();
        }

        if (destroySandwichAfterDelivery)
        {
            sandwich.Discard();
        }
    }

    private bool MatchesRecipe(
        SandwichObject sandwich)
    {
        IReadOnlyList<IngredientType> actual =
            sandwich.Ingredients;

        if (actual.Count != requiredIngredients.Count)
            return false;

        for (int i = 0;
             i < requiredIngredients.Count;
             i++)
        {
            if (actual[i] != requiredIngredients[i])
                return false;
        }

        return true;
    }
}
