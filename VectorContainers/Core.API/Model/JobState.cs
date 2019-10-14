using System;
namespace Core.API.Model
{
    /// <summary>
    /// 
    /// </summary>
    public enum JobState
    {
        Queued,
        Started,
        Running,
        Dead,
        Pending,
        Partial,
        Dialling,
        Answered,
        Blockmainia,
        Polished
    }
}
