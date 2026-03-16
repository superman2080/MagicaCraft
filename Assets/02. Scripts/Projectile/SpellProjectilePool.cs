using System.Collections.Generic;
using UnityEngine;

// SpellProjectile 전용 오브젝트 풀
// 씬에 하나만 배치한다. prefab을 지정하지 않으면 빈 프리팹을 자동 생성한다.
//
// 사용 예:
//   var proj = SpellProjectilePool.Instance.Get();
//   proj.Init(...);
//   ...
//   SpellProjectilePool.Instance.Return(proj);
public class SpellProjectilePool : MonoBehaviour
{
    [SerializeField] private SpellProjectile prefab;
    [SerializeField] private int             initialSize = 10;

    private static SpellProjectilePool _instance;
    public  static SpellProjectilePool Instance => _instance;

    private readonly Queue<SpellProjectile> _pool = new();

    private void Awake()
    {
        _instance = this;
        for (int i = 0; i < initialSize; i++)
            _pool.Enqueue(CreateNew());
    }

    public SpellProjectile Get()
    {
        var proj = _pool.Count > 0 ? _pool.Dequeue() : CreateNew();
        proj.gameObject.SetActive(true);
        return proj;
    }

    public void Return(SpellProjectile proj)
    {
        proj.OnDespawn();       // SetActive(false) 포함
        _pool.Enqueue(proj);
    }

    // ─── 내부 헬퍼 ────────────────────────────────────────────

    private SpellProjectile CreateNew()
    {
        SpellProjectile proj;

        if (prefab != null)
        {
            proj = Instantiate(prefab, transform);
        }
        else
        {
            // prefab 없을 때 기본 구성으로 생성
            var go = new GameObject("SpellProjectile");
            go.transform.SetParent(transform);

            var rb2d = go.AddComponent<Rigidbody2D>();
            rb2d.gravityScale = 0f;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius    = 0.2f;

            proj = go.AddComponent<SpellProjectile>();
        }

        proj.gameObject.SetActive(false);
        return proj;
    }
}
