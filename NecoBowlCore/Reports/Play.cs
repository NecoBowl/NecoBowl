using NecoBowl.Core.Machine;

namespace NecoBowl.Core.Reports;

public record Play : BaseReport
{
    private readonly PlayMachine Machine;
    public uint StepCount => Machine.StepCount;
    public bool IsFinished => Machine.IsFinished;
    public Playfield GetPlayfield() => new(Machine.GetField());

    internal Play(PlayMachine machine)
    {
        Machine = machine;
    }

    public Step Step()
    {
        return Machine.Step();
    }

    public void Step(uint count)
    {
        foreach (var i in Enumerable.Range(0, (int)count)) {
            Machine.Step();
        }
    }
}
