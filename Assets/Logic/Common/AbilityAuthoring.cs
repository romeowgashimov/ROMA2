using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Logic.Common
{
    public class AbilityAuthoring : MonoBehaviour
    {
        public GameObject AbilityPrefab;
        
        public float AoeAbilityCooldown;
        
        public NetCodeConfig NetCodeConfig;

        private int SimulationTickRate => NetCodeConfig.ClientServerTickRate.SimulationTickRate;
        
        public class AoeAbilityBaker : Baker<AbilityAuthoring>
        {
            public override void Bake(AbilityAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new AbilityPrefabs
                {
                    AoeAbility = GetEntity(authoring.AbilityPrefab, TransformUsageFlags.Dynamic)
                });
                AddComponent(entity, new AbilityCooldownTicks
                {
                    AoeAbility = (uint)(authoring.AoeAbilityCooldown * authoring.SimulationTickRate)
                });
                AddBuffer<AbilityCooldownTargetTicks>(entity);
            }
        }
    }
}