using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Script.Core
{
    [Serializable]
    public class GameLog
    {
        public string Id;
        public int ActionId;
        public string Type;
        public Action Action;
    }
}