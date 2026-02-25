using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;


namespace Logic.Common
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct BeginSkillShotAbilitySystem : ISystem
    {
        private EntityQuery _queryBeginTag;
        private EntityQuery _queryAfterTag;

        public void OnCreate(ref SystemState state)
        {
            _queryBeginTag = SystemAPI.QueryBuilder()
                .WithAll<AbilityInput, AbilityPrefabs,
                    AbilityCooldownTicks, Team,
                    LocalTransform, AbilityCooldownTargetTicks,
                    AimInput>()
                .WithAll<Simulate>()
                .WithNone<AimSkillShotTag>()
                .Build();

            _queryAfterTag = SystemAPI.QueryBuilder()
                .WithAll<AbilityInput, AbilityPrefabs,
                    AbilityCooldownTicks, Team,
                    LocalTransform, AbilityCooldownTargetTicks,
                    AimInput>()
                .WithAll<AimSkillShotTag, Simulate>()
                .Build();
            
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<EndPredictedSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            NetworkTime netTime = SystemAPI.GetSingleton<NetworkTime>();
            if (!netTime.IsFirstTimeFullyPredictingTick) return;
            
            EntityCommandBuffer ecb = SystemAPI
                .GetSingleton<EndPredictedSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            JobHandle jobHandle = new BeginSkillShotTagJob
            {
                NetworkTime = netTime,
                ECB = ecb
            }.Schedule(_queryBeginTag, state.Dependency);
            
            state.Dependency = new AfterSkillShotTagJob
            {
                NetworkTime = netTime,
                ECB = ecb
            }.Schedule(_queryAfterTag, jobHandle);
            
        }
    }

    [BurstCompile]
    public partial struct BeginSkillShotTagJob : IJobEntity
    {
        public EntityCommandBuffer ECB;
        public NetworkTime NetworkTime;

        private void Execute(in AbilityInput abilityInput,
            in DynamicBuffer<AbilityCooldownTargetTicks> cooldownTargetTicks, 
            in Entity champion)
        {
            NetworkTick currentTick = NetworkTime.ServerTick;
            bool isOnCooldown = true;

            for (uint i = 0u; i < NetworkTime.SimulationStepBatchSize; i++)
            {
                NetworkTick testTick = currentTick;
                testTick.Subtract(i);

                if (!cooldownTargetTicks.GetDataAtTick(testTick, out AbilityCooldownTargetTicks curTargetTicks))
                    curTargetTicks.SkillShotAbility = NetworkTick.Invalid;

                if (curTargetTicks.SkillShotAbility == NetworkTick.Invalid ||
                    !curTargetTicks.SkillShotAbility.IsNewerThan(currentTick))
                {
                    isOnCooldown = false;
                    break;
                }
            }

            if (isOnCooldown) return;

            if (!abilityInput.SkillShotAbility.IsSet) return;
            ECB.AddComponent<AimSkillShotTag>(champion);
        }
    }

    [BurstCompile]
    public partial struct AfterSkillShotTagJob : IJobEntity
    {
        public EntityCommandBuffer ECB;
        public NetworkTime NetworkTime;
        public bool IsServer;

        private void Execute(AbilityInput abilityInput, AbilityPrefabs abilityPrefabs,
            AbilityCooldownTicks abilityCooldownTicks, Team team,
            LocalTransform transform, DynamicBuffer<AbilityCooldownTargetTicks> cooldownTargetTicks,
            AimInput aimInput)
        {
            if (!abilityInput.ConfirmSkillShotAbility.IsSet) return;
            
            NetworkTick currentTick = NetworkTime.ServerTick;
            bool isOnCooldown = true;
            AbilityCooldownTargetTicks curTargetTicks;

            for (uint i = 0u; i < NetworkTime.SimulationStepBatchSize; i++)
            {
                NetworkTick testTick = currentTick;
                testTick.Subtract(i);

                if (!cooldownTargetTicks.GetDataAtTick(testTick, out curTargetTicks))
                    curTargetTicks.SkillShotAbility = NetworkTick.Invalid;

                if (curTargetTicks.SkillShotAbility == NetworkTick.Invalid ||
                    !curTargetTicks.SkillShotAbility.IsNewerThan(currentTick))
                {
                    isOnCooldown = false;
                    break;
                }
            }

            if (isOnCooldown) return;
            
            Entity skillShot = ECB.Instantiate(abilityPrefabs.SkillShotAbility);
            LocalTransform newPosition = LocalTransform.FromPositionRotation(transform.Position,
                quaternion.LookRotationSafe(aimInput.Value, math.up()));
            ECB.SetComponent(skillShot, newPosition);
            ECB.SetComponent(skillShot, team);
            // Replace RemoveComponent for SetEnabledComponent for all cases
            ECB.RemoveComponent<AimSkillShotTag>(skillShot);
            
            if (IsServer) return;
            
            cooldownTargetTicks.GetDataAtTick(currentTick, out curTargetTicks);
            
            NetworkTick newCooldownTargetTicks = currentTick;
            newCooldownTargetTicks.Add(abilityCooldownTicks.SkillShotAbility);
            curTargetTicks.SkillShotAbility = newCooldownTargetTicks;

            NetworkTick nextTick = currentTick;
            nextTick.Add(1u);
            curTargetTicks.Tick = nextTick;

            cooldownTargetTicks.AddCommandData(curTargetTicks);
        }
    }
}