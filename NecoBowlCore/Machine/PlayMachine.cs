using NecoBowl.Core.Machine.Reports;
using NecoBowl.Core.Sport.Tactics;
using NecoBowl.Core.Tags;
using NLog;

namespace NecoBowl.Core.Machine;

/// <summary>Tracks the state of a <see cref="Playfield" /> as the units perform their actions each step.</summary>
internal class PlayMachine
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly Playfield Field;

    private readonly bool LogFieldAscii;
    private readonly PlayStepper PlayStepper;

    public bool IsFinished;

    public uint StepCount;

    internal PlayMachine(Playfield field, bool autoRun = false, bool logFieldAscii = true, bool preprocessUnits = false)
    {
        Field = field;
        PlayStepper = new(field);

        LogFieldAscii = logFieldAscii;

        if (preprocessUnits) {
            InitializeField();
        }

        if (autoRun) {
            StepToFinish();
        }
    }

    public bool CanEnd => IsFinished || StepCount >= 100;

    public ReadOnlyPlayfield GetField()
    {
        return Field.AsReadOnly();
    }

    public Step Step()
    {
        if (IsFinished) {
            throw new InvalidOperationException("cannot step a play that has finished");
        }

        if (LogFieldAscii && StepCount == 0) {
            LogFieldToAscii();
        }

        var result = new Step(PlayStepper.Process());

        foreach (var (substep, i) in result.Select((c, i) => (c, i))) {
            if (result.Count() > 1) {
                Logger.Debug($"Substep {i}:");
            }

            foreach (var mut in substep.Mutations) {
                Logger.Debug(mut);
            }

            foreach (var movement in substep.Movements.Where(kv => kv.Value.IsChange)) {
                Logger.Debug(movement);
            }
        }

        if (LogFieldAscii) {
            LogFieldToAscii();
        }

        StepCount++;

        return result;
    }

    public void Step(uint count)
    {
        for (var i = 0; i < count; i++) {
            Step();
        }
    }

    public void StepToFinish()
    {
        while (!CanEnd) {
            Step();
        }
    }

    private void InitializeField()
    {
        foreach (var (pos, unit) in Field.GetAllUnits()) {
            if (Field.FieldParameters.GetPlayerAffiliation(pos) == NecoPlayerRole.Defense) {
                unit.AddMod(new UnitMod.Rotate(4));
            }
        }

        Field[Field.FieldParameters.BallSpawnPoint] = Field[Field.FieldParameters.BallSpawnPoint]
            with {
                Unit = new(BuiltInDefinitions.Ball.Instance, default),
            };
    }

    public void LogFieldToAscii(string prefix = "> ")
    {
        Logger.Debug($"Logging field state\n{prefix}STEP COUNT {StepCount}\n{Field.ToAscii(prefix)}");
    }
}
