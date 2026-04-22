using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Physics; // Нужно добавить для PhysicsVelocity
using static Unity.Mathematics.math;
using float3 = Unity.Mathematics.float3;
using quaternion = Unity.Mathematics.quaternion;

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
                    PathPositionElement, FollowPathIndex, Simulate, 
                    PhysicsVelocity>() // ОБЯЗАТЕЛЬНО: объект должен быть Dynamic
                .WithNone<NeedPath>()
                .Build();
            
            state.RequireForUpdate<GameplayingTag>();
            state.RequireForUpdate(_query);
        }

        public void OnUpdate(ref SystemState state)
        {
            // В физике часто лучше использовать FixedStep, 
            // но для PredictedSimulation оставляем DeltaTime
            float deltaTime = SystemAPI.Time.DeltaTime;

            MoveJob moveJob = new() { DeltaTime = deltaTime };
            state.Dependency = moveJob.ScheduleParallel(_query, state.Dependency);
        }       
    }

    [BurstCompile]
    public partial struct MoveJob : IJobEntity
    {
        public float DeltaTime;
        
        private void Execute(
            ref LocalTransform transform,
            ref PhysicsVelocity velocity, // Управляем через скорость
            in MoveTargetPosition target, 
            in MoveSpeed moveSpeed,
            ref FollowPathIndex followPathIndex, 
            ref DynamicBuffer<PathPositionElement> pathPositions)
        {
            if (pathPositions.IsEmpty) 
            {
                velocity.Linear = float3.zero; // Останавливаем, если пути нет
                return;
            }
            
            float2 targetInt2;
            int currentIndex = followPathIndex.Value;

            if (currentIndex == 0)
            {
                targetInt2 = new float2(target.Value.x, target.Value.z);
            }
            else if (currentIndex > 0 && currentIndex < pathPositions.Length)
            {
                targetInt2 = pathPositions[currentIndex].Value;
            }
            else
            {
                velocity.Linear = float3.zero;
                return;
            }

            float3 selfPosition = transform.Position;
            float3 targetFloat3 = new float3(targetInt2.x, selfPosition.y, targetInt2.y);
            float3 vectorToTarget = targetFloat3 - selfPosition;
            float distanceSq = lengthsq(vectorToTarget);

            // Если мы уже на месте
            if (distanceSq <= 0.09f) // 0.3f в квадрате
            {
                followPathIndex.Value -= 1;
                velocity.Linear = float3.zero;
                return;
            }

            float3 moveDirection = normalizesafe(vectorToTarget);

            // ПРИМЕНЕНИЕ ФИЗИКИ:
            // Вместо transform.Position += ... мы задаем линейную скорость.
            // Физический движок сам применит эту скорость и остановит объект при столкновении.
            velocity.Linear = moveDirection * moveSpeed.Value;

            // Поворот оставляем через трансформацию (мгновенный взгляд на цель)
            if (!moveDirection.Equals(float3.zero))
            {
                transform.Rotation = quaternion.LookRotationSafe(moveDirection, up());
            }

            // Важно: для Dynamic объектов в DOTS не стоит менять Y вручную, 
            // если вы хотите, чтобы работала гравитация. 
            // velocity.Linear.y останется нетронутым, если вы будете задавать только X и Z.
        }
    }
}
