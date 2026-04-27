using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Logic.Common
{
    public class NpcAttackAuthoring : MonoBehaviour
    {
        public float NpcTargetRadius;
        public float NpcDetectionRadius;
        public Vector3 FirePointOffset;
        public float AttackCooldownTime;
        public GameObject AttackPrefab;

        public NetCodeConfig NetCodeConfig;
        public int SimulationTickRate => NetCodeConfig.ClientServerTickRate.SimulationTickRate;

        public class NpcAttackBaker : Baker<NpcAttackAuthoring>
        {
            public override void Bake(NpcAttackAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new NpcAttackRadius { Value = authoring.NpcTargetRadius });
                AddComponent(entity, new NpcAttackProperties
                {
                    AttackPrefab = GetEntity(authoring.AttackPrefab, TransformUsageFlags.Dynamic),
                    FirePointOffset = authoring.FirePointOffset,
                    CooldownTickCount = (uint)(authoring.AttackCooldownTime * authoring.SimulationTickRate) 
                });
                AddComponent<NpcTargetEntity>(entity);
                AddBuffer<NpcAttackCooldown>(entity);
                
                AddComponent<AggressionTag>(entity);
                SetComponentEnabled<AggressionTag>(entity, false);
                AddComponent(entity, new NpcDetectionRadius { Value = authoring.NpcDetectionRadius });
                AddComponent<InAttackArea>(entity);
                SetComponentEnabled<InAttackArea>(entity, false);
                
                AddComponent<ReAggrRequest>(entity);
                SetComponentEnabled<ReAggrRequest>(entity, false);
            }
        }
    }
}