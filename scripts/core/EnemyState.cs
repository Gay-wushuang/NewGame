public class EnemyState
{
    public string Name;
    public int MaxHp;
    public int Hp;
    public EnemyIntent CurrentIntent;
    
    public EnemyState(string name, int maxHp)
    {
        Name = name;
        MaxHp = maxHp;
        Hp = maxHp;
        CurrentIntent = new EnemyIntent();
    }
    
    public void TakeDamage(int damage)
    {
        Hp -= damage;
    }
    
    public bool IsAlive()
    {
        return Hp > 0;
    }
}

public class EnemyIntent
{
    public enum IntentType
    {
        Attack,
        Defend,
        Buff,
        Debuff
    }
    
    public IntentType Type;
    public int Value;
    public string Description;
    
    public EnemyIntent()
    {
        Type = IntentType.Attack;
        Value = 6;
        Description = $"攻击 {Value}";
    }
    
    public void SetAttack(int damage)
    {
        Type = IntentType.Attack;
        Value = damage;
        Description = $"攻击 {Value}";
    }
}