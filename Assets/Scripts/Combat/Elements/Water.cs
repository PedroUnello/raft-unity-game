namespace Assets.Script.Combat
{
    public class Water : Element
    {
        void Apply(Stats stats)
        {
            //Does nothing. . . .
        }

        void Awake()
        {
            _effect = new(Apply, "Wet", 40, 41);
        }
    }
}