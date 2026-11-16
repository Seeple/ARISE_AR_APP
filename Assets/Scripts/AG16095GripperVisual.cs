using UnityEngine;

/// <summary>
/// Visual parallel-grasp configuration of an AG-160-95 with custom soft fingers.
/// Coordinates are metres: X is the aperture, -Y points toward the fingertips.
/// Keeps the existing GripperJawController / jawWidth interface used by the HUD.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(GripperJawController))]
public sealed class AG16095GripperVisual : MonoBehaviour
{
    // Model source datum: flange mounting annulus is 202 + 12/2 mm on +Y.
    public const float SourceFlangeMountY = .208f;
    public const float TcpBelowFlange = .200f;
    public const float GeometryOffsetY = TcpBelowFlange - SourceFlangeMountY;
    public const float MaximumOpening = 0.095f;
    public const float FingerLength = 0.0916f;
    public const float FingerWidth = 0.03955f;
    public const float FingerThickness = 0.027f;
    public const float EmbeddedFingerLength = 0.008f;

    public GripperJawController widthController;
    public Transform leftInnerLink;
    public Transform leftOuterLink;
    public Transform rightInnerLink;
    public Transform rightOuterLink;

    [Header("Visual linkage geometry (metres)")]
    public float pivotHeight = 0.100f;
    public float innerPivotX = 0.025f;
    public float carrierInnerPivotX = 0.005f;
    public float parallelLinkSpacing = 0.028f;
    public float linkLength = 0.064f;

    void OnEnable() { ApplyWidthNow(); }
    void LateUpdate() { ApplyWidthNow(); }
    void OnValidate() { ApplyWidthNow(); }

    public void SetJawWidth(float widthMetres)
    {
        if (widthController == null) widthController = GetComponent<GripperJawController>();
        if (widthController == null) return;
        widthController.jawWidth = widthMetres;
        ApplyWidthNow();
    }

    public void ApplyWidthNow()
    {
        if (widthController == null) widthController = GetComponent<GripperJawController>();
        if (widthController == null || widthController.jawLeft == null || widthController.jawRight == null) return;

        float width = widthController.jawWidth;
        if (float.IsNaN(width) || float.IsInfinity(width)) width = 0f;
        width = Mathf.Clamp(width, 0f, MaximumOpening);
        widthController.jawWidth = width;

        float half = width * 0.5f;
        float dx = half + carrierInnerPivotX - innerPivotX;
        float drop = Mathf.Sqrt(Mathf.Max(0f, linkLength * linkLength - dx * dx));
        float y = pivotHeight - drop;
        widthController.jawLeft.localPosition = new Vector3(-half, y, 0f);
        widthController.jawRight.localPosition = new Vector3(half, y, 0f);
        UpdateSide(-1f, half, y, leftInnerLink, leftOuterLink);
        UpdateSide(1f, half, y, rightInnerLink, rightOuterLink);
    }

    void UpdateSide(float side, float half, float y, Transform inner, Transform outer)
    {
        Vector3 a = new Vector3(side * innerPivotX, pivotHeight, 0f);
        Vector3 b = new Vector3(side * (half + carrierInnerPivotX), y, 0f);
        PlaceLink(inner, a, b);
        Vector3 offset = new Vector3(side * parallelLinkSpacing, 0f, 0f);
        PlaceLink(outer, a + offset, b + offset);
    }

    static void PlaceLink(Transform link, Vector3 a, Vector3 b)
    {
        if (link == null) return;
        Vector3 delta = b - a;
        link.localPosition = a;
        link.localRotation = Quaternion.FromToRotation(Vector3.up, delta);
        link.localScale = new Vector3(1f, delta.magnitude, 1f);
    }
}
