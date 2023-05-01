using UnityEngine;
using UnityEngine.Pool;

namespace Assets.Script.Gameplay
{
    public class Magazine : MonoBehaviour
    {
        //Maybe exchange to static ref ?
        [SerializeField] private GameObject origin;
        [SerializeField] private Projectile _baseProj;
        [SerializeField] private int _baseMagazineSize;

        private ObjectPool<Projectile> _projectilePool;
        private Projectile[] _projectiles = new Projectile[0];
        private int _currentProjectile = 0;

        Projectile CreateFuncProjectile()
        {
            Projectile proj = Instantiate(_baseProj, GameObject.Find("Projectile Pool").transform);
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
            _projectilePool = new(CreateFuncProjectile, null, ActionOnReleaseProjectile, ActionOnDestroyProjectile, maxSize:(int)(_baseMagazineSize * 1.5f));
        }

        public void Shoot(Vector3 dir)
        {
            if (_currentProjectile < _projectiles.Length)
            {
                _projectiles[_currentProjectile++].Shoot(transform.position + dir, dir, origin);
            } else { Recharge(); }
        }

        public void Recharge()
        {
            _projectiles = new Projectile[_baseMagazineSize];
            for (int i = 0; i < _baseMagazineSize; i++)
            {
                _projectiles[i] = _projectilePool.Get();
            }
            _currentProjectile = 0;
        }

        public void Charge(Projectile[] projArray)
        {
            _projectiles = projArray;
            _currentProjectile = 0;
        }
    }
}
