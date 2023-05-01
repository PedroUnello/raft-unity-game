using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Script.Scenario
{
    public class Map : MonoBehaviour
    {
        private static Map _instance;
        public static Map Instance => _instance;

        private GameObject[] _spawnPoints;

        public GameObject SpawnPoint
        {
            get
            {
                return AccessPoint(Random.Range(0, _spawnPoints.Length));
            }
        }

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
            }
            else
            {
                _instance = this;
            }
        }

        void Start()
        {
            List<GameObject> points = new();

            for (int i = 0; i < transform.childCount; i++)
            {
                points.Add(transform.GetChild(i).gameObject);
            }

            _spawnPoints = points.ToArray();
        }

        public GameObject AccessPoint(int id)
        {

            if (id < _spawnPoints.Length && _spawnPoints[id].activeInHierarchy)
            {
                return _spawnPoints[id];
            }

            return null;

        }
    }
}