using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;


public class GeminiSceneAssistant : EditorWindow
{
    private string apiKey = "";
    // 新增：允许自定义 Base URL，方便使用国内 API 中转代理
    private string baseUrl = "https://generativelanguage.googleapis.com";
    // 新增：允许自定义模型名称
    private string modelName = "gemini-1.5-flash";

    private string userPrompt = "帮我看看当前场景，并在原点创建一个叫 'PlayerBase' 的 Cube。";
    private string aiResponse = "";
    private Vector2 scrollPos;
    private bool isRequesting = false;

    [MenuItem("Tools/Gemini AI 助手 (场景修改)")]
    public static void ShowWindow()
    {
        GetWindow<GeminiSceneAssistant>("Gemini AI 助手");
    }

    private void OnEnable()
    {
        // 自动读取上次保存的配置
        apiKey = EditorPrefs.GetString("Gemini_API_Key", "");
        baseUrl = EditorPrefs.GetString("Gemini_Base_Url", "https://generativelanguage.googleapis.com");
        modelName = EditorPrefs.GetString("Gemini_Model_Name", "gemini-1.5-flash");
    }

    private void OnGUI()
    {
        GUILayout.Label("Gemini Unity 场景协作插件 V2", EditorStyles.boldLabel);

        // --- 核心配置区 ---
        EditorGUI.BeginChangeCheck();
        apiKey = EditorGUILayout.TextField("API Key", apiKey);
        baseUrl = EditorGUILayout.TextField("API Base URL", baseUrl);
        modelName = EditorGUILayout.TextField("模型名称", modelName);
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetString("Gemini_API_Key", apiKey);
            EditorPrefs.SetString("Gemini_Base_Url", baseUrl);
            EditorPrefs.SetString("Gemini_Model_Name", modelName);
        }

        GUILayout.Space(10);
        GUILayout.Label("输入你的需求：", EditorStyles.boldLabel);
        userPrompt = EditorGUILayout.TextArea(userPrompt, GUILayout.Height(60));

        GUI.enabled = !isRequesting && !string.IsNullOrEmpty(apiKey);
        if (GUILayout.Button("发送给 Gemini 并执行", GUILayout.Height(40)))
        {
            _ = SendToGemini();
        }
        GUI.enabled = true;

        GUILayout.Space(10);
        GUILayout.Label("AI 回复与执行日志：", EditorStyles.boldLabel);
        scrollPos = GUILayout.BeginScrollView(scrollPos, EditorStyles.helpBox);
        EditorGUILayout.TextArea(aiResponse, EditorStyles.wordWrappedLabel);
        GUILayout.EndScrollView();
    }

    private async Task SendToGemini()
    {
        isRequesting = true;
        aiResponse = "正在读取场景...\n";
        Repaint();

        string sceneContext = GetSceneHierarchy();

        string systemInstruction = @"你是一个Unity引擎助手。
当前场景包含以下物体：" + sceneContext + @"
请回答用户的问题。如果你需要直接修改场景，请严格在回复末尾使用以下特定格式的指令（每行一个）：
- 创建物体: [CREATE:物体名称:基础类型] (例如: [CREATE:MyCube:Cube])
- 重命名: [RENAME:旧名称:新名称] (例如: [RENAME:Directional Light:MainLight])
不要随意编造格式，只能用这两种。";

        string fullPrompt = systemInstruction + "\n\n用户需求：" + userPrompt;
        string jsonPayload = $"{{\"contents\":[{{\"parts\":[{{\"text\":\"{EscapeJson(fullPrompt)}\"}}]}}]}}";

        aiResponse += "正在发送网络请求...\n";
        Repaint();

        // 动态构建 URL
        string url = $"{baseUrl.TrimEnd('/')}/v1beta/models/{modelName}:generateContent?key={apiKey}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            var operation = request.SendWebRequest();
            while (!operation.isDone) await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                // 打印更详细的错误信息
                aiResponse = $"❌ 网络请求失败!\n错误码: {request.responseCode}\n报错信息: {request.error}\n";
                aiResponse += $"\n[排查建议]:\n1. 请检查你的 Base URL 是否正确 (当前为: {baseUrl})\n2. 检查模型名是否正确 ({modelName})\n3. 如果在国内直连 Google 官方接口，请确保代理软件开启了 TUN(虚拟网卡) 模式。";

                // 如果服务器有返回具体的错误 JSON，也打印出来
                if (!string.IsNullOrEmpty(request.downloadHandler.text))
                {
                    aiResponse += "\n\n服务器返回详情:\n" + request.downloadHandler.text;
                }
            }
            else
            {
                string responseText = ExtractTextFromJson(request.downloadHandler.text);
                aiResponse = responseText + "\n\n--- 执行结果 ---\n";
                ExecuteCommands(responseText);
            }
        }

        isRequesting = false;
        Repaint();
    }

    private string GetSceneHierarchy()
    {
        StringBuilder sb = new StringBuilder();
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var go in rootObjects)
        {
            sb.AppendLine("- " + go.name);
        }
        return sb.ToString();
    }

    private void ExecuteCommands(string aiText)
    {
        string[] lines = aiText.Split('\n');
        foreach (string line in lines)
        {
            if (line.Contains("[CREATE:"))
            {
                string[] parts = line.Split(':');
                if (parts.Length >= 3)
                {
                    string objName = parts[1].Trim();
                    string typeStr = parts[2].Replace("]", "").Trim();
                    if (System.Enum.TryParse(typeStr, true, out PrimitiveType pType))
                    {
                        GameObject newObj = GameObject.CreatePrimitive(pType);
                        newObj.name = objName;
                        Undo.RegisterCreatedObjectUndo(newObj, "AI Create Object");
                        aiResponse += $"✅ 成功创建: {objName}\n";
                    }
                }
            }
            else if (line.Contains("[RENAME:"))
            {
                string[] parts = line.Split(':');
                if (parts.Length >= 3)
                {
                    string oldName = parts[1].Trim();
                    string newName = parts[2].Replace("]", "").Trim();
                    GameObject target = GameObject.Find(oldName);
                    if (target != null)
                    {
                        Undo.RecordObject(target, "AI Rename Object");
                        target.name = newName;
                        aiResponse += $"✅ 成功重命名: {oldName} -> {newName}\n";
                    }
                    else
                    {
                        aiResponse += $"❌ 找不到物体: {oldName}\n";
                    }
                }
            }
        }
    }

    private string EscapeJson(string text)
    {
        return text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
    }

    private string ExtractTextFromJson(string json)
    {
        int textIndex = json.IndexOf("\"text\": \"");
        if (textIndex == -1) return "解析 JSON 失败:\n" + json;
        int startIndex = textIndex + 9;
        int endIndex = json.IndexOf("\"", startIndex);
        while (json[endIndex - 1] == '\\') endIndex = json.IndexOf("\"", endIndex + 1);
        string extracted = json.Substring(startIndex, endIndex - startIndex);
        return extracted.Replace("\\n", "\n").Replace("\\\"", "\"");
    }
}
