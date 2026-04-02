using System;
using UnityEngine;
using KINEMATION.FPSAnimationPack.Scripts.Player;

namespace Core
{
    [DefaultExecutionOrder(-50)] // 确保在 FPSController 之后，动画更新之前执行
    [RequireComponent(typeof(IInputProvider))]
    [RequireComponent(typeof(CharacterController))]
    public class PlayerCombatBridge : MonoBehaviour
    {
        [Header("1P ViewModel (First Person)")]
        [Tooltip("拖入挂载了 FPSPlayer 的 1P 武器手臂节点")]
        public FPSPlayer fpsPlayer1P;

        [Header("3P Body (Third Person)")]
        [Tooltip("拖入 A FP T Pose 节点上的 3P Animator")]
        public Animator animator3P;

        private IInputProvider _inputProvider;
        private CharacterController _cc;
        private Animator _animator1P;

        private bool _wasFiring = false;

        // 缓存 3P Animator 的 Hash 值，极大提升性能
        private readonly int HASH_FIRE = Animator.StringToHash("Fire");
        private readonly int HASH_RELOAD = Animator.StringToHash("Reload");
        private readonly int HASH_GRENADE = Animator.StringToHash("ThrowGrenade");
        private readonly int HASH_PHYSICAL_SPEED = Animator.StringToHash("PhysicalSpeed");

        //private bool _isChangingWeapon = false;
        private float _weaponChangeCooldown = 0.5f;
        private float _lastWeaponChangeTime = -1f;
        private int _lastWeaponType = -1;
        private readonly int HASH_WEAPON_TYPE = Animator.StringToHash("WeaponType");
        private void Awake()
        {
            _inputProvider = GetComponent<IInputProvider>();
            _cc = GetComponent<CharacterController>();
        }

        private void Start()
        {
            if (fpsPlayer1P != null)
            {
                _animator1P = fpsPlayer1P.GetComponent<Animator>();
            }
        }

        private void Update()
        {
            if (_inputProvider == null || fpsPlayer1P == null) return;

            // 获取这一帧的所有玩家输入
            FrameInput input = _inputProvider.GetInput();
            fpsPlayer1P.SetMoveInput(input.Move);
            fpsPlayer1P.SetAimState(input.Aim);

            if (_cc != null && _animator1P != null)
            {
                // 我们只取 X 和 Z 轴的速度（平面移动），忽略 Y 轴（防止跳跃下落时手臂狂摆）
                Vector3 flatVelocity = new Vector3(_cc.velocity.x, 0f, _cc.velocity.z);
                float currentSpeed = flatVelocity.magnitude;

                // 将真实的物理速度传递给你刚才在 Blend Tree 设置的 PhysicalSpeed 参数
                _animator1P.SetFloat(HASH_PHYSICAL_SPEED, currentSpeed);
            }


            // 判断滚轮是否有明显的滚动（这里用 0.1 作为阈值，防止微小误触）
            // 并且加入一个简单的冷却，防止一次滚动切了好几把枪
            if (Mathf.Abs(input.ChangeWeaponScroll) > 0.1f && Time.time > _lastWeaponChangeTime + _weaponChangeCooldown)
            {
                // 注意：KINEMATION 原本的 OnMouseWheel 接收的是 InputValue，
                // 由于我们剥离了 PlayerInput，我们需要稍微修改一下 FPSPlayer 里的方法，
                // 或者直接调用它内部处理切枪的逻辑。
                //
                // 为了简单起见，我们假设你已经在 FPSPlayer 中添加了一个接收 float 的公共方法（见下一步）。

                fpsPlayer1P.HandleWeaponScroll(input.ChangeWeaponScroll);
                _lastWeaponChangeTime = Time.time;
            }
            // 获取 1P 视角当前手里拿着的武器
            var activeWeapon = fpsPlayer1P.GetActiveWeapon();
            if (activeWeapon == null) return;

            float currentWeaponType = (float)activeWeapon.weaponSettings.weaponClass;
            if (currentWeaponType != _lastWeaponType)
            {
                if (animator3P != null)
                {
                    animator3P.SetFloat("fWeaponType", currentWeaponType);
                    // 【新增】强行打断 3P 正在播放的任何动作（包括换弹），瞬间切回待机枢纽！
                    // 参数 1 代表你的 UpperBody 所在的层级索引 (Base Layer 是 0, UpperBody 是 1)
                    animator3P.Play("Weapon_Idle_Hub", 1, 0f);
                }
                _lastWeaponType = (int)currentWeaponType;
            }

            // ==========================================
            // 1. 处理开火 (Fire)
            // ==========================================
            if (input.Fire && !_wasFiring)
            {
                // 指挥 1P 武器开火（生成真实后坐力、扣除弹药、播放开火音效）
                activeWeapon.OnFirePressed();

                // 指挥 3P 身体播放开火震动（给别人看）
                //if (animator3P != null) animator3P.SetTrigger(HASH_FIRE);

                _wasFiring = true;
            }
            else if (!input.Fire && _wasFiring)
            {
                // 松开左键，停止 1P 的全自动连射逻辑
                activeWeapon.OnFireReleased();
                _wasFiring = false;
            }

            // ==========================================
            // 2. 处理换弹 (Reload)
            // ==========================================
            if (input.Reload)
            {
                // 判断当前弹药是否已满，不满才允许换弹
                if (activeWeapon.GetActiveAmmo() < activeWeapon.GetMaxAmmo())
                {
                    // 指挥 1P 播放精细的换弹流程和音效
                    fpsPlayer1P.OnReload();

                    // 指挥 3P 播放粗略的换弹动作（配合 UpperBody 遮罩）
                    if (animator3P != null) animator3P.SetTrigger(HASH_RELOAD);
                }
            }

            // ==========================================
            // 3. 处理扔手雷 (Grenade)
            // ==========================================
            if (input.ThrowGrenade)
            {
                // 指挥 1P 播放收枪、掏手雷、扔出的精细动作
                fpsPlayer1P.OnThrowGrenade();

                // 指挥 3P 播放扔手雷动作
                if (animator3P != null) animator3P.SetTrigger(HASH_GRENADE);
            }
            //处理切换射击模式
            if (input.ChangeFireMode)
            {
                fpsPlayer1P.OnChangeFireMode();
            }
        }
    }
}
