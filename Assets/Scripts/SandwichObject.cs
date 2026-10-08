using UnityEngine;

public class SandwichObject : MonoBehaviour
{
    private Rigidbody rb;
    private BoxCollider sandwichCollider;

    private Transform holdAnchor;
    private Vector3 holdLocalPosition;
    private Quaternion holdLocalRotation;

    private bool isHeld;

    public void Initialize(
        Rigidbody sandwichRigidbody,
        BoxCollider collider)
    {
        rb = sandwichRigidbody;
        sandwichCollider = collider;
    }

    public void BeginHold(
        Transform controllerAnchor,
        Vector3 localPosition,
        Quaternion localRotation)
    {
        if (controllerAnchor == null)
            return;

        holdAnchor = controllerAnchor;
        holdLocalPosition = localPosition;
        holdLocalRotation = localRotation;
        isHeld = true;

        if (rb != null)
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            rb.useGravity = false;
            rb.isKinematic = true;
        }

        // While held, collision is disabled completely. This prevents the
        // sandwich from fighting the hand, counter, or nearby geometry.
        if (sandwichCollider != null)
            sandwichCollider.enabled = false;

        UpdateHeldPose();
    }

    private void LateUpdate()
    {
        if (isHeld)
            UpdateHeldPose();
    }

    private void UpdateHeldPose()
    {
        if (holdAnchor == null)
            return;

        Vector3 targetPosition =
            holdAnchor.TransformPoint(holdLocalPosition);

        Quaternion targetRotation =
            holdAnchor.rotation * holdLocalRotation;

        transform.SetPositionAndRotation(
            targetPosition,
            targetRotation
        );
    }

    public void Launch(
        Vector3 linearVelocity,
        Vector3 angularVelocity)
    {
        if (!isHeld)
            return;

        // Make sure release uses the newest tracked controller pose.
        UpdateHeldPose();

        isHeld = false;
        holdAnchor = null;

        if (sandwichCollider != null)
            sandwichCollider.enabled = true;

        if (rb == null)
            return;

        rb.position = transform.position;
        rb.rotation = transform.rotation;
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearVelocity = linearVelocity;
        rb.angularVelocity = angularVelocity;
    }

    public void Discard()
    {
        Destroy(gameObject);
    }
}