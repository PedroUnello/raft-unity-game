using Assets.Script.Combat;
using UnityEngine;
using Assets.Script.Core;
using Assets.Script.Gameplay;
using Assets.Script.Collectables;
using Assets.Script.Comm;

namespace Assets.Script.Player
{
    public class Player : MonoBehaviour, IDamageable, IBuffable
    {
        public enum Status
        {
            Idle,
            Basic,
            Melee,
            Special,
            Super
        }



        [SerializeField] private Stats _stats;
        private bool _isAlive;
        private Movement _moveHandle;

        private CollectablePoint _acessable;
        private Status _status;
        private string _pointType;
        private int _pointId;
        

        public CollectablePoint Acessable => _acessable;
        public Status PlayerStatus => _status;
        public string PointType => _pointType;
        public int PointId => _pointId;

        public string playerID;

        void Start()
        {
            _moveHandle = GetComponent<Movement>();
        }

        void Update()
        {
            if (!_isAlive) return;

            _stats.Buffs.Manage(Time.deltaTime);

            if (_moveHandle != null)
            {
                _moveHandle.speed = _stats.Speed;
            }
        }

        public void Apply(Element elem)
        {
            _stats.Buffs.Buff(elem.GetType(), elem.Effect);
        }

        public void Damage(float dmg)
        {
            _stats.CurHealth -= dmg;

            _isAlive = _stats.CurHealth > 0;
            if (!_isAlive)
            {
                GameLog gameLog = new() { Id = playerID, Type = "Game", Action = new() { Type = Action.ActionType.Die } };
                RaftManager.Instance.AppendAction(gameLog);
            }
        }

        public void Spawn( Vector3 position ) 
        {
            transform.position = position;
            
            _stats = new(225, 20);
            _isAlive = true;

            gameObject.SetActive(true);
        }

        public void Die() 
        {
            //Play animation
            //Cancel input replication (could maintain a dictionary of {playerID - bool alive} in raftmanager
            //Invoke Spawn (not the function, the message send) to raftmanager
            //Respawn at given time in all players games
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Collectable"))
            {
                _acessable = other.GetComponent<CollectablePoint>();
                if (other.TryGetComponent(out ElementalPoint eP)) { _pointType = "ElementalPoint"; _pointId = eP.PointID; }
                else if (other.TryGetComponent<UltimatePoint>(out _)) { _pointType = "UltimatePoint"; }
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Collectable"))
            {
                _acessable = null;
                _pointType = null;
                _pointId = -1;
            }
        }

    }
}
