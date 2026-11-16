using UnityEngine;
using TouchButton = PhysicalTouchInput.Button;
using TouchState = PhysicalTouchInput.State;

public class Calibration : MonoBehaviour
{

    public static Calibration instance;
    public GameObject coord;
    bool running = false;
    public bool IsRunning => running;
    public float WorldYawDegrees => Mathf.Repeat(
        Mathf.Atan2(transform.forward.x, transform.forward.z) * Mathf.Rad2Deg, 360f);

    public bool AdjustWorldYaw(float degrees)
    {
        if (!running || float.IsNaN(degrees) || float.IsInfinity(degrees)) return false;
        // Rotate around gravity at the existing calibration origin, not the headset.
        transform.rotation = Quaternion.AngleAxis(degrees, Vector3.up) * transform.rotation;
        return true;
    }

    private void Awake()
    {
        instance = this;
        running = false;
        coord.SetActive(false);
        DontDestroyOnLoad(this.gameObject);
    }

    public void SwitchAlign(bool run)
    {
        running = run;
        draggingLeft = draggingRight = false;
        coord.SetActive(run);
    }

    Vector3 startPositionRight;
    Vector3 startPositionLeft;
    Vector3 startPositionTransform;
    bool draggingRight;
    bool draggingLeft;

    internal void ApplyTouchInput(TouchState left, TouchState right, bool calibrationChord)
    {
        if (!running) return;

        if (!right.IsHeld(TouchButton.Index)) draggingRight = false;
        if (right.IsPressed(TouchButton.Index))
        {
            startPositionRight = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
            draggingRight = true;
        }
        if (draggingRight)
        {
            Vector3 offset = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch) - startPositionRight;
            offset.y = 0;
            if (offset.sqrMagnitude > .000001f) transform.LookAt(transform.position + offset);
        }

        if (!left.IsHeld(TouchButton.Index)) draggingLeft = false;
        if (left.IsPressed(TouchButton.Index))
        {
            startPositionTransform = transform.position;
            startPositionLeft = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);
            draggingLeft = true;
        }
        if (draggingLeft)
        {
            Vector3 offset = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch) - startPositionLeft;
            transform.position = startPositionTransform + offset;
        }

        var axes = transform.GetChild(0);
        if (!calibrationChord && (right.IsPressed(TouchButton.Primary) || left.IsPressed(TouchButton.Primary)))
        {
            if (axes.localScale.magnitude > 0.2f) axes.localScale -= Vector3.one * 0.1f;
        }
        if (right.IsPressed(TouchButton.Secondary) || left.IsPressed(TouchButton.Secondary))
            axes.localScale += Vector3.one * 0.1f;

        // Left stick click is reserved for the keyboard, including during calibration.
        if (right.IsPressed(TouchButton.Stick))
        {
            transform.position = Vector3.zero;
            axes.localScale = Vector3.one;
        }
    }

    public Vector3 GetEuler(Vector3 worldEuler)
    {
        return Quaternion.Inverse(transform.rotation) * worldEuler;
    }

    public Quaternion GetRotation(Quaternion worldEuler)
    {
        return Quaternion.Inverse(transform.rotation) * worldEuler;
    }

    public Vector3 GetPosition(Vector3 worldPosition)
    {
        return transform.InverseTransformPoint(worldPosition);
    }
}
