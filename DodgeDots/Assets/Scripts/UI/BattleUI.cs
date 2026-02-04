using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DodgeDots.Player;
using DodgeDots.Enemy;

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

        [Header("玩家能量条")]
        [SerializeField] private Image playerEnergyBar;
        [SerializeField] private TextMeshProUGUI playerEnergyText;
        [SerializeField] private Color energyBarColor = new Color(0, 0.8f, 1f); // 青色
        [SerializeField] private Color energyFullColor = Color.green;

        [Header("技能状态")]
        [SerializeField] private Image skillStatusIcon;
        [SerializeField] private TextMeshProUGUI skillStatusText;

        [Header("引用")]
        [SerializeField] private BossBase boss;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerEnergy playerEnergy;
        [SerializeField] private PlayerSkillSystem playerSkill;

        private void Start()
        {
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

            if (playerHealth == null)
            {
                playerHealth = FindObjectOfType<PlayerHealth>();
                if (playerHealth != null)
                {
                    Debug.Log("BattleUI: 自动找到PlayerHealth");
                }
            }

            if (playerEnergy == null)
            {
                playerEnergy = FindObjectOfType<PlayerEnergy>();
                if (playerEnergy != null)
                {
                    Debug.Log("BattleUI: 自动找到PlayerEnergy");
                }
            }

            if (playerSkill == null)
            {
                playerSkill = FindObjectOfType<PlayerSkillSystem>();
                if (playerSkill != null)
                {
                    Debug.Log("BattleUI: 自动找到PlayerSkillSystem");
                }
            }

            // 监听事件
            if (boss != null)
            {
                boss.OnHealthChanged += UpdateBossHealthBar;
                boss.OnStateChanged += OnBossStateChanged;
                Debug.Log("BattleUI: 已绑定Boss事件");
            }

            if (playerEnergy != null)
            {
                playerEnergy.OnEnergyChanged += UpdatePlayerEnergyBar;
                playerEnergy.OnEnergyFull += OnEnergyFull;
            }

            if (playerSkill != null)
            {
                playerSkill.OnSkillStarted += OnSkillStarted;
                playerSkill.OnSkillEnded += OnSkillEnded;
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
            // 实时更新血条（防止事件失火）
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

            // 实时更新能量条
            if (playerEnergy != null)
            {
                float energyFillAmount = playerEnergy.CurrentEnergy / playerEnergy.MaxEnergy;
                if (playerEnergyBar != null)
                {
                    playerEnergyBar.fillAmount = energyFillAmount;
                }

                if (playerEnergyText != null)
                {
                    playerEnergyText.text = $"{playerEnergy.CurrentEnergy:F0} / {playerEnergy.MaxEnergy:F0}";
                }
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
    }
}
