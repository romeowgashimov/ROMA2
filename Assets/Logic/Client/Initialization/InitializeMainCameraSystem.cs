using Unity.Entities;
using UnityEngine;

namespace Assets.Logic.Client
{
    public partial class InitializeMainCameraSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<MainCameraTag>();
        }

        protected override void OnUpdate()
        {
            Enabled = false;
            Entity entity = SystemAPI.GetSingletonEntity<MainCameraTag>();
            EntityManager.SetComponentData(entity, new MainCamera { Value = Camera.main });
        }
    }
}