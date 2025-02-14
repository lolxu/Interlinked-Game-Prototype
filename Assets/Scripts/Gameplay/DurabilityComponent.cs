using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DurabilityComponent : MonoBehaviour
{
    [Header("Durability Settings")]
    public int m_maxDurability = 10;
    public int m_currentDurability = 10;
    public UnityEvent UsedDurabilityEvent = new UnityEvent();
    [SerializeField] private float m_lifeTime = 20.0f;
    
    [Header("Visual Settings")]
    [SerializeField] private float m_shrinkSpeed = 1.25f;
    [SerializeField] private List<SpriteRenderer> m_spriteRends = new List<SpriteRenderer>();
    [SerializeField] private List<Color> m_playerConnectedTint = new List<Color>();
    [SerializeField] private List<Color> m_enemyConnectedTint = new List<Color>();

    private List<Color> m_orgColors = new List<Color>();

    private float m_timer = 0.0f;
    private bool m_isDespawning = false;
    private bool m_isConnected = false;
    private bool m_isConnectedByEnemy = false;
    private bool m_isConnectedByPlayer = false;
    private GameObject m_durabilityBar = null;
    private Coroutine m_despawnSequence = null;

    private void Start()
    {
        // Create durability
        m_durabilityBar = SingletonMaster.Instance.UI.AddDurabilityBar(this);

        foreach (var sp in m_spriteRends)
        {
            m_orgColors.Add(sp.color);
        }
        
        SingletonMaster.Instance.EventManager.LinkEvent.AddListener(OnLinked);
        SingletonMaster.Instance.EventManager.UnlinkEvent.AddListener(OnUnlinked);
    }

    private void OnUnlinked(GameObject obj, GameObject instigator)
    {
        
        if (obj == gameObject && instigator.CompareTag("Player"))
        {
            m_isConnectedByPlayer = false;
            if (!m_isConnectedByEnemy && !m_isConnectedByPlayer)
            {
                m_isConnected = false;
                for (int i = 0; i < m_spriteRends.Count; i++)
                {
                    m_spriteRends[i].color = m_orgColors[i];
                }
            }
            else if (m_isConnectedByEnemy)
            {
                for (int i = 0; i < m_spriteRends.Count; i++)
                {
                    m_spriteRends[i].color = m_enemyConnectedTint[i];
                }
            }
        }
        
        if (obj == gameObject && instigator.CompareTag("Enemy"))
        {
            m_isConnectedByEnemy = false;
            if (!m_isConnectedByEnemy && !m_isConnectedByPlayer)
            {
                m_isConnected = false;
                for (int i = 0; i < m_spriteRends.Count; i++)
                {
                    m_spriteRends[i].color = m_orgColors[i];
                }
            }
            else if (m_isConnectedByPlayer)
            {
                for (int i = 0; i < m_spriteRends.Count; i++)
                {
                    m_spriteRends[i].color = m_playerConnectedTint[i];
                }
            }
        }
    }

    private void OnLinked(GameObject obj, GameObject instigator)
    {
        if (obj == gameObject && instigator.CompareTag("Player"))
        {
            m_isConnected = true;
            m_isConnectedByPlayer = true;
            for (int i = 0; i < m_spriteRends.Count; i++)
            {
                m_spriteRends[i].color = m_playerConnectedTint[i];
            }
        }
        
        if (obj == gameObject && instigator.CompareTag("Enemy"))
        {
            m_isConnected = true;
            m_isConnectedByEnemy = true;
            for (int i = 0; i < m_spriteRends.Count; i++)
            {
                m_spriteRends[i].color = m_enemyConnectedTint[i];
            }
        }
    }

    private void OnDisable()
    {
        if (m_durabilityBar != null)
        {
            Destroy(m_durabilityBar);
        }
    }

    public void UseDurability()
    {
        m_currentDurability--;
        UsedDurabilityEvent.Invoke();
    }

    private void Update()
    {
        if (m_despawnSequence == null)
        {
            if (!m_isConnected)
            {
                m_timer += Time.deltaTime;
                if (m_timer >= m_lifeTime)
                {
                    m_despawnSequence = StartCoroutine(DespawnSequence());
                }
            }
            else
            {
                m_timer = 0.0f;
            }


            if (m_currentDurability == 0)
            {
                RopeComponent rc = GetComponent<RopeComponent>();
                if (rc != null && !m_isDespawning)
                {
                    m_isDespawning = true;
                    rc.DetachRope(SingletonMaster.Instance.PlayerBase.gameObject);
                    m_despawnSequence = StartCoroutine(DespawnSequence());
                }
            }
        }
    }

    private IEnumerator DespawnSequence()
    {
        gameObject.layer = 0;
        while (transform.localScale.x >= 0.0f)
        {
            transform.localScale -= Vector3.one * m_shrinkSpeed * Time.fixedDeltaTime;
            yield return null;
        }
        transform.localScale = Vector3.zero;
        yield return null;
        Destroy(gameObject);
    }
}
