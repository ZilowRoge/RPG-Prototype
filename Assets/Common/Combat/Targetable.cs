using UnityEngine;
using Player.Interfaces;

namespace Common.Combat
{
    public class Targetable : MonoBehaviour, ITargetable
    {
        [SerializeField] private Transform targetRoot;
        public Transform TargetTransform => targetRoot != null ? targetRoot : transform;
    }
}

