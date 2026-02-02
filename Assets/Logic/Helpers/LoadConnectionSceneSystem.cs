#if UNITY_EDITOR

using Unity.Entities;
using static UnityEngine.SceneManagement.SceneManager;

namespace Assets.Logic.Helpers
{
    public partial class LoadConnectionSceneSystem : SystemBase
    {
        protected override void OnCreate()
        {
            Enabled = false;

            if (GetActiveScene() == GetSceneByBuildIndex(0)) 
                return;

            LoadScene(0);
        }

        protected override void OnUpdate()
        {
            
        }
    }
}

#endif