using ROMA2.Logic.Data;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace ROMA2.Logic.Common.Bakers
{
    public class AbilityAuthoring : MonoBehaviour
    {
        public GameObject Ability1;
        public GameObject Ability2;
        
        public float Ability1Cooldown;
        public float Ability2Cooldown;
        
        public int Ability1ManaCost;
        public int Ability2ManaCost;
        
        public NetCodeConfig NetCodeConfig;

        private int SimulationTickRate => NetCodeConfig.ClientServerTickRate.SimulationTickRate;
        
        public class AbilityBaker : Baker<AbilityAuthoring>
        {
            public override void Bake(AbilityAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new AbilityCommands
                {
                    Ability1 = GetEntity(authoring.Ability1, TransformUsageFlags.Dynamic),
                    Ability2 = GetEntity(authoring.Ability2, TransformUsageFlags.Dynamic)
                });
                AddComponent(entity, new AbilityCooldownTicks
                {
                    Ability1 = (uint)(authoring.Ability1Cooldown * authoring.SimulationTickRate),
                    Ability2 = (uint)(authoring.Ability2Cooldown * authoring.SimulationTickRate)
                });
                AddBuffer<AbilityCooldownTargetTicks>(entity);
                AddComponent<AbilityManaCost>(entity, new()
                {
                    Ability1 = authoring.Ability1ManaCost,
                    Ability2 = authoring.Ability2ManaCost
                });
            }
        }
    }
}