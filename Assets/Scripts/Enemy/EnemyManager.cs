using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class EnemyManager : MonoBehaviour
{
    [Header("Spawning Settings")] 
    public int m_waveCount = 0;
    public List<EnemySpawnScriptable> m_waves = new List<EnemySpawnScriptable>();
    public float m_waveCoolDown = 10.0f;
    public float m_spawnPadding = 1.0f;
    public bool m_canSpawn = false;

    private EnemySpawnScriptable m_currentWave;
    private float m_waveTime = 0.0f;
    private int m_maxEnemyCount = 20;
    public List<GameObject> m_enemies = new List<GameObject>();

    private void Start()
    {
        if (m_waves.Count > 0)
        {
            m_currentWave = m_waves[0];
            m_maxEnemyCount = m_currentWave.m_maxEnemyCount;
            m_waveTime = m_currentWave.m_waveTime;
        }
        else
        {
            m_canSpawn = false;
            Debug.LogError("No Waves Setup!!");
        }

        SingletonMaster.Instance.EventManager.EnemyDeathEvent.AddListener(RemoveEnemy);
        SingletonMaster.Instance.EventManager.EnemyDeathEvent.AddListener(SpawnLoot);
    }

    private void OnDisable()
    {
        SingletonMaster.Instance.EventManager.EnemyDeathEvent.RemoveListener(RemoveEnemy);
        SingletonMaster.Instance.EventManager.EnemyDeathEvent.RemoveListener(SpawnLoot);
    }

    private IEnumerator StartCooldown()
    {
        m_canSpawn = false;
        SingletonMaster.Instance.EventManager.CooldownStarted.Invoke(m_waveCoolDown);
        yield return new WaitForSeconds(m_waveCoolDown);
        
        if (m_waveCount < m_waves.Count)
        {
            m_waveCount++;
            m_currentWave = m_waves[m_waveCount];
            m_maxEnemyCount = m_currentWave.m_maxEnemyCount;
            m_waveTime = m_currentWave.m_waveTime;
            
            m_canSpawn = true;
        }
        else
        {
            SingletonMaster.Instance.EventManager.WinEvent.Invoke();
        }
    }

    void Update()
    {
        if (m_canSpawn)
        {
            m_waveTime -= Time.deltaTime;
            
            int enemyCount = m_enemies.Count;
            if (enemyCount < m_maxEnemyCount)
            {
                SpawnEnemies(m_maxEnemyCount - enemyCount);
            }
            
            if (m_waveTime <= 0.0f)
            {
                ClearAllEnemies();
                StartCoroutine(StartCooldown());
            }
        }
    }

    private void ClearAllEnemies()
    {
        for (int i = m_enemies.Count - 1; i >= 0; --i)
        {
            Destroy(m_enemies[i]);
        }
        m_enemies.Clear();
    }

    private void SpawnEnemies(int num)
    {
        for (int i = 0; i < num; i++)
        {
            Vector2 spawnPos = Camera.main.transform.position;
            spawnPos += GetRandomSpawnPosition();
            EnemyScriptable.Enemy newEnemy = m_currentWave.GetRandomEnemyToSpawn();
            GameObject enemyPrefab = newEnemy.m_prefab;
            GameObject spawned = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            
            spawned.transform.GetChild(0).GetComponent<BaseEnemyBehavior>().m_lootDropRate = 
                newEnemy.m_lootSpawnRate;
            m_enemies.Add(spawned);
        }
    }

    private Vector2 GetRandomSpawnPosition()
    {
        float camHeight = Camera.main.orthographicSize;
        float camWidth = camHeight * Camera.main.aspect;
        
        int side = Random.Range(0, 4); // 0:Top, 1:Bottom, 2:Left, 3:Right
        float x = 0f, y = 0f;

        switch (side) {
            case 0: // Top edge
                x = Random.Range(-camWidth, camWidth);
                y = camHeight + m_spawnPadding;
                break;
            case 1: // Bottom edge
                x = Random.Range(-camWidth, camWidth);
                y = -camHeight - m_spawnPadding;
                break;
            case 2: // Left edge
                x = -camWidth - m_spawnPadding;
                y = Random.Range(-camHeight, camHeight);
                break;
            case 3: // Right edge
                x = camWidth + m_spawnPadding;
                y = Random.Range(-camHeight, camHeight);
                break;
        }

        return new Vector2(x, y);
    }

    private void SpawnLoot(GameObject enemy)
    {
        float roll = Random.Range(0.0f, 1.0f);
        if (roll <= enemy.GetComponent<BaseEnemyBehavior>().m_lootDropRate)
        {
            GameObject loot = SingletonMaster.Instance.LootSpawnScriptableObject.GetRandomLootToSpawn().m_prefab;
            Instantiate(loot, enemy.transform.position, Quaternion.identity);
        }
    }

    private void RemoveEnemy(GameObject enemy)
    {
        m_enemies.Remove(enemy.transform.parent.gameObject);
        
        // Checking for any connected stuff to this enemy
        RopeComponent rc = enemy.GetComponent<RopeComponent>();
        for (int i = rc.m_connectedTo.Count - 1; i >= 0; --i)
        {
            var connectedObj = rc.m_connectedTo[i];
            connectedObj.GetComponent<RopeComponent>().DetachEnemy(enemy);
        }
        
        Destroy(enemy.transform.parent.gameObject);
    }
}
