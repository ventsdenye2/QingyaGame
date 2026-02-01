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
        [SerializeField] private Image speakerImage;
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

            // 显示说话者立绘
            if (speakerImage != null)
            {
                if (node.speakerSprite != null)
                {
                    speakerImage.sprite = node.speakerSprite;
                    speakerImage.gameObject.SetActive(true);
                }
                else
                {
                    speakerImage.gameObject.SetActive(false);
                }
            }

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
