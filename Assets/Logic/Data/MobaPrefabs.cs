using Unity.Entities;
using UnityEngine;

namespace ROMA2.Logic.Data
{
    public struct MobaPrefabs : IComponentData
    {
        public Entity Champion;
        public Entity Minion;
        public Entity GameOverEntity;
        public Entity RespawnEntity;
    }

    // UI лучше вынести в Client
    public class UIPrefabs : IComponentData
    {
        public GameObject HealthBar;
        public GameObject SkillShot;
    }
}
