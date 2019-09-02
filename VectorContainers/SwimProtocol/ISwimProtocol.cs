using System.Collections.Generic;

namespace SwimProtocol
{
    public interface ISwimProtocol
    {
        IEnumerable<ISwimNode> Members { get; }
    }
}