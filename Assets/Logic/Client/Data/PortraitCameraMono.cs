using UnityEngine;
using Unity.Entities;

namespace ROMA2.Logic.Client.Data
{
    [RequireComponent(typeof(Camera))]
    public class PortraitCameraMono : MonoBehaviour
    {
        [SerializeField] private Camera _camera;

        private void Reset()
        {
            if (_camera == null) _camera = GetComponent<Camera>();
        }

        private void Awake()
        {
            if (_camera == null) _camera = GetComponent<Camera>();

            // Получаем доступ к текущему запущенному ECS-миру
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;

            EntityManager entityManager = world.EntityManager;

            // Создаем пустую сущность в ECS-мире для этой камеры
            Entity cameraEntity = entityManager.CreateEntity();

            // Добавляем на неё ваш тег-компонент
            entityManager.AddComponentData(cameraEntity, new PortraitCameraTag());

            // Добавляем управляемый компонент (Managed Component) со ссылкой на саму камеру
            // Важно: PortraitCameraComponent должен быть class, как в Шаге 2 ниже
            entityManager.AddComponentObject(cameraEntity, new PortraitCamera
            { 
                Value = _camera 
            });
        }
    }
}