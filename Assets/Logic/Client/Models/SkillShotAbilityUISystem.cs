using ROMA2.Logic.Client.Data;
using ROMA2.Logic.Common.Abilities;
using ROMA2.Logic.Data;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ROMA2.Logic.Client.Models
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct SkillShotAbilityUISystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = SystemAPI
                .GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach ((LocalTransform transform, Entity owner) in SystemAPI
                         .Query<LocalTransform>()
                         .WithAll<DrawAbilityUITag<SkillShotAbilityCommand>, OwnerChampTag, Simulate>()
                         .WithAll<SkillShotAbilityCommand>()
                         .WithNone<UpdateAbilityUITag<SkillShotAbilityCommand>>()
                         .WithEntityAccess())
            {
                ecb.SetComponentEnabled<DrawAbilityUITag<SkillShotAbilityCommand>>(owner, false);
                ecb.SetComponentEnabled<UpdateAbilityUITag<SkillShotAbilityCommand>>(owner, true);
                
                GameObject skillShotPrefab = SystemAPI.ManagedAPI.GetSingleton<UIPrefabs>().SkillShot;
                GameObject skillShotUI =
                    Object.Instantiate(skillShotPrefab, transform.Position, Quaternion.identity);
                ecb.AddComponent(owner, new SkillShotUIReference { Value = skillShotUI });
            }
            
            foreach ((RefRW<AimInput> aimInput, RefRW<LocalTransform> localTransform,
                         SkillShotUIReference skillShotUIReference) in SystemAPI
                         .Query<RefRW<AimInput>, RefRW<LocalTransform>, SkillShotUIReference>()
                         .WithAll<UpdateAbilityUITag<SkillShotAbilityCommand>, OwnerChampTag, SkillShotAbilityCommand>())
            {
                skillShotUIReference.Value.transform.position = localTransform.ValueRO.Position;
                float3 direction = aimInput.ValueRO.Value;
                float angleRag = math.atan2(direction.z, direction.x);
                float angleDeg = math.degrees(angleRag);
                skillShotUIReference.Value.transform.rotation = Quaternion.Euler(0, -angleDeg, 0);
            }
            
            foreach ((SkillShotUIReference skillShotUIReference, Entity owner) in SystemAPI
                         .Query<SkillShotUIReference>()
                         .WithAll<UpdateAbilityUITag<SkillShotAbilityCommand>, OwnerChampTag>()
                         .WithAll<AimInput, LocalTransform>()
                         .WithNone<SkillShotAbilityCommand>()
                         .WithEntityAccess())
            {
                Object.Destroy(skillShotUIReference.Value);
                ecb.RemoveComponent<SkillShotUIReference>(owner);
                ecb.SetComponentEnabled<UpdateAbilityUITag<SkillShotAbilityCommand>>(owner, false);
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