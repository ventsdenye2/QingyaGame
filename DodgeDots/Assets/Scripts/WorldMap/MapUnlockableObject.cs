using DodgeDots.Save;
using UnityEngine;

namespace DodgeDots.WorldMap
{
    public class MapUnlockableObject : MonoBehaviour
    {
        public enum InitialState { Show, Hide }

        [Header("基础设置")]
        [Tooltip("在没有任何 Flag 满足的情况下，物体的默认状态")]
        public InitialState defaultState = InitialState.Hide;

        [Header("显示条件")]
        [Tooltip("满足该 Flag 时，物体变为【显示】状态")]
        public string showIfFlag;

        [Header("隐藏条件 (优先级最高)")]
        [Tooltip("满足该 Flag 时，物体变为【隐藏】状态 (哪怕满足了显示条件也会被隐藏)")]
        public string hideIfFlag;

        [Header("目标物体")]
        public GameObject targetObject;

        private void Start()
        {
            // 自动关联逻辑：如果没填，找子物体；没子物体，找自己
            if (targetObject == null)
            {
                if (transform.childCount > 0) targetObject = transform.GetChild(0).gameObject;
                else targetObject = this.gameObject;
            }

            RefreshState();
        }

        private void Update()
        {
            // 每 0.5 秒同步一次状态，确保交互后立即反应
            if (Time.frameCount % 30 == 0) RefreshState();
        }

        public void RefreshState()
        {
            if (targetObject == null) return;
            if (SaveSystem.Current == null) SaveSystem.LoadOrCreate();

            // --- 核心逻辑计算 ---

            // 1. 设定初始值
            bool finalState = (defaultState == InitialState.Show);

            // 2. 检查显示条件：如果填了且满足，状态设为 true
            if (!string.IsNullOrEmpty(showIfFlag) && SaveSystem.HasFlag(showIfFlag))
            {
                finalState = true;
            }

            // 3. 检查隐藏条件：如果填了且满足，状态强制设为 false
            if (!string.IsNullOrEmpty(hideIfFlag) && SaveSystem.HasFlag(hideIfFlag))
            {
                finalState = false;
            }

            // 应用状态
            if (targetObject.activeSelf != finalState)
            {
                targetObject.SetActive(finalState);
            }
        }
    }
}