using UnityEngine;

namespace Assets.Script.Combat
{
    [System.Serializable]
    public class Stats
    {
        [Min(0)]
        [SerializeField] private float _curHealth, _maxHealth;
        public float CurHealth
        { get { return _curHealth; } set { _curHealth = Mathf.Clamp(value, 0, _maxHealth); } }
        public float MaxHealth
        { get { return _maxHealth; } set { _maxHealth = value; _curHealth = Mathf.Clamp(_curHealth, 0, _maxHealth); } }

        [Min(0)] [SerializeField] private float _speed;
        public float Speed
        { get { return _speed; } set { _speed = Mathf.Clamp(value, 0, Mathf.Infinity); ; } }

        private readonly Buffs _buffs;

        public Buffs Buffs => _buffs;

        public Stats(float h, float s)
        {
            _maxHealth = h;
            _curHealth = _maxHealth;
            Speed = s;
            _buffs = new(this);
        }
    }
}