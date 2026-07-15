using System;

public class DiceInstance
{
    private static Random _rand = new Random();
    
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
    
    public int RollAndConsume()
    {
        Value = _rand.Next(1, Sides + 1);
        IsRolled = true;
        IsConsumed = true;
        return Value.Value;
    }
}
