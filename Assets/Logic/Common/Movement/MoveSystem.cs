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
    public partial struct MoveSystem : ISystem
    {
        private EntityQuery _query;
        
        public void OnCreate(ref SystemState state)
        {
            _query = QueryBuilder()
                .WithAll<LocalTransform, MoveTargetPosition, MoveSpeed,
                    PathPositionElement, FollowPathProperties, Simulate, 
                    PhysicsVelocity>()
                .WithAll<AttackRadius, TargetEntity>()
                .WithNone<PathFindingRequest, MinionTag, IncorrectPathProperties, CleanPath>()
                .Build();
            
            state.RequireForUpdate<GameplayingTag>();
            state.RequireForUpdate(_query);
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
    
    /* Может быть, стоит рассмотреть простой пропуск одного индекса впереди
     вместо нынешней проверки границы с окружностью. 
     Потому что в любом случае пропускается один-два индекса максимум, 
     чтобы не пропустить препятствия размером одну-две клетки на границах */
    [BurstCompile]
    public partial struct MoveJob : IJobEntity
    {
        [ReadOnly] [NativeDisableContainerSafetyRestriction]
        public ComponentLookup<LocalTransform> TransformLookup;
        public float DeltaTime;

        private void Execute(
            ref LocalTransform transform,
            ref PhysicsVelocity velocity,
            in MoveTargetPosition goalPos, 
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
                    velocity.Linear = zero;
                    float3 dir = normalizesafe(targetPos - selfPosition);
                    quaternion targetRot = LookRotationSafe(new(dir.x, 0, dir.z), up());
                    transform.Rotation = slerp(transform.Rotation, targetRot, DeltaTime * 10f);
                    return;
                }
            }
            else attackTarget.Value = Null;
            
            // Инициализируем стартовую позицию для проверки "застревания"
            if (followPathProperties.Index == pathPositions.Length - 1)
            {
                followPathProperties.FuturePosition = selfPosition.xz;
                // Чтобы после обновления пути персонаж не дрифтил
                velocity.Linear = zero;
            }
            // Логика переключения вейпоинтов через окружности
            float2 target = pathPositions[followPathProperties.Index].Value;
            float2 self = transform.Position.xz;
            float2 move = normalizesafe(target - self);
            float dist = lengthsq(target - self);
            // Опережаем на 1.6 вейпоинта
            while (dist <= 4.41 // 2.1^2
                   && followPathProperties.Index > 0
                   // Обновляем будущий вейпоинт, когда дойдём до позиции опережающего вейпоинта
                   && lengthsq(followPathProperties.FuturePosition - selfPosition.xz) <= 0.25) // 0.5^2
            {
                followPathProperties.Index--;
                self += move * moveSpeed.Value;
                target = pathPositions[followPathProperties.Index].Value;
                move = normalizesafe(target - self);
                dist += lengthsq(target - self);
            }
            followPathProperties.FuturePosition = self;

            float3 targetFloat3;
            if (followPathProperties.Index == 0) 
                targetFloat3 = new(goalPos.Value.x, selfPosition.y, goalPos.Value.z);
            else targetFloat3 = new(target.x, selfPosition.y, target.y);
            
            float3 noise = followPathProperties.Index == 0 
                ? zero 
                : velocity.Linear / moveSpeed.Value;
            
            // Считаем чистый вектор и расстояние до цели БЕЗ ноиза для точной остановки
            float3 vectorToTarget = targetFloat3 - selfPosition;
            float distanceToTarget = length(vectorToTarget);

            // Направление движения (с шумом, как у вас)
            float3 moveDirection = normalizesafe(vectorToTarget + noise);

            // Рассчитываем желаемую скорость
            float desiredSpeed = moveSpeed.Value;

            // Критически важный шаг: скорость за кадр (Speed * DeltaTime) не должна превышать расстояние до цели
            // Делим расстояние на DeltaTime, чтобы получить максимально допустимую скорость для этого кадра
            float maxAllowedSpeed = distanceToTarget / DeltaTime;
            float finalSpeed = min(desiredSpeed, maxAllowedSpeed);

            // Применяем линейную скорость
            velocity.Linear = moveDirection * finalSpeed;

            // Поворот
            if (lengthsq(velocity.Linear) > 0.01) 
                transform.Rotation = LookRotationSafe(velocity.Linear, up());
        }
    }
}
