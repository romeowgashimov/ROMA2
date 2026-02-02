using Unity.Entities;
using UnityEngine;

namespace Assets.Logic.Common
{
    public class MobaPrefabsAuthoring : MonoBehaviour
    {
        public GameObject Champion;

        public class MobaPrefabsBaker : Baker<MobaPrefabsAuthoring>
        {
            public override void Bake(MobaPrefabsAuthoring authoring)
            {
                Entity prefabContainerEntity = GetEntity(TransformUsageFlags.None);
                AddComponent(prefabContainerEntity, new MobaPrefabs
                {
                    Champion = GetEntity(authoring.Champion, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}
