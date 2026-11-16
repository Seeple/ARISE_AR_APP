using System.Collections.Generic;
using UnityEngine;
using BoneId = OVRSkeleton.BoneId;

// Reads the existing left-hand pinch and only the two fingertip bones.
internal sealed class TrajectoryMapHandPose
{
    OVRHand sourceHand;
    OVRSkeleton skeleton;
    IList<OVRBone> boundBones;
    Transform indexTip;
    Transform thumbTip;

    internal bool ReadPinch(OVRHand hand, out Vector3 gesturePoint)
    {
        gesturePoint = Vector3.zero;
        if (hand == null || !hand.IsDataValid || !hand.IsDataHighConfidence || hand.IsSystemGestureInProgress ||
            !hand.GetFingerIsPinching(OVRHand.HandFinger.Index))
            return false;

        if (sourceHand != hand || skeleton == null)
        {
            sourceHand = hand;
            skeleton = hand.GetComponent<OVRSkeleton>();
            boundBones = null;
        }

        if (skeleton == null || !skeleton.IsInitialized || !skeleton.IsDataValid || !skeleton.IsDataHighConfidence ||
            skeleton.GetSkeletonType() != OVRSkeleton.SkeletonType.HandLeft)
            return false;

        var bones = skeleton.Bones;
        if (bones == null) return false;
        if (boundBones != bones || indexTip == null || thumbTip == null)
        {
            indexTip = thumbTip = null;
            foreach (var bone in bones)
            {
                if (bone.Id == BoneId.Hand_IndexTip) indexTip = bone.Transform;
                else if (bone.Id == BoneId.Hand_ThumbTip) thumbTip = bone.Transform;
            }
            boundBones = bones;
        }
        if (indexTip == null || thumbTip == null) return false;

        // Proximity alone isn't sufficient: a relaxed hand may have nearly touching tips.
        gesturePoint = (indexTip.position + thumbTip.position) * .5f;
        return true;
    }
}
