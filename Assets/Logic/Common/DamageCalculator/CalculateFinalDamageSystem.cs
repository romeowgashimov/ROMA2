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
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer.ParallelWriter ecb = SystemAPI
                .GetSingleton<EndPredictedSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged)
                .AsParallelWriter();

            state.Dependency = new CalculateFinalDamageJob
            {
                AttackCommandLookup = SystemAPI.GetComponentLookup<AttackCommand>(true),
                ecb = ecb
            }.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    [WithNone(typeof(DestroyEntityTag))]
    public partial struct CalculateFinalDamageJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<AttackCommand> AttackCommandLookup; 
        public EntityCommandBuffer.ParallelWriter ecb;
        
        public void Execute(
            [ChunkIndexInQuery] int key,
            in PhysicalArmor physicalArmor,
            in MagicalArmor magicalArmor,
            in DynamicBuffer<IncomingDamageChangerElement> damageChangerBuffer,
            ref DynamicBuffer<DamageBufferElement> damageBuffer,
            ref DynamicBuffer<AttackCommandElement> attackCommands,
            Entity receiver)
        {
            if (attackCommands.IsEmpty) return;
            
            for (int i = 0; i < attackCommands.Length; i++)
            {
                Entity mainCommand = attackCommands[i].Value;
                if (!AttackCommandLookup.TryGetComponent(mainCommand, out AttackCommand attackCommand))
                    return;

                float physicalDamage = attackCommand.PhysicalDamage;
                float magicalDamage = attackCommand.MagicalDamage;
                float trueDamage = attackCommand.TrueDamage;
                
                physicalDamage *= math.clamp((100 - physicalArmor.Value) / 100f, 0, 1);
                magicalDamage *= math.clamp((100 - magicalArmor.Value) / 100f, 0, 1);

                foreach (IncomingDamageChangerElement changer in damageChangerBuffer)
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

                float totalDamage = physicalDamage + magicalDamage + trueDamage;

                damageBuffer.Add(new()
                {
                    Value = totalDamage,
                    DealingDamageEntity = attackCommand.Owner
                });
                attackCommands.RemoveAtSwapBack(i);

                ecb.SetComponent<AttackCommand>(key, mainCommand, new()
                {
                    PhysicalDamage = physicalDamage,
                    MagicalDamage = magicalDamage,
                    TrueDamage = trueDamage,
                    Owner = attackCommand.Owner,
                    Receiver = receiver
                });
                ecb.SetComponentEnabled<ProcessedAttackCommand>(key, mainCommand, true);
            }
        }
    }
}