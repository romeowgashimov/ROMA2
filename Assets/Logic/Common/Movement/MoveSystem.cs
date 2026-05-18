using Logic.Common;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using static Unity.Entities.Entity;
using static Unity.Entities.SystemAPI;
using static Unity.Mathematics.float3;
using static Unity.Mathematics.math;
using static Unity.Mathematics.quaternion;
using float2 = Unity.Mathematics.float2;
using float3 = Unity.Mathematics.float3;

namespace ROMA2.Logic.Common.Movement
{
    [BurstCompile]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(PathFindingSystem))]
    public partial struct MoveSystem : ISystem
    {
        private EntityQuery _query;
        
        public void OnCreate(ref SystemState state)
        {
            _query = QueryBuilder()
                .WithAll<LocalTransform, MoveTargetPosition, MoveSpeed,
                    PathPositionElement, FollowPathProperties, Simulate, PhysicsVelocity>()
                .WithAll<AttackRadius, TargetEntity>()
                .WithNone<PathFindingRequest, MinionTag, IncorrectPathProperties, CleanPath>()
                .Build();
            
            state.RequireForUpdate<GameplayingTag>();
            state.RequireForUpdate(_query);
            state.Enabled = false;
        }
        
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new MoveJob
            {
                TransformLookup = GetComponentLookup<LocalTransform>(true),
                DeltaTime = SystemAPI.Time.DeltaTime 
            }.ScheduleParallel(_query, state.Dependency);
        }       
    }
    
    [BurstCompile]
    public partial struct MoveJob : IJobEntity
    {
        [ReadOnly] [NativeDisableContainerSafetyRestriction]
        public ComponentLookup<LocalTransform> TransformLookup;
        public float DeltaTime;

        private void Execute(
            ref LocalTransform transform,
            ref PhysicsVelocity velocity,
            ref MoveTargetPosition goalPos, 
            in MoveSpeed moveSpeed,
            ref FollowPathProperties followPathProperties, 
            ref DynamicBuffer<PathPositionElement> pathPositions,
            in AttackRadius radius,
            ref TargetEntity attackTarget)
        {
            if (pathPositions.IsEmpty) 
            {
                velocity.Linear = zero;
                return;
            }
            
            float3 selfPosition = transform.Position;
            Entity targetEntity = attackTarget.Value;
            // Проверка дистанции атаки до цели
            if (targetEntity != Null && TransformLookup.HasComponent(targetEntity))
            {
                float3 targetPos = TransformLookup[targetEntity].Position;
                float distSq = distancesq(targetPos, selfPosition);
                float stopDist = radius.Value;
    
                if (distSq <= stopDist * stopDist)
                {
                    pathPositions.Clear();
                    velocity.Linear = zero;
                    float3 dir = normalizesafe(targetPos - selfPosition);
                    quaternion targetRot = LookRotationSafe(new(dir.x, 0, dir.z), up());
                    transform.Rotation = slerp(transform.Rotation, targetRot, DeltaTime * 10f);
                    return;
                }
            }
            
            if (followPathProperties.Index == pathPositions.Length - 1) velocity.Linear = zero;

            float2 target = pathPositions[followPathProperties.Index].Value;

            // Упростил логику, в любом случае пропускаем два индекса
            if (lengthsq(target - selfPosition.xz) <= 0.25 && followPathProperties.Index >= 2)
                followPathProperties.Index -= 2;
            
            float3 targetFloat3;
            // <= 1 обусловлено тем, что при нечётном количестве вейпоинтов концом пути будет 1
            if (followPathProperties.Index <= 1) 
                targetFloat3 = new(goalPos.Value.x, selfPosition.y, goalPos.Value.z);
            else targetFloat3 = new(target.x, selfPosition.y, target.y);
            
            float3 noise = followPathProperties.Index >= 2 
                ? velocity.Linear / moveSpeed.Value 
                : zero;
            
            // Считаем чистый вектор и расстояние до цели БЕЗ ноиза для точной остановки
            float3 vectorToTarget = targetFloat3 - selfPosition;
            float distanceToTarget = length(vectorToTarget);

            // Направление движения (с шумом, как у вас)
            float3 moveDirection = normalizesafe(vectorToTarget + noise);

            // Рассчитываем желаемую скорость
            float desiredSpeed = moveSpeed.Value;

            // Скорость за кадр (Speed * DeltaTime) не должна превышать расстояние до цели
            // Делим расстояние на DeltaTime, чтобы получить максимально допустимую скорость для этого кадра
            float maxAllowedSpeed = distanceToTarget / DeltaTime;
            float finalSpeed = min(desiredSpeed, maxAllowedSpeed);

            // Применяем линейную скорость
            velocity.Linear = moveDirection * finalSpeed;

            // Поворот
            if (lengthsq(moveDirection) > 0.25) 
                transform.Rotation = LookRotationSafe(moveDirection, up());
        }
    }
}
