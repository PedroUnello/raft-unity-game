using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Script.Collectables
{
    public abstract class CollectablePoint : MonoBehaviour
    {
        [SerializeField] protected float _cooldown;
        public abstract void Access<T>(ref T destiny);
        protected abstract IEnumerator ResetPoint();
    }
}
