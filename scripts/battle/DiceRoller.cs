using Godot;

public class DiceRoller
{
    private RandomNumberGenerator _rng;

    public RollMode Mode { get; set; }
    public int FixedValue { get; set; }

    public DiceRoller()
    {
        Mode = RollMode.Random;
        FixedValue = 1;
        _rng = new RandomNumberGenerator();
        _rng.Randomize();
    }

    public int Roll(int sides)
    {
        if (Mode == RollMode.Fixed)
        {
            return Mathf.Clamp(FixedValue, 1, sides);
        }

        return _rng.RandiRange(1, sides);
    }

    public void Randomize()
    {
        _rng.Randomize();
    }
}