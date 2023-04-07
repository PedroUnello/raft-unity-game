using Assets.Script.Combat;
using UnityEngine;
using Assets.Script.Core;
using Assets.Script.Gameplay;

namespace Assets.Script.Player
{
    public class Player : MonoBehaviour, IDamageable, IBuffable
    {
        [SerializeField] private Stats _stats;
        private bool _isAlive;
        private Movement _moveHandle;

        public int playerID;

        void Awake()
        {
            //Remove. (Should be call by Connect.)
            Spawn();
        }

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
                //Send to interpreter message that we died....
                //Interpreter.Instance.Receive();
            }
        }

        /*
                void IBuffable.Buff(float timeLeft, float buffValue, string buffName, Buffs.BuffType buffType)
        {
            Coroutine newBuff = StartCoroutine(Stats.Buffs.UnBuff(timeLeft, buffValue, buffName, buffType));
            Coroutine isThereSameBuff = Stats.Buffs.AddBuff(buffValue, buffName, buffType, newBuff);
            if (isThereSameBuff != null)
            { StopCoroutine(isThereSameBuff); }
        }
        void IBuffable.Buff(float buffValue, string buffName, Buffs.BuffType buffType)
        {
            Stats.Buffs.AddBuff(buffValue, buffName, buffType);
        }
        void IBuffable.UnBuff(float buffValue, string buffName, Buffs.BuffType buffType)
        {
            Stats.Buffs.UnBuff(0, buffValue, buffName, buffType);
        }
         */

        public void Spawn() 
        {
            /*
            |---------------------------Italian func to generate deterministic position-------------------------|
            var idPos, _ = strconv.Atoi(fmt.Sprint(playerID))
            var mapHeight = len(GameMap)
            var mapWidth = len(GameMap[0])
            var nOfCells = mapHeight * mapWidth
            if idPos < nOfCells {
                idPos += nOfCells
            }
            var found = false
            var position = Position{ 0, 0, 0}
            for !found {
                idPos = idPos % nOfCells
                position.X = float64(idPos / mapWidth)
                position.Y = float64(idPos % mapWidth)
        
                if checkHitWall(position.X, position.Y) 
                {
                    idPos++
                }
                else
                {
                    found = true
                }
            }
            return position
            |--------------------------------------------------------------------------------------------------|
            */

            //Convertion would be:
            //1. Static ref to all spawnpoints (that would define max player count)
            //2. Acess with player id
            // --by so every player would only spawn in the same place--

            _stats = new(225, 20);
            _isAlive = true;
        }

        public void Die() 
        {
            //Play animation
            //Cancel input replication (could maintain a dictionary of {playerID - bool alive} in raftmanager
            //Invoke Spawn (not the function, the message send) to raftmanager
            //Respawn at given time in all players games
        }

    }
}
