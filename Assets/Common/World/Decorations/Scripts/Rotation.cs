using UnityEngine;

namespace Common.World.Decorations
{
    public class Rotation : MonoBehaviour
    {
        [SerializeField] private float rotationSpeed = 45f;
        [SerializeField] private Transform rotationTarget;

        private void Update()
        {
            if (rotationTarget == null) return;

            rotationTarget.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
        }
    }
}
