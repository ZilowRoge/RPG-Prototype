using UnityEngine;

namespace Player.Statistics {
[CreateAssetMenu(fileName = "PlayerStatistics", menuName = "Player/Statistics")]
public class StatisticsData  : ScriptableObject  {
    public float walkSpeed = 3.0f;
    public float runSpeed = 6.0f;
}
}