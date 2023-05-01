using static UnityEngine.InputSystem.InputAction;
using UnityEngine;
using UnityEngine.UI;
using Cinemachine;
using System.Collections;
using Assets.Script.Gameplay;
using Assets.Script.Comm;
using Assets.Script.Core;
using Assets.Script.Utils;
using Assets.Script.Scenario;

namespace Assets.Script.Player
{
    public class PlayerController : MonoBehaviour
    {
        private enum Status
        {
            Idle,
            Basic,
            Melee,
            Special,
            Super
        }

        [Range(0.1f, 1)] 
        [SerializeField] 
        private float _percentageDistanceAim;
        private float _initialCamDistance;
        private readonly float _meleeCD = 0.65f, _shootCD = 0.185f;
        private readonly int _actionLimiterFactor = 20;
        private Status _status;
        private Vector2 _movement, _rotation;
        private Vector3 _recordPosition;
        private Quaternion _recordRotation;
        private Action _action;
        private Cinemachine3rdPersonFollow _camComposer;

        //Player only Interface.
        [SerializeField] 
        private GameObject _UI;
        private Player _player;

        //This will be moved to gameplay modules

        private Special _special;

        void Awake()
        {
            Cursor.lockState = CursorLockMode.Locked;
            _action = new();
        }
        void Start()
        {
            _movement = Vector2.zero;
            _rotation = Vector2.zero;

            _recordPosition = new Vector3(-9999, -9999, -9999);
            _recordRotation = new Quaternion(1,1,1,1);

            _status = Status.Idle;
            StartCoroutine(nameof(ApplyActions));
        }
        private void Update()
        {
            Vector3 movementTranslatedToCam = (_movement.y * Camera.main.transform.forward + _movement.x * Camera.main.transform.right).normalized;
            movementTranslatedToCam.y = 0;

            //bool moving = (!Equals(movementTranslatedToCam, Vector3.zero) || !Equals(_rotation, Vector2.zero)) && _status <= Status.Basic;

            if (_status <= Status.Basic && _player.IsAlive)
            {
                _player.Movement.Move(movementTranslatedToCam, _rotation);
            }
        }

        IEnumerator ApplyActions()
        {
            for (;;)
            {
                if (_player.IsAlive)
                {

                    bool moved = !Equals(_recordPosition, _player.transform.position) || !Equals(_recordRotation, _player.transform.rotation);

                    if (_action.Type > Action.ActionType.None || moved)
                    {
                        _action.Position = _player.transform.position;// movementTranslatedToCam;
                        _action.Rotation = _player.transform.rotation;//_rotation;

                        _recordPosition = _action.Position;
                        _recordRotation = _action.Rotation;

                        if (_player.Collectable.Acessable != null
                            && _action.Type > Action.ActionType.None)
                        {
                            Action.CollectArguments cArgs = new();
                            switch (_action.Type)
                            {
                                case Action.ActionType.Shoot:
                                    cArgs.Got = "Magazine";
                                    break;
                                case Action.ActionType.Melee:
                                    cArgs.Got = "Melee";
                                    break;
                            }
                            cArgs.Point = _player.Collectable.Type;
                            cArgs.Id = _player.Collectable.Id;
                            _action.Arg = JsonUtility.ToJson(cArgs);
                            _action.Type = Action.ActionType.Take;
                        }
                        else
                        {
                            switch (_action.Type)
                            {
                                case Action.ActionType.Shoot:
                                    Action.ShootArguments sArgs = new();
                                    sArgs.dir = Camera.main.transform.forward;
                                    _action.Arg = JsonUtility.ToJson(sArgs);
                                    break;
                            }
                        }

                        GameLog gameLog = new() { Id = _player.playerID.ToString(), Type = "Game", Action = _action };
                        //Send to raft network (there the _actionId will be switched)
                        RaftManager.Instance.AppendAction(gameLog);
                        _action = new();
                    }

                    yield return Util.GetWaitForSeconds(Time.deltaTime * _actionLimiterFactor);
                }
                else
                {
                    GameLog gameLog = new() { Id = _player.playerID, Type = "Game", 
                        Action = new() { Position = _player.transform.position, Rotation = _player.transform.rotation, Type = Action.ActionType.Die } };
                    RaftManager.Instance.AppendAction(gameLog);

                    yield return Util.GetWaitForSeconds(5);

                    Action.SpawnArguments spawnArg = new();

                    GameObject point = Map.Instance.SpawnPoint;
                    spawnArg.pos = point.transform.position;
                    spawnArg.rot = point.transform.rotation;

                    gameLog = new() { Id = _player.playerID, Type = "Game", 
                        Action = new() { Position = _player.transform.position, Rotation = _player.transform.rotation, Arg = JsonUtility.ToJson(spawnArg), Type = Action.ActionType.Spawn } };
                    RaftManager.Instance.AppendAction(gameLog);
                }
            }
        }

