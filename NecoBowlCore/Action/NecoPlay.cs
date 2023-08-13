using System;

using neco_soft.NecoBowlCore.Tactics;
using neco_soft.NecoBowlCore.Tags;

using NLog;

namespace neco_soft.NecoBowlCore.Action;

/// <summary>
/// Tracks the state of a <see cref="NecoField"/> as the units perform their actions each step.
/// </summary>
public class NecoPlay
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly bool LogFieldAscii;

    public uint StepCount = 0;
    
    private readonly NecoField Field;
    private readonly NecoPlayStepperNew PlayStepper;

    public bool IsFinished => StepCount > 100;

    public ReadOnlyNecoField GetField() => Field.AsReadOnly();

    internal NecoPlay(NecoField field, bool autoRun = false, bool logFieldAscii = true, bool preprocessUnits = false)
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

    public IEnumerable<NecoPlayfieldMutation> Step()
    {
        if (IsFinished)
            throw new InvalidOperationException("cannot step a play that has finished");

        if (LogFieldAscii && StepCount == 0) {
            LogFieldToAscii();
        }

        var step = PlayStepper.Process();

        if (LogFieldAscii) {
            LogFieldToAscii();
        }

        return step;
    }

    public void Step(uint count)
    {
        for (var i = 0; i < count; i++) {
            Step();
        }
    }

    public void StepToFinish()
    {
        while (!IsFinished) {
            Step();
        }
    }

    private void InitializeField()
    {
        foreach (var (pos, unit) in Field.GetAllUnits()) {
            if (Field.FieldParameters.GetPlayerAffiliation(pos) == NecoPlayerRole.Defense)
                unit.Mods.Add(new NecoUnitMod.Rotate(4)); 
        }
        
        Field[Field.FieldParameters.BallSpawnPoint] = Field[Field.FieldParameters.BallSpawnPoint] 
            with { Unit = new NecoUnit(BuiltInDefinitions.Ball.Instance, default) };
    }

    public void LogFieldToAscii(string prefix = "> ")
    {
        Logger.Debug($"Logging field state\n{prefix}STEP COUNT {StepCount}\n{Field.ToAscii(prefix)}");
    }
}