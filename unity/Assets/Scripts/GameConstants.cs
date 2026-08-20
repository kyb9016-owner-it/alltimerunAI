/// <summary>
/// 오케스트레이터 / ACCEPTANCE와 동기화할 게임 상수.
/// 변경 시 docs/UNITY_ORCHESTRATOR_SYNC.md 및 orchestrator/game_state.py 와 일치 여부 확인.
/// </summary>
public static class GameConstants
{
    public const float TickIntervalSec = 1.0f;

    // Offline earnings (ACCEPTANCE)
    public const long OfflineMaxSeconds = 8L * 3600L;
    public const float OfflineMultiplier = 0.6f;

    // Base resource tuning
    public const float EnergyGenPerTick = 2.0f;
    public const float LearnEnergyCost = 1.5f;
    public const float BaseDataPerTick = 3.0f;
    public const float BaseMoneyPerTick = 0.8f;
    public const float BaseEventChance = 0.03f;

    // Evolution (MVP: Core -> ProtoHuman)
    public const int EvolutionLevelMin = 10;
    public const float EvolutionDataMin = 2500f;
    public const float EvolutionMoneyMin = 1500f;
    public const float EvolutionIntelligenceMult = 1.25f;
    public const float EvolutionEventRiskMult = 1.10f;

    // Upgrade caps
    public const int MaxUpgradeLevel = 5;

    // Upgrade costs (Money) per level 0..4
    public static readonly float[] GeneratorCosts = { 30f, 80f, 160f, 300f, 500f };
    public static readonly float[] BatteryCosts = { 20f, 60f, 120f, 220f, 380f };
    public static readonly float[] ModelTrainingCosts = { 40f, 120f, 250f, 450f, 700f };
    public static readonly float[] OptimizationCosts = { 60f, 160f, 330f, 600f, 900f };
    public static readonly float[] OpsCosts = { 50f, 140f, 300f, 520f, 800f };

    // Events
    public const float EventServerOverheatEnergy = 20f;
    public const float EventSecurityBreachMoney = 50f;
    public const float EventFirewallReductionPerLevel = 0.10f;
    public const float EventEfficiencyBoostIntelligence = 0.05f;

    // XP (Unity MVP linear; orchestrator uses tiered table in level_table.py)
    public static float XpToNextLevel(int level)
    {
        return 50f + (level - 1) * 25f;
    }
}
