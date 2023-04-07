using Assets.Script.Combat;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Script.Gameplay
{
    [RequireComponent(typeof(Element))]
    public class Attack : MonoBehaviour
    {
        private Element _elem;
        [SerializeField] private float _hitSize;
        [SerializeField] private float _damage;
        [SerializeField] private LayerMask canHit;


        void Awake()
        {
            _elem = GetComponent<Element>();
        }

        void Start()
        {
            //Run animation


            //StartupEffect
            //Invoke Hit()
            Invoke(nameof(Hit), 0.2f); //Maybe change to anim event
            //Invoke End()
            Invoke(nameof(End), 0.35f);
        }

        void Hit()
        {
            //Climax effect

            //If collide
            foreach (Collider hit in Physics.OverlapSphere(transform.position, _hitSize, canHit))
            {
                //And not behind walls                                                  //Use static reference to walls layer
                if (Physics.Raycast(transform.position, hit.transform.position, out RaycastHit hitInfo, Vector3.Distance(transform.position, hit.transform.position) * 1.15f) && hitInfo.collider == hit)
                {
                    if (hit.TryGetComponent(out IDamageable iDmg))
                    {
                        iDmg.Damage(_damage);
                    }
                    if (hit.TryGetComponent(out IBuffable iBuff))
                    {
                        iBuff.Apply(_elem);
                    }
                }
            }
        }

        void End()
        {
            Destroy(gameObject, 0.1f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawSphere(transform.position, _hitSize);
        }
    }
}
