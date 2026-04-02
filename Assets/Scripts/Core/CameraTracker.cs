using UnityEngine;
using RootMotion.FinalIK;

public class CameraTracker : MonoBehaviour
{
    public AimIK aimIK;
    public Transform cameraContainer;
    public Transform headBone;

    private void OnEnable()
    {
        // 告诉 AimIK：当你把全身骨头都扭完之后，叫我一声！
        if (aimIK != null)
            aimIK.solver.OnPostUpdate += SnapCameraToHead;
    }

    private void OnDisable()
    {
        if (aimIK != null)
            aimIK.solver.OnPostUpdate -= SnapCameraToHead;
    }

    // 这个方法会在 AimIK 刚刚把头转到正确位置的瞬间执行
    private void SnapCameraToHead()
    {
        if (cameraContainer != null && headBone != null)
        {
            cameraContainer.position = headBone.position;
            cameraContainer.rotation = headBone.rotation;
        }
    }
}
