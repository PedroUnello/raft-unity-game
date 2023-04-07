using System;
using UnityEngine;


namespace Assets.Script.Utils
{
    [Serializable]
    public class NormalizedVector3
    {
        [Range(-1,1)] [SerializeField] float _x, _y, _z;
        public Vector3 Value => new Vector3(_x,_y,_z).normalized;
    }
}