using Player;
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
            if (other != null && other.GetComponentInParent<Interactor>() != null && lessonConsumer != null && lessonConsumer.IsLessonActive)
            {
                lessonConsumer.CancelLesson();
            }
        }
    }
}


