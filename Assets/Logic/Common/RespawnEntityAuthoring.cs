using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Logic.Common
{
    public class RespawnEntityAuthoring : MonoBehaviour
    {
        public float RespawnTime = 2f;
        public NetCodeConfig NetCodeConfig;

        public int SimulationTickRate => NetCodeConfig.ClientServerTickRate.SimulationTickRate;
        
        private class RespawnEntityBaker : Baker<RespawnEntityAuthoring>
        {
            public override void Bake(RespawnEntityAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent<RespawnEntityTag>(entity);
                AddComponent(entity, new RespawnTickCount
                {
                    Value = (uint)(authoring.RespawnTime * authoring.SimulationTickRate)
                });
                AddBuffer<RespawnBufferElement>(entity);
            }
        }
    }
}