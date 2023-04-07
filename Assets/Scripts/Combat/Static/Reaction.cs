using System.Collections.Generic;

namespace Assets.Script.Combat
{
    public static class Reaction
    {
        private static readonly Effect _vaporize = new(stats => { }, "Vaporize", 1, 1);
        private static readonly Effect _melt = new(stats => { }, "Melt", 1, 1);
        private static readonly Effect _tase = new(stats => { }, "Tase", 1, 1);
        private static readonly Effect _freeze = new(stats => { stats.Speed *= 0.001f; }, stats => { stats.Speed /= 0.001f; }, "Freeze", 5, 6);

        private readonly static Dictionary<(System.Type, System.Type), Effect> _reactions = new()
        {

            //Fire + Water

            { (typeof(Fire), typeof(Water)), _vaporize },

            //Fire + Ice

            { (typeof(Earth), typeof(Water)), _melt },


            // Water + Lightning

            { (typeof(Lightning), typeof(Water)), _tase },

            //Ice + Water

            { (typeof(Ice), typeof(Water)), _freeze },

        };

        public static Dictionary<(System.Type, System.Type), Effect> Reactions => _reactions;
    }
}

