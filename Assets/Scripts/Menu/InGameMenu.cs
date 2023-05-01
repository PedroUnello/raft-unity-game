using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Script.Menu
{
    public class InGameMenu : MonoBehaviour
    {
        [SerializeField]
        private GameObject _menu;
        private PlayerInput _playerInput;
        private bool _show = false;

        void Start()
        {
            _menu.SetActive(_show);
            _playerInput = GetComponent<PlayerInput>();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _show = !_show;
                _menu.SetActive(_show);

                if (_show) 
                { 
                    _playerInput.SwitchCurrentActionMap("UI");
                    Cursor.lockState = CursorLockMode.None;
                }
                else 
                { 
                    _playerInput.SwitchCurrentActionMap("Player");
                    Cursor.lockState = CursorLockMode.Locked;
                }
            }
        }

        public void Quit() { Application.Quit(); }

    }
}
