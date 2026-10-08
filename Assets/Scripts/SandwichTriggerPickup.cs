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
    [SerializeField] private Vector3 localLaunchDirection = Vector3.forward;

    private bool holdingSandwich;
    private OVRInput.Controller activeController;
    private Transform activeAnchor;
    private SandwichObject heldSandwich;

    private Vector3 previousAnchorPosition;
    private Quaternion previousAnchorRotation;

    private Vector3 releaseLinearVelocity;
    private Vector3 releaseAngularVelocity;

    private void Update()
    {
        if (assemblyManager == null)
            return;

        // If the held sandwich was destroyed (for example by a trash
        // trigger), immediately clear this input state.
        if (holdingSandwich && heldSandwich == null)
        {
            ClearHoldState();
            return;
        }

        if (holdingSandwich)
        {
            UpdateThrowVelocity();

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

                Vector3 launchedVelocity =
                    releaseLinearVelocity +
                    launchDirection * launchSpeed +
                    Vector3.up * upwardBoost;

                heldSandwich.Launch(
                    launchedVelocity,
                    releaseAngularVelocity
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

        float leftDistance = leftPressed
            ? Vector3.Distance(
                leftControllerAnchor.position,
                sandwichPosition
            )
            : float.PositiveInfinity;

        float rightDistance = rightPressed
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

        previousAnchorPosition = activeAnchor.position;
        previousAnchorRotation = activeAnchor.rotation;

        releaseLinearVelocity = Vector3.zero;
        releaseAngularVelocity = Vector3.zero;
    }

    private void UpdateThrowVelocity()
    {
        if (activeAnchor == null || Time.deltaTime <= 0f)
            return;

        float dt = Time.deltaTime;

        Vector3 currentPosition = activeAnchor.position;
        Quaternion currentRotation = activeAnchor.rotation;

        releaseLinearVelocity =
            (currentPosition - previousAnchorPosition) / dt;

        Quaternion delta =
            currentRotation *
            Quaternion.Inverse(previousAnchorRotation);

        delta.ToAngleAxis(
            out float angleDegrees,
            out Vector3 axis
        );

        if (angleDegrees > 180f)
            angleDegrees -= 360f;

        if (axis.sqrMagnitude > 0.0001f)
        {
            releaseAngularVelocity =
                axis.normalized *
                angleDegrees *
                Mathf.Deg2Rad /
                dt;
        }
        else
        {
            releaseAngularVelocity = Vector3.zero;
        }

        previousAnchorPosition = currentPosition;
        previousAnchorRotation = currentRotation;
    }

    private void ClearHoldState()
    {
        holdingSandwich = false;
        heldSandwich = null;
        activeAnchor = null;
        activeController = OVRInput.Controller.None;
        releaseLinearVelocity = Vector3.zero;
        releaseAngularVelocity = Vector3.zero;
    }
}

