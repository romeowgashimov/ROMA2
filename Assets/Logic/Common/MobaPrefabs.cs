using Unity.Entities;
using UnityEngine;

namespace Logic.Common
{
    public struct MobaPrefabs : IComponentData
    {
        public Entity Champion;
        public Entity Minion;
        public Entity GameOverEntity;
        public Entity RespawnEntity;
    }

    public struct PlayerSettings : IComponentData
    {
        public bool NeedToConfirmAbilities;
        public float MouseSpeed;
    }

    public class UIPrefabs : IComponentData
    {
        public GameObject HealthBar;
        public GameObject SkillShot;
    }
}
