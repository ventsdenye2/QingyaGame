using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace DodgeDots.UI
{
    public class EndingSceneController : MonoBehaviour
    {
        [SerializeField] private string startMenuSceneName = "StartMenu";
        [SerializeField] private TextMeshProUGUI endingText;
        [SerializeField, TextArea] private string endingMessage = "就这样，血统高贵的魔法使和勇者们成功地化解了危机，厨房又迎来了祥和的一天";

        private void Start()
        {
            if (endingText != null)
            {
                endingText.text = endingMessage;
            }
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (LoadingManager.Instance != null)
                {
                    LoadingManager.Instance.LoadScene(startMenuSceneName);
                }
                else
                {
                    SceneManager.LoadScene(startMenuSceneName);
                }
            }
        }
    }
}
