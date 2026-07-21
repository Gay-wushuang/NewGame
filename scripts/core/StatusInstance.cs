public enum StatusType
{
    Vulnerable,
    Weak,
    Slow,
    ArmorBreak,
    EnergyDrain,
    AttackUp,
    DefenseUp,
    EnergyRegen,
    DiceBonus,
    CriticalRateUp
}

public class StatusInstance
{
    public StatusType Type;
    public int Stacks;
    public int Duration;

    public StatusInstance(StatusType type, int stacks, int duration = 0)
    {
        Type = type;
        Stacks = stacks;
        Duration = duration;
    }
}