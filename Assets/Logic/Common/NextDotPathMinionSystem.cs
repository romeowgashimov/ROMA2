using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using static Unity.Mathematics.float3;
using static Unity.Mathematics.math;
using float3 = Unity.Mathematics.float3;

namespace Logic.Common
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct NextDotPathMinionSystem : ISystem
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
            
            state.Dependency = new NextDotPathJob
            {
                Ecb = ecbSingleton
            }.ScheduleParallel(state.Dependency);
        }
    }

    // Сделать через dot не получится из-за топ лейна, миньоны не могут пересечь перпендикуляр
    // Да и подумав, посидев, сука, целый день, нахуй надо...
    [BurstCompile]
    [WithDisabled(typeof(NeedPath), typeof(AggressionTag))]
    public partial struct NextDotPathJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;

        // IJobEntity автоматически подберет нужные компоненты на основе параметров метода Execute
        public void Execute(Entity entity, [ChunkIndexInQuery] int chunkIndex,
            RefRW<LocalTransform> transform, 
            DynamicBuffer<PathPositionElement> pathPositions,
            DynamicBuffer<MinionPathPosition> pathMinions, 
            RefRW<MinionPathIndex> pathIndex,
            RefRW<MoveTargetPosition> moveTargetPosition, 
            RefRW<LastTargetPosition> lastTargetPosition)
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
                Ecb.SetComponentEnabled<NeedPath>(chunkIndex, entity, true);
            }

            if (!lastTargetPosition.ValueRO.Value.Equals(zero))
            {
                lastTargetPosition.ValueRW.Value = zero;
                moveTargetPosition.ValueRW.Value = curTarget;
                Ecb.SetComponentEnabled<NeedPath>(chunkIndex, entity, true);
            }
        }
    }
}
