using Assets.Script.Combat;
using Assets.Script.Utils;
using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

namespace Assets.Script.Gameplay
{
    [System.Serializable]
    public struct Course
    {
        public NormalizedVector3 direction;
        public float time;
        [Min(1)] public float speed; 
        [Min(0)] public float colSize;
    }

    [RequireComponent(typeof(Element))]
    public class Projectile : MonoBehaviour
    {

        public IObjectPool<Projectile> selfPool;
        [SerializeField] private float _damage;
        [SerializeField] private Course[] _trajectory;
        private GameObject origin;
        private Element _elem;
        private SphereCollider _collider;
        private int _projectileIndex;
        private float _timeCount;
        private bool _destroyed;

        void Awake()
        {
            _timeCount = 0;
            _destroyed = false;
            _elem = GetComponent<Element>();
            TryGetComponent(out _collider);
        }

        void Update()
        {

            if (_destroyed) return;

            if (_projectileIndex == _trajectory.Length) { StartCoroutine(nameof(DestroySelf)); return; }

            _timeCount += Time.deltaTime;

            Vector3 dir = _trajectory[_projectileIndex].direction.Value;
            Vector3 translatedDir = dir.x * transform.right + dir.y * transform.up + dir.z * transform.forward;

            //Change properties by time
            // - Position - & - Speed -
            transform.position = Vector3.MoveTowards(transform.position, transform.position + translatedDir, Time.deltaTime * _trajectory[_projectileIndex].speed);
            // - Collider size -
            if (_collider != null) { _collider.radius *= _trajectory[_projectileIndex].colSize; }            

            if (_timeCount > _trajectory[_projectileIndex].time)
            {
                 _projectileIndex++;
                 _timeCount = 0;
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject != origin && !other.transform.IsChildOf( origin.transform ))
            {
                if (other.CompareTag("Player"))
                {
                    //Do Damage
                    if (other.TryGetComponent(out IDamageable iDmg))
                    {
                        iDmg.Damage(_damage);
                    }
                    if (other.TryGetComponent(out IBuffable iBuff))
                    {
                        iBuff.Apply(_elem);
                    }
                }
                StartCoroutine(nameof(DestroySelf));
            }
        }

        public void Shoot(Vector3 shootFrom, Vector3 aimingDir, GameObject cameFrom)
        {
            transform.position = shootFrom;
            transform.LookAt( transform.position + aimingDir );

            origin = cameFrom;
            _projectileIndex = 0;
            
            _destroyed = false;
            _collider.enabled = true;
            gameObject.SetActive(true);
        }

        IEnumerator DestroySelf()
        {
            _destroyed = true;
            _collider.enabled = false;

            //VFX Effects.
            yield return Util.GetWaitForSeconds(0.65f);

            selfPool?.Release(this);

        }
    }
}
 