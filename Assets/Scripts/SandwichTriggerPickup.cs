using UnityEngine;

public class SandwichTriggerPickup : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AssemblyManager assemblyManager;
    [SerializeField] private Transform leftControllerAnchor;
    [SerializeField] private Transform rightControllerAnchor;

    [Header("Pickup")]
    [SerializeField] private float pickupDistance = 0.25f;
    [SerializeField, Range(0f, 1f)] private float pressThreshold = 0.75f;
    [SerializeField, Range(0f, 1f)] private float releaseThreshold = 0.20f;

    [Header("Hold Position")]
    [SerializeField] private Vector3 holdLocalPosition =
        new Vector3(0f, 0f, 0.08f);

    [SerializeField] private Vector3 holdLocalEulerAngles =
        Vector3.zero;

    [Header("Sandwich Launch")]
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
        if (assemblyManager == null)
            return;

        if (holdingSandwich && heldSandwich == null)
        {
            ClearHoldState();
            return;
        }

        if (holdingSandwich)
        {
            float triggerValue =
                OVRInput.Get(
                    OVRInput.Axis1D.PrimaryIndexTrigger,
                    activeController
                );

            if (triggerValue <= releaseThreshold)
            {
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

                ClearHoldState();
            }

            return;
        }

        if (!assemblyManager.CanTakeSandwich)
            return;

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

        Vector3 sandwichPosition =
            assemblyManager.SandwichPickupPosition;

        float leftDistance =
            leftPressed
                ? Vector3.Distance(
                    leftControllerAnchor.position,
                    sandwichPosition
                )
                : float.PositiveInfinity;

        float rightDistance =
            rightPressed
                ? Vector3.Distance(
                    rightControllerAnchor.position,
                    sandwichPosition
                )
                : float.PositiveInfinity;

        if (leftDistance > pickupDistance &&
            rightDistance > pickupDistance)
        {
            return;
        }

        if (leftDistance <= rightDistance)
        {
            BeginPickup(
                leftControllerAnchor,
                OVRInput.Controller.LTouch
            );
        }
        else
        {
            BeginPickup(
                rightControllerAnchor,
                OVRInput.Controller.RTouch
            );
        }
    }

    private void BeginPickup(
        Transform controllerAnchor,
        OVRInput.Controller controller)
    {
        Quaternion holdRotation =
            Quaternion.Euler(holdLocalEulerAngles);

        if (!assemblyManager.TryTakeSandwich(
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

    private void ClearHoldState()
    {
        holdingSandwich = false;
        heldSandwich = null;
        activeAnchor = null;
        activeController = OVRInput.Controller.None;
    }
}