        public void Register(Player player)
        {

            _player = player;

            GameObject temp = GameObject.Find("CameraControl");
            if (temp != null && temp.TryGetComponent(out CinemachineVirtualCamera virtualCamera))
            {
                _camComposer = virtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
                if (_camComposer != null) _initialCamDistance = _camComposer.CameraDistance;
                virtualCamera.Follow = _player.transform.GetChild(0);
            }

            //Subscribe OnDamage package send method
            _player.OnDamage.AddListener(
                value =>
                {
                    Action.DamageArguments dmgArg = new();
                    dmgArg.value = value;
                    GameLog gameLog = new() { Id = _player.playerID, Type = "Game", Action = new() { Arg = JsonUtility.ToJson(dmgArg), Type = Action.ActionType.Damage } };
                    RaftManager.Instance.AppendAction(gameLog);
                }
            );

            //Delete player prefab health bar, and change to UI one
            _player.HealthBar.gameObject.SetActive(false);
            Destroy(_player.HealthBar);

            _player.HealthBar = _UI.transform.GetChild(0).GetComponent<Slider>();
            _player.HealthFill = _UI.transform.GetChild(0).GetChild(1).GetChild(0).GetComponent<Image>();

            Animator magazineUIAnimator = _UI.transform.GetChild(1).GetChild(2).GetComponent<Animator>();
            Animator meleeUIAnimator = _UI.transform.GetChild(2).GetChild(2).GetComponent<Animator>();
            Animator specialUIAnimator = _UI.transform.GetChild(3).GetChild(0).GetComponent<Animator>();

            //Subscribe UI changes to event;
            _player.ProjectileChanged.AddListener( element => { magazineUIAnimator.SetInteger("Element", element); } );
            _player.MeleeChanged.AddListener(element => { meleeUIAnimator.SetInteger("Element", element); } );
            _player.SpecialChanged.AddListener(element => { specialUIAnimator.SetInteger("Element", element); } );
            
            _UI.SetActive(true);
        }

        public void Ultimate(CallbackContext iValue)
        {

        }

        public void Special03(CallbackContext iValue)
        {

        }

        public void Special02(CallbackContext iValue)
        {

        }

        public void Special01(CallbackContext iValue)
        {

        }

        public void Melee(CallbackContext iValue)
        {
            if (iValue.performed && _status < Status.Melee)
            {

                _action.Type = Action.ActionType.Melee;
                _status = Status.Melee;

                Invoke(nameof(ResetStatus), _meleeCD);
            }
        }

        public void Fire(CallbackContext iValue)
        {
            if (iValue.performed && _status < Status.Basic)
            {
                _action.Type = Action.ActionType.Shoot;
                _status = Status.Basic;

                Invoke(nameof(ResetStatus), _shootCD);
            }
        }

        public void Look(CallbackContext iValue)
        {
            _rotation = RoundVector2(iValue.ReadValue<Vector2>(), 4);
        }

        public void Move(CallbackContext iValue)
        {
            _movement = RoundVector3(iValue.ReadValue<Vector2>(), 4);
        }

        public void Aim(CallbackContext iValue)
        {
            if (iValue.performed)
            {
                if (_camComposer != null) _camComposer.CameraDistance = _initialCamDistance * _percentageDistanceAim;
            } 
            else if (iValue.canceled)
            {
                if (_camComposer != null) _camComposer.CameraDistance = _initialCamDistance;
            }
        }

        void ResetStatus()
        {
            _status = Status.Idle;
        }
        float RoundFloat(float val, int decimalCases)
        {
            float convert = Mathf.Pow(10, decimalCases);
            return Mathf.Round(val * convert) / convert;
        }
        Vector2 RoundVector2(Vector2 v, int decimalCases)
        {
            v.x = RoundFloat(v.x, decimalCases);
            v.y = RoundFloat(v.y, decimalCases);
            return v;
        }
        Vector3 RoundVector3(Vector3 v, int decimalCases)
        {
            v.x = RoundFloat(v.x, decimalCases);
            v.y = RoundFloat(v.y, decimalCases);
            v.z = RoundFloat(v.z, decimalCases);
            return v;
        }
        void OnApplicationQuit()
        {
            StopAllCoroutines();
        }
    }
}