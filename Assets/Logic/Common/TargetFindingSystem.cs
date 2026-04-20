using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Logic.Common
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct TargetFindingSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GameplayingTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach ((RefRW<LocalTransform> transform, DynamicBuffer<PathPositionElement> pathPositions, 
                         DynamicBuffer<MinionPathPosition> pathMinions, RefRW<MinionPathIndex> pathIndex,
                         RefRW<MoveTargetPosition> moveTargetPosition, RefRW<LastTargetPosition> lastTargetPosition,
                         Entity entity) in SystemAPI
                         .Query<RefRW<LocalTransform>, DynamicBuffer<PathPositionElement>, 
                             DynamicBuffer<MinionPathPosition>, RefRW<MinionPathIndex>, RefRW<MoveTargetPosition>, 
                             RefRW<LastTargetPosition>>()
                         .WithDisabled<NeedPath, AggressionTag>()
                         .WithEntityAccess())
            {
                bool isAtStart = pathPositions.IsEmpty;
                float dist = 0;
                float3 curTarget = float3.zero;
                
                if (!isAtStart)
                {
                    curTarget = pathMinions[pathIndex.ValueRO.Value].Value;
                    dist = math.distance(curTarget, transform.ValueRO.Position);
                }
                
                if (isAtStart || dist <= 1.5f)
                {
                    if (!isAtStart && pathIndex.ValueRO.Value < pathMinions.Length - 1)
                    {
                        pathIndex.ValueRW.Value++;
                        curTarget = pathMinions[pathIndex.ValueRO.Value].Value;
                    }

                    moveTargetPosition.ValueRW.Value = curTarget;
                    ecb.SetComponentEnabled<NeedPath>(entity, true);
                }

                if (lastTargetPosition.ValueRO.Value.Equals(float3.zero)) continue;
                lastTargetPosition.ValueRW.Value = float3.zero;
                moveTargetPosition.ValueRW.Value = curTarget;
                ecb.SetComponentEnabled<NeedPath>(entity, true);
            }
        }
    }
}