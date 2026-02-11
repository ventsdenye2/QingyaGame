using System.Collections.Generic;
using UnityEngine;

namespace DodgeDots.WorldMap
{
    /// <summary>
    /// 路径记录器
    /// 挂在主角身上，记录历史位置供跟随者使用
    /// </summary>
    public class SnakePathRecorder : MonoBehaviour
    {
        // 存储位置历史记录
        // 我们使用 List 而不是 Queue，因为我们需要随机访问（索引访问）
        private List<Vector3> _positionHistory = new List<Vector3>();

        // 最大记录帧数（决定了队伍能拉多长）
        [SerializeField] private int maxRecordFrames = 200;

        // 公开访问历史记录
        public List<Vector3> PositionHistory => _positionHistory;

        private Vector3 _lastRecordPos;

        private void Awake()
        {
            // 初始化列表，填满当前位置，防止跟随者一开始报错或乱飞
            for (int i = 0; i < maxRecordFrames; i++)
            {
                _positionHistory.Add(transform.position);
            }
            _lastRecordPos = transform.position;
        }

        private void FixedUpdate()
        {
            // 只有当主角移动时才记录新路径点
            // 这样当主角停下时，跟随者也会保持在身后的相对位置停下，而不是撞上主角
            if (Vector3.Distance(transform.position, _lastRecordPos) > 0.001f)
            {
                // 在列表头部插入当前位置
                _positionHistory.Insert(0, transform.position);

                // 移除列表尾部多余的位置，保持列表长度固定
                if (_positionHistory.Count > maxRecordFrames)
                {
                    _positionHistory.RemoveAt(_positionHistory.Count - 1);
                }

                _lastRecordPos = transform.position;
            }
        }
    }
}