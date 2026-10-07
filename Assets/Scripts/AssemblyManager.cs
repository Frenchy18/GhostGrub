using System;
using UnityEngine;
using UnityEngine.Events;

public class AssemblyManager : MonoBehaviour
{
    [Serializable]
    public class AssemblyStep
    {
        public IngredientType requiredIngredient;

        [Tooltip("Exact position/rotation for this ingredient.")]
        public Transform snapPoint;

        [Tooltip("Ghost/outline showing where this ingredient belongs.")]
        public GameObject ghostVisual;
    }

    [Header("Recipe")]
    [SerializeField] private AssemblyStep[] steps;

    [Header("Events")]
    [SerializeField] private UnityEvent onAssemblyComplete;

    private int currentStep = 0;

    private void Start()
    {
        RefreshGhosts();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (currentStep >= steps.Length)
            return;

        IngredientItem ingredient = null;

        if (other.attachedRigidbody != null)
        {
            ingredient =
                other.attachedRigidbody.GetComponent<IngredientItem>();
        }

        if (ingredient == null)
        {
            ingredient =
                other.GetComponentInParent<IngredientItem>();
        }

        if (ingredient == null || ingredient.IsPlaced)
            return;

        AssemblyStep step = steps[currentStep];

        // Wrong ingredient.
        if (ingredient.Type != step.requiredIngredient)
            return;

        // Correct ingredient.
        ingredient.PlaceAt(step.snapPoint);

        currentStep++;

        RefreshGhosts();

        if (currentStep >= steps.Length)
        {
            Debug.Log("Assembly complete!");
            onAssemblyComplete?.Invoke();
        }
    }

    private void RefreshGhosts()
    {
        for (int i = 0; i < steps.Length; i++)
        {
            if (steps[i].ghostVisual != null)
            {
                steps[i].ghostVisual.SetActive(i == currentStep);
            }
        }
    }
}
