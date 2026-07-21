public class CardInstance
{
    public CardData Data;
    public int RemainingUses;
    public int CurseStacks = 1;
    public bool HasTriggeredThisTurn = false;

    public CardInstance(CardData data)
    {
        Data = data;
        RemainingUses = data.UsesPerBattle;
        CurseStacks = 1;
        HasTriggeredThisTurn = false;
    }

    public int CalculateDamage(DiceInstance dice)
    {
        if (Data.DamageFormula == null)
            return 0;

        return Data.DamageFormula(this, dice);
    }
}
