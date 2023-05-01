using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Script.Gameplay;
using Assets.Script.Utils;
using UnityEngine.Pool;
using Assets.Script.Combat;

namespace Assets.Script.Collectables
{
    public class ElementalPoint : CollectablePoint
    {
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

            Projectile proj = Instantiate(_proj, GameObject.Find("Projectile Pool").transform);
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

        public override int Access<T>(ref T destiny)
        {

            System.Type got = typeof(Neutral);

            switch (destiny)
            {
                case Magazine mag:
                    
                    List<Projectile> projList = new();
                    for (int i = 0; i < 8; i++) { projList.Add(_projectilePool.Get()); }
                    mag.Charge(projList.ToArray());
                    
                    got = _proj.GetComponent<Element>().GetType();
                    
                    break;

                case Special spcl:

                    got = _specialOne.GetComponent<Element>().GetType();

                    break;
                
                case Melee melee:
                    
                    melee.Exchange(_melee);

                    got = _melee.GetComponent<Element>().GetType();

                    break;
            }
            
            StartCoroutine(nameof(ResetPoint));

            return Elemental.ElementIndexer.GetValueOrDefault(got);
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