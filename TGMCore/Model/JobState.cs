// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

namespace TGMCore.Model
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
