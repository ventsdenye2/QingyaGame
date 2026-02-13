using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DodgeDots.Player;
using DodgeDots.Enemy;
using System.Collections;

namespace DodgeDots.UI
{
    /// <summary>
    /// 战斗UI系统
    /// 显示Boss血条和玩家能量条
    /// </summary>
    public class BattleUI : MonoBehaviour
    {
        [Header("Boss血条")]
        [SerializeField] private Image bossHealthBar;
        [SerializeField] private TextMeshProUGUI bossHealthText;
        [SerializeField] private TextMeshProUGUI bossNameText;
        [SerializeField] private Color healthBarColor = Color.red;

        [Header("玩家血条")]
        [SerializeField] private Image playerHealthBar;
        [SerializeField] private TextMeshProUGUI playerHealthText;
        [SerializeField] private Color playerHealthBarColor = Color.green;

        [Header("玩家能量条")]
        [SerializeField] private Image playerEnergyBar;
        [SerializeField] private TextMeshProUGUI playerEnergyText;
        [SerializeField] private Color energyBarColor = new Color(0, 0.8f, 1f); // 青色
        [SerializeField] private Color energyFullColor = Color.green;

        [Header("技能状态")]
        [SerializeField] private Image skillStatusIcon;
        [SerializeField] private TextMeshProUGUI skillStatusText;

        [Header("技能能量消耗")]
        [SerializeField] private TextMeshProUGUI skillEnergyCostText;

        [Header("受击反馈")]
        [SerializeField] private Image damageFlashIcon;
        [SerializeField] private Color damageFlashColor = new Color(1f, 0.2f, 0.2f, 1f);
        [SerializeField] private float damageFlashDuration = 0.15f;
        [SerializeField] private Camera shakeCamera;
        [SerializeField] private float shakeDuration = 0.12f;
        [SerializeField] private float shakeStrength = 0.2f;

        [Header("引用")]
        [SerializeField] private BossBase boss;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerEnergy playerEnergy;
        [SerializeField] private PlayerSkillSystem playerSkill;

        private Coroutine _damageFlashRoutine;
        private Coroutine _cameraShakeRoutine;
        private bool _damageFlashColorCached;
        private Color _damageFlashOriginalColor;

        private void Start()
        {
            TryFindPlayerReferences();

            // 自动查找Boss（如果没有手动设置）
            if (boss == null)
            {
                boss = FindObjectOfType<BossBase>();
                if (boss == null)
                {
                    Debug.LogError("BattleUI: 无法找到Boss！");
                    return;
                }
                else
                {
                    Debug.Log($"BattleUI: 自动找到Boss: {boss.name}");
                }
            }

            // 监听事件
            if (boss != null)
            {
                boss.OnHealthChanged += UpdateBossHealthBar;
                boss.OnStateChanged += OnBossStateChanged;
                boss.OnDamageTaken += OnBossDamageTaken;
                Debug.Log("BattleUI: 已绑定Boss事件");
            }

            UpdateSkillEnergyCostText();

            if (damageFlashIcon == null)
            {
                damageFlashIcon = skillStatusIcon;
            }

            if (damageFlashIcon != null && !_damageFlashColorCached)
            {
                _damageFlashOriginalColor = damageFlashIcon.color;
                _damageFlashColorCached = true;
            }

            if (shakeCamera == null)
            {
                shakeCamera = Camera.main;
            }

            // 初始化显示
            if (boss != null && bossNameText != null)
            {
                // 从Boss配置中读取Boss的实际名称
                bossNameText.text = boss.BossName;
            }

            if (boss != null)
            {
                UpdateBossHealthBar(boss.CurrentHealth, boss.MaxHealth);
                Debug.Log($"BattleUI: 初始化Boss血条 - 当前: {boss.CurrentHealth}, 最大: {boss.MaxHealth}");
            }
        }

        private void TryFindPlayerReferences()
        {
            if (playerHealth == null)
            {
                playerHealth = FindObjectOfType<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.OnHealthChanged += UpdatePlayerHealthBar;
                    playerHealth.OnDamageTaken += OnPlayerDamageTaken;
                    Debug.Log("BattleUI: 自动找到并绑定 PlayerHealth");
                }
            }

            if (playerEnergy == null)
            {
                playerEnergy = FindObjectOfType<PlayerEnergy>();
                if (playerEnergy != null)
                {
                    playerEnergy.OnEnergyChanged += UpdatePlayerEnergyBar;
                    playerEnergy.OnEnergyFull += OnEnergyFull;
                    Debug.Log("BattleUI: 自动找到并绑定 PlayerEnergy");
                }
            }

