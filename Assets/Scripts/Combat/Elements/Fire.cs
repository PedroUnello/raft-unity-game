namespace Assets.Script.Combat
{
    public class Fire : Element
    {
        void Apply(Stats stats)
        {
            stats.CurHealth -= 1.2f;
        }

        void Awake()
        {
            _effect = new(Apply, "Burn", 20, 1);   
        }
    }
}