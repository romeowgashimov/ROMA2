using ROMA2.Logic.Client.Data;
using ROMA2.Logic.Data;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace ROMA2.Logic.Client.Models
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct ModelSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<ModelPrefabs>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = SystemAPI
                .GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            
            foreach ((LocalTransform transform, ModelId id, Team team, Entity entity) in SystemAPI
                        .Query<LocalTransform, ModelId, Team>()
                        .WithAny<ChampTag, MinionTag>()
                        .WithNone<ModelReference, URPMaterialPropertyBaseColor>()
                        .WithEntityAccess())
            {
                GameObject modelPrefab = SystemAPI.ManagedAPI.GetSingleton<ModelPrefabs>().Get(id.Value);
                GameObject newModel = Object.Instantiate(modelPrefab, transform.Position, transform.Rotation);
                
                float4 teamColor = team.Value switch
                {
                    TeamType.Blue => new(0, 0, 1, 1),
                    TeamType.Red => new(1, 0, 0, 1),
                    _ => new(1, 1, 1, 1)
                };
                Color unityColor = new(teamColor.x, teamColor.y, teamColor.z, teamColor.w);

                if (newModel.TryGetComponent<Renderer>(out var renderer)) 
                    renderer.material.color = unityColor; 
                
                ecb.AddComponent<LastOutlinedEntity>(entity);
                ecb.AddComponent(entity, new ModelReference { Value = newModel });
            }

            foreach ((LocalTransform targetTransform, ModelReference reference) in SystemAPI
                    .Query<LocalTransform, ModelReference>())
            {
                reference.Value.transform.SetPositionAndRotation(targetTransform.Position, targetTransform.Rotation);
            }

            foreach ((ModelReference reference, Entity entity) in SystemAPI
                        .Query<ModelReference>()
                        .WithNone<LocalTransform>()
                        .WithEntityAccess())
            {
                Object.Destroy(reference.Value);
                ecb.RemoveComponent<ModelReference>(entity);
            }
        }
    }
}
