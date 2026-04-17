using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace Logic.Common
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct AbilityCommandSenderSystem : ISystem
    {
        private EntityQuery _query;

        public void OnCreate(ref SystemState state)
        {
            _query = SystemAPI.QueryBuilder()
                .WithAll<AbilityInput, AbilityPrefabs, LocalTransform,
                    AbilityCooldownTicks, AbilityCooldownTargetTicks, Simulate>()
                .WithAll<Team, AimInput, ActivatedAbilitiesCommands>()
                .Build();

            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<EndPredictedSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_query.CalculateEntityCount() == 0) return;
            
            NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();
            if (!networkTime.IsFirstTimeFullyPredictingTick) return;
            
            EntityCommandBuffer ecb = SystemAPI
                .GetSingleton<EndPredictedSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            state.Dependency = new AbilityCommandJob
            {
                ECB = ecb.AsParallelWriter(),
                NetworkTime = networkTime,
            }.ScheduleParallel(_query, state.Dependency);
        }
    }

    public partial struct AbilityCommandJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        public NetworkTime NetworkTime;
        
        private void Execute([EntityIndexInQuery] int key, AbilityInput input, AbilityPrefabs abilityPrefab,
            DynamicBuffer<AbilityCooldownTargetTicks> cooldownTargetTicks,
            RefRW<ActivatedAbilitiesCommands> activatedAbilitiesCommands, Entity owner)
        {
            for (int i = 0; i < input.Length; ++i)
            {
                if (!input[i].IsSet) continue; // Сделать так, чтобы нельзя было активировать два умения одновременно
                if (activatedAbilitiesCommands.ValueRO[i]) continue;
                if (cooldownTargetTicks.IsOnCooldown(NetworkTime, i)) continue;
                Entity abilityCommandEntity = ECB.Instantiate(key, abilityPrefab[i]);
                ECB.SetComponent(key, abilityCommandEntity, new AbilityCommand
                {
                    Owner = owner, AbilityIndex = i, NeedToConfirmAbilities = input.NeedToConfirmAbilities
                });
                activatedAbilitiesCommands.ValueRW[i] = true;
            }
        }
    }
}