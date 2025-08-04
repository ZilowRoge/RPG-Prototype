using UnityEngine;

namespace Player.Statistics
{
    public class Statistics : MonoBehaviour
    {
        [SerializeField] private StatisticsData data;

        public float WalkSpeed => data != null ? data.walkSpeed : 0f;
        public float RunSpeed => data != null ? data.runSpeed : 0f;

        // Możesz dodać więcej getterów jeśli StatisticsData będzie mieć więcej pól.
    }
}
