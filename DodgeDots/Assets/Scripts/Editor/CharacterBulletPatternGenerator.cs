using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using DodgeDots.Enemy;
using DodgeDots.Bullet;

namespace DodgeDots.Editor
{
    /// <summary>
    /// 汉字弹幕模式生成器
    /// 可以输入汉字并自动生成弹幕位置数据
    /// </summary>
    public class CharacterBulletPatternGenerator : EditorWindow
    {
        // 输入参数
        private string inputCharacter = "弹";
        private Font font;
        private int fontSize = 128;
        private int textureSize = 256;
        private float samplingDensity = 0.5f;
        private bool useOutlineOnly = false;
        private float outlineThickness = 2f;
        private int maxBulletCount = 500;
        private bool limitBulletCount = true;

        // 输出配置
        private CharacterBulletPattern targetPattern;
        private BulletConfig defaultBulletConfig;
        private string savePath = "Assets/ScriptableObjects/CharacterPatterns";

        // 预览
        private Texture2D previewTexture;
        private Vector2 scrollPosition;
        private List<Vector2> generatedPositions = new List<Vector2>();

        [MenuItem("DodgeDots/Character Bullet Pattern Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<CharacterBulletPatternGenerator>("汉字弹幕生成器");
            window.minSize = new Vector2(400, 600);
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.LabelField("汉字弹幕模式生成器", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 输入参数
            EditorGUILayout.LabelField("输入参数", EditorStyles.boldLabel);
            inputCharacter = EditorGUILayout.TextField("汉字", inputCharacter);
            font = (Font)EditorGUILayout.ObjectField("字体", font, typeof(Font), false);
            fontSize = EditorGUILayout.IntSlider("字体大小", fontSize, 32, 512);
            textureSize = EditorGUILayout.IntSlider("纹理分辨率", textureSize, 128, 1024);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("弹幕数量控制", EditorStyles.boldLabel);
            samplingDensity = EditorGUILayout.Slider("采样密度 (越小弹幕越少)", samplingDensity, 0.1f, 2f);
            limitBulletCount = EditorGUILayout.Toggle("限制最大弹幕数量", limitBulletCount);
            if (limitBulletCount)
            {
                maxBulletCount = EditorGUILayout.IntSlider("最大弹幕数量", maxBulletCount, 50, 2000);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("样式选项", EditorStyles.boldLabel);
            useOutlineOnly = EditorGUILayout.Toggle("仅使用轮廓", useOutlineOnly);
            if (useOutlineOnly)
            {
                outlineThickness = EditorGUILayout.Slider("轮廓厚度", outlineThickness, 1f, 10f);
            }

            EditorGUILayout.Space();

            // 生成按钮
            if (GUILayout.Button("生成预览", GUILayout.Height(30)))
            {
                GeneratePreview();
            }

            EditorGUILayout.Space();

            // 预览
            if (previewTexture != null)
            {
                EditorGUILayout.LabelField($"预览 (弹幕数量: {generatedPositions.Count})", EditorStyles.boldLabel);
                GUILayout.Label(previewTexture, GUILayout.Width(textureSize), GUILayout.Height(textureSize));
            }

            EditorGUILayout.Space();

            // 保存配置
            EditorGUILayout.LabelField("保存配置", EditorStyles.boldLabel);
            targetPattern = (CharacterBulletPattern)EditorGUILayout.ObjectField("目标配置", targetPattern, typeof(CharacterBulletPattern), false);
            defaultBulletConfig = (BulletConfig)EditorGUILayout.ObjectField("默认子弹配置", defaultBulletConfig, typeof(BulletConfig), false);
            savePath = EditorGUILayout.TextField("保存路径", savePath);

            EditorGUILayout.Space();

            if (GUILayout.Button("保存为新配置", GUILayout.Height(30)))
            {
                SaveAsNewPattern();
            }

            if (targetPattern != null && GUILayout.Button("更新现有配置", GUILayout.Height(30)))
            {
                UpdateExistingPattern();
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 生成预览
        /// </summary>
        private void GeneratePreview()
        {
            if (string.IsNullOrEmpty(inputCharacter))
            {
                EditorUtility.DisplayDialog("错误", "请输入汉字", "确定");
                return;
            }

            // 生成汉字纹理
            Texture2D charTexture = GenerateCharacterTexture(inputCharacter);
            if (charTexture == null)
            {
                EditorUtility.DisplayDialog("错误", "生成汉字纹理失败", "确定");
                return;
            }

            // 采样生成弹幕位置
            generatedPositions = SampleTextureForBullets(charTexture);

            // 生成预览纹理（显示弹幕位置）
            previewTexture = GeneratePreviewTexture(charTexture, generatedPositions);

            // 清理临时纹理
            DestroyImmediate(charTexture);

            Debug.Log($"生成完成：{generatedPositions.Count} 个弹幕位置");
        }

        /// <summary>
        /// 生成汉字纹理
        /// </summary>
        private Texture2D GenerateCharacterTexture(string character)
        {
            // 创建临时 GameObject 和 TextMesh
            GameObject tempObj = new GameObject("TempTextMesh");
            TextMesh textMesh = tempObj.AddComponent<TextMesh>();

            // 配置 TextMesh
            textMesh.text = character;
            textMesh.font = font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
            textMesh.fontSize = fontSize;
            textMesh.color = Color.white;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;

            // 获取 MeshRenderer
            MeshRenderer meshRenderer = tempObj.GetComponent<MeshRenderer>();
            if (meshRenderer != null && textMesh.font != null)
            {
                meshRenderer.material = textMesh.font.material;
            }

            // 创建临时相机
            GameObject cameraObj = new GameObject("TempCamera");
            Camera tempCamera = cameraObj.AddComponent<Camera>();
            tempCamera.clearFlags = CameraClearFlags.SolidColor;
            tempCamera.backgroundColor = Color.clear;
            tempCamera.orthographic = true;
            tempCamera.orthographicSize = 5f;
            tempCamera.nearClipPlane = 0.1f;
            tempCamera.farClipPlane = 100f;

            // 定位对象
            tempObj.transform.position = new Vector3(0, 0, 10);
            cameraObj.transform.position = new Vector3(0, 0, 0);
            cameraObj.transform.LookAt(tempObj.transform);

            // 创建 RenderTexture
            RenderTexture rt = RenderTexture.GetTemporary(textureSize, textureSize, 24, RenderTextureFormat.ARGB32);
            tempCamera.targetTexture = rt;

            // 渲染
            tempCamera.Render();

            // 读取到 Texture2D
            RenderTexture.active = rt;
            Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, textureSize, textureSize), 0, 0);
            texture.Apply();

            // 清理
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            GameObject.DestroyImmediate(tempObj);
            GameObject.DestroyImmediate(cameraObj);

            return texture;
        }

        /// <summary>
        /// 采样纹理生成弹幕位置
        /// </summary>
        private List<Vector2> SampleTextureForBullets(Texture2D texture)
        {
            List<Vector2> positions = new List<Vector2>();
            int step = Mathf.Max(1, Mathf.RoundToInt(1f / samplingDensity));

            // 计算中心点
            float centerX = textureSize / 2f;
            float centerY = textureSize / 2f;

            // 采样纹理
            for (int y = 0; y < textureSize; y += step)
            {
                for (int x = 0; x < textureSize; x += step)
                {
                    Color pixel = texture.GetPixel(x, y);

                    // 如果像素不透明（有内容）
                    if (pixel.a > 0.5f)
                    {
                        if (useOutlineOnly)
                        {
                            // 检查是否是轮廓像素（周围有透明像素）
                            if (IsOutlinePixel(texture, x, y, (int)outlineThickness))
                            {
                                // 转换为相对中心的坐标，并归一化
                                float posX = (x - centerX) / textureSize * 10f;
                                float posY = (centerY - y) / textureSize * 10f; // Y轴翻转
                                positions.Add(new Vector2(posX, posY));
                            }
                        }
                        else
                        {
                            // 填充模式：所有不透明像素都生成弹幕
                            float posX = (x - centerX) / textureSize * 10f;
                            float posY = (centerY - y) / textureSize * 10f;
                            positions.Add(new Vector2(posX, posY));
                        }
                    }
                }
            }

            // 如果启用了弹幕数量限制且超过最大值，进行降采样
            if (limitBulletCount && positions.Count > maxBulletCount)
            {
                positions = DownsamplePositions(positions, maxBulletCount);
            }

            return positions;
        }

        /// <summary>
        /// 降采样位置列表（均匀采样）
        /// </summary>
        private List<Vector2> DownsamplePositions(List<Vector2> positions, int targetCount)
        {
            if (positions.Count <= targetCount)
                return positions;

            List<Vector2> result = new List<Vector2>();
            float step = (float)positions.Count / targetCount;

            for (int i = 0; i < targetCount; i++)
            {
                int index = Mathf.RoundToInt(i * step);
                if (index < positions.Count)
                {
                    result.Add(positions[index]);
                }
            }

            return result;
        }

        /// <summary>
        /// 检查是否是轮廓像素
        /// </summary>
        private bool IsOutlinePixel(Texture2D texture, int x, int y, int thickness)
        {
            // 检查周围像素，如果有透明像素则是轮廓
            for (int dy = -thickness; dy <= thickness; dy++)
            {
                for (int dx = -thickness; dx <= thickness; dx++)
                {
                    if (dx == 0 && dy == 0) continue;

                    int checkX = x + dx;
                    int checkY = y + dy;

                    if (checkX >= 0 && checkX < textureSize && checkY >= 0 && checkY < textureSize)
                    {
                        Color neighborPixel = texture.GetPixel(checkX, checkY);
                        if (neighborPixel.a < 0.5f)
                        {
                            return true; // 周围有透明像素，是轮廓
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 生成预览纹理（显示弹幕位置）
        /// </summary>
        private Texture2D GeneratePreviewTexture(Texture2D originalTexture, List<Vector2> bulletPositions)
        {
            Texture2D preview = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);

            // 复制原始纹理作为背景（半透明）
            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    Color pixel = originalTexture.GetPixel(x, y);
                    pixel.a *= 0.3f; // 降低透明度
                    preview.SetPixel(x, y, pixel);
                }
            }

            // 绘制弹幕位置（红点）
            float centerX = textureSize / 2f;
            float centerY = textureSize / 2f;
            int dotSize = Mathf.Max(1, Mathf.RoundToInt(2f / samplingDensity));

            foreach (Vector2 pos in bulletPositions)
            {
                // 转换回纹理坐标
                int x = Mathf.RoundToInt(pos.x / 10f * textureSize + centerX);
                int y = Mathf.RoundToInt(centerY - pos.y / 10f * textureSize);

                // 绘制红点
                for (int dy = -dotSize; dy <= dotSize; dy++)
                {
                    for (int dx = -dotSize; dx <= dotSize; dx++)
                    {
                        int px = x + dx;
                        int py = y + dy;
                        if (px >= 0 && px < textureSize && py >= 0 && py < textureSize)
                        {
                            preview.SetPixel(px, py, Color.red);
                        }
                    }
                }
            }

            preview.Apply();
            return preview;
        }

        /// <summary>
        /// 保存为新配置
        /// </summary>
        private void SaveAsNewPattern()
        {
            if (generatedPositions == null || generatedPositions.Count == 0)
            {
                EditorUtility.DisplayDialog("错误", "请先生成预览", "确定");
                return;
            }

            // 确保保存路径存在
            if (!System.IO.Directory.Exists(savePath))
            {
                System.IO.Directory.CreateDirectory(savePath);
            }

            // 创建新配置
            CharacterBulletPattern newPattern = ScriptableObject.CreateInstance<CharacterBulletPattern>();
            newPattern.character = inputCharacter;
            newPattern.description = $"自动生成于 {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            newPattern.bulletConfig = defaultBulletConfig;
            newPattern.bulletPositions = generatedPositions.ToArray();
            newPattern.bulletCount = generatedPositions.Count;
            newPattern.generatedTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // 保存为资源文件
            string fileName = $"CharacterPattern_{inputCharacter}_{System.DateTime.Now:yyyyMMdd_HHmmss}.asset";
            string fullPath = $"{savePath}/{fileName}";
            AssetDatabase.CreateAsset(newPattern, fullPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("成功", $"配置已保存到：{fullPath}", "确定");
            EditorGUIUtility.PingObject(newPattern);
        }

        /// <summary>
        /// 更新现有配置
        /// </summary>
        private void UpdateExistingPattern()
        {
            if (targetPattern == null)
            {
                EditorUtility.DisplayDialog("错误", "请选择目标配置", "确定");
                return;
            }

            if (generatedPositions == null || generatedPositions.Count == 0)
            {
                EditorUtility.DisplayDialog("错误", "请先生成预览", "确定");
                return;
            }

            // 更新配置
            targetPattern.character = inputCharacter;
            targetPattern.bulletPositions = generatedPositions.ToArray();
            targetPattern.bulletCount = generatedPositions.Count;
            targetPattern.generatedTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            if (defaultBulletConfig != null)
            {
                targetPattern.bulletConfig = defaultBulletConfig;
            }

            EditorUtility.SetDirty(targetPattern);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("成功", "配置已更新", "确定");
        }
    }
}
