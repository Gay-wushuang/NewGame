public class CardInstance
{
    public CardData Data;
    
    public CardInstance(CardData data)
    {
        Data = data;
    }
    
    public int CalculateDamage(DiceInstance dice)
    {
        if (dice == null || Data.DamageFormula == null)
            return 0;
        
        return Data.DamageFormula(this, dice);
    }
}
