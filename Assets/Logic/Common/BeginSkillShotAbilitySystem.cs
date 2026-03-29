using Logic.Client;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Logic.Common
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct BeginSkillShotAbilitySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<BeginPredictedSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            NetworkTime netTime = SystemAPI.GetSingleton<NetworkTime>();
            if (!netTime.IsFirstTimeFullyPredictingTick) return;

            EntityCommandBuffer ecb = SystemAPI
                .GetSingleton<BeginPredictedSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            bool isServer = state.WorldUnmanaged.IsServer();

            foreach ((AbilityInput abilityInput, DynamicBuffer<AbilityCooldownTargetTicks> cooldownTargetTicks,
                         LocalTransform transform, Entity champion) in SystemAPI
                         .Query<AbilityInput, DynamicBuffer<AbilityCooldownTargetTicks>, LocalTransform>()
                         .WithAll<AbilityPrefabs, AbilityCooldownTicks, Team>()
                         .WithAll<AimInput, Simulate>()
                         .WithNone<AimSkillShotTag>()
                         .WithEntityAccess())
            {
                if (cooldownTargetTicks.IsOnCooldown(netTime)) continue;

                if (!abilityInput.SkillShotAbility.IsSet) continue;
                ecb.AddComponent<AimSkillShotTag>(champion);

                if (isServer || !SystemAPI.HasComponent<OwnerChampTag>(champion)) continue;

                GameObject skillShotPrefab = SystemAPI.ManagedAPI.GetSingleton<UIPrefabs>().SkillShot;
                GameObject skillShotUI = Object.Instantiate(skillShotPrefab, transform.Position, Quaternion.identity);
                ecb.AddComponent(champion, new SkillShotUIReference { Value =  skillShotUI });
            }

            foreach ((AbilityInput abilityInput, AbilityPrefabs abilityPrefabs,
                         AbilityCooldownTicks abilityCooldownTicks, Team team,
                         LocalTransform transform, DynamicBuffer<AbilityCooldownTargetTicks> cooldownTargetTicks,
                         AimInput aimInput, Entity entity) in SystemAPI
                         .Query<AbilityInput, AbilityPrefabs, AbilityCooldownTicks, Team,
                             LocalTransform, DynamicBuffer<AbilityCooldownTargetTicks>, AimInput>()
                         .WithAll<AimSkillShotTag>()
                         .WithAll<Simulate>()
                         .WithEntityAccess())
            {
                if (!abilityInput.ConfirmSkillShotAbility.IsSet) continue;
            
                if (cooldownTargetTicks.IsOnCooldown(netTime)) continue;

                NetworkTick currentTick = netTime.ServerTick;
                Entity skillShot = ecb.Instantiate(abilityPrefabs.SkillShotAbility);
                LocalTransform newPosition = LocalTransform.FromPositionRotation(transform.Position,
                    quaternion.LookRotationSafe(aimInput.Value, math.up()));
                ecb.SetComponent(skillShot, newPosition);
                ecb.SetComponent(skillShot, team);
                // Replace RemoveComponent for SetEnabledComponent for all cases
                ecb.RemoveComponent<AimSkillShotTag>(entity);
            
                if (isServer) continue;
            
                cooldownTargetTicks.GetDataAtTick(currentTick, out AbilityCooldownTargetTicks curTargetTicks);
            
                NetworkTick newCooldownTargetTicks = currentTick;
                newCooldownTargetTicks.Add(abilityCooldownTicks.SkillShotAbility);
                curTargetTicks.SkillShotAbility = newCooldownTargetTicks;

                NetworkTick nextTick = currentTick;
                nextTick.Add(1u);
                curTargetTicks.Tick = nextTick;

                cooldownTargetTicks.AddCommandData(curTargetTicks);
            }

            foreach ((AbilityInput abilityInput, SkillShotUIReference skillShotUIReference, Entity entity) in SystemAPI
                         .Query<AbilityInput, SkillShotUIReference>()
                         .WithAll<Simulate>()
                         .WithEntityAccess())
            {
                if (!abilityInput.ConfirmSkillShotAbility.IsSet) continue;
                Object.Destroy(skillShotUIReference.Value);
                ecb.RemoveComponent<SkillShotUIReference>(entity);
            }
            
            foreach ((SkillShotUIReference skillShotUIReference, Entity entity) in SystemAPI
                         .Query<SkillShotUIReference>()
                         .WithAll<Simulate>()
                         .WithNone<LocalTransform>()
                         .WithEntityAccess())
            {
                Object.Destroy(skillShotUIReference.Value);
                ecb.RemoveComponent<SkillShotUIReference>(entity);
            }
        }
    }
}