using UnityEngine;
using RootMotion.FinalIK;

namespace Core
{
    // 优先级 10：在 FPSController 之后执行
    [DefaultExecutionOrder(10)]
    public class FPSAimBridge : MonoBehaviour
    {
        [Header("Final IK Setup")]
        public AimIK aimIK;

        [Header("Transforms (Camera & Target)")]
        public Transform cameraContainer; // 你的 Main Camera
        public Transform headBone;        // Eye_Socket
        public Transform aimTarget;       // 场景里的靶子

        public float aimDistance = 50f;

        private CharacterController _cc;

        private void Start()
        {
            if (aimIK == null) aimIK = GetComponent<AimIK>();
            _cc = GetComponent<CharacterController>();

            // 订阅 Final IK 结算完成的瞬间事件
            if (aimIK != null) aimIK.solver.OnPostUpdate += SnapCameraToHead;
        }

        private void OnDestroy()
        {
            if (aimIK != null) aimIK.solver.OnPostUpdate -= SnapCameraToHead;
        }

        private void LateUpdate()
        {
            if (aimIK == null || cameraContainer == null || aimTarget == null) return;

            // 1. 生成绝对免疫 IK 干扰的稳定靶子
            float currentHeight = _cc != null ? _cc.height : 2.0f;
            Vector3 stableEyeAnchor = transform.position + Vector3.up * (currentHeight * 0.85f);
            aimTarget.position = stableEyeAnchor + cameraContainer.forward * aimDistance;
        }

        // AimIK 弯完腰后，只同步摄像机，不再去碰任何手部和枪械骨骼！
        private void SnapCameraToHead()
        {
            if (cameraContainer != null && headBone != null)
            {
                cameraContainer.position = headBone.position;
            }
        }
    }
}
