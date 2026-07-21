public static class TrainingEnemyFactory
{
    public static EnemyState CreateEnemy(EnemyType type, TrainingConfig config)
    {
        EnemyState enemy;

        switch (type)
        {
            case EnemyType.TrainingDummy:
                enemy = new EnemyState("TrainingDummy", config.EnemyHp);
                enemy.Shield = 0;
                enemy.CurrentIntent.SetNone();
                break;
            case EnemyType.TrainingBeast:
            default:
                enemy = new EnemyState("TrainingBeast", config.EnemyHp);
                enemy.Shield = config.EnemyShield;
                int intentValue = config.EnemyIntent == EnemyIntentType.Attack
                    ? config.EnemyAttack
                    : config.EnemyIntentValue;
                SetIntent(enemy, config.EnemyIntent, intentValue);
                break;
        }

        return enemy;
    }

    private static void SetIntent(EnemyState enemy, EnemyIntentType intentType, int value)
    {
        switch (intentType)
        {
            case EnemyIntentType.Attack:
                enemy.CurrentIntent.SetAttack(value);
                break;
            case EnemyIntentType.Defend:
                enemy.CurrentIntent.SetDefend(value);
                break;
            case EnemyIntentType.Buff:
                enemy.CurrentIntent.SetBuff(value);
                break;
            case EnemyIntentType.Debuff:
                enemy.CurrentIntent.SetDebuff(value);
                break;
        }
    }
}
