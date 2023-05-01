using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Script.Core
{
    [Serializable]
    public class Action
    {

        public struct CollectArguments
        {
            public string Got;
            public string Point;
            public int Id;
        }
        public struct ShootArguments
        {
            public Vector3 dir;
        }
        public struct SpawnArguments
        {
            public Vector3 pos;
            public Quaternion rot;
        }
        public struct DamageArguments
        {
            public float value;
        }

        public enum ActionType
        {
            None,
            Take,
            Shoot,
            Melee,
            Special,
            Super,
            Damage,
            Spawn,
            Die
        };

        public Vector3 Position;
        public Quaternion Rotation;
        public ActionType Type;
        public string Arg;

        public override string ToString() => "\nMovement: " + Position.ToString() + "\nRotation: " + Rotation.ToString() + "\nType: " + Type.ToString() + "\nArgs: " + Arg;
    }
}