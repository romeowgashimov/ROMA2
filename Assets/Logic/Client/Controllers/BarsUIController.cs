using ROMA2.Logic.Client.Models;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using static Unity.Entities.World;

namespace ROMA2.Logic.Client.Controllers
{
    public class BarsUIController : MonoBehaviour
    {
        [SerializeField] public Slider HealthBarSlider;
        [SerializeField] public Slider ManaBarSlider;

        [SerializeField] private float LerpDuration = 1.0f; // Ровно 1 секунда

        // Здоровье
        private float _startHealth;
        private float _targetHealth;
        private float _healthTimer;
        private bool _reachedTargetHealth = true;

        // Мана
        private float _startMana;
        private float _targetMana;
        private float _manaTimer;
        private bool _reachedTargetMana = true;

        private void OnEnable()
        {
            if (DefaultGameObjectInjectionWorld == null) return;

            ChangeCharsEventSystem eventSystem = DefaultGameObjectInjectionWorld
                .GetExistingSystemManaged<ChangeCharsEventSystem>();
            if (eventSystem == null) return;
            
            eventSystem.OnHealthChanged += StoreHealthTarget;
            eventSystem.OnManaChanged += StoreManaTarget;

            if (HealthBarSlider != null) _targetHealth = HealthBarSlider.value;
            if (ManaBarSlider != null) _targetMana = ManaBarSlider.value;
        }

        private void OnDisable()
        {
            if (DefaultGameObjectInjectionWorld == null) return;

            ChangeCharsEventSystem eventSystem = DefaultGameObjectInjectionWorld
                .GetExistingSystemManaged<ChangeCharsEventSystem>();
            if (eventSystem == null) return;

            eventSystem.OnHealthChanged -= StoreHealthTarget;
            eventSystem.OnManaChanged -= StoreManaTarget;
        }

        private void LateUpdate()
        {
            if (HealthBarSlider != null && !_reachedTargetHealth)
            {
                _healthTimer += Time.deltaTime;
                // Линейный прогресс от 0.0f до 1.0f
                float linearProgress = math.clamp(_healthTimer / LerpDuration, 0f, 1f);
        
                // Сглаживание: делает старт и финиш анимации мягкими
                float smoothedProgress = math.smoothstep(0f, 1f, linearProgress);
        
                // Интерполяция с использованием сглаженного коэффициента
                HealthBarSlider.value = math.lerp(_startHealth, _targetHealth, smoothedProgress);

                if (linearProgress >= 1.0f) _reachedTargetHealth = true;
            }

            if (ManaBarSlider != null && !_reachedTargetMana)
            {
                _manaTimer += Time.deltaTime;
                float linearProgress = math.clamp(_manaTimer / LerpDuration, 0f, 1f);
        
                float smoothedProgress = math.smoothstep(0f, 1f, linearProgress);

                ManaBarSlider.value = math.lerp(_startMana, _targetMana, smoothedProgress);

                if (linearProgress >= 1.0f) _reachedTargetMana = true;
            }
        }


        private void StoreHealthTarget(float curPoints, int maxPoints)
        {
            if (HealthBarSlider == null) return;
            HealthBarSlider.maxValue = maxPoints;
            
            // Запоминаем текущее состояние ползунка как точку старта анимации
            _startHealth = HealthBarSlider.value; 
            _targetHealth = curPoints;
            _healthTimer = 0f; // Сбрасываем таймер для новой анимации
            _reachedTargetHealth = false;
        }
        
        private void StoreManaTarget(float curPoints, int maxPoints)
        {
            if (ManaBarSlider == null) return;
            ManaBarSlider.maxValue = maxPoints;
            
            _startMana = ManaBarSlider.value;
            _targetMana = curPoints;
            _manaTimer = 0f;
            _reachedTargetMana = false;
        }
    }
}
