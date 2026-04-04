using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Logic.Common
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(CalculateFrameDamageSystem))]
    public partial struct ApplyDamageSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<GameplayingTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            NetworkTick currentTick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
            EntityCommandBuffer ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach ((RefRW<CurrentHealthPoints> currentHealthPoints, DynamicBuffer<DamageThisTick> damageThisTickBuffer,
                         Entity entity) in SystemAPI
                         .Query<RefRW<CurrentHealthPoints>, DynamicBuffer<DamageThisTick>>()
                         .WithAll<Simulate>()
                         .WithEntityAccess())
            {
                if (!damageThisTickBuffer.GetDataAtTick(currentTick, out DamageThisTick damageThisTick)) continue;
                if (damageThisTick.Tick != currentTick) continue;
                
                int health = currentHealthPoints.ValueRO.Value;
                health = math.clamp(health - damageThisTick.Value, 0, health);
                currentHealthPoints.ValueRW.Value = health;
                
                if (health == 0)
                    ecb.AddComponent<DestroyEntityTag>(entity);
            }
        }
    }
}