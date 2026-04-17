using Logic.Client;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Logic.Common
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

            foreach ((SkillShotAbilityCommand skillShotAbilityCommand, LocalTransform transform,
                         Entity owner) in SystemAPI
                         .Query<SkillShotAbilityCommand, LocalTransform>()
                         .WithAll<DrawAbilityUITag, OwnerChampTag, Simulate>()
                         .WithNone<UpdateAbilityUITag>()
                         .WithEntityAccess())
            {
                ecb.SetComponentEnabled<DrawAbilityUITag>(owner, false);
                ecb.SetComponentEnabled<UpdateAbilityUITag>(owner, true);
                
                GameObject skillShotPrefab = SystemAPI.ManagedAPI.GetSingleton<UIPrefabs>().SkillShot;
                GameObject skillShotUI =
                    Object.Instantiate(skillShotPrefab, transform.Position, Quaternion.identity);
                ecb.AddComponent(owner, new SkillShotUIReference { Value = skillShotUI });
            }
            
            foreach ((RefRW<AimInput> aimInput, RefRW<LocalTransform> localTransform,
                         SkillShotUIReference skillShotUIReference) in SystemAPI
                         .Query<RefRW<AimInput>, RefRW<LocalTransform>, SkillShotUIReference>()
                         .WithAll<AimingTag, UpdateAbilityUITag, OwnerChampTag>())
            {
                skillShotUIReference.Value.transform.position = localTransform.ValueRO.Position;
                float3 direction = aimInput.ValueRO.Value;
                float angleRag = math.atan2(direction.z, direction.x);
                float angleDeg = math.degrees(angleRag);
                skillShotUIReference.Value.transform.rotation = Quaternion.Euler(0, -angleDeg, 0);
            }
            
            foreach ((RefRW<AimInput> aimInput, RefRW<LocalTransform> localTransform,
                         SkillShotUIReference skillShotUIReference, Entity owner) in SystemAPI
                         .Query<RefRW<AimInput>, RefRW<LocalTransform>, SkillShotUIReference>()
                         .WithAll<UpdateAbilityUITag, OwnerChampTag>()
                         .WithNone<AimingTag>()
                         .WithEntityAccess())
            {
                Object.Destroy(skillShotUIReference.Value);
                ecb.RemoveComponent<SkillShotUIReference>(owner);
                ecb.SetComponentEnabled<UpdateAbilityUITag>(owner, false);
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