            if (playerSkill == null)
            {
                playerSkill = FindObjectOfType<PlayerSkillSystem>();
                if (playerSkill != null)
                {
                    playerSkill.OnSkillStarted += OnSkillStarted;
                    playerSkill.OnSkillEnded += OnSkillEnded;
                    Debug.Log("BattleUI: 自动找到并绑定 PlayerSkillSystem");
                }
            }

            if (playerEnergy != null)
            {
                UpdatePlayerEnergyBar(playerEnergy.CurrentEnergy, playerEnergy.MaxEnergy);
            }
        }

        private void OnDestroy()
        {
            if (boss != null)
            {
                boss.OnHealthChanged -= UpdateBossHealthBar;
                boss.OnStateChanged -= OnBossStateChanged;
                boss.OnDamageTaken -= OnBossDamageTaken;
            }

            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= UpdatePlayerHealthBar;
                playerHealth.OnDamageTaken -= OnPlayerDamageTaken;
            }

            if (playerEnergy != null)
            {
                playerEnergy.OnEnergyChanged -= UpdatePlayerEnergyBar;
                playerEnergy.OnEnergyFull -= OnEnergyFull;
            }

            if (playerSkill != null)
            {
                playerSkill.OnSkillStarted -= OnSkillStarted;
                playerSkill.OnSkillEnded -= OnSkillEnded;
            }
        }

        private void Update()
        {
            // 动态检查玩家引用（防止替换Prefab或复活后引用丢失）
            if (playerHealth == null || playerEnergy == null || playerSkill == null)
            {
                TryFindPlayerReferences();
            }

            // 实时更新Boss血条（防止事件失火）
            if (boss != null && boss.IsAlive)
            {
                float fillAmount = boss.CurrentHealth / boss.MaxHealth;
                if (bossHealthBar != null)
                {
                    bossHealthBar.fillAmount = fillAmount;
                }

                if (bossHealthText != null)
                {
                    bossHealthText.text = $"{boss.CurrentHealth:F0} / {boss.MaxHealth:F0}";
                }
            }

            // 实时更新玩家血条
            if (playerHealth != null && playerHealth.IsAlive)
            {
                float fillAmount = playerHealth.CurrentHealth / playerHealth.MaxHealth;
                if (playerHealthBar != null)
                {
                    playerHealthBar.fillAmount = fillAmount;
                }

                if (playerHealthText != null)
                {
                    playerHealthText.text = $"{playerHealth.CurrentHealth:F0} / {playerHealth.MaxHealth:F0}";
                }
            }

            // 实时更新能量条
            if (playerEnergy != null)
            {
                float energyFillAmount = playerEnergy.CurrentEnergy / playerEnergy.MaxEnergy;
                if (playerEnergyBar != null)
                {
                    playerEnergyBar.fillAmount = energyFillAmount;

                    // 根据能量是否满盈改变颜色
                    if (Mathf.Approximately(playerEnergy.CurrentEnergy, playerEnergy.MaxEnergy))
                    {
                        playerEnergyBar.color = energyFullColor;
                    }
                    else
                    {
                        playerEnergyBar.color = energyBarColor;
                    }
                }

                if (playerEnergyText != null)
                {
                    playerEnergyText.text = $"{playerEnergy.CurrentEnergy:F0} / {playerEnergy.MaxEnergy:F0}";
                }
            }
        }

        /// <summary>
        /// 更新玩家血条
        /// </summary>
        private void UpdatePlayerHealthBar(float currentHealth, float maxHealth)
        {
            if (playerHealthBar != null)
            {
                float fillAmount = Mathf.Max(0, currentHealth / maxHealth);
                playerHealthBar.fillAmount = fillAmount;
                playerHealthBar.color = playerHealthBarColor;
                Debug.Log($"UpdatePlayerHealthBar: {currentHealth:F0} / {maxHealth:F0}, fillAmount: {fillAmount:F2}");
            }

            if (playerHealthText != null)
            {
                playerHealthText.text = $"{Mathf.Max(0, currentHealth):F0} / {maxHealth:F0}";
            }
        }

        /// <summary>
        /// 更新Boss血条
        /// </summary>
        private void UpdateBossHealthBar(float currentHealth, float maxHealth)
        {
            if (bossHealthBar != null)
            {
                float fillAmount = Mathf.Max(0, currentHealth / maxHealth);
                bossHealthBar.fillAmount = fillAmount;
                bossHealthBar.color = healthBarColor;
                Debug.Log($"UpdateBossHealthBar: {currentHealth:F0} / {maxHealth:F0}, fillAmount: {fillAmount:F2}");
            }

            if (bossHealthText != null)
            {
                bossHealthText.text = $"{Mathf.Max(0, currentHealth):F0} / {maxHealth:F0}";
            }
        }

        /// <summary>
        /// 更新玩家能量条
        /// </summary>
        private void UpdatePlayerEnergyBar(float currentEnergy, float maxEnergy)
        {
            if (playerEnergyBar != null)
            {
                float fillAmount = Mathf.Max(0, currentEnergy / maxEnergy);
                playerEnergyBar.fillAmount = fillAmount;

                // 根据能量是否满盈改变颜色
                if (Mathf.Approximately(currentEnergy, maxEnergy))
                {
                    playerEnergyBar.color = energyFullColor;
                }
                else
                {
                    playerEnergyBar.color = energyBarColor;
                }
            }

            if (playerEnergyText != null)
            {
                playerEnergyText.text = $"{Mathf.Max(0, currentEnergy):F0} / {maxEnergy:F0}";
            }
        }

        /// <summary>
        /// Boss状态改变时调用
        /// </summary>
        private void OnBossStateChanged(BossState state)
        {
            // 可以根据Boss状态改变UI显示
            switch (state)
            {
                case BossState.Idle:
                    break;
                case BossState.Fighting:
                    break;
                case BossState.Defeated:
                    if (bossHealthBar != null)
                    {
                        bossHealthBar.fillAmount = 0;
                    }
                    break;
            }
        }

        /// <summary>
        /// 能量满盈时调用
        /// </summary>
        private void OnEnergyFull()
        {
            if (skillStatusText != null)
            {
                skillStatusText.text = "准备就绪";
                skillStatusText.color = Color.green;
            }
        }

        /// <summary>
        /// 技能开始时调用
        /// </summary>
        private void OnSkillStarted()
        {
            if (skillStatusText != null)
            {
                skillStatusText.text = "技能释放中...";
                skillStatusText.color = Color.yellow;
            }
        }

        /// <summary>
        /// 技能结束时调用
        /// </summary>
        private void OnSkillEnded()
        {
            if (skillStatusText != null)
            {
                skillStatusText.text = "冷却中";
                skillStatusText.color = Color.gray;
            }
        }

        private void OnPlayerDamageTaken()
        {
            if (damageFlashIcon != null)
            {
                if (_damageFlashRoutine != null)
                {
                    StopCoroutine(_damageFlashRoutine);
                }
                _damageFlashRoutine = StartCoroutine(FlashDamageIcon());
            }
        }

        private void OnBossDamageTaken()
        {
            if (shakeCamera == null) return;

            if (_cameraShakeRoutine != null)
            {
                StopCoroutine(_cameraShakeRoutine);
            }
            _cameraShakeRoutine = StartCoroutine(ShakeCameraOnce());
        }

        private IEnumerator FlashDamageIcon()
        {
            if (!_damageFlashColorCached)
            {
                _damageFlashOriginalColor = damageFlashIcon.color;
                _damageFlashColorCached = true;
            }

            damageFlashIcon.color = damageFlashColor;
            yield return new WaitForSeconds(damageFlashDuration);
            damageFlashIcon.color = _damageFlashOriginalColor;
        }

        private IEnumerator ShakeCameraOnce()
        {
            Transform camTransform = shakeCamera.transform;
            Vector3 startPos = camTransform.localPosition;
            float time = 0f;

            while (time < shakeDuration)
            {
                float offsetX = Random.Range(-shakeStrength, shakeStrength);
                float offsetY = Random.Range(-shakeStrength, shakeStrength);
                camTransform.localPosition = startPos + new Vector3(offsetX, offsetY, 0f);
                time += Time.deltaTime;
                yield return null;
            }

            camTransform.localPosition = startPos;
        }

        private void UpdateSkillEnergyCostText()
        {
            if (skillEnergyCostText == null) return;

            if (playerSkill == null)
            {
                playerSkill = FindFirstObjectByType<PlayerSkillSystem>();
            }

            if (playerSkill != null)
            {
                skillEnergyCostText.text = $"攻击技能：{playerSkill.AttackEnergyCost:F0}点\n护盾技能：{playerSkill.ShieldEnergyCost:F0}点";
            }
            else
            {
                skillEnergyCostText.text = string.Empty;
            }
        }
    }
}
