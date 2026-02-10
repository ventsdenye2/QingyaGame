using System.Collections;
using UnityEngine;
using DodgeDots.Save;

namespace DodgeDots.Managers
{
    public class GameSession : MonoBehaviour
    {
        [SerializeField] private float autosaveInterval = 30f;
        private Coroutine _autosaveCoroutine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            var existing = FindFirstObjectByType<GameSession>();
            if (existing != null) return;

            var go = new GameObject("GameSession");
            DontDestroyOnLoad(go);
            go.AddComponent<GameSession>();
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            SaveSystem.LoadOrCreate();
        }

        private void OnEnable()
        {
            if (_autosaveCoroutine == null)
            {
                _autosaveCoroutine = StartCoroutine(AutoSaveLoop());
            }
        }

        private void OnDisable()
        {
            if (_autosaveCoroutine != null)
            {
                StopCoroutine(_autosaveCoroutine);
                _autosaveCoroutine = null;
            }
        }

        private IEnumerator AutoSaveLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(Mathf.Max(5f, autosaveInterval));
                SaveSystem.Save();
            }
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                SaveSystem.Save();
            }
        }

        private void OnApplicationQuit()
        {
            SaveSystem.Save();
        }
    }
}
