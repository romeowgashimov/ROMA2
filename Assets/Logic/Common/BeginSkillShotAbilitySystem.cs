using Logic.Client;
using Unity.Collections;
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

            /*Без точки синхронизации при отставании сервера, он не успеет обработать команду,
            высока вероятность появления новых команд, так как старые ещё не обработаны, следовательно,
            могут появиться непредвиденные новые объекты.
            Лучше сделать точки синхронизации, а именно новые new ecb(temp), а в конце Playback
            Многопоток можно и нужно использовать в случаях, когда есть сложные и продолжительные вычисления*/
            EntityCommandBuffer ecb = new(Allocator.Temp);

            bool isServer = state.WorldUnmanaged.IsServer();

            foreach ((AbilityInput abilityInput, DynamicBuffer<AbilityCooldownTargetTicks> cooldownTargetTicks,
                         LocalTransform transform, Entity champion) in SystemAPI
                         .Query<AbilityInput, DynamicBuffer<AbilityCooldownTargetTicks>, LocalTransform>()
                         .WithAll<AbilityPrefabs, AbilityCooldownTicks, Team>()
                         .WithAll<AimInput, Simulate>()
                         .WithNone<AimSkillShotTag>()
                         .WithEntityAccess())
            {
                if (cooldownTargetTicks.IsOnCooldown(netTime, AbilityType.SkillShotAbility)) continue;

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
            
                if (cooldownTargetTicks.IsOnCooldown(netTime, AbilityType.SkillShotAbility)) continue;
                
                Entity skillShot = ecb.Instantiate(abilityPrefabs.SkillShotAbility);
                LocalTransform newPosition = LocalTransform.FromPositionRotation(transform.Position,
                    quaternion.LookRotationSafe(aimInput.Value, math.up()));
                ecb.SetComponent(skillShot, newPosition);
                ecb.SetComponent(skillShot, team);
                // Replace RemoveComponent for SetEnabledComponent for all cases
                ecb.RemoveComponent<AimSkillShotTag>(entity);
            
                if (isServer) continue;
            
                cooldownTargetTicks.UpdateCooldown(abilityCooldownTicks, netTime, AbilityType.SkillShotAbility);
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
            
            ecb.Playback(state.EntityManager);
        }
    }
}