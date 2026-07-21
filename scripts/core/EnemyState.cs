using Godot;
using System.Collections.Generic;

public class EnemyState
{
    public string Name;
    public int MaxHp;
    public int Hp;
    public int Shield;
    public EnemyIntent CurrentIntent;
    public Dictionary<StatusType, StatusInstance> Statuses = new Dictionary<StatusType, StatusInstance>();

    public EnemyState(string name, int maxHp)
    {
        Name = name;
        MaxHp = maxHp;
        Hp = maxHp;
        Shield = 0;
        CurrentIntent = new EnemyIntent();
    }

    public int GetStatusStacks(StatusType type)
    {
        return Statuses.TryGetValue(type, out StatusInstance status) ? status.Stacks : 0;
    }

    public void AddStatus(StatusType type, int stacks, int duration = 0)
    {
        if (!Statuses.ContainsKey(type))
        {
            Statuses[type] = new StatusInstance(type, stacks, duration);
        }
        else
        {
            Statuses[type].Stacks += stacks;
            if (duration > 0)
            {
                Statuses[type].Duration = duration;
            }
        }
    }

    public void ReduceStatus(StatusType type, int stacks)
    {
        if (Statuses.ContainsKey(type))
        {
            Statuses[type].Stacks = Mathf.Max(0, Statuses[type].Stacks - stacks);
            if (Statuses[type].Stacks == 0)
            {
                Statuses.Remove(type);
            }
        }
    }

    public int GetVulnerableStacks()
    {
        return GetStatusStacks(StatusType.Vulnerable);
    }

    public void AddVulnerable(int stacks)
    {
        AddStatus(StatusType.Vulnerable, stacks, 0);
    }

    public void ReduceVulnerable(int stacks)
    {
        ReduceStatus(StatusType.Vulnerable, stacks);
    }

    public int GetWeakStacks()
    {
        return GetStatusStacks(StatusType.Weak);
    }

    public void AddWeak(int stacks)
    {
        AddStatus(StatusType.Weak, stacks, 2);
    }

    public int GetSlowStacks()
    {
        return GetStatusStacks(StatusType.Slow);
    }

    public void AddSlow(int stacks)
    {
        AddStatus(StatusType.Slow, stacks, 2);
    }

    public int CalculateDamage(int baseDamage)
    {
        int vulnerable = GetVulnerableStacks();
        return baseDamage + vulnerable;
    }

    public int TakeDamage(int damage)
    {
        int finalDamage = CalculateDamage(Mathf.Max(0, damage));

        if (Shield > 0)
        {
            int shieldDamage = Mathf.Min(Shield, finalDamage);
            Shield -= shieldDamage;
            finalDamage -= shieldDamage;
        }

        Hp = Mathf.Max(0, Hp - finalDamage);

        return finalDamage;
    }

    public void EndTurn()
    {
        List<StatusType> expiredStatuses = new List<StatusType>();

        foreach (var pair in Statuses)
        {
            if (pair.Value.Duration > 0)
            {
                pair.Value.Duration--;
                if (pair.Value.Duration <= 0)
                {
                    expiredStatuses.Add(pair.Key);
                }
            }
            else if (pair.Key == StatusType.Vulnerable)
            {
                pair.Value.Stacks = Mathf.Max(0, pair.Value.Stacks - 1);
                if (pair.Value.Stacks <= 0)
                {
                    expiredStatuses.Add(pair.Key);
                }
            }
        }

        foreach (var statusType in expiredStatuses)
        {
            if (Statuses.ContainsKey(statusType) && Statuses[statusType].Stacks <= 0)
            {
                Statuses.Remove(statusType);
            }
        }
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
        None,
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
        Type = IntentType.None;
        Value = 0;
        Description = "None";
    }

    public void SetNone()
    {
        Type = IntentType.None;
        Value = 0;
        Description = "None";
    }

    public void SetAttack(int damage)
    {
        Type = IntentType.Attack;
        Value = damage;
        Description = $"Attack {Value}";
    }

    public void SetDefend(int shield)
    {
        Type = IntentType.Defend;
        Value = shield;
        Description = $"Defend {shield}";
    }

    public void SetBuff(int value)
    {
        Type = IntentType.Buff;
        Value = value;
        Description = $"Buff {value}";
    }

    public void SetDebuff(int value)
    {
        Type = IntentType.Debuff;
        Value = value;
        Description = $"Debuff {value}";
    }
}
