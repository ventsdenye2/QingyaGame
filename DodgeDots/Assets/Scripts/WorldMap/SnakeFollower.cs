using UnityEngine;

namespace DodgeDots.WorldMap
{
    /// <summary>
    /// 跟随者控制器
    /// 读取主角的路径记录并设置自己的位置
    /// </summary>
    public class SnakeFollower : MonoBehaviour
    {
        [Header("设置")]
        [Tooltip("主角身上的记录器")]
        [SerializeField] private SnakePathRecorder pathRecorder;

        [Tooltip("延迟帧数：决定该角色落后主角多少距离。建议每人间隔 10-20")]
        [SerializeField] private int frameDelay = 20;

        private void Update()
        {
            if (pathRecorder == null) return;

            var history = pathRecorder.PositionHistory;

            // 确保我们想要读取的索引在历史记录范围内
            int targetIndex = Mathf.Clamp(frameDelay, 0, history.Count - 1);

            // 直接设置位置到历史记录中的某一点
            // 这样跟随者会完全重走主角的路径，包括拐弯
            transform.position = history[targetIndex];
        }
    }
}