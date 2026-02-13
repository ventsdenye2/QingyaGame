using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using DodgeDots.Level;

namespace DodgeDots.UI
{
    /// <summary>
    /// 战斗结算界面控制
    /// Boss死亡 -> 胜利（仅世界地图按钮）
    /// 玩家死亡 -> 失败（重开 + 世界地图）
    /// </summary>
    public class BattleResultUI : MonoBehaviour
    {
        [Header("界面引用")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private Button worldMapButton;
        [SerializeField] private Button restartButton;

        [Header("音频")]
        [SerializeField] private DodgeDots.Audio.BGMManager bgmManager;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip winSfx;
        [SerializeField] private AudioClip loseSfx;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

        [Header("场景")]
        [SerializeField] private string worldMapSceneName = "WorldMap";
        [SerializeField] private bool pauseOnShow = true;

        [Header("引用")]
        [SerializeField] private BossBattleLevel battleLevel;

        [Header("数据展示")]
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI hpText;

        private bool _isShowing;

        private void Start()
        {
            if (battleLevel == null)
            {
                battleLevel = FindObjectOfType<BossBattleLevel>();
            }
            if (bgmManager == null)
            {
                bgmManager = FindObjectOfType<DodgeDots.Audio.BGMManager>();
            }
            if (sfxSource == null)
            {
                sfxSource = gameObject.GetComponent<AudioSource>();
                if (sfxSource == null)
                {
                    sfxSource = gameObject.AddComponent<AudioSource>();
                }
                sfxSource.playOnAwake = false;
            }
            sfxSource.volume = sfxVolume;

            if (battleLevel != null)
            {
                battleLevel.OnBattleWin += OnBattleWin;
                battleLevel.OnBattleLose += OnBattleLose;
            }
            else
            {
                Debug.LogWarning("BattleResultUI: 未找到 BossBattleLevel，无法监听结算事件。");
            }

            if (worldMapButton != null)
            {
                worldMapButton.onClick.AddListener(OnClickWorldMap);
            }
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnClickRestart);
            }

            SetPanelActive(false);
        }

        private void OnDestroy()
        {
            if (battleLevel != null)
            {
                battleLevel.OnBattleWin -= OnBattleWin;
                battleLevel.OnBattleLose -= OnBattleLose;
            }

            if (worldMapButton != null)
            {
                worldMapButton.onClick.RemoveListener(OnClickWorldMap);
            }
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(OnClickRestart);
            }
        }

        private void OnBattleWin()
        {
            ShowResult(
                text: "你赢了！",
                showRestart: false,
                showWorldMap: true
            );
            StopBgm();
            PlaySfx(winSfx);
        }

        private void OnBattleLose()
        {
            ShowResult(
                text: "你被干掉了。。。",
                showRestart: true,
                showWorldMap: true
            );
            StopBgm();
            PlaySfx(loseSfx);
        }

        private void ShowResult(string text, bool showRestart, bool showWorldMap)
        {
            if (_isShowing) return;
            _isShowing = true;

            if (resultText != null)
            {
                resultText.text = text;
            }

            UpdateStatsText();

            if (restartButton != null)
            {
                restartButton.gameObject.SetActive(showRestart);
            }

            if (worldMapButton != null)
            {
                worldMapButton.gameObject.SetActive(showWorldMap);
            }

            SetPanelActive(true);

            if (pauseOnShow)
            {
                Time.timeScale = 0f;
            }
        }

        private void SetPanelActive(bool active)
        {
            if (resultPanel != null)
            {
                resultPanel.SetActive(active);
            }
        }

        public void OnClickRestart()
        {
            Time.timeScale = 1f;
            Scene current = SceneManager.GetActiveScene();
            DodgeDots.UI.LoadingManager.Instance.LoadScene(current.name);
        }

        public void OnClickWorldMap()
        {
            Time.timeScale = 1f;
            if (string.IsNullOrWhiteSpace(worldMapSceneName))
            {
                Debug.LogError("BattleResultUI: worldMapSceneName 为空，无法跳转世界地图。");
                return;
            }
            DodgeDots.UI.LoadingManager.Instance.LoadScene(worldMapSceneName);
        }

        private void StopBgm()
        {
            if (bgmManager != null && bgmManager.audioSource != null)
            {
                bgmManager.audioSource.Stop();
            }
        }

        private void PlaySfx(AudioClip clip)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clip, sfxVolume);
        }

        private void UpdateStatsText()
        {
            if (battleLevel == null)
            {
                if (timeText != null) timeText.text = string.Empty;
                if (hpText != null) hpText.text = string.Empty;
                return;
            }

            float duration = battleLevel.BattleDurationSeconds;
            int minutes = Mathf.FloorToInt(duration / 60f);
            int seconds = Mathf.FloorToInt(duration % 60f);
            if (timeText != null) timeText.text = $"用时: {minutes:00}:{seconds:00}";

            if (hpText != null)
            {
                float hp = battleLevel.PlayerRemainingHp;
                float maxHp = battleLevel.PlayerMaxHp;
                if (maxHp > 0f)
                {
                    int resurrectCount = 0;
                    var skillSystem = FindFirstObjectByType<DodgeDots.Player.PlayerSkillSystem>();
                    if (skillSystem != null)
                    {
                        resurrectCount = skillSystem.GetResurrectionUsedCount();
                    }
                    hpText.text = $"剩余生命: {hp:F0}（复活{resurrectCount}次）";
                }
                else
                {
                    hpText.text = $"剩余生命: {hp:F0}";
                }
            }
        }
    }
}
