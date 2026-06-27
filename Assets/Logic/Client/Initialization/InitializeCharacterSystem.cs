using ROMA2.Logic.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;

namespace ROMA2.Logic.Client.Initialization
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial struct InitializeCharacterSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new(Allocator.Temp);

            foreach((RefRW<PhysicsMass> mass, Entity newCharacterEntity) in SystemAPI
                    .Query<RefRW<PhysicsMass>>()
                    .WithAny<NewChampTag, NewMinionTag>()
                    .WithEntityAccess())
            {
                mass.ValueRW.InverseInertia[0] = 0;
                mass.ValueRW.InverseInertia[1] = 0;
                mass.ValueRW.InverseInertia[2] = 0;

                ecb.RemoveComponent<NewChampTag>(newCharacterEntity);
                ecb.RemoveComponent<NewMinionTag>(newCharacterEntity);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}