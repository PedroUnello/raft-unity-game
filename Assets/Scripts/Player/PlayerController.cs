using Assets.Script.Gameplay;
using UnityEngine;
using static UnityEngine.InputSystem.InputAction;
using Assets.Script.Core;
using Cinemachine;
using Assets.Script.Comm;

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

        [Range(0.1f, 1)] [SerializeField] private float _percentageDistanceAim;
        private readonly float _meleeCD = 0.65f, _shootCD = 0.185f;
        private float _initialCamDistance;
        private Action _action;
        private Status _status;
        private Vector2 _movement, _rotation;
        private Vector3 _recordPosition;
        private Quaternion _recordRotation;
        private Cinemachine3rdPersonFollow _camComposer;

        private int _inputDecimalPlaces = 4;

        public Player Player;

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
            _recordPosition = Player.transform.position;
            _recordRotation = Player.transform.rotation;
            _status = Status.Idle;
        }
        void Update()
        {

            Vector3 movementTranslatedToCam = (_movement.y * Camera.main.transform.forward + _movement.x * Camera.main.transform.right).normalized;
            movementTranslatedToCam.y = 0;

            if (Player.TryGetComponent(out Movement handle)) handle.Move(movementTranslatedToCam, _rotation);

            bool moved = (!Equals(Player.transform.position, _recordPosition) || !Equals(Player.transform.rotation, _recordRotation)) && _status <= Status.Basic;

            if (_action.Type > Action.ActionType.None || moved)
            {
                _action.Position = Player.transform.position;
                _action.Rotation = Player.transform.rotation;

                _recordPosition = _action.Position;
                _recordRotation = _action.Rotation;

                if (Player.Acessable != null 
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
                    cArgs.Point = Player.PointType;
                    cArgs.Id = Player.PointId;
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

                GameLog gameLog = new() { Id = Player.playerID.ToString(), Type = "Game", Action = _action };
                print(JsonUtility.ToJson(_action));
                //Send to raft network (there the _actionId will be switched)
                RaftManager.Instance.AppendAction(gameLog);
                
                _action = new();
            }
        }

        public void Register()
        {
            GameObject temp = GameObject.Find("CameraControl");
            if (temp != null && temp.TryGetComponent(out CinemachineVirtualCamera virtualCamera))
            {
                _camComposer = virtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
                if (_camComposer != null) _initialCamDistance = _camComposer.CameraDistance;
                virtualCamera.Follow = Player.transform.GetChild(0);
            }
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
            _rotation = RoundVector2(iValue.ReadValue<Vector2>(), _inputDecimalPlaces);
        }

        public void Move(CallbackContext iValue)
        {
            _movement = RoundVector2(iValue.ReadValue<Vector2>(), _inputDecimalPlaces);
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
        float RoundFloat( float val, int decimalCases)
        {
            float convert = Mathf.Pow(10, decimalCases);
            return Mathf.Round(val * convert) / convert;
        }

        //Get all inputs

        //Apply logic to all thats being input
        //Create message {as json}
        //Send to network
        //Send to player interpreter.

        //If need be -> Send to other VFX/UI scripts <-

    }
}