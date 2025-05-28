using System.Runtime.CompilerServices;

namespace ArgentiRotations.Common;

public static class RotationExtensions
{
    public static bool DebugMethod(Func<IAction?, bool> method, out IAction? act,
        [CallerMemberName] string methodName = "")
    {
        ArgumentNullException.ThrowIfNull(method);
        return RotationDebugManager.Debug(method, out act, methodName);
    }
}