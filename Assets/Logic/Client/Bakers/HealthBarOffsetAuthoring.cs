using ROMA2.Logic.Client.Data;
using Unity.Entities;
using UnityEngine;

namespace ROMA2.Logic.Client.Bakers
{
    public class HealthBarOffsetAuthoring : MonoBehaviour
    {
        public Vector3 Offset = new(0, 2, 0);
        private class RenderBaker : Baker<HealthBarOffsetAuthoring>
        {
            public override void Bake(HealthBarOffsetAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new HealthBarOffset { Value =  authoring.Offset });
            }
        }
    }
}