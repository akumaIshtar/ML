using UnityEngine;
using KINEMATION.KAnimationCore.Runtime.Core;

public class FPSBoneDebugger : MonoBehaviour
{
    public Transform cameraJoint; // 拖入 saiyide 的 camera_joint
    public Transform mainCamera;  // 拖入你的 Main Camera

    void Update()
    {
        if (cameraJoint == null || mainCamera == null) return;

        // 1. 获取 Root 和 Camera Joint 的朝向对比
        Vector3 rootForward = transform.forward;
        Vector3 jointForward = cameraJoint.forward;
        float dotProduct = Vector3.Dot(rootForward, jointForward);

        // 2. 计算 Camera Joint 相对于 Root 的局部旋转偏移
        Quaternion relativeRot = Quaternion.Inverse(transform.rotation) * cameraJoint.rotation;
        Vector3 eulerRot = relativeRot.eulerAngles;

        // 3. 计算位姿偏差信息
        string report = $"<color=yellow>[FPS Debugger]</color>\n";
        report += $"Z轴一致性 (Root vs Joint): {dotProduct:F2} (1为同向, -1为反向)\n";
        report += $"Joint 局部欧拉角: {eulerRot}\n";
        report += $"相机当前世界位姿: Pos {mainCamera.position}, Rot {mainCamera.rotation.eulerAngles}\n";

        // 4. 打印修复建议
        if (dotProduct < -0.9f)
        {
            report += $"<color=red>检测到 Z 轴完全反向！</color> 建议：相机 Point 需增加 Y 轴 180 度修正。\n";
        }

        if (Mathf.Abs(eulerRot.x - 180) < 10 || Mathf.Abs(eulerRot.z - 180) < 10)
        {
            report += $"<color=red>检测到倒置旋转！</color> 建议：CameraAnimator 需要 X/Z 轴 180 度翻转。\n";
        }

        Debug.Log(report);

        // 在 Scene 窗口画出辅助线
        Debug.DrawRay(cameraJoint.position, cameraJoint.forward * 0.5f, Color.blue); // Joint 前向
        Debug.DrawRay(cameraJoint.position, cameraJoint.up * 0.5f, Color.green);    // Joint 向上
        Debug.DrawRay(transform.position, transform.forward * 1f, Color.cyan);      // Root 前向
    }
}
