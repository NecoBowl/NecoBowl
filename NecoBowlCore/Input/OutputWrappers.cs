using NecoBowl.Core.Machine;
using NecoBowl.Core.Model;
using NecoBowl.Core.Sport.Play;
using NecoBowl.Core.Sport.Tactics;
using NecoBowl.Core.Tags;

namespace NecoBowl.Core.Input;

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

    public NecoUnitInformation GetUnit(NecoUnitId uid)
    {
        return new(Field.GetUnit(uid));
    }

    public NecoUnitInformation? LookupUnit(string shortUid)
    {
        var unit = Field.LookupUnit(shortUid);
        return unit is null ? null : new(unit);
    }

    public Vector2i GetUnitPosition(NecoUnitId uid, bool includeInventories = false)
    {
        return Field.GetUnitPosition(uid, includeInventories);
    }

    public IReadOnlyList<Unit> GetGraveyard()
    {
        return Field.GetGraveyard();
    }

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
    private readonly NecoSpaceContents Contents;
    public readonly (int X, int Y) Coords;
    public readonly NecoPlayerRole? PlayerRole;

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
    private readonly Unit Unit;

    internal NecoUnitInformation(Unit unit)
    {
        Unit = unit;
    }

    public NecoUnitModel UnitModel => Unit.UnitModel;
    public NecoUnitId Id => Unit.Id;
    public int Power => Unit.Power;
    public int MaxHealth => Unit.MaxHealth;
    public int CurrentHealth => Unit.CurrentHealth;

    public string FullName => Unit.FullName;
    public NecoPlayerId OwnerId => Unit.OwnerId;

    public IReadOnlyList<NecoUnitActionInformation> Actions
        => Unit.ActionStack.Select(a => new NecoUnitActionInformation(a)).ToList();

    public IReadOnlyList<NecoUnitInformation> Inventory
        => Unit.Inventory.Select(u => new NecoUnitInformation(u)).ToList();

    public IReadOnlyList<NecoUnitTag> Tags => Unit.Tags.AsReadOnly();

    public T GetMod<T>() where T : NecoUnitMod, new()
    {
        return Unit.GetMod<T>();
    }
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
    private readonly PlayMachine PlayMachine;

    internal NecoPlayInformation(PlayMachine playMachine)
    {
        PlayMachine = playMachine;
    }

    public uint StepCount => PlayMachine.StepCount;
    public bool IsFinished => PlayMachine.IsFinished;
    public NecoFieldInformation Field => new(PlayMachine.GetField());

    public PlayStepResult Step()
    {
        return PlayMachine.Step();
    }

    public void Step(uint count)
    {
        PlayMachine.Step(count);
    }

    public void StepToFinish()
    {
        PlayMachine.StepToFinish();
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
            if (cardPlay is not null) {
                return cardPlay;
            }
        }

        return null;
    }
}
