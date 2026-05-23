using Unity.Entities;
using UnityEngine;

namespace Assets.Logic.Client
{
    public class MainCameraAuthoring : MonoBehaviour
    {
        public class MainCameraBaker : Baker<MainCameraAuthoring>
        {
            public override void Bake(MainCameraAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponentObject(entity, new MainCamera());
                AddComponent<MainCameraTag>(entity);                
            }
        }
    }
}