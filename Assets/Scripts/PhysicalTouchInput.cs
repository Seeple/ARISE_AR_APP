using System;
using System.Runtime.InteropServices;
using UnityEngine;

// Per-side ownership and neutral re-arm shared by local and workstation input.
internal sealed class PhysicalTouchInput
{
    internal enum Source { Unavailable, BareHand, Touch }

    [Flags]
    internal enum Button { None = 0, Primary = 1, Secondary = 2, Stick = 4, Index = 8, Grip = 16 }

    internal struct State
    {
        internal bool Ready;
        internal Button Held, Pressed, Released;
        internal Vector2 Stick;
        internal float Trigger;
        internal bool IsHeld(Button button) => (Held & button) != 0;
        internal bool IsPressed(Button button) => (Pressed & button) != 0;
        internal bool IsReleased(Button button) => (Released & button) != 0;

        internal void WriteButtons(bool[] output)
        {
            output[0] = IsHeld(Button.Secondary);
            output[1] = IsHeld(Button.Primary);
            output[2] = IsHeld(Button.Stick);
            output[3] = IsHeld(Button.Index);
            output[4] = IsHeld(Button.Grip);
        }

        // On the wire slot 0 is Secondary, and slot 1 is Primary.
        internal int WireMask => ((int)Held & ~3) | (((int)Held & 1) << 1) | (((int)Held & 2) >> 1);
    }

    internal const float StickDeadzone = .2f;
    const float TriggerNeutral = .05f;

    State current;

    internal void Reset() => current = default;

    internal State Filter(Source source, Button held, Vector2 stick, float trigger, float grip)
    {
        if (source != Source.Touch || !Finite(stick.x) || !Finite(stick.y) || !Finite(trigger) || !Finite(grip))
        {
            Reset();
            return current;
        }

        if (!current.Ready)
        {
            // Do not synthesize an edge from a button held while picking up a controller.
            if (held == Button.None && stick.sqrMagnitude <= StickDeadzone * StickDeadzone &&
                trigger <= TriggerNeutral && grip <= TriggerNeutral)
                current.Ready = true;
            return current;
        }

        var previous = current.Held;
        current.Held = held;
        current.Pressed = held & ~previous;
        current.Released = previous & ~held;
        current.Stick = stick;
        current.Trigger = Mathf.Clamp01(trigger);
        return current;
    }

    internal State ForSend(Source source)
    {
        // FixedUpdate can run before the next input Update. Drop a stale command on lost ownership.
        if (source != Source.Touch) Reset();
        return current;
    }

    internal State Poll(bool left, Source source)
    {
        if (source != Source.Touch) return Filter(source, Button.None, Vector2.zero, 0, 0);
        var touch = left ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
        var held = Check(left ? OVRInput.RawButton.X : OVRInput.RawButton.A, touch, Button.Primary) |
                   Check(left ? OVRInput.RawButton.Y : OVRInput.RawButton.B, touch, Button.Secondary) |
                   Check(left ? OVRInput.RawButton.LThumbstick : OVRInput.RawButton.RThumbstick, touch, Button.Stick) |
                   Check(left ? OVRInput.RawButton.LIndexTrigger : OVRInput.RawButton.RIndexTrigger, touch, Button.Index) |
                   Check(left ? OVRInput.RawButton.LHandTrigger : OVRInput.RawButton.RHandTrigger, touch, Button.Grip);
        return Filter(source, held,
            OVRInput.Get(left ? OVRInput.RawAxis2D.LThumbstick : OVRInput.RawAxis2D.RThumbstick, touch),
            OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, touch),
            OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, touch));
    }

    static Button Check(OVRInput.RawButton raw, OVRInput.Controller touch, Button button) =>
        OVRInput.Get(raw, touch) ? button : Button.None;

    internal static Source ResolveSource(bool focused, bool connected, bool ownershipKnown, bool inHand, bool tracked)
    {
        if (!focused) return Source.Unavailable;
        if (!connected) return Source.BareHand;
        if (!ownershipKnown) return Source.Unavailable;
        if (!inHand) return Source.BareHand;
        return tracked ? Source.Touch : Source.Unavailable;
    }

    internal static Source GetSource(bool left, bool focused)
    {
        if (!focused) return Source.Unavailable;
        var touch = left ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
        if (!OVRInput.IsControllerConnected(touch)) return Source.BareHand;
        // The SDK's bool wrapper reports "in hand" when this native call fails.
        // Use the same SDK entry point, but require a successful ownership query.
        bool known = TryGetInHand(left, out bool inHand);
        return ResolveSource(focused, true, known, inHand, known && inHand &&
            OVRInput.GetControllerPositionTracked(touch) && OVRInput.GetControllerOrientationTracked(touch));
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    [DllImport("OVRPlugin", CallingConvention = CallingConvention.Cdecl)]
    static extern OVRPlugin.Result ovrp_GetControllerIsInHand(OVRPlugin.Step step, OVRPlugin.Node node, ref OVRPlugin.Bool inHand);
    static bool ownershipApiUnavailable;
#endif

    static bool TryGetInHand(bool left, out bool held)
    {
        held = false;
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!ownershipApiUnavailable)
        {
            try
            {
                var nativeHeld = OVRPlugin.Bool.False;
                var node = left ? OVRPlugin.Node.ControllerLeft : OVRPlugin.Node.ControllerRight;
                var result = ovrp_GetControllerIsInHand(OVRPlugin.Step.Render, node, ref nativeHeld);
                held = nativeHeld == OVRPlugin.Bool.True;
                return result == OVRPlugin.Result.Success;
            }
            catch (EntryPointNotFoundException) { ownershipApiUnavailable = true; }
            catch (DllNotFoundException) { ownershipApiUnavailable = true; }
        }
#endif
        return false;
    }

    static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
