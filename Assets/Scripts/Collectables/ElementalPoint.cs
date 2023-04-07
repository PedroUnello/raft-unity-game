using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Script.Gameplay;
using Assets.Script.Utils;
using UnityEngine.Pool;

namespace Assets.Script.Collectables
{
    public class ElementalPoint : CollectablePoint
    {
        /*
        [System.Serializable]
        private struct Container
        {
            public Projectile _proj;
            //public Special _specialOne;
            //public Special _specialTwo;
            //public Ultimate _ultimate;
            //public Melee _melee;

            ObjectPool<GameObject> objectPool;
        }


        [System.Serializable]
        private class Container
        {
            public Projectile _proj;
            //public Special _specialOne;
            //public Special _specialTwo;
            //public Ultimate _ultimate;
            //public Melee _melee;

            ObjectPool<Projectile> _projectilePool = new(OnCreateProjectile, OnGetProjectile, OnReleaseProjectile, OnDestroyProjectile);

            static Projectile OnCreateProjectile()
            {
                return Instantiate(_proj, GameObject.Find(_proj.gameObject.name + " Pool").transform);
            }

            Projectile OnGetProjectile()
            {

            }

            Projectile OnReleaseProjectile()
            {

            }

            Projectile OnDestroyProjectile()
            {

            }

        }

        [SerializeField] private Element[] _possibleElements;
        private Dictionary<Element, Container> _elementalPoints;
        [SerializeField] private Container _accessableElement;

        IEnumerator ResetPoint()
        {
            gameObject.SetActive(false);
            yield return Util.GetWaitForSeconds(_cooldown);
            _elementalPoints.TryGetValue(_possibleElements[Random.Range(0, _possibleElements.Length)], out Container newElement);
            _accessableElement = newElement;
            gameObject.SetActive(true);
        }

        */

        private int _pointId;
        public int PointID => _pointId;

        [SerializeField] private Projectile _proj;
        [SerializeField] private Special _specialOne;
        [SerializeField] private Special _specialTwo;
        [SerializeField] private Attack _melee;

        ObjectPool<Projectile> _projectilePool;

        Projectile CreateFuncProjectile()
        {
            //Maybe instance element without mono (here) ? 

            Projectile proj = Instantiate(_proj, GameObject.Find(_proj.gameObject.name + " Pool").transform);
            proj.selfPool = _projectilePool;
            proj.gameObject.SetActive(false);
            return proj;
        }

        void ActionOnReleaseProjectile(Projectile proj)
        {
            proj.gameObject.SetActive(false);
        }

        void ActionOnDestroyProjectile(Projectile proj)
        {
            Destroy(proj.gameObject);
        }

        void Awake()
        {
            _projectilePool = new(CreateFuncProjectile, null, ActionOnReleaseProjectile, ActionOnDestroyProjectile);
        }

        void Start()
        {
            for (int i = 0; i < transform.parent.childCount; i++)
            {
                if (transform.parent.GetChild(i) == transform)
                {
                    _pointId = i;
                    break;
                }
            }
            
        }

        public override void Access<T>(ref T destiny)
        {
            switch (destiny)
            {
                case Magazine mag:
                    List<Projectile> projList = new();
                    for (int i = 0; i < 8; i++) { projList.Add(_projectilePool.Get()); }
                    mag.Charge(projList.ToArray());
                    break;
                case Special spcl:
                    break;
                case Melee melee:
                    melee.Exchange(_melee);
                    break;
            }
            
            StartCoroutine(nameof(ResetPoint));
        }

        protected override IEnumerator ResetPoint()
        {
            Vector3 _pos = transform.position;
            transform.position = new Vector3(-9999, -9999, -9999);
            yield return Util.GetWaitForSeconds(_cooldown);
            transform.position = _pos;
        }
    }
}