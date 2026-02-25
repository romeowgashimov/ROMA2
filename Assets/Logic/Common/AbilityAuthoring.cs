using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Logic.Common
{
    public class AbilityAuthoring : MonoBehaviour
    {
        public GameObject AoeAbilityPrefab;
        public GameObject SkillShotAbilityPrefab;
        
        public float AoeAbilityCooldown;
        public float SkillShotAbilityCooldown;
        
        public NetCodeConfig NetCodeConfig;

        private int SimulationTickRate => NetCodeConfig.ClientServerTickRate.SimulationTickRate;
        
        public class AoeAbilityBaker : Baker<AbilityAuthoring>
        {
            public override void Bake(AbilityAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new AbilityPrefabs
                {
                    AoeAbility = GetEntity(authoring.AoeAbilityPrefab, TransformUsageFlags.Dynamic),
                    SkillShotAbility = GetEntity(authoring.SkillShotAbilityPrefab, TransformUsageFlags.Dynamic)
                });
                AddComponent(entity, new AbilityCooldownTicks
                {
                    AoeAbility = (uint)(authoring.AoeAbilityCooldown * authoring.SimulationTickRate),
                    SkillShotAbility = (uint)(authoring.SkillShotAbilityCooldown * authoring.SimulationTickRate)
                });
                AddBuffer<AbilityCooldownTargetTicks>(entity);
            }
        }
    }
}