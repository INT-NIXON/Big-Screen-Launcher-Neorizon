using BSLN.Core.Domain;

namespace BSLN.Core.Input;

public sealed class ControllerActionTranslator
{
    private const short ThumbDeadzone = 12000;
    private static readonly TimeSpan InitialRepeatDelay = TimeSpan.FromMilliseconds(350);
    private static readonly TimeSpan RepeatInterval = TimeSpan.FromMilliseconds(120);

    private readonly Dictionary<SemanticInputAction, DateTimeOffset> _nextRepeatTimes = [];

    public IReadOnlyList<SemanticInputAction> Translate(ControllerSnapshot previous, ControllerSnapshot current)
    {
        if (!current.IsConnected)
        {
            _nextRepeatTimes.Clear();
            return [];
        }

        var actions = new List<SemanticInputAction>();
        var now = DateTimeOffset.UtcNow;

        AddRepeatableAction(actions, now, SemanticInputAction.MoveLeft, current.DPadLeft || current.LeftThumbX <= -ThumbDeadzone);
        AddRepeatableAction(actions, now, SemanticInputAction.MoveRight, current.DPadRight || current.LeftThumbX >= ThumbDeadzone);
        AddRepeatableAction(actions, now, SemanticInputAction.MoveUp, current.DPadUp || current.LeftThumbY >= ThumbDeadzone);
        AddRepeatableAction(actions, now, SemanticInputAction.MoveDown, current.DPadDown || current.LeftThumbY <= -ThumbDeadzone);
        AddEdge(actions, current.ButtonSouth && !previous.ButtonSouth, SemanticInputAction.Accept);
        AddEdge(actions, current.ButtonEast && !previous.ButtonEast, SemanticInputAction.Back);
        AddEdge(actions, current.ButtonMenu && !previous.ButtonMenu, SemanticInputAction.Menu);
        AddEdge(actions, current.ButtonGuide && !previous.ButtonGuide, SemanticInputAction.Menu);
        AddRepeatableAction(actions, now, SemanticInputAction.PageLeft, current.LeftShoulder);
        AddRepeatableAction(actions, now, SemanticInputAction.PageRight, current.RightShoulder);

        return actions;
    }

    private void AddRepeatableAction(List<SemanticInputAction> actions, DateTimeOffset now, SemanticInputAction action, bool isPressed)
    {
        if (!isPressed)
        {
            _nextRepeatTimes.Remove(action);
            return;
        }

        if (!_nextRepeatTimes.TryGetValue(action, out var nextRepeatTime))
        {
            actions.Add(action);
            _nextRepeatTimes[action] = now + InitialRepeatDelay;
            return;
        }

        if (now < nextRepeatTime)
        {
            return;
        }

        actions.Add(action);
        _nextRepeatTimes[action] = now + RepeatInterval;
    }

    private static void AddEdge(List<SemanticInputAction> actions, bool condition, SemanticInputAction action)
    {
        if (condition)
        {
            actions.Add(action);
        }
    }
}
