using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class CustomerQueueController : MonoBehaviour
{
    [Serializable]
    public class RecipeDefinition
    {
        public string ticketName = "Sandwich";

        public List<IngredientType> ingredients =
            new List<IngredientType>();
    }

    private class ActiveCustomer
    {
        public CustomerWalker walker;
        public RecipeDefinition recipe;
        public bool enteringQueue;
    }

    [Header("Customer")]
    [SerializeField]
    private GameObject customerPrefab;

    [SerializeField]
    private Transform spawner;

    [SerializeField, Min(1)]
    private int maxCustomers = 3;

    [Header("Spawn Timing")]
    [SerializeField]
    private float initialSpawnDelay = 1f;

    [SerializeField]
    private float spawnInterval = 4f;

    [Header("Queue")]
    [SerializeField]
    private Transform servicePoint;

    [SerializeField]
    private Transform queuePoint1;

    [SerializeField]
    private Transform queuePoint2;

    [SerializeField]
    private Transform queuePoint3;

    [Header("Exit")]
    [SerializeField]
    private Transform exitPoint1;

    [SerializeField]
    private Transform exitPoint2;

    [SerializeField, Min(0.1f)]
    private float serviceClearanceDistance = 0.6f;

    [Header("Orders")]
    [SerializeField]
    private CustomerDeliveryZone deliveryZone;

    [SerializeField]
    private GameObject ticketRoot;

    [SerializeField]
    private TMP_Text ticketText;

    [SerializeField]
    private SandwichStation sandwichStation;

    [SerializeField]
    private List<RecipeDefinition> recipes =
        new List<RecipeDefinition>();

    [Header("Customer Reactions")]
    [SerializeField]
    private string correctTrigger = "CorrectOrder";

    [SerializeField]
    private string wrongTrigger = "WrongOrder";

    [SerializeField]
    private AudioClip correctSfx;

    [SerializeField]
    private AudioClip wrongSfx;

    [SerializeField, Range(0f, 1f)]
    private float sfxVolume = 1f;

    [SerializeField]
    private float correctReactionDelay = 1.5f;

    private readonly List<ActiveCustomer> queue =
        new List<ActiveCustomer>();

    private int liveCustomerCount;

    private bool orderActive;
    private bool completingCustomer;

    private void Start()
    {
        if (sandwichStation != null)
            sandwichStation.AssemblyChanged += RefreshAssemblyTicket;

        if (ticketRoot != null)
        {
            ticketRoot.SetActive(false);
        }

        if (deliveryZone != null)
        {
            deliveryZone.SetAcceptingOrders(false);
        }

        StartCoroutine(SpawnLoop());
    }

    private void OnDestroy()
    {
        if (sandwichStation != null)
            sandwichStation.AssemblyChanged -= RefreshAssemblyTicket;
    }

    private void RefreshAssemblyTicket()
    {
        if (orderActive && queue.Count > 0)
            UpdateTicket(queue[0].recipe);
    }

    private void Update()
    {
        TryActivateFrontOrder();
    }

    private IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(
            initialSpawnDelay
        );

        while (true)
        {
            if (liveCustomerCount < Mathf.Clamp(maxCustomers, 1, 3))
            {
                SpawnCustomer();

                yield return new WaitForSeconds(
                    spawnInterval
                );
            }
            else
            {
                yield return new WaitForSeconds(
                    0.25f
                );
            }
        }
    }

    private void SpawnCustomer()
    {
        if (customerPrefab == null ||
            spawner == null)
        {
            return;
        }

        if (recipes.Count == 0)
        {
            Debug.LogWarning(
                "CustomerQueueController has no recipes."
            );

            return;
        }

        GameObject customerObject =
            Instantiate(
                customerPrefab,
                spawner.position,
                spawner.rotation
            );

        CustomerWalker walker =
            customerObject.GetComponent<CustomerWalker>();

        if (walker == null)
        {
            Debug.LogError(
                "Spawned customer has no CustomerWalker."
            );

            Destroy(customerObject);
            return;
        }

        ActiveCustomer customer =
            new ActiveCustomer
            {
                walker = walker,
                enteringQueue = true,
                recipe =
                    recipes[
                        UnityEngine.Random.Range(
                            0,
                            recipes.Count
                        )
                    ]
            };

        queue.Add(customer);

        liveCustomerCount++;

        StartCoroutine(
            EnterQueue(customer)
        );
    }

    private IEnumerator EnterQueue(ActiveCustomer customer)
    {
        // Always enter from the rear/staging point.
        yield return MoveAndWait(
            customer.walker,
            queuePoint3
        );

        // Slot 2 = QueuePoint2.
        if (queue.IndexOf(customer) <= 2)
        {
            yield return MoveAndWait(
                customer.walker,
                queuePoint2
            );
        }

        // Slot 1 = QueuePoint1.
        if (queue.IndexOf(customer) <= 1)
        {
            yield return MoveAndWait(
                customer.walker,
                queuePoint1
            );
        }

        // Slot 0 = counter/service point.
        if (queue.IndexOf(customer) == 0)
        {
            yield return MoveAndWait(
                customer.walker,
                servicePoint
            );
        }

        customer.enteringQueue = false;
    }

    private IEnumerator MoveAndWait(
        CustomerWalker walker,
        Transform destination)
    {
        if (walker == null ||
            destination == null)
        {
            yield break;
        }

        walker.MoveTo(destination);

        while (walker != null &&
               walker.IsMoving)
        {
            yield return null;
        }
    }

    private void TryActivateFrontOrder()
    {
        if (orderActive ||
            completingCustomer ||
            queue.Count == 0 ||
            servicePoint == null)
        {
            return;
        }

        ActiveCustomer front =
            queue[0];

        if (front.enteringQueue ||
            front.walker == null ||
            front.walker.IsMoving)
        {
            return;
        }

        // CustomerWalker preserves spawn height and arrives on the XZ plane.
        Vector3 difference =
            front.walker.transform.position - servicePoint.position;
        difference.y = 0f;

        float distance = difference.magnitude;

        if (distance > 0.1f)
            return;

        ActivateOrder(front);
    }

    private void ActivateOrder(
        ActiveCustomer customer)
    {
        orderActive = true;

        if (sandwichStation != null)
            sandwichStation.SetRecipe(customer.recipe.ingredients);

        if (deliveryZone != null)
        {
            deliveryZone.SetRequiredRecipe(
                customer.recipe.ingredients
            );

            deliveryZone.SetAcceptingOrders(true);
        }

        UpdateTicket(customer.recipe);

        if (ticketRoot != null)
        {
            ticketRoot.SetActive(true);
        }
    }

    private void UpdateTicket(
        RecipeDefinition recipe)
    {
        if (ticketText == null ||
            recipe == null)
        {
            return;
        }

        StringBuilder builder =
            new StringBuilder();

        builder.AppendLine(recipe.ticketName);
        builder.AppendLine();
        int completed = sandwichStation != null ? sandwichStation.CompletedSteps : 0;
        builder.AppendLine($"ASSEMBLY {completed}/{recipe.ingredients.Count}");
        bool mistake = sandwichStation != null && sandwichStation.HasAssemblyMistake;
        builder.AppendLine(mistake
            ? "Wrong layer - pick up, discard and rebuild."
            : completed >= recipe.ingredients.Count
            ? "COMPLETE - Grab and deliver!"
            : $"NEXT: Grab {recipe.ingredients[completed]}");
        builder.AppendLine("Pick up at any time to deliver or discard.");
        builder.AppendLine();

        for (int i = 0; i < recipe.ingredients.Count; i++)
        {
            string marker = i < completed ? "[OK]" : i == completed ? ">" : "";
            builder.AppendLine($"{marker} {i + 1}. {recipe.ingredients[i]}");
        }

        ticketText.text =
            builder.ToString();
    }

    public void HandleCorrectOrder()
    {
        if (!orderActive ||
            completingCustomer ||
            queue.Count == 0)
        {
            return;
        }

        orderActive = false;
        completingCustomer = true;

        if (sandwichStation != null)
            sandwichStation.SetRecipe(null);

        if (deliveryZone != null)
        {
            deliveryZone.SetAcceptingOrders(false);
        }

        if (ticketRoot != null)
        {
            ticketRoot.SetActive(false);
        }

        ActiveCustomer customer =
            queue[0];

        Animator animator =
            customer.walker != null
                ? customer.walker.CustomerAnimator
                : null;

        if (animator != null &&
            !string.IsNullOrEmpty(correctTrigger))
        {
            animator.SetTrigger(correctTrigger);
        }

        PlaySfx(
            correctSfx,
            customer.walker
        );

        StartCoroutine(
            FinishCustomer(customer)
        );
    }

    public void HandleWrongOrder()
    {
        if (!orderActive ||
            queue.Count == 0)
        {
            return;
        }

        ActiveCustomer customer =
            queue[0];

        Animator animator =
            customer.walker != null
                ? customer.walker.CustomerAnimator
                : null;

        if (animator != null &&
            !string.IsNullOrEmpty(wrongTrigger))
        {
            animator.SetTrigger(wrongTrigger);
        }

        PlaySfx(
            wrongSfx,
            customer.walker
        );

        // Wrong order:
        // customer stays and the ticket remains active.
    }

    private IEnumerator FinishCustomer(
        ActiveCustomer customer)
    {
        yield return new WaitForSeconds(
            correctReactionDelay
        );

        // Exit travel continues independently after the service point clears.
        StartCoroutine(ExitCustomer(customer));

        while (customer.walker != null && servicePoint != null)
        {
            Vector3 difference =
                customer.walker.transform.position - servicePoint.position;
            difference.y = 0f;

            float clearance = Mathf.Max(0.1f, serviceClearanceDistance);
            if (difference.sqrMagnitude >= clearance * clearance)
                break;

            yield return null;
        }

        queue.Remove(customer);
        completingCustomer = false;
        AdvanceQueue();
    }

    private IEnumerator ExitCustomer(ActiveCustomer customer)
    {
        if (customer.walker != null)
        {
            yield return MoveAndWait(
                customer.walker,
                exitPoint1
            );

            yield return MoveAndWait(
                customer.walker,
                exitPoint2
            );

            Destroy(
                customer.walker.gameObject
            );
        }

        liveCustomerCount =
            Mathf.Max(
                0,
                liveCustomerCount - 1
            );

    }

    private void AdvanceQueue()
    {
        for (int i = 0;
             i < queue.Count;
             i++)
        {
            Transform destination =
                GetOccupiedSlot(i);

            if (destination != null &&
                !queue[i].enteringQueue &&
                queue[i].walker != null)
            {
                queue[i].walker.MoveTo(
                    destination
                );
            }
        }
    }

    private Transform GetOccupiedSlot(
        int index)
    {
        switch (index)
        {
            case 0:
                return servicePoint;

            case 1:
                return queuePoint1;

            case 2:
                return queuePoint2;

            default:
                return null;
        }
    }

    private void PlaySfx(
        AudioClip clip,
        CustomerWalker walker)
    {
        if (clip == null ||
            walker == null)
        {
            return;
        }

        AudioSource.PlayClipAtPoint(
            clip,
            walker.transform.position,
            sfxVolume
        );
    }
}
