using UnityEngine;

namespace Common.World.Decorations
{
    public class LightPulse : MonoBehaviour
    {
        [SerializeField] private Light targetLight;
        [SerializeField] private float minIntensity = 0.6f;
        [SerializeField] private float maxIntensity = 1.4f;
        [SerializeField] private float pulsesPerSecond = 1f;
        [SerializeField] private float phaseOffset;

        private void Reset()
        {
            TryGetComponent(out targetLight);
        }

        private void Awake()
        {
            if (targetLight == null)
            {
                TryGetComponent(out targetLight);
            }
        }

        private void Update()
        {
            if (targetLight == null)
            {
                return;
            }

            float phase = (Time.time * pulsesPerSecond + phaseOffset) * Mathf.PI * 2f;
            float pulse = (Mathf.Sin(phase) + 1f) * 0.5f;
            targetLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, pulse);
        }

        private void OnValidate()
        {
            minIntensity = Mathf.Max(0f, minIntensity);
            maxIntensity = Mathf.Max(minIntensity, maxIntensity);
            pulsesPerSecond = Mathf.Max(0f, pulsesPerSecond);
        }
    }
}
