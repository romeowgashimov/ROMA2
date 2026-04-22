using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
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

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            ComponentLookup<LocalTransform> transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(isReadOnly: true);
            EntityCommandBuffer.ParallelWriter ecb = SystemAPI
                .GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged)
                .AsParallelWriter();
            float deltaTime = SystemAPI.Time.DeltaTime;

            state.Dependency = new DefaultAttackTargetingJob
            {
                TransformLookup = transformLookup,
                DeltaTime = deltaTime,
                ECB = ecb
            }.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct DefaultAttackTargetingJob : IJobEntity
    {
        [NativeDisableContainerSafetyRestriction]
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        public EntityCommandBuffer.ParallelWriter ECB;
        public float DeltaTime;
        
        private void Execute([ChunkIndexInQuery] int sortKey, RefRW<LocalTransform> transform, AbilityMoveSpeed speed,
            DefaultAttackTarget target, Entity attack)
        {
            if (target.Value == Entity.Null) return;
                
            if (!TransformLookup.TryGetComponent(target.Value, out LocalTransform targetTransform))
            {
                ECB.AddComponent<DestroyEntityTag>(sortKey, attack);
                return;
            }
                
            float3 direction = normalize(targetTransform.Position - transform.ValueRO.Position);
            transform.ValueRW.Position += direction * speed.Value * DeltaTime;
            transform.ValueRW.Rotation = LookRotationSafe(direction, up());
        }
    }
}