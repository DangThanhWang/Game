using UnityEngine;
using System.Collections;

public class MeteorSpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] meteorPrefabs;
    [SerializeField] private int meteorsCount = 20;
    [SerializeField] private float spawnDelay = 1f;

    private GameObject[] meteors;

    private void Start()
    {
        PrepareMeteors();
        StartCoroutine(SpawnMeteors());
    }

    private void PrepareMeteors()
    {
        meteors = new GameObject[meteorsCount];
        int prefabsCount = meteorPrefabs.Length;

        for (int i = 0; i < meteorsCount; i++)
        {
            meteors[i] = Instantiate(meteorPrefabs[Random.Range(0, prefabsCount)], transform.position, Quaternion.identity);
            meteors[i].SetActive(false);
        }
    }

    private IEnumerator SpawnMeteors()
    {
        for (int i = 0; i < meteorsCount; i++)
        {
            meteors[i].SetActive(true);
            yield return new WaitForSeconds(spawnDelay);
        }
    }
}
