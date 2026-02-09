using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DodgeDots.Dialogue
{
    /// <summary>
    /// 对话UI组件
    /// 显示Galgame风格的对话框
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        [Header("UI组件")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private TextMeshProUGUI dialogueText;

        [Header("立绘设置")]
        [SerializeField] private Image leftSpeakerImage;  // 拖入左边的Image组件
        [SerializeField] private Image rightSpeakerImage; // 拖入右边的Image组件

        [Tooltip("非说话时的颜色（变暗）")]
        [SerializeField] private Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 1f); // 默认灰色
        [Tooltip("说话时的颜色（高亮）")]
        [SerializeField] private Color activeColor = Color.white; // 默认白色

        [SerializeField] private GameObject choiceButtonContainer;
        [SerializeField] private Button choiceButtonPrefab;

        [Header("打字机效果")]
        [SerializeField] private bool useTypewriterEffect = true;
        [SerializeField] private float typewriterSpeed = 0.05f;

        private DialogueManager _dialogueManager;
        private Coroutine _typewriterCoroutine;
        private bool _isTyping = false;

        private void Start()
        {
            _dialogueManager = DialogueManager.Instance;
            if (_dialogueManager != null)
            {
                _dialogueManager.OnDialogueStarted += HandleDialogueStarted;
                _dialogueManager.OnDialogueNodeChanged += HandleNodeChanged;
                _dialogueManager.OnDialogueEnded += HandleDialogueEnded;
            }

            HideDialogue();
        }

        private void OnDestroy()
        {
            if (_dialogueManager != null)
            {
                _dialogueManager.OnDialogueStarted -= HandleDialogueStarted;
                _dialogueManager.OnDialogueNodeChanged -= HandleNodeChanged;
                _dialogueManager.OnDialogueEnded -= HandleDialogueEnded;
            }
        }

        private void Update()
        {
            if (_dialogueManager != null && _dialogueManager.IsDialogueActive)
            {
                // --- 新增：ESC 退出功能 ---
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    _dialogueManager.EndDialogue();
                    return; // 退出当前帧处理
                }

                if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                {
                    if (_isTyping)
                    {
                        SkipTypewriter();
                    }
                    else if (_dialogueManager.CurrentNode.nodeType != DialogueNodeType.Choice)
                    {
                        _dialogueManager.AdvanceDialogue();
                    }
                }
            }
        }
        private void HandleDialogueStarted(DialogueConfig dialogue)
        {
            // 对话开始前，把左右立绘都关掉
            ResetPortraits();
            ShowDialogue();
        }

        private void HandleNodeChanged(DialogueNode node)
        {
            DisplayNode(node);
        }

        private void HandleDialogueEnded()
        {
            HideDialogue();
        }

        private void ShowDialogue()
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(true);
            }
        }

        private void HideDialogue()
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }
        }

        private void DisplayNode(DialogueNode node)
        {
            if (node == null) return;

            // 显示说话者名称
            if (speakerNameText != null)
            {
                speakerNameText.text = node.speakerName;
            }

            // 处理立绘逻辑
            UpdatePortraits(node);

            // 显示对话文本
            if (useTypewriterEffect)
            {
                StartTypewriter(node.dialogueText);
            }
            else
            {
                if (dialogueText != null)
                {
                    dialogueText.text = node.dialogueText;
                }
            }

            // 处理选项
            ClearChoices();
            if (node.nodeType == DialogueNodeType.Choice && node.choices != null)
            {
                DisplayChoices(node.choices);
            }
        }

        // 重置立绘状态
        private void ResetPortraits()
        {
            if (leftSpeakerImage != null) leftSpeakerImage.gameObject.SetActive(false);
            if (rightSpeakerImage != null) rightSpeakerImage.gameObject.SetActive(false);
        }

        /// <summary>
        /// 新增：专门处理立绘高亮和更新的逻辑
        /// </summary>
        private void UpdatePortraits(DialogueNode node)
        {
            // 确保两个Image组件都已赋值
            if (leftSpeakerImage == null || rightSpeakerImage == null) return;

            // 只有当提供了 Sprite 时才更新图片
            // 这样做的目的是：如果A在左边说话，切到B在右边说话，
            // 如果B的节点里没有配置Sprite，我们通常希望右边保持B之前的样子，或者你需要根据需求清空

            // 逻辑：更新当前说话者的立绘，并将其设为高亮，另一边设为变暗
            if (node.speakerPos == SpeakerPosition.Left)
            {
                // 左边说话
                if (node.speakerSprite != null)
                {
                    leftSpeakerImage.sprite = node.speakerSprite;
                    leftSpeakerImage.gameObject.SetActive(true);
                    //leftSpeakerImage.SetNativeSize(); // 可选：根据图片原比例调整大小
                }

                // 左边亮，右边暗
                leftSpeakerImage.color = activeColor;
                rightSpeakerImage.color = inactiveColor;

                // 只有当右边有图片时才显示，否则保持原样或隐藏
                // (这里假设右边如果之前有人，现在只是变暗，不隐藏)
            }
            else
            {
                // 右边说话
                if (node.speakerSprite != null)
                {
                    rightSpeakerImage.sprite = node.speakerSprite;
                    rightSpeakerImage.gameObject.SetActive(true);
                    //rightSpeakerImage.SetNativeSize(); // 可选
                }

                // 右边亮，左边暗
                rightSpeakerImage.color = activeColor;
                leftSpeakerImage.color = inactiveColor;
            }
        }

        private void StartTypewriter(string text)
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
            }
            _typewriterCoroutine = StartCoroutine(TypewriterCoroutine(text));
        }

        private IEnumerator TypewriterCoroutine(string text)
        {
            _isTyping = true;
            dialogueText.text = "";

            foreach (char c in text)
            {
                dialogueText.text += c;
                yield return new WaitForSeconds(typewriterSpeed);
            }

            _isTyping = false;
        }

        private void SkipTypewriter()
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }

            if (_dialogueManager != null && _dialogueManager.CurrentNode != null)
            {
                dialogueText.text = _dialogueManager.CurrentNode.dialogueText;
            }

            _isTyping = false;
        }

        private void DisplayChoices(DialogueChoice[] choices)
        {
            if (choiceButtonContainer == null || choiceButtonPrefab == null) return;

            for (int i = 0; i < choices.Length; i++)
            {
                int choiceIndex = i;
                Button choiceButton = Instantiate(choiceButtonPrefab, choiceButtonContainer.transform);

                TextMeshProUGUI buttonText = choiceButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = choices[i].choiceText;
                }

                choiceButton.onClick.AddListener(() => OnChoiceSelected(choiceIndex));
                choiceButton.gameObject.SetActive(true);
            }
        }

        private void ClearChoices()
        {
            if (choiceButtonContainer == null) return;

            foreach (Transform child in choiceButtonContainer.transform)
            {
                Destroy(child.gameObject);
            }
        }

        private void OnChoiceSelected(int choiceIndex)
        {
            if (_dialogueManager != null)
            {
                _dialogueManager.SelectChoice(choiceIndex);
            }
        }
    }
}
