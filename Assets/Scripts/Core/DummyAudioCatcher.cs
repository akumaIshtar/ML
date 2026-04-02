using UnityEngine;

namespace Core
{
    // 这个脚本的作用就是充当“消音器”和“事件垃圾桶”
    // 专门吸收 3P 模型动画里携带的音频事件，防止控制台报错
    public class DummyAudioCatcher : MonoBehaviour
    {
        // 武器相关声音
        public void PlayWeaponSound(int clipIndex) { }
        public void PlayFireSound() { }

        // 玩家肢体相关声音
        public void PlayPlayerSound(int soundIndex) { }
        public void PlayEquipSound() { }
        public void PlayUnEquipSound() { } // 解决你当前报错的正是这个！
        public void PlayAimSound() { }

        // 步态相关声音（如果跑动动画里也有事件的话）
        public void PlayWalkSound() { }
        public void PlaySprintSound() { }
        public void PlayJumpSound() { }
        public void PlayLandSound() { }
    }
}
