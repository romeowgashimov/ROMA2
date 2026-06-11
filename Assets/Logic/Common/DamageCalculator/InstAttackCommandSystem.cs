using ROMA2.Logic.Common.Databases;
using ROMA2.Logic.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace ROMA2.Logic.Common.DamageCalculator
{
    [BurstCompile]
    [UpdateInGroup(typeof(DamageCalculatorSystemGroup))]
    public partial struct InstAttackCommandSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<EndPredictedSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            NetworkTime netTime = SystemAPI.GetSingleton<NetworkTime>();
            if (!netTime.IsFirstTimeFullyPredictingTick) return;

            EntityCommandBuffer.ParallelWriter ecb = SystemAPI
                .GetSingleton<EndPredictedSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged)
                .AsParallelWriter();

            state.Dependency = new InstAttackCommandJob
            {
                OutDamageChangerLookup = SystemAPI.GetBufferLookup<OutgoingDamageChangerElement>(true),
                ecb = ecb
            }.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    [WithNone(typeof(DestroyEntityTag))]
    [WithDisabled(typeof(ProcessedAttackCommand))]
    public partial struct InstAttackCommandJob : IJobEntity
    {
        [ReadOnly] public BufferLookup<OutgoingDamageChangerElement> OutDamageChangerLookup;
        public EntityCommandBuffer.ParallelWriter ecb;

        public void Execute(
            [ChunkIndexInQuery] int key,
            ref AttackCommand attackCommand,
            EnabledRefRW<NewAttackCommand> newCommand,
            Entity mainCommand)
        {
            float physicalDamage = attackCommand.PhysicalDamage;
            float magicalDamage = attackCommand.MagicalDamage;
            float trueDamage = attackCommand.TrueDamage;

            if (OutDamageChangerLookup.TryGetBuffer(attackCommand.Owner, 
                out DynamicBuffer<OutgoingDamageChangerElement> outChangerBuffer))
            {
                foreach (OutgoingDamageChangerElement changer in outChangerBuffer)
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
            }

            attackCommand.PhysicalDamage = physicalDamage;
            attackCommand.MagicalDamage = magicalDamage;
            attackCommand.TrueDamage = trueDamage;

            ecb.AppendToBuffer<AttackCommandElement>(key, attackCommand.Receiver, new() 
            { 
                Value = mainCommand 
            });
            newCommand.ValueRW = false;
        }
    }
}