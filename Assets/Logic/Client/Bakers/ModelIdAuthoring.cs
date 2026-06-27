using ROMA2.Logic.Client.Data;
using Unity.Entities;
using UnityEngine;

namespace ROMA2.Logic.Client.Bakers
{
    public class ModelIdAuthoring : MonoBehaviour
    {
        public int Id;

        private class ModelIdBaker : Baker<ModelIdAuthoring>
        {
            public override void Bake(ModelIdAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new ModelId { Value = authoring.Id });
            }
        }
    }
}