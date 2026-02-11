using UnityEngine;
using DodgeDots.Save; // 确保命名空间与你的项目一致
using DodgeDots.Dialogue;
using System.Text;

public class InteractionDebugger : MonoBehaviour
{
    [Header("调试设置")]
    [SerializeField] private KeyCode debugKey = KeyCode.F12; // 按下 F12 打印状态

    void Update()
    {
        if (Input.GetKeyDown(debugKey))
        {
            PrintAllInteractionStates();
        }
    }

    public void PrintAllInteractionStates()
    {
        if (SaveSystem.Current == null)
        {
            Debug.LogWarning("<color=yellow>[Debugger]</color> 存档系统未初始化或当前无存档。");
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<b><color=cyan>==== 当前 NPC 存档状态汇总 ====</color></b>");

        // 1. 打印所有已记录的交互状态
        sb.AppendLine($"<color=white>记录总数: {SaveSystem.Current.interactionStates.Count}</color>");

        foreach (var state in SaveSystem.Current.interactionStates)
        {
            if (state.EndsWith("_DISABLED"))
            {
                sb.AppendLine($"[禁用] <color=red>{state}</color>");
            }
            else
            {
                // 解析 ID:BranchIndex:StageIndex
                string[] parts = state.Split(':');
                if (parts.Length >= 3)
                {
                    sb.AppendLine($"[进度] ID: <color=green>{parts[0]}</color> | 分支索引: {parts[1]} | 阶段索引: {parts[2]}");
                }
                else
                {
                    sb.AppendLine($"[未知格式] {state}");
                }
            }
        }

        // 2. 打印当前激活的所有 Flags (这些会影响分支判定)
        sb.AppendLine("\n<b><color=cyan>==== 当前激活的游戏 Flags ====</color></b>");
        if (SaveSystem.Current.gameFlags != null && SaveSystem.Current.gameFlags.Count > 0)
        {
            foreach (var flag in SaveSystem.Current.gameFlags)
            {
                sb.AppendLine($"<color=orange>Flag: {flag}</color>");
            }
        }
        else
        {
            sb.AppendLine("无活跃 Flag");
        }

        Debug.Log(sb.ToString());
    }
}