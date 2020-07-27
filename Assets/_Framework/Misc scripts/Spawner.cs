using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EXPToolkit
{
    public class Spawner : MonoBehaviour
    {        
        public Vector3 m_Volume = Vector3.one;
        public Vector3 m_RotationRange = Vector3.zero;
        public Vector2 m_ScaleRange = Vector3.one;

        public GameObject[] m_ObjectsToSpawn;

        public int m_NumberToPool = 30;

        List<GameObject> m_ActiveObjects = new List<GameObject>();
        List<GameObject> m_InctiveObjects = new List<GameObject>();

        public bool _SpawnOnStart = false;

        void Start()
        {
            for (int i = 0; i < m_NumberToPool; i++)
            {
                // Instantiate object
                GameObject newGo = Instantiate(m_ObjectsToSpawn[Random.Range(0, m_ObjectsToSpawn.Length)], Vector3.one * 10000, Quaternion.identity) as GameObject;
                newGo.SetActive(false);

                // Add object to the list
                m_InctiveObjects.Add(newGo);
            }

            if(_SpawnOnStart)
            {
                for (int i = 0; i < m_NumberToPool; i++)
                {
                    Spawn();
                }
            }
        }

        void Update()
        {
            //// Remove any inactive items
            //m_ActiveObjects.RemoveAll(item => item == null);

            //List<GameObject> inactives = new List<GameObject>();
            //// Find inactive objects in the active list
            //for (int i = 0; i < m_ActiveObjects.Count; i++)
            //{
            //    if (!m_ActiveObjects[i].activeSelf)
            //        inactives.Add(m_ActiveObjects[i]);
            //}

            //// Remove any inactive items
            //m_ActiveObjects.RemoveAll(item => !item.activeSelf);

            //// Add to inactive list
            //for (int i = 0; i < inactives.Count; i++)
            //{
            //    m_InctiveObjects.Add(inactives[i]);
            //}

            //if (Input.GetKeyDown(KeyCode.A))
            //    Spawn();
        }

        void Spawn()
        {
            if (m_InctiveObjects.Count == 0)
            {
                print("No inactive objects left to spawn, consider increasing your pooling amount.");
                return;
            }

            Vector3 randVec = new Vector3(Random.Range(-m_Volume.x / 2f, m_Volume.x / 2f), Random.Range(-m_Volume.y / 2f, m_Volume.y / 2f), Random.Range(-m_Volume.z / 2f, m_Volume.z / 2f));
            Vector3 pos = transform.position + randVec;

            Vector3 rot = new Vector3(Random.Range(-m_RotationRange.x / 2f, m_RotationRange.x / 2f), Random.Range(-m_RotationRange.y / 2f, m_RotationRange.y / 2f), Random.Range(-m_RotationRange.z / 2f, m_RotationRange.z / 2f));
            float scale = Random.Range(m_ScaleRange.x, m_ScaleRange.y);

            // Get Go from incative list
            GameObject newGo = m_InctiveObjects[0];
            newGo.transform.position = pos;
            newGo.transform.rotation = Quaternion.Euler(rot);
            newGo.transform.localScale = scale * Vector3.one;

            m_InctiveObjects.Remove(newGo);

            newGo.SetActive(true);

            // Add Go to active object list
            m_ActiveObjects.Add(newGo);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0, 1, 1, 1F);
            Gizmos.DrawWireCube(transform.position, m_Volume);
        }
    }
}
