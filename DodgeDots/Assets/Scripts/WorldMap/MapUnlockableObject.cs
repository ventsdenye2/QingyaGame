using DodgeDots.Save; // 引用存档系统
using UnityEngine;

namespace DodgeDots.WorldMap
{
    public class MapUnlockableObject : MonoBehaviour
    {
        [Header("状态保存")]
        [Tooltip("用于记住这个路障是否已经被消除了，这将作为 Flag Key 存入 SaveSystem，例如 'Bridge_Level1_Unlocked'")]
        public string unlockSaveKey;

        [Header("场景物体")]
        public GameObject objectToShow; // 桥
        public GameObject objectToHide; // 空气墙
        public ParticleSystem unlockEffect;

        private void Start()
        {
            // 初始化时，从 SaveSystem 读取 Flag
            bool isUnlocked = false;

            if (!string.IsNullOrEmpty(unlockSaveKey))
            {
                isUnlocked = SaveSystem.HasFlag(unlockSaveKey);
            }

            UpdateState(isUnlocked);
        }

        // --- 给 GeneralInteraction 调用的方法 ---
        public void Unlock()
        {
            // 播放特效
            if (unlockEffect != null) unlockEffect.Play();

            // 切换显隐
            UpdateState(true);

            // 永久保存状态 (存入 SaveSystem)
            if (!string.IsNullOrEmpty(unlockSaveKey))
            {
                SaveSystem.SetFlag(unlockSaveKey);
            }
        }

        private void UpdateState(bool isUnlocked)
        {
            if (objectToShow != null) objectToShow.SetActive(isUnlocked);
            if (objectToHide != null) objectToHide.SetActive(!isUnlocked);
        }
    }
}