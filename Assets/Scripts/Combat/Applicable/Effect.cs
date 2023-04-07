namespace Assets.Script.Combat
{
    public class Effect
    {

        public delegate void Apply(Stats stats);
        public delegate void Deapply(Stats stats);
        private Apply _apply;
        private Deapply _deapply;
        private string _name;
        private float _duration, _tick, _timer, _cd;
        
        public Effect(Apply apply, string name, float duration, float tick)
        {
            _apply = apply;
            _name = name;
            _duration = duration;
            _tick = tick;
            _cd = 0;
            _timer = 0;
        }

        public Effect(Apply apply, Deapply deapply, string name, float duration, float tick)
        {
            _apply = apply;
            _deapply = deapply;
            _name = name;
            _duration = duration;
            _tick = tick;
            _cd = 0;
            _timer = 0;
        }

        public bool Affect(float passedTime, Stats stats)
        {
            _timer += passedTime;
            bool onTime = _timer <= _duration;

            if (onTime)
            {
                if (_timer > _cd + _tick)
                {
                    _apply(stats);
                    _cd = _timer;
                }
            } 
            else
            {
                _deapply?.Invoke(stats);
            }

            return !onTime;
        }
    }
}
