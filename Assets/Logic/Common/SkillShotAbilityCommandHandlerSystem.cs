using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace Logic.Common
{
    [UpdateInGroup(typeof(AbilityCommandSystemGroup))]
    public partial struct SkillShotAbilityCommandHandlerSystem : ISystem
    {
        private EntityQuery _query;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<EndPredictedSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<SkillShotAbilityCommand>();

            _query = SystemAPI.QueryBuilder()
                .WithAll<AbilityInput, AbilityPrefabs, AbilityCooldownTicks, Team,
                    LocalTransform, AbilityCooldownTargetTicks, AimInput>()
                .WithAll<SkillShotAbilityCommand, AimingTag, ActivatedAbilitiesCommands, Simulate>()
                .Build();
        }

        public void OnUpdate(ref SystemState state)
        {
            NetworkTime netTime = SystemAPI.GetSingleton<NetworkTime>();
            if (!netTime.IsFirstTimeFullyPredictingTick) return;

            EntityCommandBuffer ecb = SystemAPI
                .GetSingleton<EndPredictedSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            bool isServer = state.WorldUnmanaged.IsServer();
            
            foreach ((SkillShotAbilityCommand skillShotAbilityCommand, AbilityCommand abilityCommand,
                         Entity commandEntity) in SystemAPI
                         .Query<SkillShotAbilityCommand, AbilityCommand>()
                         .WithAll<Simulate>()
                         .WithEntityAccess())
            {
                skillShotAbilityCommand.Send(
                    abilityCommand, commandEntity, state.EntityManager, ecb, isServer);
            }
            
            state.Dependency = new SkillShotAbilityCommandJob
            {
                ECB = ecb.AsParallelWriter(),
                NetTime = netTime,
                IsServer = isServer
            }.ScheduleParallel(_query, state.Dependency);
        }
    }
    
    [BurstCompile]
    public partial struct SkillShotAbilityCommandJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        public NetworkTime NetTime;
        public bool IsServer;

        private void Execute([ChunkIndexInQuery] int key, AbilityInput abilityInput,
            AbilityCooldownTicks abilityCooldownTicks, Team team, 
            RefRW<ActivatedAbilitiesCommands> activatedAbilitiesCommands,
            LocalTransform transform, DynamicBuffer<AbilityCooldownTargetTicks> cooldownTargetTicks,
            AimInput aimInput, SkillShotAbilityCommand skillShotAbilityCommand, Entity owner)
        {
            if (!skillShotAbilityCommand.IsConfirmParallel(
                    key, ECB, activatedAbilitiesCommands, abilityInput, owner)) return;

            skillShotAbilityCommand.InstantSkillShotParallel(key,  ECB, team, transform, aimInput, owner);
            
            skillShotAbilityCommand.CancelParallel(ECB, key, owner, activatedAbilitiesCommands);

            if (IsServer) return;

            cooldownTargetTicks.UpdateCooldown(abilityCooldownTicks, NetTime, skillShotAbilityCommand.AbilityIndex);
        }
    }
}