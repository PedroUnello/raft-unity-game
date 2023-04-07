namespace Assets.Script.Combat
{
    public class Earth : Element
    {
        void Apply(Stats stats)
        {

        }

        void Awake()
        {
            _effect = new(Apply, "Nothing", 0, 0);
        }
    }
}


