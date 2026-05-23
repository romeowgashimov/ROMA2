using Logic.Common;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using static Unity.Mathematics.math;
using float3 = Unity.Mathematics.float3;

namespace ROMA2.Logic.Common.Movement
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct NextPathPointSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GameplayingTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer.ParallelWriter ecb = SystemAPI
                .GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged)
                .AsParallelWriter();
            
            state.Dependency = new NextPathPointJob
            {
                Ecb = ecb
            }.ScheduleParallel(state.Dependency);
        }
    }
    
    [WithNone(typeof(DestroyEntityTag))]
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
            in TargetEntity targetEntity)
        {
            if (pathMinions.IsEmpty) return;

            bool isEmpty = pathPositions.IsEmpty;
            float3 targetPos = pathMinions[pathIndex.ValueRO.Value].Value;
            bool passed = lengthsq(targetPos - transform.ValueRO.Position) <= 64;
            if (passed && pathIndex.ValueRO.Value < pathMinions.Length - 1)
            {
                pathIndex.ValueRW.Value++;
                targetPos = pathMinions[pathIndex.ValueRO.Value].Value;
            }

            bool recovery = !isEmpty
                            && lengthsq(pathPositions[0].Value - (int2)targetPos.xz) >= 4;
            
            if (targetEntity.Value == Entity.Null && (isEmpty || passed || recovery))
            {
                moveTargetPosition.ValueRW.Value = targetPos;
                Ecb.SetComponentEnabled<PathFindingRequest>(chunkIndex, entity, true);
            }
        }
    }
}
