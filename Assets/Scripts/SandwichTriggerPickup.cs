using UnityEngine;

public class SandwichTriggerPickup : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SandwichStation sandwichStation;
    [SerializeField] private Transform leftControllerAnchor;
    [SerializeField] private Transform rightControllerAnchor;

    [Header("Pickup")]
    [SerializeField] private float pickupDistance = 0.25f;

    [SerializeField, Range(0f, 1f)]
    private float pressThreshold = 0.75f;

    [SerializeField, Range(0f, 1f)]
    private float releaseThreshold = 0.20f;

    [Header("Hold Position")]
    [SerializeField] private Vector3 holdLocalPosition =
        new Vector3(0f, 0f, 0.08f);

    [SerializeField] private Vector3 holdLocalEulerAngles =
        Vector3.zero;

    [Header("Launch")]
    [SerializeField] private float launchSpeed = 5f;
    [SerializeField] private float upwardBoost = 0.35f;

    [SerializeField] private Vector3 localLaunchDirection =
        Vector3.forward;

    private bool holdingSandwich;

    private OVRInput.Controller activeController;
    private Transform activeAnchor;

    private SandwichObject heldSandwich;

    private void Update()
    {
        if (sandwichStation == null)
            return;

        if (holdingSandwich &&
            heldSandwich == null)
        {
            ClearHold();
            return;
        }

        if (holdingSandwich)
        {
            CheckRelease();
            return;
        }

        CheckPickup();
    }

    private void CheckPickup()
    {
        float leftTrigger =
            OVRInput.Get(
                OVRInput.Axis1D.PrimaryIndexTrigger,
                OVRInput.Controller.LTouch
            );

        float rightTrigger =
            OVRInput.Get(
                OVRInput.Axis1D.PrimaryIndexTrigger,
                OVRInput.Controller.RTouch
            );

        bool leftPressed =
            leftControllerAnchor != null &&
            leftTrigger >= pressThreshold;

        bool rightPressed =
            rightControllerAnchor != null &&
            rightTrigger >= pressThreshold;

        if (!leftPressed && !rightPressed)
            return;

        if (leftPressed)
        {
            if (TryPickupWithHand(
                leftControllerAnchor,
                OVRInput.Controller.LTouch))
            {
                return;
            }
        }

        if (rightPressed)
        {
            TryPickupWithHand(
                rightControllerAnchor,
                OVRInput.Controller.RTouch
            );
        }
    }

    private bool TryPickupWithHand(
        Transform controllerAnchor,
        OVRInput.Controller controller)
    {
        if (controllerAnchor == null)
            return false;

        //
        // FIRST:
        // Check the sandwich currently being built.
        //
        if (sandwichStation.CanTakeSandwich)
        {
            float distanceToStationSandwich =
                Vector3.Distance(
                    controllerAnchor.position,
                    sandwichStation.SandwichPickupPosition
                );

            if (distanceToStationSandwich <= pickupDistance)
            {
                BeginStationPickup(
                    controllerAnchor,
                    controller
                );

                return true;
            }
        }

        //
        // SECOND:
        // Look for an already-launched sandwich nearby.
        //
        SandwichObject looseSandwich =
            FindNearbySandwich(controllerAnchor);

        if (looseSandwich != null)
        {
            BeginLooseSandwichPickup(
                looseSandwich,
                controllerAnchor,
                controller
            );

            return true;
        }

        return false;
    }

    private SandwichObject FindNearbySandwich(
        Transform controllerAnchor)
    {
        Collider[] nearbyColliders =
            Physics.OverlapSphere(
                controllerAnchor.position,
                pickupDistance
            );

        SandwichObject closestSandwich = null;
        float closestDistance = float.MaxValue;

        foreach (Collider hit in nearbyColliders)
        {
            SandwichObject sandwich =
                hit.GetComponentInParent<SandwichObject>();

            if (sandwich == null ||
                sandwich.IsHeld)
            {
                continue;
            }

            float distance =
                Vector3.Distance(
                    controllerAnchor.position,
                    sandwich.PickupPosition
                );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestSandwich = sandwich;
            }
        }

        return closestSandwich;
    }

    private void BeginStationPickup(
        Transform controllerAnchor,
        OVRInput.Controller controller)
    {
        Quaternion holdRotation =
            Quaternion.Euler(
                holdLocalEulerAngles
            );

        if (!sandwichStation.TryTakeSandwich(
                controllerAnchor,
                holdLocalPosition,
                holdRotation,
                out SandwichObject sandwich))
        {
            return;
        }

        heldSandwich = sandwich;
        holdingSandwich = true;

        activeAnchor = controllerAnchor;
        activeController = controller;
    }

    private void BeginLooseSandwichPickup(
        SandwichObject sandwich,
        Transform controllerAnchor,
        OVRInput.Controller controller)
    {
        if (sandwich == null)
            return;

        Quaternion holdRotation =
            Quaternion.Euler(
                holdLocalEulerAngles
            );

        sandwich.BeginHold(
            controllerAnchor,
            holdLocalPosition,
            holdRotation
        );

        heldSandwich = sandwich;
        holdingSandwich = true;

        activeAnchor = controllerAnchor;
        activeController = controller;
    }

    private void CheckRelease()
    {
        float triggerValue =
            OVRInput.Get(
                OVRInput.Axis1D.PrimaryIndexTrigger,
                activeController
            );

        if (triggerValue > releaseThreshold)
            return;

        Vector3 launchDirection =
            activeAnchor.TransformDirection(
                localLaunchDirection.normalized
            );

        Vector3 launchVelocity =
            launchDirection * launchSpeed +
            Vector3.up * upwardBoost;

        heldSandwich.Launch(
            launchVelocity,
            Vector3.zero
        );

        ClearHold();
    }

    private void ClearHold()
    {
        holdingSandwich = false;

        heldSandwich = null;
        activeAnchor = null;

        activeController =
            OVRInput.Controller.None;
    }
}