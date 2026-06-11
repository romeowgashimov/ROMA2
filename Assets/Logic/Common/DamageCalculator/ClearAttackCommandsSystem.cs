using ROMA2.Logic.Common.Databases;
using ROMA2.Logic.Data;
using Unity.Entities;

namespace ROMA2.Logic.Common.DamageCalculator
{
    [UpdateInGroup(typeof(DamageCalculatorSystemGroup), OrderLast = true)]
    public partial struct ClearAttackCommandSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer.ParallelWriter ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged)
                .AsParallelWriter();

            state.Dependency = new ClearAttackCommandJob
            {
                ecb = ecb
            }.ScheduleParallel(state.Dependency);
        }
    }

    [WithAll(typeof(ProcessedAttackCommand), typeof(AttackCommand))]
    public partial struct ClearAttackCommandJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;

        public void Execute(
            [ChunkIndexInQuery] int key,
            Entity mainCommand)
        {
            ecb.DestroyEntity(key, mainCommand);
        }
    }
}