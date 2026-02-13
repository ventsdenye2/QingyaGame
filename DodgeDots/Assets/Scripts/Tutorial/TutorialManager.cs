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
        [SerializeField, TextArea] private string energyTutorial = "这是你的【能量条】。\n释放技能需要消耗能量。点击左键继续...";
        [SerializeField, TextArea] private string movementTutorial = "【移动方式】\n通过移动鼠标来控制角色位置。试着动一动...";
        [SerializeField, TextArea] private string attackTutorial = "【攻击技能】\n点击 [鼠标左键] 释放近战攻击！";
        [SerializeField, TextArea] private string shieldTutorial = "【护盾技能】\n点击 [鼠标右键] 开启无敌护盾！";
        [SerializeField, TextArea] private string gameStartMessage = "教程结束，干掉 Boss 吧！";
        [SerializeField, TextArea] private string lowHealthTutorial = "【新技能解锁】\n检测到 Boss 血量进入衰弱期！\n现在点击 [鼠标左键] 可发射强力追踪弹！";

        [Header("交互配置")]
        [SerializeField] private float movementDistanceThreshold = 800f; // 累计移动像素距离
        [SerializeField] private float gameStartMessageDuration = 2f;

        [Header("场景引用")]
        [SerializeField] private BossBattleLevel battleLevel;
        [SerializeField] private BossBase boss;
        [SerializeField] private BGMManager bgmManager;

        private PlayerSkillSystem _playerSkillSystem;
        private PlayerWeapon _playerWeapon;
        private PlayerEnergy _playerEnergy;

        private enum TutorialStep { Energy, Movement, Attack, Shield, Final }
        private TutorialStep _currentStep;
        
        private bool _isStepCompleted = false;
        private bool _lowHealthTriggered = false;
        private Vector3 _lastMousePos;
        private float _movementAccumulatedDistance = 0f;

        private void Start()
        {
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            
            _playerSkillSystem = FindFirstObjectByType<PlayerSkillSystem>();
            _playerWeapon = FindFirstObjectByType<PlayerWeapon>();
            _playerEnergy = FindFirstObjectByType<PlayerEnergy>();

            if (bgmManager == null) bgmManager = FindFirstObjectByType<BGMManager>();
            if (bgmManager != null)
            {
                bgmManager.playOnStart = false;
                bgmManager.StopBgm();
            }

            _lastMousePos = Input.mousePosition;
            StartCoroutine(TutorialSequence());
        }

        private void SetInputLocked(bool locked)
        {
            if (_playerSkillSystem != null) _playerSkillSystem.SetInputLocked(locked);
            if (_playerWeapon != null) _playerWeapon.SetInputLocked(locked);
        }

        private void Update()
        {
            // 监测 Boss 血量触发最后阶段
            if (!_lowHealthTriggered && boss != null && boss.CurrentHealth > 0)
            {
                float healthPercent = boss.CurrentHealth / boss.MaxHealth;
                if (healthPercent <= 0.1f)
                {
                    _lowHealthTriggered = true;
                    StartCoroutine(ShowLowHealthTip());
                }
            }

            if (_isStepCompleted) return;

            switch (_currentStep)
            {
                case TutorialStep.Energy:
                    if (Input.GetMouseButtonDown(0)) _isStepCompleted = true;
                    break;
                case TutorialStep.Movement:
                    float mouseDist = Vector3.Distance(Input.mousePosition, _lastMousePos);
                    _movementAccumulatedDistance += mouseDist;
                    _lastMousePos = Input.mousePosition;
                    if (_movementAccumulatedDistance >= movementDistanceThreshold) _isStepCompleted = true;
                    break;
                case TutorialStep.Attack:
                    if (Input.GetMouseButtonDown(0)) _isStepCompleted = true;
                    break;
                case TutorialStep.Shield:
                    if (_playerEnergy != null && _playerEnergy.CurrentEnergy >= 60f && Input.GetMouseButtonDown(1))
                        _isStepCompleted = true;
                    break;
            }
        }

        private IEnumerator TutorialSequence()
        {
            yield return null;

            if (battleLevel == null) battleLevel = FindFirstObjectByType<BossBattleLevel>();
            if (boss == null) boss = FindFirstObjectByType<BossBase>();

            if (battleLevel != null)
            {
                // 允许 Boss 在教程期间正常攻击
                battleLevel.allowBossBattle = true;
                battleLevel.StartBattle();
            }

            if (tutorialPanel != null) tutorialPanel.SetActive(true);

            // 1. 能量条
            _currentStep = TutorialStep.Energy;
            SetInputLocked(true);
            yield return ShowStep(energyTutorial, true);

            // 2. 移动
            _currentStep = TutorialStep.Movement;
            SetInputLocked(true);
            _movementAccumulatedDistance = 0f;
            yield return ShowStep(movementTutorial, false);

            // 3. 攻击
            _currentStep = TutorialStep.Attack;
            SetInputLocked(false);
            yield return ShowStep(attackTutorial, false);

            // 4. 护盾
            _currentStep = TutorialStep.Shield;
            SetInputLocked(false);
            yield return ShowStep(shieldTutorial, false);

            // 5. 正式开始
            SetInputLocked(false);
            if (continueHintText != null) continueHintText.gameObject.SetActive(false);
            tutorialText.text = gameStartMessage;
            
            float timer = 0f;
            while (timer < gameStartMessageDuration)
            {
                timer += Time.unscaledDeltaTime;
                yield return null;
            }

            CloseTutorialPanel();
            if (bgmManager != null) bgmManager.PlayBgm();
            if (battleLevel != null)
            {
                battleLevel.allowBossBattle = true;
                battleLevel.StartBattle();
            }
        }

        private IEnumerator ShowStep(string content, bool showHint)
        {
            tutorialText.text = content;
            if (continueHintText != null)
            {
                continueHintText.gameObject.SetActive(showHint);
                continueHintText.text = "点击鼠标左键继续...";
            }
            _isStepCompleted = false;
            _lastMousePos = Input.mousePosition;
            while (!_isStepCompleted) yield return null;
            yield return new WaitForSecondsRealtime(0.2f);
        }

        private void CloseTutorialPanel()
        {
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            Time.timeScale = 1f;
            SetInputLocked(false);
            if (battleLevel != null) battleLevel.allowBossBattle = true;
        }

        private IEnumerator ShowLowHealthTip()
        {
            if (tutorialPanel != null) tutorialPanel.SetActive(true);
            tutorialText.text = lowHealthTutorial;
            if (continueHintText != null) continueHintText.gameObject.SetActive(false);
            SetInputLocked(false);

            float timer = 0f;
            while (timer < 3f)
            {
                timer += Time.unscaledDeltaTime;
                yield return null;
            }

            if (tutorialPanel != null) tutorialPanel.SetActive(false);
        }
    }
}
