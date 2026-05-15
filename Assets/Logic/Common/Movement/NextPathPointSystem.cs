using Logic.Common;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using static Unity.Mathematics.float3;
using static Unity.Mathematics.math;
using float3 = Unity.Mathematics.float3;

namespace ROMA2.Logic.Common.Behaviour
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct NextPathPointSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GameplayingTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer.ParallelWriter ecbSingleton = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged)
                .AsParallelWriter();
            
            state.Dependency = new NextPathPointJob
            {
                Ecb = ecbSingleton
            }.ScheduleParallel(state.Dependency);
        }
    }
    
    [BurstCompile]
    [WithDisabled(typeof(PathFindingRequest))]
    public partial struct NextPathPointJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;
        
        public void Execute(
            Entity entity, 
            [ChunkIndexInQuery] int chunkIndex,
            RefRW<LocalTransform> transform, 
            DynamicBuffer<PathPositionElement> pathPositions,
            DynamicBuffer<MinionPathPosition> pathMinions, 
            RefRW<MinionPathIndex> pathIndex,
            RefRW<MoveTargetPosition> moveTargetPosition, 
            RefRW<LastTargetPosition> lastTargetPosition,
            in TargetEntity targetEntity)
        {
            bool isAtStart = pathPositions.IsEmpty;
            float dist = 0;
            float3 curTarget = zero;

            if (!isAtStart)
            {
                curTarget = pathMinions[pathIndex.ValueRO.Value].Value;
                dist = distancesq(curTarget, transform.ValueRO.Position);
            }

            if (isAtStart || dist <= 64f) // 8^2
            {
                if (!isAtStart && pathIndex.ValueRO.Value < pathMinions.Length - 1)
                {
                    pathIndex.ValueRW.Value++;
                    curTarget = pathMinions[pathIndex.ValueRO.Value].Value;
                }

                moveTargetPosition.ValueRW.Value = curTarget;
                Ecb.SetComponentEnabled<PathFindingRequest>(chunkIndex, entity, true);
            }

            if (!lastTargetPosition.ValueRO.Value.Equals(zero) && targetEntity.Value == Entity.Null)
            {
                lastTargetPosition.ValueRW.Value = zero;
                moveTargetPosition.ValueRW.Value = curTarget;
                Ecb.SetComponentEnabled<PathFindingRequest>(chunkIndex, entity, true);
            }
        }
    }
}
