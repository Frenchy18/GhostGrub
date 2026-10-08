using UnityEngine;

public class SandwichObject : MonoBehaviour
{
    private Rigidbody rb;

    public void Initialize(Rigidbody sandwichRigidbody)
    {
        rb = sandwichRigidbody;
    }

    public void AttachTo(
        Transform controllerAnchor,
        Vector3 localPosition,
        Quaternion localRotation)
    {
        if (controllerAnchor == null)
            return;

        // Preserve the sandwich's world scale when leaving the scaled
        // AssemblyArea, then give it a predictable controller-local pose.
        transform.SetParent(controllerAnchor, true);
        transform.localPosition = localPosition;
        transform.localRotation = localRotation;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }
    }

    public void Launch(
        Vector3 linearVelocity,
        Vector3 angularVelocity)
    {
        transform.SetParent(null, true);

        if (rb == null)
            return;

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

