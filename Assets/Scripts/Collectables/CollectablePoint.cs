using System.Collections;
using UnityEngine;

namespace Assets.Script.Collectables
{
    public abstract class CollectablePoint : MonoBehaviour
    {
        [SerializeField] protected float _cooldown;
        public abstract int Access<T>(ref T destiny);
        protected abstract IEnumerator ResetPoint();
    }
}
