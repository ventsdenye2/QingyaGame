using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DodgeDots.Audio;

public class BeatMapperWindow : EditorWindow
{
    BGMManager bgmManager;
    List<double> beatTimes = new List<double>();
    Vector2 scroll;
    bool recording = false;

    [MenuItem("Window/Beat Mapper")]
    public static void Open()
    {
        GetWindow<BeatMapperWindow>("Beat Mapper");
    }

    void OnGUI()
    {
        GUILayout.Label("Beat Mapper (在 Play Mode 下录制)", EditorStyles.boldLabel);
        bgmManager = EditorGUILayout.ObjectField("BGM Manager", bgmManager, typeof(BGMManager), true) as BGMManager;

        if (!EditorApplication.isPlaying)
            EditorGUILayout.HelpBox("请进入 Play Mode 后使用窗口录制节拍（或手动点击 'Record Beat'）。", MessageType.Info);
        else
        {
            // Play Mode 下如果没有指定，尝试自动查找场景中的 BGMManager
            if (bgmManager == null)
            {
                bgmManager = FindObjectOfType<BGMManager>();
                if (bgmManager != null)
                    EditorGUILayout.HelpBox("已自动找到场景中的 BGMManager（你也可以手动拖拽）。", MessageType.Info);
                else
                    EditorGUILayout.HelpBox("未找到场景中的 BGMManager：请把场景中的 BGMManager 拖到窗口，或在层级中添加一个。", MessageType.Warning);
            }
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button(recording ? "Stop Recording" : "Start Recording"))
        {
            ToggleRecording();
        }
        if (GUILayout.Button("Record Beat (Space)"))
        {
            RecordBeat();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(6);

        GUILayout.Label($"已录制 {beatTimes.Count} 个节拍");
        scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(150));
        for (int i = 0; i < beatTimes.Count; i++)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label((i + 1).ToString(), GUILayout.Width(30));
            beatTimes[i] = EditorGUILayout.DoubleField(beatTimes[i]);
            if (GUILayout.Button("X", GUILayout.Width(24)))
            {
                beatTimes.RemoveAt(i);
                i--;
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();

        GUILayout.Space(6);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear")) beatTimes.Clear();
        if (GUILayout.Button("Sort")) beatTimes.Sort();
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        if (GUILayout.Button("Save to BeatMap Asset"))
        {
            SaveBeatMap();
        }

        // 捕获空格按键在窗口内生效
        var e = Event.current;
        if (recording && e != null && e.type == EventType.KeyDown && e.keyCode == KeyCode.Space)
        {
            RecordBeat();
            e.Use();
        }
    }

    void ToggleRecording()
    {
        recording = !recording;
    }

    void RecordBeat()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("请在 Play Mode 下录制以获得准确的 DSP 时间。");
            return;
        }

        if (bgmManager == null)
        {
            // 尝试再次自动查找，可能在 Play Mode 切换时引用丢失
            bgmManager = FindObjectOfType<BGMManager>();
            if (bgmManager == null)
            {
                Debug.LogWarning("未指定 BGMManager，且场景中未找到实例。请把场景中的 BGMManager 拖到窗口。");
                return;
            }
        }

        double dspStart = bgmManager.GetDspStart();
        double now = bgmManager.GetDspNow();

        if (dspStart == 0)
        {
            Debug.LogWarning("检测到 BGMManager 尚未初始化播放起点 (dspStart==0)。请确认 BGMManager 的 Start 已执行并已经开始播放，或稍后再录制。");
            // 仍然允许记录相对于现在的时间，但提示用户
        }

        double t = now - dspStart;
        if (t < 0) t = 0;
        beatTimes.Add(t);
        Repaint();
        Debug.Log($"Recorded beat #{beatTimes.Count} at {t:F3}s (dspNow={now:F6}, dspStart={dspStart:F6})");
    }

    void SaveBeatMap()
    {
        string path = EditorUtility.SaveFilePanelInProject("Save BeatMap", "NewBeatMap", "asset", "Choose a path to save BeatMap");
        if (string.IsNullOrEmpty(path)) return;

        var asset = ScriptableObject.CreateInstance<BeatMap>();
        asset.beatTimes = beatTimes.ToArray();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
        Debug.Log($"Saved BeatMap ({beatTimes.Count} beats) to {path}");
    }
}
