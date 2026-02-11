using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DodgeDots.Player;
using DodgeDots.Level;
using DodgeDots.Enemy;
using DodgeDots.Audio;

namespace DodgeDots.Tutorial
{
    public class TutorialManager : MonoBehaviour
    {
        [Header("UI 引用")]
        [SerializeField] private GameObject tutorialPanel;
        [SerializeField] private TextMeshProUGUI tutorialText;
        [SerializeField] private TextMeshProUGUI continueHintText;

        [Header("教程内容")]
        [SerializeField, TextArea] private string energyTutorial = "这是你的【能量条】。\n释放技能需要消耗能量，能量会随时间恢复。";
        [SerializeField, TextArea] private string movementTutorial = "【移动方式】\n使用 WASD 或方向键控制你的角色移动。";
        [SerializeField, TextArea] private string attackTutorial = "【攻击技能】\n点击 [鼠标左键] 释放近战攻击，可对 Boss 造成大量伤害。";
        [SerializeField, TextArea] private string shieldTutorial = "【护盾技能】\n点击 [鼠标右键] 开启无敌护盾，可抵挡所有弹幕。";
        [SerializeField, TextArea] private string gameStartMessage = "教程结束，干掉 Boss 吧！";
        [SerializeField] private float gameStartMessageDuration = 2f; // 可配置的正式开始提示显示时长
        [SerializeField, TextArea] private string lowHealthTutorial = "【新技能解锁】\n检测到 Boss 血量进入衰弱期！\n现在点击 [鼠标左键] 可发射强力追踪弹！";

        [Header("场景引用")]
        [SerializeField] private BossBattleLevel battleLevel;
        [SerializeField] private BossBase boss;
        [SerializeField] private BGMManager bgmManager; // 增加 BGMManager 引用

        private bool _isWaitingForClick = false;
        private bool _lowHealthTriggered = false;
        private Coroutine _tutorialCoroutine;

        private void Start()
        {
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            if (continueHintText != null) continueHintText.text = "点击鼠标左键继续...";
            
            // 教程期间禁用 BGM 自动播放，并停止当前播放
            if (bgmManager == null) bgmManager = FindFirstObjectByType<BGMManager>();
            if (bgmManager != null)
            {
                bgmManager.playOnStart = false;
                bgmManager.StopBgm();
            }

            // 确保 BossBattleLevel 不要自动开始战斗，由教程控制
            _tutorialCoroutine = StartCoroutine(TutorialSequence());
        }

        private void Update()
        {
            if (_isWaitingForClick && Input.GetMouseButtonDown(0))
            {
                _isWaitingForClick = false;
            }

            // 实时监测 Boss 血量
            if (!_lowHealthTriggered && boss != null && boss.CurrentHealth > 0)
            {
                float healthPercent = boss.CurrentHealth / boss.MaxHealth;
                if (healthPercent <= 0.1f)
                {
                    _lowHealthTriggered = true;
                    StartCoroutine(ShowLowHealthTip());
                }
            }
        }

        private IEnumerator TutorialSequence()
        {
            // 等待一帧确保所有对象初始化完成
            yield return null;

            // 暂停时间，开始教程
            Time.timeScale = 0f;
            if (tutorialPanel != null) tutorialPanel.SetActive(true);

            // 1. 能量条
            yield return ShowStep(energyTutorial);

            // 2. 移动方式
            yield return ShowStep(movementTutorial);

            // 3. 攻击
            yield return ShowStep(attackTutorial);

            // 4. 护盾
            yield return ShowStep(shieldTutorial);

            // 5. 正式开始
            _isWaitingForClick = false; // 确保不处于等待点击状态
            if (continueHintText != null) continueHintText.gameObject.SetActive(false);
            tutorialText.text = gameStartMessage;
            
            Debug.Log("[Tutorial] 进入正式开始提示，等待 2 秒...");
            float timer = 0f;
            while (timer < 2f)
            {
                timer += Time.unscaledDeltaTime; // 使用不受 timescale 影响的真实时间
                yield return null;
            }

            Debug.Log("[Tutorial] 2 秒已到，关闭教程面板并开始战斗。");
            CloseTutorialPanel();
            
            if (bgmManager != null) bgmManager.PlayBgm();
            if (battleLevel != null) battleLevel.StartBattle();
        }

        private void CloseTutorialPanel()
        {
            _isWaitingForClick = false; 
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            Time.timeScale = 1f;
            Debug.Log("[Tutorial] 教程面板已关闭，Time.timeScale 恢复为 1。");
        }

        private IEnumerator ShowStep(string content)
        {
            tutorialText.text = content;
            if (continueHintText != null) continueHintText.gameObject.SetActive(true);
            
            _isWaitingForClick = true;
            while (_isWaitingForClick)
            {
                yield return null;
            }
            
            // 点击后的短暂缓冲，防止连续触发
            float buffer = 0f;
            while (buffer < 0.1f)
            {
                buffer += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private IEnumerator ShowLowHealthTip()
        {
            _isWaitingForClick = false;
            Time.timeScale = 0f;
            if (tutorialPanel != null) tutorialPanel.SetActive(true);
            if (continueHintText != null) continueHintText.gameObject.SetActive(false);
            tutorialText.text = lowHealthTutorial;

            Debug.Log("[Tutorial] 触发 Boss 低血量提示，等待 2 秒...");
            float timer = 0f;
            while (timer < 2f)
            {
                timer += Time.unscaledDeltaTime;
                yield return null;
            }

            CloseTutorialPanel();
        }
    }
}
