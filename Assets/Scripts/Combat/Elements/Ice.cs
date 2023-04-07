namespace Assets.Script.Combat
{
    public class Ice : Element
    {
        void Apply(Stats stats)
        {
            stats.Speed *= 0.8f;
        }
        void Deapply(Stats stats)
        {
            stats.Speed *= 1.2f;
        }

        void Awake()
        {
            _effect = new(Apply, Deapply, "Freeze", 20, 21);
        }
    }
}
