using UnityEngine;
using UnityEngine.UI;
using Assets.Script.Combat;
using Assets.Script.Core;
using Assets.Script.Gameplay;
using Assets.Script.Collectables;
using Assets.Script.Comm;
using Assets.Script.Scenario;
using UnityEngine.Events;

namespace Assets.Script.Player
{
    public struct Collectable
    {
        public CollectablePoint Acessable;
        public string Type;
        public int Id;
    }

    [RequireComponent(typeof(Movement))]
    [RequireComponent(typeof(Magazine))]
    [RequireComponent(typeof(Melee))]
    public class Player : MonoBehaviour, IDamageable, IBuffable
    {
        private bool _isAlive;
        private Collectable _collectable;
        [SerializeField]
        private Gradient _healthColor;
        [SerializeField]
        private Stats _stats;
        private Movement _moveHandle;
        private Magazine _magHandle;
        private Melee _meleeHandle;
        private Special _specialHandle;

        //Billboard health
        public Slider HealthBar;
        public Image HealthFill;

        public bool IsAlive => _isAlive;
        public Collectable Collectable => _collectable;
        public UnityEvent<float> OnDamage { get; private set; } = new();
        public UnityEvent<int> MeleeChanged { get; private set; } = new();
        public UnityEvent<int> ProjectileChanged { get; private set; } = new();
        public UnityEvent<int> SpecialChanged { get; private set; } = new();
        public Movement Movement => _moveHandle;

        public string playerID;

        void Awake()
        {
            _moveHandle = GetComponent<Movement>();
            _magHandle = GetComponent<Magazine>();
            _meleeHandle = GetComponent<Melee>();
        }
        void Update()
        {
            if (!_isAlive) return;

            _stats.Buffs.Manage(Time.deltaTime);

            UpdateHealthBar(_stats.CurHealth / _stats.MaxHealth);

            if (_moveHandle != null)
            {
                _moveHandle.speed = _stats.Speed;
            }
        }

        public void Apply(Element elem)
        {
            _stats.Buffs.Buff(elem.GetType(), elem.Effect);
        }

        public void Damage(float dmg) //Maybe change the name to ~Hit~ or smth
        {

            OnDamage.Invoke(dmg); //Keep this

            //Exchange this to another method
            _stats.CurHealth -= dmg;
            _isAlive = _stats.CurHealth > 0;
            
        }

        public void Take(string component, ElementalPoint eP)
        {
            switch (component)
            {
                case "Magazine":
                    ProjectileChanged.Invoke( eP.Access(ref _magHandle) );
                    break;
                case "Melee":
                    MeleeChanged.Invoke(eP.Access(ref _meleeHandle));
                    break;
                case "Special":
                    SpecialChanged.Invoke(eP.Access(ref _specialHandle));
                    break;
            }
        }

        public void Spawn(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);

            _stats = new(225, _stats.Speed);
            _isAlive = true;

            MeleeChanged.Invoke(0);
            ProjectileChanged.Invoke(0);
            SpecialChanged.Invoke(0);

            _magHandle.Recharge();
            _meleeHandle.Exchange(null);

            gameObject.SetActive(true);
        }

        public void Die()
        {
            // 1 - Play animation

            // 2 - Cancel input replication
            //          (could maintain a dictionary of {playerID - bool alive} in raftmanager
            //          (Or using the bool is alive in controller)

            // 3 - Disable body physics and collider

            // 4 - Invoke Spawn (not the function, the message send) to raftmanager
            //          (Done by player controller)
            
        }

        void UpdateHealthBar(float percentage)
        {
            HealthBar.value = percentage;
            HealthFill.color = _healthColor.Evaluate(percentage);
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Collectable"))
            {
                _collectable.Acessable = other.GetComponent<CollectablePoint>();
                if (other.TryGetComponent(out ElementalPoint eP)) { _collectable.Type = "ElementalPoint"; _collectable.Id = eP.PointID; }
                else if (other.TryGetComponent<UltimatePoint>(out _)) { _collectable.Type = "UltimatePoint"; }
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Collectable"))
            {
                _collectable.Acessable = null;
                _collectable.Type = null;
                _collectable.Id = -1;
            }
        }
    }
}
