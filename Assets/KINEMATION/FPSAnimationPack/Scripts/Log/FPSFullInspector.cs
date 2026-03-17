using UnityEngine;
using KINEMATION.KAnimationCore.Runtime.Core;

public class FPSFullInspector : MonoBehaviour
{
    public Transform cameraJoint;
    public Transform weaponBone;
    public Transform mainCamera;

    void Update()
    {
        // 我们需要对比：Root(世界) -> Joint(局部) -> Camera(实际)
        // 以及：Root(世界) -> WeaponBone(局部)

        string report = "<color=cyan>[FPS Full Inspector Report]</color>\n";

        // 1. 摄像机链路检查
        if (cameraJoint != null)
        {
            report += $"[Camera Joint] LocalPos: {cameraJoint.localPosition.ToString("F4")}, " +
                      $"LocalRot: {cameraJoint.localRotation.eulerAngles.ToString("F2")}\n";
            report += $"[Camera Joint] WorldForward: {cameraJoint.forward.ToString("F2")}\n";
        }

        if (mainCamera != null)
        {
            report += $"[Main Camera] LocalPos: {mainCamera.localPosition.ToString("F4")}, " +
                      $"LocalRot: {mainCamera.localRotation.eulerAngles.ToString("F2")}\n";
        }

        // 2. 武器骨骼检查 (检查是否存在导致漂移的初始旋转)
        if (weaponBone != null)
        {
            report += $"[Weapon Bone] LocalPos: {weaponBone.localPosition.ToString("F4")}, " +
                      $"LocalRot: {weaponBone.localRotation.eulerAngles.ToString("F2")}\n";
            report += $"[Weapon Bone] WorldForward: {weaponBone.forward.ToString("F2")}\n";
        }

        // 3. 计算 Kinemation 脚本逻辑内部的偏差
        Vector3 rootForward = transform.forward;
        if (cameraJoint != null)
        {
            float camDot = Vector3.Dot(rootForward, cameraJoint.forward);
            report += $"相机对齐度: {camDot:F2} (应为1, 若为-1则计算必出错)\n";
        }

        if (weaponBone != null)
        {
            float wpDot = Vector3.Dot(rootForward, weaponBone.forward);
            report += $"武器对齐度: {wpDot:F2}\n";
        }

        // 4. 实时 AdsWeight 状态
        var player = GetComponent<KINEMATION.FPSAnimationPack.Scripts.Player.FPSPlayer>();
        if(player != null)
        {
            report += $"当前 AdsWeight: {player.AdsWeight:F2}\n";
        }

        Debug.Log(report);
    }
}
