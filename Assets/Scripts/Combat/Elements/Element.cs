using UnityEngine;

namespace Assets.Script.Combat
{
    public abstract class Element : MonoBehaviour
    {
        protected Effect _effect;
        public Effect Effect => _effect;
    }
}