public class DiceInstance
{
    public int Sides;
    public int? Value;
    public bool IsRolled;
    public bool IsConsumed;
    public string Source;

    public DiceInstance(int sides, string source = "Default")
    {
        Sides = sides;
        Source = source;
        Value = null;
        IsRolled = false;
        IsConsumed = false;
    }

    public int RollAndConsume(DiceRoller roller = null)
    {
        if (roller != null)
        {
            Value = roller.Roll(Sides);
        }
        else
        {
            var rng = new Godot.RandomNumberGenerator();
            rng.Randomize();
            Value = rng.RandiRange(1, Sides);
        }
        IsRolled = true;
        IsConsumed = true;
        return Value.Value;
    }
}