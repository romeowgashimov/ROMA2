using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;
using static Unity.Mathematics.quaternion;

namespace Logic.Common
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct DefaultAttackTargetingSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            ComponentLookup<LocalTransform> transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(isReadOnly: true);
            EntityCommandBuffer ecb = SystemAPI
                .GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            float deltaTime = SystemAPI.Time.DeltaTime;
            
            foreach ((RefRW<LocalTransform> transform, AbilityMoveSpeed speed,
                         DefaultAttackTarget target, Entity attack) in SystemAPI
                         .Query<RefRW<LocalTransform>, AbilityMoveSpeed, DefaultAttackTarget>()
                         .WithAll<Simulate>()
                         .WithEntityAccess())
            {
                if (target.Value == Entity.Null) continue;
                
                if (!transformLookup.TryGetComponent(target.Value, out LocalTransform targetTransform))
                {
                    ecb.AddComponent<DestroyEntityTag>(attack);
                    continue;
                }
                
                float3 direction = normalize(targetTransform.Position - transform.ValueRO.Position);
                transform.ValueRW.Position += direction * speed.Value * deltaTime;
                transform.ValueRW.Rotation = LookRotationSafe(direction, up());
            }
        }
    }
}