using ROMA2.Logic.Common.Databases;
using ROMA2.Logic.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace ROMA2.Logic.Common.DamageCalculator
{
    [BurstCompile]
    [UpdateInGroup(typeof(DamageCalculatorSystemGroup))]
    public partial struct CalculateFinalDamageSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndPredictedSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer.ParallelWriter ecb = SystemAPI
                .GetSingleton<EndPredictedSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged)
                .AsParallelWriter();

            state.Dependency = new CalculateFinalDamageJob
            {
                OutgoingDamageChangerLookup = SystemAPI.GetBufferLookup<OutgoingDamageChangerElement>(true), 
                ProcessedDamageLookup = SystemAPI.GetBufferLookup<ProcessedDamageElement>(true),
                ecb = ecb
            }.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct CalculateFinalDamageJob : IJobEntity
    {
        [ReadOnly] public BufferLookup<OutgoingDamageChangerElement> OutgoingDamageChangerLookup; 
        [ReadOnly] public BufferLookup<ProcessedDamageElement> ProcessedDamageLookup; 

        public EntityCommandBuffer.ParallelWriter ecb;
        
        public void Execute(
            [ChunkIndexInQuery] int key,
            in PhysicalArmor physicalArmor,
            in MagicalArmor magicalArmor,
            in DynamicBuffer<IncomingDamageChangerElement> incomingDamageChangerBuffer,
            ref DynamicBuffer<DamageBufferElement> damageBuffer,
            ref DynamicBuffer<IncomingDamageElement> incomingDamageBuffer,
            Entity receiver)
        {
            if (incomingDamageBuffer.IsEmpty) return;
            
            for (int i = 0; i < incomingDamageBuffer.Length; i++)
            {
                IncomingDamageElement element = incomingDamageBuffer[i];
                Entity damageSender = element.Owner;
                float physicalDamage = element.PhysicalDamage;
                float magicalDamage = element.MagicalDamage;
                float trueDamage = element.TrueDamage;
                
                // Бафы/дебафы отправителя урона
                if (OutgoingDamageChangerLookup.TryGetBuffer(damageSender, 
                    out DynamicBuffer<OutgoingDamageChangerElement> outgoingDamageChangerBuffer))
                    foreach(OutgoingDamageChangerElement changer in outgoingDamageChangerBuffer)
                    {
                        switch (changer.Type)
                        {
                            case DamageType.Physical when changer.IsPercentage:
                                physicalDamage *= changer.Value;
                                break;
                            case DamageType.Physical:
                                physicalDamage += changer.Value;
                                break;
                            case DamageType.Magical when changer.IsPercentage:
                                magicalDamage *= changer.Value;
                                break;
                            case DamageType.Magical:
                                magicalDamage += changer.Value;
                                break;
                            case DamageType.All when changer.IsPercentage:
                                physicalDamage *= changer.Value;
                                magicalDamage *= changer.Value;
                                trueDamage *= changer.Value;
                                break;
                            case DamageType.All:
                                physicalDamage += changer.Value;
                                magicalDamage += changer.Value;
                                trueDamage += changer.Value;
                                break;
                        }
                    }

                physicalDamage *= math.clamp((100 - physicalArmor.Value) / 100f, 0, 1);
                magicalDamage *= math.clamp((100 - magicalArmor.Value) / 100f, 0, 1);

                // Бафы/дебафы получателя урона
                foreach (IncomingDamageChangerElement changer in incomingDamageChangerBuffer)
                {
                    switch (changer.Type)
                    {
                        case DamageType.Physical when changer.IsPercentage:
                            physicalDamage *= changer.Value;
                            break;
                        case DamageType.Physical:
                            physicalDamage += changer.Value;
                            break;
                        case DamageType.Magical when changer.IsPercentage:
                            magicalDamage *= changer.Value;
                            break;
                        case DamageType.Magical:
                            magicalDamage += changer.Value;
                            break;
                        case DamageType.All when changer.IsPercentage:
                            physicalDamage *= changer.Value;
                            magicalDamage *= changer.Value;
                            trueDamage *= changer.Value;
                            break;
                        case DamageType.All:
                            physicalDamage += changer.Value;
                            magicalDamage += changer.Value;
                            trueDamage += changer.Value;
                            break;
                    }
                }

                int totalDamage = (int)(physicalDamage + magicalDamage + trueDamage);

                damageBuffer.Add(new()
                {
                    Value = totalDamage,
                    DealingDamageEntity = damageSender
                });
                incomingDamageBuffer.RemoveAtSwapBack(i);

                // Если буфер есть, то команда его не тронет, нужен только из-за отката состояний неткода
                if (ProcessedDamageLookup.HasBuffer(damageSender))
                    ecb.AppendToBuffer<ProcessedDamageElement>(key, damageSender, new()
                    {
                        PhysicalDamage = (int)physicalDamage,
                        MagicalDamage = (int)magicalDamage,
                        TrueDamage = (int)trueDamage,
                        Receiver = receiver,
                        AbilityIndex = element.AbilityIndex
                    });
            }
        }
    }
}