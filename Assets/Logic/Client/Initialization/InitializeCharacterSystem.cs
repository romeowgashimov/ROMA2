using ROMA2.Logic.Client.UI;
using ROMA2.Logic.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;

namespace ROMA2.Logic.Client.Initialization
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial struct InitializeCharacterSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new(Allocator.Temp);

            foreach((RefRW<PhysicsMass> mass, Team team, Entity newCharacterEntity) in SystemAPI
            .Query<RefRW<PhysicsMass>, Team>()
            .WithAny<NewChampTag, NewMinionTag>()
            .WithNone<URPMaterialPropertyBaseColor>()
            .WithEntityAccess())
            {
                mass.ValueRW.InverseInertia[0] = 0;
                mass.ValueRW.InverseInertia[1] = 0;
                mass.ValueRW.InverseInertia[2] = 0;

                float4 teamColor = team.Value switch
                {
                    TeamType.Blue => new float4(0, 0, 1, 1),
                    TeamType.Red => new float4(1, 0, 0, 1),
                    _ => new float4(1)
                };
                
                ecb.AddComponent<LastOutlinedEntity>(newCharacterEntity);
                ecb.AddComponent(newCharacterEntity, new URPMaterialPropertyBaseColor { Value = teamColor });
                ecb.RemoveComponent<NewChampTag>(newCharacterEntity);
                ecb.RemoveComponent<NewMinionTag>(newCharacterEntity);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}