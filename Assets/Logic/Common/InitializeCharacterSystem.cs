using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;

namespace Logic.Common
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

                ecb.SetComponent(newCharacterEntity, new URPMaterialPropertyBaseColor { Value = teamColor });
                ecb.RemoveComponent<NewChampTag>(newCharacterEntity);
                ecb.RemoveComponent<NewMinionTag>(newCharacterEntity);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}