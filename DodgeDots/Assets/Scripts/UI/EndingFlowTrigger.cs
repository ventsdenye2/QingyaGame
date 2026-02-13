using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DodgeDots.UI
{
    public class EndingFlowTrigger : MonoBehaviour
    {
        [SerializeField] private string endingSceneName = "Ending";
        [SerializeField] private float delaySeconds = 3f;

        private bool _started;

        public void StartEndingTransition()
        {
            if (_started) return;
            _started = true;
            StartCoroutine(LoadEndingAfterDelay());
        }

        private IEnumerator LoadEndingAfterDelay()
        {
            if (delaySeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(delaySeconds);
            }

            if (LoadingManager.Instance != null)
            {
                LoadingManager.Instance.LoadScene(endingSceneName);
            }
            else
            {
                SceneManager.LoadScene(endingSceneName);
            }
        }
    }
}
