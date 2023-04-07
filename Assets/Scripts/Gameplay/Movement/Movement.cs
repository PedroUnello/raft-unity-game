using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Script.Gameplay
{
    [RequireComponent(typeof(CharacterController))]
    public class Movement : MonoBehaviour
    {

        [SerializeField] [Min(1)] private float _rotationSpeed;
        [SerializeField] private GameObject _cameraTarget;
        private CharacterController _characterController;
        private Animator _animator;
        private Vector3 _movement;
        private Vector2 _rotation;

        [HideInInspector] public float speed;

        void Awake()
        {
            _movement = Vector3.zero;
            _animator = GetComponent<Animator>();
            _characterController = GetComponent<CharacterController>();
        }

        void Update()
        {
            Move();
        }

        public void SetMovement(Vector3 movement, Vector2 rotation)
        {
            _movement = movement;
            _rotation = rotation;
        }

        void Move()
        {
            transform.RotateAround(transform.position, transform.up, _rotation.x * Time.deltaTime * _rotationSpeed);
            _cameraTarget.transform.RotateAround(_cameraTarget.transform.position, _cameraTarget.transform.right, -_rotation.y * Time.deltaTime * _rotationSpeed);
            Vector3 preAdjustRot = _cameraTarget.transform.eulerAngles;
            preAdjustRot.x = ClampAngle(preAdjustRot.x, -45, 45);
            _cameraTarget.transform.eulerAngles = preAdjustRot;
            _characterController.Move(_movement * speed * Time.deltaTime);
            _animator.SetFloat("Move", Mathf.Max(Mathf.Abs(_movement.x), Mathf.Abs(_movement.z)));
        }

        float ClampAngle(float angleInDegrees, float min, float max)
        {
            if (angleInDegrees > 180f) return Mathf.Max(angleInDegrees, 360 + min);
            return Mathf.Min(angleInDegrees < 0f ? 360 + angleInDegrees : angleInDegrees, max);
        }
    }
}