using ROMA2.Logic.Common.Databases;
using ROMA2.Logic.Data;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

namespace ROMA2.Logic.Common.DamageCalculator
{
    [BurstCompile]
    [UpdateInGroup(typeof(DamageCalculatorSystemGroup))]
    public partial struct DamageSenderSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer.ParallelWriter ecb = SystemAPI
                .GetSingleton<EndPredictedSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged)
                .AsParallelWriter();

            state.Dependency = new DamageSenderJob
            {
                ecb = ecb
            }.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct DamageSenderJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;

        public void Execute(
            [ChunkIndexInQuery] int key,
            ref DynamicBuffer<SendDamageElement> sendDamages)
        {
            if (sendDamages.IsEmpty) return;

            for (int i = 0; i < sendDamages.Length; i++)
            {
                SendDamageElement element = sendDamages[i];
                // Если буфер есть, то команда его не тронет, нужен только из-за отката состояний неткода
                ecb.AddBuffer<IncomingDamageElement>(key, element.Receiver);
                ecb.AppendToBuffer<IncomingDamageElement>(key, element.Receiver, new()
                {
                    PhysicalDamage = element.PhysicalDamage,
                    MagicalDamage = element.MagicalDamage,
                    TrueDamage = element.TrueDamage,
                    Owner = element.Owner,
                    AbilityIndex = element.AbilityIndex
                });
            }
            sendDamages.Clear();
        }
    }
}