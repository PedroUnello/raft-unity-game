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
        }

        public enum ActionType
        {
            None,
            Take,
            Shoot,
            Melee,
            Special,
            Super,
            Die,
            Spawn
        };

        public Vector3 Movement;
        public Vector2 Rotation;
        public ActionType Type;
        public string Arg;

        public override string ToString() => "\nMovement: " + Movement.ToString() + "\nRotation: " + Rotation.ToString() + "\nType: " + Type.ToString() + "\nArgs: " + Arg;
    }
}