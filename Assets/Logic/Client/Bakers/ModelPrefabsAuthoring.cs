using ROMA2.Logic.Client.Data;
using Unity.Entities;
using UnityEngine;

namespace ROMA2.Logic.Client.Bakers
{
    public class ModelPrefabsAuthoring : MonoBehaviour
    {
        public GameObject MinionPrefab;
        public GameObject Champion1Prefab;
        public GameObject Champion2Prefab;

        private class ModelPrefabsBaker : Baker<ModelPrefabsAuthoring>
        {
            public override void Bake(ModelPrefabsAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponentObject(entity, new ModelPrefabs
                {
                    Minion = authoring.MinionPrefab,
                    Champion1 = authoring.Champion1Prefab,
                    Champion2 = authoring.Champion2Prefab
                });
            }
        }
    }
}