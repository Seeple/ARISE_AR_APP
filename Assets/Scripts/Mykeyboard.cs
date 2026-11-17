using System.Net;
using TMPro;
using UnityEngine;

public class MyKeyboard : MonoBehaviour
{
    public const string ControlHelp =
        "Enter the workstation IP address to communicate with Quest.\n\n" +
        "<color=#52D9EF>X + A</color>  Calibration\n" +
        "During calibration, view the TCP from different angles.\n" +
        "If its position stays fixed, calibration is complete.\n" +
        "<color=#52D9EF>Pinch</color>  Select / drag point\n\n" +
        "<color=#52D9EF>X</color>  Action chunk (release)\n" +
        "<color=#52D9EF>A</color>  Ghost grippers (release)\n" +
        "<color=#52D9EF>B</color>  Selection mode\n" +
        "<color=#52D9EF>Y</color>  Last point\n\n" +
        "<color=#52D9EF>L grip / R grip</color>  Close / open gripper\n" +
        "<color=#52D9EF>L trigger</color>  Start / finish trajectory editing\n" +
        "<color=#52D9EF>R stick click</color>  Interpolate anchor to\nlast edit / chunk end\n\n" +
        "<color=#52D9EF>L stick click</color>  Show / hide keyboard\n" +
        "<color=#52D9EF>L stick</color>  Move keyboard while visible\n" +
        "Up / down: farther / closer\nLeft / right: move sideways\n\n" +
        "<color=#52D9EF>Trajectory Map</color> (B to switch):\n" +
        "Left pinch: drag. Right stick up/down: zoom.\n" +
        "Release the right-hand pinch before adjusting the map.\n" +
        "Reopen the map to reset position and size.";

    public TextMeshProUGUI text;
    public TextMeshProUGUI showText;
    public TextMeshProUGUI instructionText;
    public VRController Client;
    public TextMeshProUGUI calibrationYawText;
    public TextMeshProUGUI calibrationTitle;
    public GameObject yawIncreaseButton;
    public GameObject yawDecreaseButton;
    public GameObject defaultTaskButton;
    public GameObject knotTaskButton;
    public TextMeshProUGUI toolStatusText;
    [Min(.01f)] public float yawStepDegrees = 1f;
    bool? yawControlsActive;
    HeldToolVisualization heldTool;
    HeldToolVisualization.TaskMode? displayedTask;

    public void SelectDefaultTask() => SelectToolTask(HeldToolVisualization.TaskMode.Default);
    public void SelectKnotTask() => SelectToolTask(HeldToolVisualization.TaskMode.Knot);

    void SelectToolTask(HeldToolVisualization.TaskMode task)
    {
        if (heldTool != null) heldTool.SelectTask(task);
        RefreshToolControls();
    }

    public void RefreshToolControls()
    {
        var task = heldTool == null ? HeldToolVisualization.TaskMode.Default : heldTool.Mode;
        if (toolStatusText != null)
        {
            string status = heldTool == null ? "Off" : heldTool.Status;
            if (toolStatusText.text != status) toolStatusText.text = status;
        }
        if (displayedTask == task) return;
        displayedTask = task;
        SetTaskButtonLight(defaultTaskButton, task == HeldToolVisualization.TaskMode.Default);
        SetTaskButtonLight(knotTaskButton, task == HeldToolVisualization.TaskMode.Knot);
    }

    static void SetTaskButtonLight(GameObject button, bool selected)
    {
        if (button == null) return;
        var label = button.GetComponentInChildren<TMP_Text>(true);
        label.color = selected ? new Color(.32f, .91f, 1f) : new Color(.72f, .79f, .84f);
        label.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
        var face = button.GetComponentInChildren<Oculus.Interaction.RoundedBoxProperties>(true);
        face.BorderColor = selected ? new Color(.32f, .91f, 1f) : new Color(.15f, .40f, .52f);
        face.Width = face.Width; // Apply the border without changing the poke hover and press colors.
    }

    public void IncreaseCalibrationYaw() { AdjustCalibrationYaw(yawStepDegrees); }
    public void DecreaseCalibrationYaw() { AdjustCalibrationYaw(-yawStepDegrees); }
    void AdjustCalibrationYaw(float step)
    {
        if (Calibration.instance != null) Calibration.instance.AdjustWorldYaw(step);
        RefreshCalibrationControls();
    }

