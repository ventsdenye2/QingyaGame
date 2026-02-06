using System.Collections.Generic;
using UnityEngine;

namespace DodgeDots.Player
{
    public enum PlayerSkillType
    {
        AttackHoming,
        AttackMelee,
        Shield,
        Resurrection
    }

    public class PlayerSkillManager : MonoBehaviour
    {
        [Header("Available Skills (Per Level)")]
        [SerializeField] private List<PlayerSkillType> availableSkills = new List<PlayerSkillType>
        {
            PlayerSkillType.AttackHoming,
            PlayerSkillType.AttackMelee,
            PlayerSkillType.Shield,
            PlayerSkillType.Resurrection
        };

        private readonly HashSet<PlayerSkillType> _skillLookup = new HashSet<PlayerSkillType>();

        private void Awake()
        {
            RebuildLookup();
        }

        public bool HasSkill(PlayerSkillType skill)
        {
            if (_skillLookup.Count == 0)
            {
                RebuildLookup();
            }
            return _skillLookup.Contains(skill);
        }

        public void SetAvailableSkills(IEnumerable<PlayerSkillType> skills)
        {
            availableSkills.Clear();
            availableSkills.AddRange(skills);
            RebuildLookup();
        }

        private void RebuildLookup()
        {
            _skillLookup.Clear();
            foreach (var skill in availableSkills)
            {
                _skillLookup.Add(skill);
            }
        }
    }
}
