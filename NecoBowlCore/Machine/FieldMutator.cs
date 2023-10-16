using System.Collections.Immutable;

namespace NecoBowl.Core.Machine;

internal class FieldMutator
{
    private readonly ImmutableList<BaseMutation> InputMutations;
    private readonly IPlayfieldChangeReceiver MutationReceiver;
    private readonly Playfield Playfield;

    public FieldMutator(IPlayfieldChangeReceiver receiver, Playfield playfield, IEnumerable<BaseMutation> mutations)
    {
        MutationReceiver = receiver;
        Playfield = playfield;
        InputMutations = mutations.ToImmutableList();
    }

    public void MutateField()
    {
        var baseMutations = InputMutations.ToList();

        // RUN MUTATION FUNCTIONS

        // First run the Prepare() and remove the mutation if it wants to cancel
        for (var i = baseMutations.Count - 1; i >= 0; i--) {
            // i can't believe i have to use this stupid loop
            if (baseMutations[i].Prepare(MutationReceiver, Playfield.AsReadOnly())) {
                baseMutations.RemoveAt(i);
            }
        }

        // Then run the mutate passes
        foreach (var func in BaseMutation.ExecutionOrder) {
            foreach (var mutation in baseMutations) {
                func.Invoke(mutation, MutationReceiver, Playfield);
            }
        }

        foreach (var (pos, unit) in Playfield.GetAllUnits()) {
            foreach (var mutation in baseMutations) {
                var reaction = unit.Reactions.SingleOrDefault(r => r.MutationType == mutation.GetType());
                if (reaction is { }) {
                    foreach (var reactionMutation in reaction.Reaction(
                                 new(unit), new(Playfield.AsReadOnly()), mutation)) {
                        MutationReceiver.BufferMutation(reactionMutation);
                    }
                }
            }
        }

        foreach (var mut in baseMutations) {
            foreach (var resultantMut in mut.GetResultantMutations(Playfield.AsReadOnly())) {
                MutationReceiver.BufferMutation(resultantMut);
            }
        }
    }
}
