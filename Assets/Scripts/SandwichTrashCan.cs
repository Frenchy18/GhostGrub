using UnityEngine;

public class SandwichTrashCan : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        SandwichObject sandwich =
            other.GetComponentInParent<SandwichObject>();

        if (sandwich != null)
        {
            sandwich.Discard();
        }

        IngredientItem ingredient =
            other.GetComponentInParent<IngredientItem>();

        if (ingredient != null)
        {
            Destroy(ingredient.gameObject);
        }
    }
}
