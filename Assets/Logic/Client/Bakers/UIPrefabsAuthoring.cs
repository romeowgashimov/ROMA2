using ROMA2.Logic.Client.Data;
using Unity.Entities;
using UnityEngine;

namespace ROMA2.Logic.Client.Bakers
{
    public class UIPrefabsAuthoring : MonoBehaviour
    {
        [Header("GameObjects")] 
        public GameObject HealthBarPrefab;
        public GameObject SkillShotAimPrefab;
        public GameObject Visualizer;
        
        public class UIPrefabsBaker : Baker<UIPrefabsAuthoring>
        {
            public override void Bake(UIPrefabsAuthoring authoring)
            {
                Entity prefabContainerEntity = GetEntity(TransformUsageFlags.None);
                AddComponentObject(prefabContainerEntity, new UIPrefabs
                {
                    HealthBar = authoring.HealthBarPrefab,
                    SkillShot =  authoring.SkillShotAimPrefab,
                    Visualizer = authoring.Visualizer
                });
            }
        }
    }
}