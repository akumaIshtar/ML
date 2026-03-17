using UnityEngine;
using System.Text;
using System.IO;

public class SkeletonAnalyzer : MonoBehaviour
{
    [Tooltip("输出的文本文件名")]
    public string outputFileName = "A—TP—Pose_SkeletonData.txt";

    void Start()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"========== 骨架轴向分析报告 ==========");
        sb.AppendLine($"分析对象: {gameObject.name}");
        sb.AppendLine($"生成时间: {System.DateTime.Now}");
        sb.AppendLine($"说明: W_Fwd (世界Z轴指向), W_Up (世界Y轴指向), W_Rgt (世界X轴指向)");
        sb.AppendLine("=========================================\n");

        // 从根节点开始递归遍历
        DumpTransformData(transform, sb, 0);

        // 将数据写入到 Unity 项目的 Assets 目录同级（或者 Assets 内部）
        // 这里保存在项目根目录，避免污染 Assets
        string path = Path.Combine(Application.dataPath, "../", outputFileName);
        File.WriteAllText(path, sb.ToString());

        Debug.Log($"<color=green><b>[骨架分析完成]</b></color> 数据已导出至: {path}\n请打开该文本文件并将内容发给我！");
    }

    private void DumpTransformData(Transform t, StringBuilder sb, int depth)
    {
        // 树状图缩进
        string indent = new string('-', depth * 2);
        if (depth > 0) indent = " " + indent + "> ";

        // 提取局部 Transform 数据
        string pos = $"L_Pos({t.localPosition.x:F3}, {t.localPosition.y:F3}, {t.localPosition.z:F3})";
        string rot = $"L_Rot({t.localEulerAngles.x:F3}, {t.localEulerAngles.y:F3}, {t.localEulerAngles.z:F3})";
        string scl = $"Scl({t.localScale.x:F3}, {t.localScale.y:F3}, {t.localScale.z:F3})";

        // 提取局部轴向在世界空间中的实际朝向（极其重要，用于排查相机看天、人物倒转）
        string wFwd = $"W_Fwd({t.forward.x:F2}, {t.forward.y:F2}, {t.forward.z:F2})";
        string wUp  = $"W_Up({t.up.x:F2}, {t.up.y:F2}, {t.up.z:F2})";
        string wRgt = $"W_Rgt({t.right.x:F2}, {t.right.y:F2}, {t.right.z:F2})";

        // 拼接单行数据
        sb.AppendLine($"{indent}{t.name}");
        sb.AppendLine($"    | {pos} | {rot} | {scl}");
        sb.AppendLine($"    | {wFwd} | {wUp} | {wRgt}");
        sb.AppendLine(""); // 空行分隔

        // 递归遍历所有子物体
        for (int i = 0; i < t.childCount; i++)
        {
            DumpTransformData(t.GetChild(i), sb, depth + 1);
        }
    }
}
