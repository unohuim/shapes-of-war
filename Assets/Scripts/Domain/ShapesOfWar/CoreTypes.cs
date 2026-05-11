namespace ShapesOfWar.Domain
{
    public enum BaseType
    {
        Wood,
        Stone,
        Metal
    }

    public enum UnitShape
    {
        Triangle,
        Square,
        Circle
    }

    public enum ResourceType
    {
        Wood,
        Stone,
        Metal
    }

    public enum ActionCardType
    {
        RaidBase,
        ResourceTheft,
        UnitKill,
        Counter
    }

    public enum ActionPhaseChoice
    {
        None,
        BattleRoyale,
        ActionCard,
        Pass
    }
}
