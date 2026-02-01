using UnityEngine;

namespace DodgeDots.Dialogue
{
    /// <summary>
    /// NPC交互组件
    /// 处理玩家与NPC的交互，触发对话
    /// </summary>
    public class NPCInteraction : MonoBehaviour
    {
        [Header("对话配置")]
        [SerializeField] private DialogueConfig dialogueConfig;

        [Header("交互设置")]
        [SerializeField] private float interactionRange = 2f;
        [SerializeField] private KeyCode interactionKey = KeyCode.E;
        [SerializeField] private string playerTag = "Player";

        [Header("交互提示")]
        [SerializeField] private GameObject interactionPrompt;

        private bool _playerInRange = false;
        private DialogueManager _dialogueManager;

        private void Start()
        {
            _dialogueManager = DialogueManager.Instance;

            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
            }
        }

        private void Update()
        {
            if (_playerInRange && !_dialogueManager.IsDialogueActive)
            {
                if (Input.GetKeyDown(interactionKey))
                {
                    StartDialogue();
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag(playerTag))
            {
                _playerInRange = true;
                ShowInteractionPrompt();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag(playerTag))
            {
                _playerInRange = false;
                HideInteractionPrompt();
            }
        }

        private void StartDialogue()
        {
            if (_dialogueManager != null && dialogueConfig != null)
            {
                _dialogueManager.StartDialogue(dialogueConfig);
                HideInteractionPrompt();
            }
        }

        private void ShowInteractionPrompt()
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(true);
            }
        }

        private void HideInteractionPrompt()
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRange);
        }
    }
}
