using Assets.Script.Gameplay;
using Assets.Script.Collectables; //Remove ?
using UnityEngine;
using Assets.Script.Core;
using Assets.Script.Comm;

namespace Assets.Script.Player
{
    public class Interpreter : MonoBehaviour //This implementation needs only one interpreter, by so this will use singletons
    {
        private static Interpreter _instance;
        public static Interpreter Instance => _instance;

        [SerializeField] private Player playerPrefab;

        private Transform _players, _elementalPoints;

        [SerializeField] private RaftInitializer raftNetworkManager; //This too

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

            DontDestroyOnLoad(gameObject);

            _players = GameObject.Find("Players").transform;
            _elementalPoints = GameObject.Find("Points").transform;

            //This should be removed and instanced at starting screen (when player start play session)
            RaftInitializer temp = Instantiate(raftNetworkManager);
            //                       SEU IP DO HAMACHI:PORT          MEU IP DO HAMACHI:PORT
            temp.InitiazeRaftServer("25.69.216.171:55556", new string[]{ "" });

        }

        //Receives messages in json format
        //Interpret content by know actions formula
        //Direct translated actions to right gameobject.
        void Update()
        {
            GameLog act = RaftManager.Instance.NewestAction;
            if (act != null) 
            { 
                Receive(act); 
            }
        }


        void Receive(GameLog log)
        {

            if (log.Type == "Connect")
            {
                var gameObj = Instantiate(playerPrefab, _players);
                gameObj.name = log.Id;
            }
            else if (log.Type == "Game" || log.Type == "Disconnect")
            {
                GameObject actionCaller = _players.Find(log.Id).gameObject;
                Interpret(actionCaller, log.Action);
            }

            
        }
        void Interpret(GameObject destiny, Action action) 
        {

            if (action.Type < Action.ActionType.Melee && destiny.TryGetComponent(out Movement moveHandle))
            {
                moveHandle.SetMovement(action.Movement, action.Rotation);
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
            }
        }
    }
}