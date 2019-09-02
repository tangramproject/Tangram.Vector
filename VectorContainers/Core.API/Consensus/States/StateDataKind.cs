using System;
namespace Core.API.Consensus.States
{
    public enum StateDataKind
    {
        UnknownState,
        FinalState,
        HNVState,
        PreparedState,
        PrePreparedState,
        ViewState,
        ViewChangedState,
    }

    public interface StateData
    {
        ulong GetRound();
        StateDataKind SdKind();
    }

    public class Util
    {
        public static string GetStateDataKindString(StateDataKind s)
        {
            switch (s)
            {
                case StateDataKind.FinalState:
                    return "final";
                case StateDataKind.HNVState:
                    return "hnv";
                case StateDataKind.PreparedState:
                    return "prepared";
                case StateDataKind.PrePreparedState:
                    return "preprepared";
                case StateDataKind.UnknownState:
                    return "unknown";
                case StateDataKind.ViewState:
                    return "viewState";
                case StateDataKind.ViewChangedState:
                    return "viewchanged";
                default:
                    throw new Exception($"blockmania: unknown status data kind: {s}");
            }
        }
    }
}
