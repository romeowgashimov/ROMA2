using ROMA2.Logic.Common.Databases;
using ROMA2.Logic.Data;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

namespace ROMA2.Logic.Common.DamageCalculator
{
    [UpdateInGroup(typeof(DamageCalculatorSystemGroup))]
    [UpdateBefore(typeof(DamageOnTriggerSystem))]
    public partial struct CalculateFrameDamageSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            NetworkTick currentTick = SystemAPI
                .GetSingleton<NetworkTime>()
                .ServerTick;
            
            foreach ((DynamicBuffer<DamageBufferElement> damageBuffer,
                         DynamicBuffer<DamageThisTick> damageThisTickBuffer) in SystemAPI
                         .Query<DynamicBuffer<DamageBufferElement>, DynamicBuffer<DamageThisTick>>()
                         .WithAll<Simulate>())
            {
                if (damageBuffer.IsEmpty)
                    damageThisTickBuffer.AddCommandData(new() { Tick = currentTick, Value = 0 });
                else
                {
                    float totalDamage = 0;
                    if (damageThisTickBuffer.GetDataAtTick(currentTick, out DamageThisTick damageThisTick))
                        totalDamage = damageThisTick.Value;
                    
                    foreach (DamageBufferElement damage in damageBuffer)
                        totalDamage += damage.Value;
                    
                    damageThisTickBuffer.AddCommandData(new() { Tick = currentTick, Value = totalDamage });
                    damageBuffer.Clear();
                }
            }
            
        }
    }
}