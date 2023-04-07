using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Script.Gameplay
{
    public class Melee : MonoBehaviour
    {
        [SerializeField] Vector3 origin;
        private Attack _attack;

        public void Cast()
        {
            if (_attack != null) Instantiate(_attack, transform.position + transform.forward * origin.z + transform.right * origin.x + transform.up * origin.y, Quaternion.identity);
        }
        
        public void Exchange( Attack newA )
        {
            _attack = newA;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(transform.position + transform.forward * origin.z + transform.right * origin.x + transform.up * origin.y, 0.2f);
        }

    }
}
