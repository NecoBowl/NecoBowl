using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlCore.Model;
using neco_soft.NecoBowlCore.Tactics;
using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlCore.Input;

public class NecoFieldInformation
{
    private readonly ReadOnlyNecoField Field;

    internal NecoFieldInformation(ReadOnlyNecoField field)
    {
        Field = field;
    }

    public NecoSpaceInformation this[int x, int y] => this[(x, y)];
    public NecoSpaceInformation this[(int, int) coords] => Contents(coords);

    public NecoFieldParameters FieldParameters => Field.FieldParameters;

    public NecoSpaceInformation Contents((int, int) coords)
    {
        return new(Field[coords], coords, Field.FieldParameters.GetPlayerAffiliation(coords));
    }

    public (int x, int y) GetBounds()
    {
        return Field.GetBounds();
    }
}

public class NecoSpaceInformation
{
    public readonly (int X, int Y) Coords;
    public readonly NecoPlayerRole? PlayerRole;
    private readonly NecoSpaceContents Contents;

    internal NecoSpaceInformation(NecoSpaceContents contents, (int X, int Y) coords, NecoPlayerRole? playerRole)
    {
        Contents = contents;
        Coords = coords;
        PlayerRole = playerRole;
    }

    public NecoUnitInformation? Unit => Contents.Unit is null ? null : new(Contents.Unit);
}

public class NecoUnitInformation
{
    private readonly NecoUnit Unit;

    internal NecoUnitInformation(NecoUnit unit)
    {
        Unit = unit;
    }

    public NecoUnitModel UnitModel => Unit.UnitModel;
    public NecoUnitId Id => Unit.Id;
    public int Power => Unit.Power;
    public int MaxHealth => Unit.MaxHealth;
    public int CurrentHealth => Unit.CurrentHealth;
    public string Name => Unit.FullName;

    public IReadOnlyList<NecoUnitActionInformation> Actions
        => Unit.ActionStack.Select(a => new NecoUnitActionInformation(a)).ToList();

    public IReadOnlyList<NecoUnitInformation> Inventory
        => Unit.Inventory.Select(u => new NecoUnitInformation(u)).ToList();

    public IReadOnlyList<NecoUnitTag> Tags => Unit.Tags.AsReadOnly();
    public IReadOnlyList<NecoUnitMod> Mods => Unit.Mods.AsReadOnly();
}

public class NecoUnitActionInformation
{
    private readonly NecoUnitAction Action;

    internal NecoUnitActionInformation(NecoUnitAction action)
    {
        Action = action;
    }

    public string Description => Action.ToString();
}

public class NecoPlayInformation
{
    private readonly NecoPlay Play;

    internal NecoPlayInformation(NecoPlay play)
    {
        Play = play;
    }

    public uint StepCount => Play.StepCount;
    public bool IsFinished => Play.IsFinished;
    public NecoFieldInformation Field => new(Play.GetField());

    public IEnumerable<NecoPlayfieldMutation> Step()
    {
        return Play.Step();
    }

    public void StepToFinish()
    {
        Play.StepToFinish();
    }
}

public class NecoPlanInformation
{
    private readonly NecoPlan Plan;

    internal NecoPlanInformation(NecoPlan plan)
    {
        Plan = plan;
    }

    public IEnumerable<NecoPlan.CardPlay> CardPlays
        => Plan.GetCardPlays();
}

public class NecoTurnInformation
{
    private readonly NecoTurn Turn;

    internal NecoTurnInformation(NecoTurn turn)
    {
        Turn = turn;
    }

    public NecoPlan.CardPlay? CardPlayAt((int x, int y) coords)
    {
        foreach (var role in Enum.GetValues<NecoPlayerRole>()) {
            var cardPlay = Turn.CardPlaysByRole[role].SingleOrDefault(p => p.Position == coords);
            if (cardPlay is not null) return cardPlay;
        }

        return null;
    }
}
