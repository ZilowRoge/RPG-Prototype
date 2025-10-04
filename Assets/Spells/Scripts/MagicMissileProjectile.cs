using System;
using UnityEngine;
using Player.Statistics;

namespace Spells {
public class MagicMissileProjectile : MonoBehaviour
{
    private Transform target;
    private float damage = 0.0f;
    private float speed = 0.0f;

    public void Init(Transform target, float speed, float damage)
    {
        this.target = target;
        this.damage = damage;
        this.speed = speed;
    }

    private void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.position) < 0.2f)
        {
            target.GetComponent<StatsController>()?.ReceiveDamage(damage);
            Destroy(gameObject);
        }
    }
}
}