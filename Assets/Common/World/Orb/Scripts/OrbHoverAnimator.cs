using System.Collections;
using UnityEngine;

namespace Common.World.Orb
{
    /// <summary>
    /// Raises an orb and keeps it hovering. Can be triggered automatically or by other scripts.
    /// </summary>
    public class OrbHoverAnimator : MonoBehaviour
    {
        [SerializeField] private Transform orb;
        [SerializeField] private float liftHeight = 1.25f;
        [SerializeField] private float liftDuration = 1.25f;
        [SerializeField] private AnimationCurve liftEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private float hoverAmplitude = 0.15f;
        [SerializeField] private float hoverFrequency = 0.65f;
        [SerializeField] private bool autoStart = true;

        private Vector3 initialLocalPosition;
        private Vector3 hoverBaseLocalPosition;
        private Coroutine animationRoutine;
        private float hoverTime;
        private Vector3 hoverAnchorLocalPosition;

        private void Awake()
        {
            if (orb == null)
                orb = transform;

            initialLocalPosition = orb.localPosition;
            hoverBaseLocalPosition = initialLocalPosition + Vector3.up * liftHeight;
            hoverAnchorLocalPosition = orb.localPosition + Vector3.up * liftHeight;
        }

        private void OnEnable()
        {
            if (autoStart)
                BeginHover(true, true);
        }

        private void OnDisable()
        {
            StopAnimationRoutine();
            hoverTime = 0f;
            if (orb != null)
                orb.localPosition = initialLocalPosition;
        }

        /// <summary>
        /// Starts the lift and hover sequence.
        /// </summary>
        /// <param name="restart">When true, restarts the animation even if it is already running.</param>
        public void BeginHover(bool restart = false, bool recaptureInitialPosition = false)
        {
            if (orb == null)
                return;

            if (!restart && animationRoutine != null)
                return;

            if (recaptureInitialPosition)
                initialLocalPosition = orb.localPosition;

            hoverBaseLocalPosition = initialLocalPosition + Vector3.up * liftHeight;
            hoverAnchorLocalPosition = orb.localPosition + Vector3.up * liftHeight;

            StopAnimationRoutine();

            hoverTime = 0f;
            orb.localPosition = initialLocalPosition;
            animationRoutine = StartCoroutine(LiftAndHoverRoutine());
        }

        private IEnumerator LiftAndHoverRoutine()
        {
            float duration = Mathf.Max(0.01f, liftDuration);
            var start = initialLocalPosition;
            var target = hoverBaseLocalPosition;

            orb.localPosition = start;
            yield return null;

            float normalized = 0f;
            while (normalized < 1f)
            {
                normalized = Mathf.Min(1f, normalized + Time.deltaTime / duration);
                float eased = liftEase != null ? liftEase.Evaluate(normalized) : normalized;
                if (normalized >= 1f)
                    eased = 1f;
                orb.localPosition = Vector3.LerpUnclamped(start, target, eased);
                yield return null;
            }

            orb.localPosition = target;
            hoverBaseLocalPosition = target;
            hoverAnchorLocalPosition = target;
            hoverTime = 0f;

            while (true)
            {
                hoverTime += Time.deltaTime;
                float offset = Mathf.Sin(hoverTime * Mathf.PI * 2f * hoverFrequency) * hoverAmplitude;
                orb.localPosition = hoverAnchorLocalPosition + Vector3.up * offset;
                yield return null;
            }
        }

        private void StopAnimationRoutine()
        {
            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
                animationRoutine = null;
            }
        }
    }
}
