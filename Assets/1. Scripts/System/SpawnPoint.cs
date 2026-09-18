using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 이건 아마 권오석 작품일텐데? 맞을꺼임
// 그래서 솔직히 나도 잘 모르니까
// 문제 생기면 바로 권오석한테 문의 부탁.
public class SpawnPoint : MonoBehaviour
{
    [SerializeField] private GameObject[] npc;
    [SerializeField] private Transform[] target;
    [SerializeField] private Collider planeCollider;

    public Transform[] GetTarget { get { return target; } }

    private PoolingManager pool;
    private Bounds planeBounds;

    private float spawnTime = 0;
    private float spawnTimer = 3f;

    private void Awake()
    {
        pool = PoolingManager.Instance;
        planeBounds = planeCollider.bounds;
    }
    // Update is called once per frame
    void Update()
    {
        SpawnNpc();
    }
    private void SpawnNpc()
    {
        spawnTime += Time.deltaTime;

        if(spawnTime >= spawnTimer)
        {
            int npcRandom = Random.Range(0, npc.Length);
            Npc newNpc = pool.GetObj(npc[npcRandom]).GetComponent<Npc>();
            newNpc.transform.position = transform.position;
            spawnTime = 0;
        }
    }
    private Vector3 GetRandomPositionInBounds(Bounds bounds)
    {
        float randomX = Random.Range(bounds.min.x, bounds.max.x);
        float randomY = Random.Range(bounds.min.y, bounds.max.y);
        float randomZ = Random.Range(bounds.min.z, bounds.max.z);

        return new Vector3(randomX, randomY, randomZ);
    }
    public Vector3 GetRandomPositionInPlaneBounds()
    {
        float randomX = Random.Range(planeBounds.min.x, planeBounds.max.x);
        float randomZ = Random.Range(planeBounds.min.z, planeBounds.max.z);

        return new Vector3(randomX, planeBounds.size.y, randomZ);
    }
}