    public void RefreshCalibrationControls()
    {
        var calibration = Calibration.instance;
        bool active = calibration != null && calibration.IsRunning;
        Color label = active ? new Color(.32f,.85f,.94f) : new Color(.38f,.41f,.44f);
        if (calibrationYawText != null)
        {
            float angle = calibration == null ? 0f : calibration.WorldYawDegrees;
            calibrationYawText.text = angle.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + "°";
            calibrationYawText.color = label;
        }
        if (yawControlsActive == active) return;
        yawControlsActive = active;
        if (calibrationTitle != null) calibrationTitle.color = label;
        foreach (var button in new[] { yawIncreaseButton, yawDecreaseButton })
        {
            if (button == null) continue;
            foreach (var poke in button.GetComponentsInChildren<Oculus.Interaction.PokeInteractable>(true)) poke.enabled = active;
            foreach (var visual in button.GetComponentsInChildren<Oculus.Interaction.InteractableColorVisual>(true)) visual.enabled = active;
            foreach (var rounded in button.GetComponentsInChildren<Oculus.Interaction.RoundedBoxProperties>(true))
            {
                rounded.Color = active ? new Color(.045f,.11f,.17f) : new Color(.045f,.05f,.055f);
                rounded.BorderColor = label;
                rounded.Width = rounded.Width; // Push the new colors to the renderer immediately.
            }
            foreach (var text in button.GetComponentsInChildren<TMP_Text>(true)) text.color = label;
        }
    }


    [Header("Placement on summon (metres)")]
    [Min(.3f)] public float summonDistance = .45f;
    [Min(0f)] public float summonDrop = .22f;
    [Header("Left stick positioning")]
    [Min(0f)] public float moveSpeed = .25f;
    [Range(0f,.9f)] public float stickDeadzone = .2f;
    [Min(.1f)] public float minimumDepth = .25f;
    [Min(.3f)] public float maximumDepth = 1.5f;
    [Min(0f)] public float maximumSideOffset = .8f;
    public float indexTipJumpThreshold = .1f;
    public float editedPointJumpThreshold = .1f;
    public float rotationJumpThresholdDeg = 30f;

    Vector3 lastIndexTipPos, lastEditedPos;
    Quaternion lastIndexTipRot = Quaternion.identity, lastEditedRot = Quaternion.identity;
    bool hasLastSample;
    string hostStatus = "Ready";

    void OnEnable()
    {
        hasLastSample = false;
        yawControlsActive = null;
        RefreshCalibrationControls();
        heldTool = Client == null ? null : Client.GetComponent<HeldToolVisualization>();
        displayedTask = null;
        RefreshToolControls();
        if (instructionText != null) instructionText.text = ControlHelp;
        UpdateMonitor(false, Vector3.zero, false, Vector3.zero, false, -1, false);
    }

    public void InitializeHostIP(string activeIP)
    {
        if (text != null) text.text = activeIP ?? "";
        hostStatus = "Ready";
    }

    // Set the world pose only when summoned. There is deliberately no follow update.
    public void ShowAtHead(Transform head)
    {
        if (head == null) return;
        Vector3 forward = PlanarForward(head);
        Vector3 position = head.position + forward * summonDistance - Vector3.up * summonDrop;
        transform.SetPositionAndRotation(position, Quaternion.LookRotation(head.position - position, Vector3.up));
        gameObject.SetActive(true);
    }

    static Vector3 PlanarForward(Transform head)
    {
        Vector3 forward=Vector3.ProjectOnPlane(head.forward,Vector3.up);
        if(forward.sqrMagnitude<.0001f) forward=Vector3.ProjectOnPlane(head.up,Vector3.up);
        return forward.sqrMagnitude<.0001f ? Vector3.forward : forward.normalized;
    }

