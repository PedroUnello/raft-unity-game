using Assets.Script.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Script.Collectables
{
    public class UltimatePoint : CollectablePoint
    {
        public override void Access<T>(ref T destiny)
        {
            switch (destiny)
            {

            }

            StartCoroutine(nameof(ResetPoint));
        }

        protected override IEnumerator ResetPoint()
        {
            gameObject.SetActive(false);
            yield return Util.GetWaitForSeconds(_cooldown);
            gameObject.SetActive(true);
        }
    }
}
