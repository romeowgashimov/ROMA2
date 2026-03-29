using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using static Unity.Mathematics.math;
using static Unity.Mathematics.quaternion;

namespace Logic.Common
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct MoveSystem : ISystem
    {
        private EntityQuery _query;
        
        public void OnCreate(ref SystemState state)
        {
            _query = SystemAPI.QueryBuilder()
                .WithAll<LocalTransform, MoveTargetPosition, MoveSpeed,
                    PathPositionElement, FollowPathIndex, Simulate>()
                .WithNone<NeedPath>()
                .Build();
            
            state.RequireForUpdate(_query);
        }

        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            MoveJob moveJob = new() { DeltaTime = deltaTime };
            state.Dependency = moveJob.ScheduleParallel(_query, state.Dependency);
        }       
    }
    // Добавить интерполяцию
    [BurstCompile]
    public partial struct MoveJob : IJobEntity
    {
        public float DeltaTime;
        
        private void Execute(RefRW<LocalTransform> transform,
            MoveTargetPosition target, MoveSpeed moveSpeed,
            RefRW<FollowPathIndex> followPathIndex, ref DynamicBuffer<PathPositionElement> pathPositions)
        {
            if (pathPositions.IsEmpty) return;
            
            float2 targetInt2;
            switch (followPathIndex.ValueRO.Value)
            {
                case 0:
                    targetInt2 = new(target.Value.x, target.Value.z);
                    break;
                case > 0:
                    targetInt2 = pathPositions[followPathIndex.ValueRO.Value].Value;
                    break;
                default:
                    return;
            }

            float3 selfPosition = transform.ValueRO.Position;
            float3 targetFloat3 = new(targetInt2.x, selfPosition.y, targetInt2.y);
            float3 vectorToTarget = targetFloat3 - selfPosition;

            float3 moveDirection = normalizesafe(vectorToTarget);
            float3 moveVector = DeltaTime * moveSpeed.Value * moveDirection;
            
            float targetDistance = lengthsq(vectorToTarget);
            float moveDistance = lengthsq(moveVector);
            if(moveDistance > targetDistance)
                moveVector = vectorToTarget;

            transform.ValueRW.Position += moveVector;
            transform.ValueRW.Rotation = LookRotationSafe(moveDirection, up());
            
            if (distancesq(selfPosition, targetFloat3) <= 0.3f) 
                followPathIndex.ValueRW.Value -= 1;
        }
    }
}