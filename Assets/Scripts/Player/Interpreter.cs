using Assets.Script.Gameplay;
using Assets.Script.Collectables; //Remove ?
using UnityEngine;
using Assets.Script.Core;
using Assets.Script.Comm;
using System.Collections.Generic;

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

            //DontDestroyOnLoad(gameObject);

            _players = GameObject.Find("Players").transform;
            _elementalPoints = GameObject.Find("Points").transform;
        }
        void Update()
        {
            GameLog act = RaftManager.Instance.NewestAction;
            if (act != null) 
            { 
                Receive(act); 
            }
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
                    plControl.Player = player;
                    plControl.Register();
                    plControl.enabled = true;

                    Action.SpawnArguments spawnArg = new();

                    //Convertion would be:
                    //1. Static ref to all spawnpoints (that would define max player count)
                    //2. Acess with player id
                    // --by so every player would only spawn in the same place--

                    //But for now... spawn in fixed pos
                    spawnArg.pos = new Vector3(Random.Range(0, 40), 4, Random.Range(0, 40));

                    GameLog gameLog = new() { Id = log.Id, ActionId = 2, Type = "Game", Action = new() { Arg = JsonUtility.ToJson(spawnArg), Type = Action.ActionType.Spawn } };
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

            if (action.Type < Action.ActionType.Melee)
            {
                destiny.transform.SetPositionAndRotation(action.Position, action.Rotation);
            }

            switch (action.Type)
            {
                case Action.ActionType.Take:
                    Action.CollectArguments cArgs = JsonUtility.FromJson<Action.CollectArguments>(action.Arg);
                    int ePID = cArgs.Id;
                    if (cArgs.Point == "ElementalPoint")
                    {
                        var rightPoint = _elementalPoints.GetChild(ePID);
                        if (rightPoint.gameObject.activeSelf && rightPoint.TryGetComponent(out ElementalPoint eP))
                        {
                            switch (cArgs.Got)
                            {
                                case "Magazine":
                                    if (destiny.TryGetComponent(out Magazine magA))
                                    {
                                        eP.Access(ref magA);
                                    }
                                    break;
                                case "Melee":
                                    if (destiny.TryGetComponent(out Melee meleeA))
                                    {
                                        eP.Access(ref meleeA);
                                    }
                                    break;
                                case "Special":
                                    if (destiny.TryGetComponent(out Special spcl))
                                    {
                                        eP.Access(ref spcl);
                                    }
                                    break;
                            }
                        }
                    }
                    else if (cArgs.Point == "UltimatePoint")
                    {
                        switch (action.Type)
                        {

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
                    //Play animation I guess ? ...
                    if (destiny.TryGetComponent(out Player player))
                    {
                        player.Die();
                    }
                    break;
                case Action.ActionType.Spawn:
                    if (destiny.TryGetComponent(out player))
                    {
                        Action.SpawnArguments spArgs = JsonUtility.FromJson<Action.SpawnArguments>(action.Arg);
                        player.Spawn( spArgs.pos );
                    }
                    break;
            }
        }
    }
}