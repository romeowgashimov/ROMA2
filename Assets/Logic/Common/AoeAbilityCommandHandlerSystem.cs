using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace Logic.Common
{
    [UpdateInGroup(typeof(AbilityCommandSystemGroup))]
    public partial struct AoeAbilityCommandHandlerSystem : ISystem
    {
        private EntityQuery _query;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<EndPredictedSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<AoeAbilityCommand>();

            _query = SystemAPI.QueryBuilder()
                .WithAll<AbilityInput, AbilityPrefabs, AbilityCooldownTicks, Team,
                    LocalTransform, AbilityCooldownTargetTicks, AimInput>()
                .WithAll<AoeAbilityCommand, AimingTag, ActivatedAbilitiesCommands, Simulate>()
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
            
            foreach ((AoeAbilityCommand aoeAbilityCommand, AbilityCommand abilityCommand,
                         Entity commandEntity) in SystemAPI
                         .Query<AoeAbilityCommand, AbilityCommand>()
                         .WithAll<Simulate>()
                         .WithEntityAccess())
            {
                aoeAbilityCommand.AddOrSetAbilityCommand(
                    abilityCommand, commandEntity, state.EntityManager, ecb, isServer);
            }
            
            state.Dependency = new AoeAbilityCommandJob
            {
                ECB = ecb.AsParallelWriter(),
                NetTime = netTime,
                IsServer = isServer
            }.ScheduleParallel(_query, state.Dependency);
        }
    }
    
    [BurstCompile]
    public partial struct AoeAbilityCommandJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        public NetworkTime NetTime;
        public bool IsServer;

        private void Execute([ChunkIndexInQuery] int key, AbilityInput abilityInput,
            AbilityCooldownTicks abilityCooldownTicks, Team team, 
            RefRW<ActivatedAbilitiesCommands> activatedAbilitiesCommands,
            LocalTransform transform, DynamicBuffer<AbilityCooldownTargetTicks> cooldownTargetTicks,
            AimInput aimInput, AoeAbilityCommand aoeAbilityCommand, Entity owner)
        {
            aoeAbilityCommand.InstantiateAbilityParallel(
                key, IsServer, NetTime, ECB, abilityInput, abilityCooldownTicks, team, activatedAbilitiesCommands, 
                transform, cooldownTargetTicks, aimInput, owner);
        }
    }
}