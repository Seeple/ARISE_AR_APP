using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(VRController))]
[RequireComponent(typeof(ChunkVisualizer))]
public sealed class HeldToolVisualization : MonoBehaviour
{
    public enum TaskMode { Default, Knot }
    public const float KnotJawThreshold = .2f * AG16095GripperVisual.MaximumOpening;
    const float LiveWidthTimeoutSeconds = .75f;

    [SerializeField] GameObject knotRingPrefab;
    [SerializeField, Range(1024, 65535)] int graspImagePort = 10008;
    [Tooltip("Projected jaw midpoint in the incoming JPEG. The origin is the bottom left.")]
    [SerializeField] Vector2 projectedTcp = new Vector2(.5f, .5f);
    [Tooltip("Image-frame axes in trajectory TCP space: +X image right, +Y image up, +Z into the image.")]
    [SerializeField] Vector3 imageToTcpEuler;

    readonly List<GameObject> ringPool = new List<GameObject>();
    readonly List<GraspImageReceiver.Frame> pending = new List<GraspImageReceiver.Frame>();
    readonly Queue<Guid> retiredGrasps = new Queue<Guid>();
    GraspImageReceiver receiver;
    ChunkVisualizer chunk;
    VRController controller;
    string hostAddress;
    IPAddress host;
    Guid activeGrasp;
    bool liveClosed;
    bool imageProcessed;
    bool poseFound;
    bool showing;
    float lastWidthTime = float.NegativeInfinity;
    int shownRevision;
    int visibleRings;
    Pose ringInTcp;

    public TaskMode Mode { get; private set; }
    public float JawThreshold => Mode == TaskMode.Knot ? KnotJawThreshold : 0f;
    public string Status { get; private set; } = "Off";

    void Awake()
    {
        chunk = GetComponent<ChunkVisualizer>();
        controller = GetComponent<VRController>();
    }

    public void SelectTask(TaskMode mode)
    {
        if (Mode == mode && (mode == TaskMode.Default || receiver != null)) return;
        receiver?.Dispose();
        receiver = null;
        ResetCapture();
        Mode = mode;
        Status = "Off";
        if (mode != TaskMode.Knot) return;
        try
        {
            receiver = new GraspImageReceiver(graspImagePort);
            Status = "Waiting for grasp";
        }
        catch (SocketException e)
        {
            Status = "Image port unavailable";
            Debug.LogWarning($"Held tool image port {graspImagePort}: {e.Message}");
        }
    }

    void RefreshWorkstation()
    {
        if (hostAddress == controller.ip) return;
        hostAddress = controller.ip;
        IPAddress.TryParse(hostAddress, out host);
        ResetCapture();
        Status = "Waiting for grasp";
    }

    public void ObserveLiveJaw(IPAddress sender, float width, string graspId)
    {
        if (receiver == null) return;
        RefreshWorkstation();
        if (!Equals(host, sender)) return;

        lastWidthTime = Time.unscaledTime;
        liveClosed = IsBelowThreshold(width, JawThreshold);
        if (!liveClosed)
        {
            if (width >= JawThreshold && !float.IsInfinity(width) && activeGrasp != Guid.Empty) EndGrasp();
            Status = "Waiting for grasp";
            return;
        }
        if (!Guid.TryParseExact(graspId, "N", out Guid id) || id == Guid.Empty || retiredGrasps.Contains(id))
        {
            liveClosed = false;
            Status = "Waiting for grasp ID";
            return;
        }
        if (id == activeGrasp)
        {
            if (poseFound) Status = "Ring located";
            else if (!imageProcessed) Status = "Waiting for image";
            return;
        }

        EndGrasp();
        activeGrasp = id;
        Status = "Waiting for image";
    }

    static bool IsBelowThreshold(float width, float threshold) => width >= 0f && width < threshold;

    void LateUpdate()
    {
        if (receiver == null) return;
        // Re-read the workstation when the keyboard applies a new IP, even during silence.
        RefreshWorkstation();
        bool live = liveClosed && Time.unscaledTime - lastWidthTime <= LiveWidthTimeoutSeconds;
        ReceiveFrames(live);
        if (!live && liveClosed) Status = "Waiting for robot";
        bool canShow = live && poseFound && knotRingPrefab != null && chunk.showActionChunk &&
            (Calibration.instance == null || !Calibration.instance.IsRunning);
        if (!canShow)
        {
            HideRings();
            return;
        }
        if (!showing || shownRevision != chunk.TrajectoryRevision) RenderTrajectory();
    }

    void ReceiveFrames(bool live)
    {
        pending.RemoveAll(frame => frame.HasExpired);
        while (receiver.TryTakeFrame(out var frame))
        {
            if (frame.HasExpired || !Equals(host, frame.Sender) || retiredGrasps.Contains(frame.Id) ||
                (imageProcessed && frame.Id == activeGrasp)) continue;
            if (pending.Count == GraspImageReceiver.MaxPendingFrames) pending.RemoveAt(0);
            pending.Add(frame);
        }

        if (!live || imageProcessed) return;
        int index = pending.FindIndex(frame => frame.Id == activeGrasp);
        if (index < 0) return;
        var capture = pending[index];
        pending.RemoveAt(index);
        imageProcessed = true;
        var texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
        try
        {
            poseFound = ImageConversion.LoadImage(texture, capture.Jpeg) &&
                KnotGraspPose.TryEstimate(texture, projectedTcp, Quaternion.Euler(imageToTcpEuler), out ringInTcp);
        }
        finally { Destroy(texture); }
        Status = poseFound ? "Ring located" : "Ring unclear";
    }

    void RenderTrajectory()
    {
        int used = 0;
        Transform frame = chunk.GetTrajectoryRootTransform();
        for (int index = 0; chunk.TryGetTrajectoryPoint(index, out Pose tcp, out float width); index++)
        {
            if (!IsBelowThreshold(width, JawThreshold)) continue;
            if (used == ringPool.Count)
                ringPool.Add(Instantiate(knotRingPrefab, frame, false));
            var ring = ringPool[used++];
            ring.transform.SetLocalPositionAndRotation(tcp.position + tcp.rotation * ringInTcp.position,
                tcp.rotation * ringInTcp.rotation);
            ring.SetActive(true);
        }
        for (int i = used; i < visibleRings; i++) ringPool[i].SetActive(false);
        visibleRings = used;
        shownRevision = chunk.TrajectoryRevision;
        showing = true;
    }

    void HideRings()
    {
        for (int i = 0; i < visibleRings; i++) ringPool[i].SetActive(false);
        visibleRings = 0;
        showing = false;
    }

    void EndGrasp()
    {
        if (activeGrasp != Guid.Empty)
        {
            if (retiredGrasps.Count == 16) retiredGrasps.Dequeue();
            retiredGrasps.Enqueue(activeGrasp);
        }
        activeGrasp = Guid.Empty;
        imageProcessed = false;
        poseFound = false;
        HideRings();
    }

    void ResetCapture()
    {
        EndGrasp();
        retiredGrasps.Clear();
        pending.Clear();
        liveClosed = false;
        lastWidthTime = float.NegativeInfinity;
    }

    void OnDisable() => SelectTask(TaskMode.Default);

    void OnDestroy()
    {
        foreach (var ring in ringPool)
            if (ring != null) Destroy(ring);
    }
}