    // Head-relative horizontal translation only; zero input never moves or rotates the panel.
    public void MoveWithStick(Vector2 stick, Transform head, float deltaTime)
    {
        if(!gameObject.activeInHierarchy || head==null || deltaTime<=0 ||
            float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) ||
            float.IsNaN(stick.x) || float.IsNaN(stick.y) ||
            float.IsInfinity(stick.x) || float.IsInfinity(stick.y)) return;
        stick=Vector2.ClampMagnitude(stick,1f);
        float magnitude=stick.magnitude;
        float deadzone=Mathf.Clamp(stickDeadzone,0f,.9f);
        if(magnitude<=deadzone) return;
        Vector2 step=stick/magnitude*((magnitude-deadzone)/(1f-deadzone))*
            Mathf.Max(0f,moveSpeed)*Mathf.Min(deltaTime,.1f);
        Vector3 forward=PlanarForward(head),right=Vector3.Cross(Vector3.up,forward);
        Vector3 offset=transform.position-head.position;
        float depth=Vector3.Dot(offset,forward),side=Vector3.Dot(offset,right);
        float nextDepth=Mathf.Clamp(depth+step.y,minimumDepth,Mathf.Max(minimumDepth,maximumDepth));
        float nextSide=Mathf.Clamp(side+step.x,-maximumSideOffset,maximumSideOffset);
        transform.position+=forward*(nextDepth-depth)+right*(nextSide-side);
    }

    void Update()
    {
        RefreshCalibrationControls();
        RefreshToolControls();
        if (Client == null || showText == null) return;
        Vector3 indexPos, editPos; Quaternion indexRot, editRot; bool editing; int selected;
        bool hasIndex = Client.TryGetIndexTipPose(out indexPos, out indexRot);
        bool hasEdit = Client.TryGetEditedPointPose(out editPos, out editRot, out editing, out selected);
        bool jump = hasLastSample &&
            ((hasIndex && (Vector3.Distance(indexPos, lastIndexTipPos) > indexTipJumpThreshold ||
                           Quaternion.Angle(indexRot, lastIndexTipRot) > rotationJumpThresholdDeg)) ||
             (hasEdit && editing && (Vector3.Distance(editPos, lastEditedPos) > editedPointJumpThreshold ||
                           Quaternion.Angle(editRot, lastEditedRot) > rotationJumpThresholdDeg)));
        UpdateMonitor(hasIndex, indexPos, hasEdit, editPos, Client.KeyboardIsEditing, Client.KeyboardSelectedPoint, jump);
        if (hasIndex) { lastIndexTipPos=indexPos; lastIndexTipRot=indexRot; }
        if (hasEdit && editing) { lastEditedPos=editPos; lastEditedRot=editRot; }
        hasLastSample = hasIndex || (hasEdit && editing);
    }

    void UpdateMonitor(bool hasIndex, Vector3 indexPos, bool hasEdit, Vector3 editPos, bool editing, int selected, bool jump)
    {
        if (showText == null) return;
        showText.color = jump ? new Color(1f,.57f,.25f) : new Color(.81f,.91f,.97f);
        string tip = hasIndex ? $"X {indexPos.x:F3}    Y {indexPos.y:F3}    Z {indexPos.z:F3}" : "X --         Y --         Z --";
        int hover = Client == null ? -1 : Client.KeyboardHoveredPoint;
        string mode = Client == null ? "Direct" : Client.KeyboardSelectionMode;
        string edited = hasEdit && editing ? $"Edit XYZ  {editPos.x:F3}, {editPos.y:F3}, {editPos.z:F3}" : "Edit XYZ  --";
        showText.text = "<color=#52D9EF>FINGERTIP / WORLD [m]</color>\n" + tip +
            "\nPoint  " + (selected < 0 ? "None" : selected.ToString()) + "     Hover  " + (hover < 0 ? "None" : hover.ToString()) +
            "\nMode  " + mode + "     Editing  " + (editing ? "Active" : "Idle") +
            "\n" + edited + "\nHost  " + hostStatus;
    }

    public void Add(string s)
    {
        if (text != null) text.text += s;
    }

    public void Remove()
    {
        if (text != null && text.text.Length > 0) text.text = text.text.Substring(0, text.text.Length - 1);
    }

    public void RefreshIP()
    {
        IPAddress address;
        if (text == null || Client == null) return;
        if (!IPAddress.TryParse(text.text.Trim(), out address)) { hostStatus="Invalid IP address"; return; }
        text.text=address.ToString();
        Client.RefreshIP(text.text);
        hostStatus="Applied";
    }

    public void SwitchLRController()
    {
        if (Client != null) Client.LRinverse = !Client.LRinverse;
    }
}
