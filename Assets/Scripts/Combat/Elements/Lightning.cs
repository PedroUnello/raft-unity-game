namespace Assets.Script.Combat
{
    public class Lightning : Element
    {
        void Apply(Stats stats)
        {
            stats.CurHealth -= 15;
        }

        void Awake()
        {
            _effect = new(Apply, "Smite", 15, 16);
        }
    }
}
