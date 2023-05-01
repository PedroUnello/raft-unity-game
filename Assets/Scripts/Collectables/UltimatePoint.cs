using Assets.Script.Combat;
using Assets.Script.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Script.Collectables
{
    public class UltimatePoint : CollectablePoint
    {
        public override int Access<T>(ref T destiny)
        {
            System.Type got = typeof( Neutral );

            switch (destiny)
            {

            }

            StartCoroutine(nameof(ResetPoint));

            return Elemental.ElementIndexer.GetValueOrDefault(got);
        }

        protected override IEnumerator ResetPoint()
        {
            gameObject.SetActive(false);
            yield return Util.GetWaitForSeconds(_cooldown);
            gameObject.SetActive(true);
        }
    }
}
