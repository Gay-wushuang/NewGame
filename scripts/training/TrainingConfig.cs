using Godot;

public enum RollMode
{
    Random,
    Fixed
}

public enum EnemyType
{
    TrainingDummy,
    TrainingBeast
}

public enum EnemyIntentType
{
    None,
    Attack,
    Defend,
    Buff,
    Debuff
}

public class TrainingConfig
{
    public int PlayerHp = 10;
    public int PlayerEnergy = 30;
    public int DiceCount = 2;
    public int DiceSides = 6;
    public RollMode RollMode = RollMode.Random;
    public int FixedRollValue = 1;
    public EnemyType EnemyType = EnemyType.TrainingBeast;

    public int EnemyHp = 20;
    public int EnemyAttack = 14;
    public int EnemyShield = 0;
    public EnemyIntentType EnemyIntent = EnemyIntentType.Attack;
    public int EnemyIntentValue = 14;

    public void ClampValues()
    {
        PlayerHp = Mathf.Max(1, PlayerHp);
        PlayerEnergy = Mathf.Max(1, PlayerEnergy);
        DiceCount = Mathf.Clamp(DiceCount, 1, 6);
        DiceSides = Mathf.Clamp(DiceSides, 4, 20);
        FixedRollValue = Mathf.Clamp(FixedRollValue, 1, DiceSides);

        EnemyHp = Mathf.Max(1, EnemyHp);
        EnemyAttack = Mathf.Max(0, EnemyAttack);
        EnemyShield = Mathf.Max(0, EnemyShield);
        EnemyIntentValue = Mathf.Max(0, EnemyIntentValue);
    }
}