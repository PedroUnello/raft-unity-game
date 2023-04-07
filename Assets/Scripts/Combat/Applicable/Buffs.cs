using System.Collections.Generic;

namespace Assets.Script.Combat
{
    public class Buffs
    {
        private readonly Stats _stats;
        private readonly Dictionary<System.Type, Effect> _applied = new();
        private readonly List<Effect> _reactions = new(); 
        private List<System.Type> toRemove = new();
        private List<Effect> toEnd = new();

        public void Manage(float passedTime)
        {
            toRemove.Clear();
            foreach ((System.Type elem, Effect effect) in _applied)
            {
                if (effect.Affect(passedTime, _stats))
                {
                    toRemove.Add(elem);
                }
            }

            foreach(var remove in toRemove)
            {
                _applied.Remove(remove);
            }

            toEnd.Clear();
            foreach (Effect reaction in _reactions)
            {
                if (reaction.Affect(passedTime, _stats))
                {
                    toEnd.Add(reaction);
                }
            }

            foreach (var remove in toEnd)
            {
                _reactions.Remove(remove);
            }
        }

        public void Buff( System.Type elem, Effect effect)
        {

            if (effect == null || elem == null) return;

            if (!_applied.TryGetValue(elem, out _ )) 
            {
                foreach ( System.Type appliedElem in _applied.Keys)
                {
                    if (Reaction.Reactions.TryGetValue((elem, appliedElem), out Effect reaction))
                    {
                        if (!_reactions.Contains(reaction)) _reactions.Add(reaction);
                        _applied.Remove(appliedElem);
                        return;
                    }
                }

                _applied.Add(elem, effect);
            }
        }

        public Buffs(Stats stats)
        {
            _stats = stats;
        }
    }
}
