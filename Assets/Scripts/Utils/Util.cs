using System.Collections.Generic;
using UnityEngine;

namespace Assets.Script.Utils
{
    public static class Util
    {
        private static readonly Dictionary<float, WaitForSeconds> _fromToStorage = new();
        public static WaitForSeconds GetWaitForSeconds(float t)
        {
            _fromToStorage.TryAdd(t, new WaitForSeconds(t));
            return _fromToStorage.GetValueOrDefault(t);
        }
    }
}
