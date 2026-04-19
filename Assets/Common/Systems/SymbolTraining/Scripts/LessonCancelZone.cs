using UnityEngine;

namespace Common.Systems.SymbolTraining
{
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("Game/World/Symbols/Lesson Cancel Zone")]
    public class LessonCancelZone : MonoBehaviour
    {
        [SerializeField] private LessonSymbolConsumer lessonConsumer;

        private void Awake()
        {
            if (lessonConsumer == null)
                lessonConsumer = FindAnyObjectByType<LessonSymbolConsumer>(FindObjectsInactive.Include);

            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other != null && IsPlayer(other) && lessonConsumer != null && lessonConsumer.IsLessonActive)
            {
                lessonConsumer.CancelLesson();
            }
        }

        private static bool IsPlayer(Component component)
        {
            if (component == null)
                return false;

            if (component.CompareTag("Player"))
                return true;

            var root = component.GetComponentInParent<Transform>();
            return root != null && root.CompareTag("Player");
        }
    }
}


