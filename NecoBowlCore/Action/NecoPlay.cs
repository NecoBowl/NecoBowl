using System.Diagnostics;

using NLog;
using NLog.Fluent;

namespace neco_soft.NecoBowlCore.Action;

/// <summary>
/// Tracks the state of a <see cref="NecoField"/> as the units perform their actions each step.
/// </summary>
public class NecoPlay
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly bool LogFieldAscii;
    public NecoField Field { get; private set; }
    private readonly NecoPlayStepper Stepper;

    public int StepCount { get; private set; }
    private readonly NecoField OriginalField;
    public bool IsFinished => StepCount > 100;

    public NecoPlay(NecoField field, bool autoRun = false, bool logFieldAscii = true)
    {
        LogFieldAscii = logFieldAscii;
        Field = field;
        Stepper = new NecoPlayStepper(this);

        OriginalField = new NecoField(Field, deep: false);

        if (autoRun) {
            StepToFinish(); 
        }
    }

    public void Step()
    {
        if (IsFinished)
            throw new InvalidOperationException("cannot step a play that has finished");

        if (LogFieldAscii && StepCount == 0) {
            LogFieldToAscii();
        }
        
        Stepper.Step();
        Field = Stepper.Field;
        StepCount++;

        if (LogFieldAscii) {
            LogFieldToAscii();
        } 
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

    public void LogFieldToAscii(string prefix = "> ")
    {
        Logger.Debug($"Logging field state\n{prefix}STEP COUNT {StepCount}\n{Field.ToAscii(prefix)}");
    }
}