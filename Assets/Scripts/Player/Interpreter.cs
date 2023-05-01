using UnityEngine;
using Assets.Script.Gameplay;
using Assets.Script.Collectables; //Remove ?
using Assets.Script.Core;
using Assets.Script.Comm;
using Assets.Script.Scenario;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace Assets.Script.Player
{
    public class Interpreter : MonoBehaviour //This implementation needs only one interpreter, by so this will use singletons
    {
        private static Interpreter _instance;
        public static Interpreter Instance => _instance;

        public string InstanceID; //PlayerID of the game owner (this player)

        [SerializeField] private Player playerPrefab; //Move to a reference class (As ther should be multiple models)
        private readonly Dictionary<string, GameObject> _playerList = new();
        private Transform _players, _elementalPoints;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
            }
            else
            {
                _instance = this;
            }

            _players = GameObject.Find("Players").transform;
            _elementalPoints = GameObject.Find("Points").transform;
        }
        void Start()
        {
            try
            {
                var act = RaftManager.Instance.NewestAction;
            }
            catch (System.NullReferenceException)
            {
                SceneManager.LoadScene(0);
                Cursor.lockState = CursorLockMode.None;
            }
        }
        void Update()
        {
            try 
            { 
                GameLog act = RaftManager.Instance.NewestAction;
                if (act != null) { Receive(act); }
            }
            catch (System.NullReferenceException) {; }  
        }

        public void Register(string ID, string modelName)
        {
            InstanceID = ID;
        }

        void Receive(GameLog log)
        {
            bool exist = _playerList.TryGetValue(log.Id, out GameObject actionCaller);
            if (log.Type == "Connect")
            {                
                Player player = Instantiate(playerPrefab, _players);
                player.name = log.Id;
                player.playerID = log.Id;
                _playerList.Add(log.Id, player.gameObject);
                player.gameObject.SetActive(false);

                if ( log.Id == InstanceID)
                {
                    //Register controller scripts (as this is player of this instance)
                    PlayerController plControl = GetComponent<PlayerController>();
                    plControl.Register(player);
                    plControl.enabled = true;
                    Action.SpawnArguments spawnArg = new();

                    GameObject point = Map.Instance.SpawnPoint;
                    spawnArg.pos = point.transform.position;
                    spawnArg.rot = point.transform.rotation;

                    GameLog gameLog = new() { Id = log.Id, ActionId = 2, Type = "Game", 
                        Action = new() { Position = spawnArg.pos, Rotation = spawnArg.rot, Arg = JsonUtility.ToJson(spawnArg), Type = Action.ActionType.Spawn } };
                    RaftManager.Instance.AppendAction(gameLog);
                }
            }
            else if (log.Type == "Game" && exist)
            {
                Interpret(actionCaller, log.Action);
            }
            else if (log.Type == "Disconnect" && exist)
            {
                _playerList.Remove(log.Id);
                Destroy(actionCaller);
            }
        }
        void Interpret(GameObject destiny, Action action) 
        {

            if (action.Type < Action.ActionType.Melee && destiny.TryGetComponent(out Movement moveHandle))
            {
                //moveHandle.Move(action.Position, action.Rotation);
                destiny.transform.SetPositionAndRotation(action.Position, action.Rotation);
            }

            switch (action.Type)
            {
                case Action.ActionType.Take:
                    Action.CollectArguments cArgs = JsonUtility.FromJson<Action.CollectArguments>(action.Arg);
                    int ePID = cArgs.Id;
                    if (destiny.TryGetComponent(out Player player))
                    {
                        if (cArgs.Point == "ElementalPoint")
                        {
                            var rightPoint = _elementalPoints.GetChild(ePID);
                            if (rightPoint.gameObject.activeSelf && rightPoint.TryGetComponent(out ElementalPoint eP)) { player.Take(cArgs.Got, eP); }
                        }
                        else if (cArgs.Point == "UltimatePoint")
                        {
                            switch (action.Type)
                            {
                                default:
                                    print(action.Type);
                                    break;
                            }
                        }
                    }
                    break;
                case Action.ActionType.Shoot:
                    Action.ShootArguments sArgs = JsonUtility.FromJson<Action.ShootArguments>(action.Arg);
                    if (destiny.TryGetComponent(out Magazine mag)) { mag.Shoot(sArgs.dir); }
                    break;
                case Action.ActionType.Melee:
                    if (destiny.TryGetComponent(out Melee melee)) { melee.Cast(); }
                    break;
                case Action.ActionType.Special:
                    break;
                case Action.ActionType.Super:
                    break;
                case Action.ActionType.Die:
                    if (destiny.TryGetComponent(out player)) { player.Die(); }
                    break;
                case Action.ActionType.Spawn:
                    Action.SpawnArguments spArgs = JsonUtility.FromJson<Action.SpawnArguments>(action.Arg);
                    if (destiny.TryGetComponent(out player)) { player.Spawn( spArgs.pos, spArgs.rot ); }
                    break;
            }
        }
    }
}