using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Logic.Common.Authorings
{
    public class BasicRangedAttackAuthoring : MonoBehaviour
    {
        public float AttackRadius;
        public float DetectionRadius;
        public Vector3 FirePointOffset;
        public float AttackCooldownTime;
        public GameObject AttackPrefab;

        public NetCodeConfig NetCodeConfig;
        public int SimulationTickRate => NetCodeConfig.ClientServerTickRate.SimulationTickRate;

        public class BasicRangedAttackBaker : Baker<BasicRangedAttackAuthoring>
        {
            public override void Bake(BasicRangedAttackAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new AttackRadius { Value = authoring.AttackRadius });
                AddComponent(entity, new RangedAttackProperties
                {
                    AttackPrefab = GetEntity(authoring.AttackPrefab, TransformUsageFlags.Dynamic),
                    FirePointOffset = authoring.FirePointOffset,
                    CooldownTickCount = (uint)(authoring.AttackCooldownTime * authoring.SimulationTickRate) 
                });
                AddComponent<TargetEntity>(entity);
                AddBuffer<AttackCooldown>(entity);
                
                AddComponent<AggressionState>(entity);
                SetComponentEnabled<AggressionState>(entity, false);
                AddComponent(entity, new DetectionRadius { Value = authoring.DetectionRadius });
                AddComponent<InAttackArea>(entity);
                SetComponentEnabled<InAttackArea>(entity, false);
                
                AddComponent<ReAggrRequest>(entity);
                SetComponentEnabled<ReAggrRequest>(entity, false);
            }
        }
    }
}