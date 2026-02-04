using UnityEngine;

namespace DodgeDots.Enemy
{
    /// <summary>
    /// Boss发射源标记组件
    /// 挂载在Boss的子物体上，标识该位置为弹幕发射点
    /// </summary>
    public class EmitterPoint : MonoBehaviour
    {
        [Header("发射源设置")]
        [Tooltip("发射源类型")]
        [SerializeField] private EmitterType emitterType = EmitterType.MainCore;

        [Tooltip("发射源名称（用于调试）")]
        [SerializeField] private string emitterName = "Emitter";

        [Header("可视化设置")]
        [Tooltip("是否在Scene视图中显示发射源")]
        [SerializeField] private bool showGizmo = true;

        [Tooltip("Gizmo颜色")]
        [SerializeField] private Color gizmoColor = Color.red;

        [Tooltip("Gizmo大小")]
        [SerializeField] private float gizmoSize = 0.3f;

        public EmitterType EmitterType => emitterType;
        public string EmitterName => emitterName;
        public Vector2 Position => transform.position;

        private void OnDrawGizmos()
        {
            if (!showGizmo) return;

            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, gizmoSize);

            // 绘制方向指示
            Gizmos.DrawLine(transform.position, transform.position + transform.up * gizmoSize * 1.5f);
        }

        private void OnDrawGizmosSelected()
        {
            if (!showGizmo) return;

            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(transform.position, gizmoSize);
        }
    }
}
