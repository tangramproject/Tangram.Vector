using System;
using System.Collections.Generic;
using System.Text;

namespace SwimProtocol
{
    public interface IHasSubject
    {
        ISwimNode SubjectNode { get; set; }
    }
}
