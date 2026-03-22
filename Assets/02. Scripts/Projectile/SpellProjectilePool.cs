using UnityEngine;

// SpellProjectile 전용 오브젝트 풀.
// 씬에 하나만 배치한다. prefab을 지정하지 않으면 기본 구성으로 자동 생성한다.
//
// 사용 예:
//   var proj = SpellProjectilePool.Instance.Get();
//   proj.Init(...);
//   SpellProjectilePool.Instance.Return(proj);
public class SpellProjectilePool : Pool<SpellProjectile>
{
    public new static SpellProjectilePool Instance => (SpellProjectilePool)Pool<SpellProjectile>.Instance;

    protected override SpellProjectile CreateNew()
    {
        if (prefab != null)
            return Instantiate(prefab, transform);

        // prefab 없을 때 기본 구성으로 생성
        var go = new GameObject("SpellProjectile");
        go.transform.SetParent(transform);

        var rb2d = go.AddComponent<Rigidbody2D>();
        rb2d.gravityScale = 0f;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.2f;

        return go.AddComponent<SpellProjectile>();
    }
}
