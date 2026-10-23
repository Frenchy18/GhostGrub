using UnityEngine;

public class CustomerWalker : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1.2f;
    [SerializeField] private float turnSpeed = 360f;
    [SerializeField] private float stoppingDistance = 0.03f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string walkingBoolName = "Walking";

    private Transform target;

    public bool IsMoving => target != null;

    public Animator CustomerAnimator => animator;

    private void Awake()
    {
        if (animator == null)
        {
            animator =
                GetComponentInChildren<Animator>();
        }
    }

    public void MoveTo(Transform newTarget)
    {
        target = newTarget;

        SetWalking(target != null);
    }

    private void Update()
    {
        if (target == null)
            return;

        Vector3 difference =
            target.position - transform.position;

        Vector3 flatDifference =
            new Vector3(
                difference.x,
                0f,
                difference.z
            );

        // Reached destination.
        if (flatDifference.magnitude <= stoppingDistance)
        {
            Vector3 finalPosition =
                target.position;

            // Keep the customer's current Y height.
            finalPosition.y =
                transform.position.y;

            transform.position = finalPosition;

            // Match the waypoint's final facing direction.
            Vector3 targetEuler =
                target.rotation.eulerAngles;

            transform.rotation =
                Quaternion.Euler(
                    0f,
                    targetEuler.y,
                    0f
                );

            target = null;

            SetWalking(false);

            return;
        }

        // Move toward destination.
        Vector3 destination =
            target.position;

        destination.y =
            transform.position.y;

        transform.position =
            Vector3.MoveTowards(
                transform.position,
                destination,
                moveSpeed * Time.deltaTime
            );

        // Rotate toward direction of travel.
        if (flatDifference.sqrMagnitude > 0.001f)
        {
            Quaternion desiredRotation =
                Quaternion.LookRotation(
                    flatDifference.normalized,
                    Vector3.up
                );

            transform.rotation =
                Quaternion.RotateTowards(
                    transform.rotation,
                    desiredRotation,
                    turnSpeed * Time.deltaTime
                );
        }
    }

    private void SetWalking(bool walking)
    {
        if (animator == null ||
            string.IsNullOrEmpty(walkingBoolName))
        {
            return;
        }

        animator.SetBool(
            walkingBoolName,
            walking
        );
    }
}
