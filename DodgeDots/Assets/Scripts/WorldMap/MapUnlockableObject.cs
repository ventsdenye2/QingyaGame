using UnityEngine;

namespace DodgeDots.WorldMap
{
    public class MapUnlockableObject : MonoBehaviour
    {
        [Header("状态保存")]
        [Tooltip("用于记住这个路障是否已经被消除了，例如 'Bridge_Level1_Unlocked'")]
        public string unlockSaveKey;

        [Header("场景物体")]
        public GameObject objectToShow; // 桥
        public GameObject objectToHide; // 空气墙/路障
        public ParticleSystem unlockEffect;

        private void Start()
        {
            // 初始化时，只看存档记录。不看 Level 是否通过。
            // 这样保证了必须发生过“交互”这件事，状态才会改变。
            bool isUnlocked = false;
            if (!string.IsNullOrEmpty(unlockSaveKey))
            {
                isUnlocked = PlayerPrefs.GetInt(unlockSaveKey, 0) == 1;
            }
            UpdateState(isUnlocked);
        }

        // --- 新增：提供给 GeneralInteraction 调用的方法 ---
        public void Unlock()
        {
            // 1. 播放特效
            if (unlockEffect != null) unlockEffect.Play();

            // 2. 切换显隐
            UpdateState(true);

            // 3. 永久保存状态
            if (!string.IsNullOrEmpty(unlockSaveKey))
            {
                PlayerPrefs.SetInt(unlockSaveKey, 1);
                PlayerPrefs.Save();
            }
        }

        private void UpdateState(bool isUnlocked)
        {
            if (objectToShow != null) objectToShow.SetActive(isUnlocked);
            if (objectToHide != null) objectToHide.SetActive(!isUnlocked);
        }
    }
}