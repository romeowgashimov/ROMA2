using Unity.Entities;
using UnityEngine;

namespace Logic.Common
{
    public class MobaPrefabsAuthoring : MonoBehaviour
    {
        [Header("Entities")]
        public GameObject Champion;
        public GameObject Minion;
        
        [Header("GameObjects")] 
        public GameObject HealthBarPrefab;
        public GameObject SkillShotAimPrefab;
        
        public class MobaPrefabsBaker : Baker<MobaPrefabsAuthoring>
        {
            public override void Bake(MobaPrefabsAuthoring authoring)
            {
                Entity prefabContainerEntity = GetEntity(TransformUsageFlags.None);
                AddComponent(prefabContainerEntity, new MobaPrefabs
                {
                    Champion = GetEntity(authoring.Champion, TransformUsageFlags.Dynamic),
                    Minion = GetEntity(authoring.Minion, TransformUsageFlags.Dynamic),
                });
                AddComponentObject(prefabContainerEntity, new UIPrefabs
                {
                    HealthBar = authoring.HealthBarPrefab,
                    SkillShot =  authoring.SkillShotAimPrefab,
                });
            }
        }
    }
}
