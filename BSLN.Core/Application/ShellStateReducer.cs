using BSLN.Core.Domain;

namespace BSLN.Core.Application;

public sealed class ShellStateReducer
{
    public ShellState Reduce(ShellState state, SemanticInputAction action)
    {
        if (state.Games.Count == 0)
        {
            return state;
        }

        var nextIndex = action switch
        {
            SemanticInputAction.MoveLeft => Math.Max(0, state.SelectedIndex - 1),
            SemanticInputAction.MoveRight => Math.Min(state.Games.Count - 1, state.SelectedIndex + 1),
            SemanticInputAction.PageLeft => Math.Max(0, state.SelectedIndex - 4),
            SemanticInputAction.PageRight => Math.Min(state.Games.Count - 1, state.SelectedIndex + 4),
            _ => state.SelectedIndex,
        };

        return state with
        {
            SelectedIndex = nextIndex,
            ErrorMessage = null,
        };
    }
}
