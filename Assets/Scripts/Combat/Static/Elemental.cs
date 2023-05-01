using System;
using System.Collections.Generic;

namespace Assets.Script.Combat
{
    public static class Elemental
    {
        private readonly static Dictionary<Type, int> _elementIndexer = new()
        {
            { typeof(Neutral), 0 },
            { typeof(Fire), 1 },
            { typeof(Water), 2 },
            { typeof(Earth), 3 },
            { typeof(Lightning), 4 },
            { typeof(Ice), 5 },
        };

        public static Dictionary<Type, int> ElementIndexer => _elementIndexer;
    }
}
