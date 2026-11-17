# Knot tool rendering

Open the keyboard and tap **Knot** in **Tool Task**, to the right of **Monitor**. The lit button shows the active task. **Default** is selected at startup and disables tool rendering and the image listener. If Knot is selected after a grasp, open and close the jaws again to trigger a new photograph.

Knot uses a jaw width strictly below 0.019 m (20% of the AG16095's 95 mm opening). A ring is displayed at each visible action-chunk point whose own `gripperWidth` is also below 0.019 m. Missing widths are skipped. The rings are hidden on release, if current jaw data becomes stale, while calibrating, or when the action chunk is hidden.

The rigid red ring has outside diameters of 60 × 50 mm, a 9 mm cord and material alpha of 0.5. Its pose is relative to the trajectory TCP and its mesh uses metres in the calibrated scene. Changing the ghost gripper scale has no effect on the ring.

## Workstation message contract

The workstation code lives outside this repository. The headset expects the following from the IP shown in its keyboard.

- Keep publishing the existing robot BSON datagrams to UDP **10001**. When the measured left jaw width crosses from at or above 0.019 m to below it, generate a fresh random UUID, capture one wrist JPEG and publish that UUID as the string `leftGraspId` (32 hexadecimal digits, no hyphens) in the robot state. Keep publishing the same ID in every closed-state datagram for that grasp. Omit it on opening. `leftGripperState[0]` remains the measured width in metres.
- Open one TCP connection to Quest port **10008** for the exposure. Send `ARK1` as four ASCII bytes, the 32 ASCII bytes of the same UUID, the JPEG byte count as an unsigned 32-bit big-endian integer, then the JPEG bytes. Close the connection after sending. Send an upright, physically oriented 8-bit JPEG with a long side of at most 640 px and at most 512 KiB. Rotate the pixels before sending when needed. The receiver accepts the image and robot state in either order, with a two-second pending-image window.
- Continue sending the normal action chunk to UDP **10006**, with each point's explicit `gripperWidth` in metres. The headset times out the live jaw after 750 ms of silence.

## Wrist camera setup

In the **Teleoperation** scene, **Main → Held Tool Visualization** contains the following installation settings.

- **Projected Tcp** is the approximate jaw midpoint in normalized image coordinates with the origin at the bottom left. The default is the image center. Use a fixed camera crop centered on the gripping point or set this value once for the actual crop.
- **Image To Tcp Euler** rotates the image-plane axes into the incoming trajectory TCP frame. Image +X is right, +Y is up and +Z points into the image. The default uses aligned axes. Set this once for the wrist camera mount.

A downsampled frame is searched for the red ring's opening near the projected jaw midpoint. The known ring dimensions give the scale for estimating the contact point on the ring and its in-plane direction. One estimate is held for the duration of the grasp. A frame with an ambiguous, heavily occluded or out-of-frame opening produces no overlay. The single-view estimate assumes the ring lies approximately in the image plane at the jaws. Large tilt, depth offset and camera-mount errors reduce alignment accuracy.

For a new setup, test one grab near each side of the ring while the opening is visible. Check the offset direction in the headset as the TCP rotates before using the overlay for close alignment.
