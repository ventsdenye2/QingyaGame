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
        [SerializeField] private float movementDistanceThreshold = 800f; // 累计移动像素距离阈值

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
        private Vector2 _lastPlayerPos;
        private float _movementAccumulatedDistance = 0f;

        private void Start()
        {
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            
            _playerSkillSystem = FindFirstObjectByType<PlayerSkillSystem>();
            _playerWeapon = FindFirstObjectByType<PlayerWeapon>();
            _playerEnergy = FindFirstObjectByType<PlayerEnergy>();

            // 教程期间禁用 BGM 自动播放，并停止当前播放
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
            // 持续监测 Boss 血量，用于触发最后阶段教程
            if (!_lowHealthTriggered && boss != null && boss.CurrentHealth > 0)
            {
                float healthPercent = boss.CurrentHealth / boss.MaxHealth;
                if (healthPercent <= 0.1f)
                {
                    _lowHealthTriggered = true;
                    StartCoroutine(ShowLowHealthTip());
                }
            }

            if (_lowHealthTriggered && _isStepCompleted == false && Input.GetMouseButtonDown(0))
            {
                _isStepCompleted = true;
                return;
            }

            if (_isStepCompleted) return;

            switch (_currentStep)
            {
                case TutorialStep.Energy:
                    // 能量条阶段：点击左键继续
                    if (Input.GetMouseButtonDown(0)) _isStepCompleted = true;
                    break;
                case TutorialStep.Movement:
                    // 移动阶段：累计鼠标移动距离，达到阈值后进入下一阶段
                    float mouseDist = Vector3.Distance(Input.mousePosition, _lastMousePos);
                    _movementAccumulatedDistance += mouseDist;
                    _lastMousePos = Input.mousePosition;

                    if (_movementAccumulatedDistance >= movementDistanceThreshold)
                    {
                        _isStepCompleted = true;
                        _movementAccumulatedDistance = 0f;
                        Debug.Log("[Tutorial] 移动阶段完成");
                    }
                    break;
                case TutorialStep.Attack:
                    // 攻击阶段：允许左键
                    if (Input.GetMouseButtonDown(0)) _isStepCompleted = true;
                    break;
                case TutorialStep.Shield:
                    // 护盾阶段：需要能量>=60，且右键成功触发
                    if (_playerEnergy != null && _playerEnergy.CurrentEnergy >= 60f && Input.GetMouseButtonDown(1))
                    {
                        _isStepCompleted = true;
                    }
                    break;
            }
        }

        private IEnumerator TutorialSequence()
        {
            yield return null;

            if (battleLevel == null) battleLevel = FindFirstObjectByType<BossBattleLevel>();
            if (boss == null) boss = FindFirstObjectByType<BossBase>();

            if (tutorialPanel != null) tutorialPanel.SetActive(true);

            // 1. 能量条 - 左键点击继续 (锁定输入，不触发攻击)
            _currentStep = TutorialStep.Energy;
            SetInputLocked(true);
            yield return ShowStep(energyTutorial, true);

            // 2. 移动方式 - 鼠标移动 (锁定输入，不触发攻击)
            _currentStep = TutorialStep.Movement;
            SetInputLocked(true);
            yield return ShowStep(movementTutorial, false);

            // 3. 攻击 - 左键 (解锁输入，允许发动攻击)
            _currentStep = TutorialStep.Attack;
            SetInputLocked(false);
            yield return ShowStep(attackTutorial, false);

            // 4. 护盾 - 右键 (解锁输入，允许开盾)
            _currentStep = TutorialStep.Shield;
            SetInputLocked(false);
            yield return ShowStep(shieldTutorial, false);

            // 5. 正式开始
            SetInputLocked(false); // 确保完全解锁
            if (continueHintText != null) continueHintText.gameObject.SetActive(false);
            tutorialText.text = gameStartMessage;
            
            float timer = 0f;
            while (timer < 2f)
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

            while (!_isStepCompleted)
            {
                yield return null;
            }
            
            yield return new WaitForSecondsRealtime(0.2f);
        }

        private void CloseTutorialPanel()
        {
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            Time.timeScale = 1f;
            SetInputLocked(false);
            
            // 确保教程彻底关闭后恢复 Boss 战斗许可
            if (battleLevel != null)
            {
                battleLevel.allowBossBattle = true;
            }
        }

        private IEnumerator ShowLowHealthTip()
        {
            // 最后阶段不再暂停游戏，不再锁定输入
            if (tutorialPanel != null) tutorialPanel.SetActive(true);
            tutorialText.text = lowHealthTutorial;
            if (continueHintText != null)
            {
                continueHintText.gameObject.SetActive(false); // 不再显示点击提示
            }

            SetInputLocked(false); // 确保此时可以正常释放技能

            // 展示 3 秒后自动消失
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
