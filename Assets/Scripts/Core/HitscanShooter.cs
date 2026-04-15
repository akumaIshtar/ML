using UnityEngine;
using KINEMATION.FPSAnimationPack.Scripts.Weapon;
using KINEMATION.FPSAnimationPack.Scripts.Player;

namespace Core
{
    [RequireComponent(typeof(FPSPlayer))]
    [DefaultExecutionOrder(10)]
    public class HitscanShooter : MonoBehaviour
    {
        [Header("Hitscan Settings")]
        public Transform cameraTransform;
        public float damagePerShot = 20f;
        public float range = 1000f;
        public LayerMask hitMask = ~0;

        private FPSPlayer _fpsPlayer;
        private HitscanVisuals _visuals;

        private void Start()
        {
            // 因 HitscanShooter 可能挂载在身体子节点而不是根节点，所以使用 GetComponentInParent
            _fpsPlayer = GetComponentInParent<FPSPlayer>();
            _visuals = GetComponent<HitscanVisuals>();

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            // 使用协同程序延迟订阅，确保 FPSPlayer.Start() 中实例化的武器全部生成完毕
            StartCoroutine(SubscribeDelayed());
        }

        private System.Collections.IEnumerator SubscribeDelayed()
        {
            // 等待直到同一帧的所有 Start 被调用完毕
            yield return new WaitForEndOfFrame();

            if (_fpsPlayer != null)
            {
                // 从 fpsPlayer 根节点向下寻找所有的 FPSWeapon（包含被隐藏的克隆体）
                FPSWeapon[] weapons = _fpsPlayer.GetComponentsInChildren<FPSWeapon>(true);

                Debug.Log($"[HitscanShooter] 延迟获取完毕：找到了 {weapons.Length} 把武器，正在订阅开火事件。");

                foreach (var weapon in weapons)
                {
                    weapon.OnWeaponFired += HandleWeaponFired;
                }
            }
            else
            {
                Debug.LogError("[HitscanShooter] 错误：未能在父节点或自身找到 FPSPlayer 组件！");
            }
        }

        private void OnDestroy()
        {
            if (_fpsPlayer != null)
            {
                FPSWeapon[] weapons = _fpsPlayer.GetComponentsInChildren<FPSWeapon>(true);
                foreach (var weapon in weapons)
                {
                    weapon.OnWeaponFired -= HandleWeaponFired;
                }
            }
        }



        private void HandleWeaponFired()
        {
            // 【日志 2：检查事件是否成功触发】
            Debug.Log("<color=green>[HitscanShooter] HandleWeaponFired 被触发！玩家开枪了。</color>");

            if (cameraTransform == null)
            {
                Debug.LogWarning("[HitscanShooter] 警告：尚未设置 cameraTransform！");
                return;
            }

            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

            // 【Pro Tip：在 Scene 视图画出射线，留存 2 秒，方便你肉眼观察射线对不对】
            Debug.DrawRay(ray.origin, ray.direction * range, Color.red, 2f);

            Vector3 hitPoint = ray.GetPoint(range);
            Vector3 hitNormal = -ray.direction;
            bool didHit = false;

            if (Physics.Raycast(ray, out RaycastHit hitInfo, range, hitMask))
            {
                hitPoint = hitInfo.point;
                hitNormal = hitInfo.normal;
                didHit = true;

                // 【日志 3：检查射线打中了什么】
                Debug.Log($"[HitscanShooter] 射线命中了物体: <b>{hitInfo.collider.name}</b> (Layer: {LayerMask.LayerToName(hitInfo.collider.gameObject.layer)})");

                IDamageable damageable = hitInfo.collider.GetComponentInParent<IDamageable>();

                // 【日志 4：检查目标能否受伤】
                if (damageable != null)
                {
                    Debug.Log($"[HitscanShooter] 目标 {hitInfo.collider.name} 具有 IDamageable 接口，造成伤害 {damagePerShot}！");
                    damageable.TakeDamage(damagePerShot);
                }
                else
                {
                    Debug.LogWarning($"[HitscanShooter] 目标 {hitInfo.collider.name} 上没有找到 IDamageable 接口，无法造成伤害。");
                }
            }
            else
            {
                // 【日志 5：检查未命中情况】
                Debug.Log("[HitscanShooter] 射线未命中任何有效物体（打空了）。");
            }

            if (_visuals != null)
            {
                FPSWeapon activeWeapon = _fpsPlayer.GetActiveWeapon();
                Transform barrelTransform = activeWeapon != null && activeWeapon.aimPoint != null
                                            ? activeWeapon.aimPoint
                                            : cameraTransform;

                _visuals.PlayHitscanVisuals(barrelTransform.position, hitPoint, hitNormal, didHit);
            }
        }
    }
}
