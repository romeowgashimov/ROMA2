using ROMA2.Logic.Client.Data;
using Unity.Entities;
using UnityEngine;

namespace ROMA2.Logic.Client.Initialization
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