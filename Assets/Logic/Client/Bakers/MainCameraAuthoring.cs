using ROMA2.Logic.Client.Data;
using Unity.Entities;
using UnityEngine;

namespace ROMA2.Logic.Client.Bakers
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