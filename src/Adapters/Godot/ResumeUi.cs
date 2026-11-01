#nullable enable
namespace Hmi.GodotAdapter;
/// <summary>Coordinator-supplied display state. No file or gameplay authority.</summary>
internal sealed class ResumeUi
{
    public bool CanContinue { get; }
    public string Summary { get; }
    public string RollbackWarning { get; }
    public string SaveStatus { get; }
    public string ErrorText { get; }
    public bool CanRetry { get; }
    public ResumeUi(bool canContinue=false,string summary="",string rollbackWarning="",string saveStatus="",string errorText="",bool canRetry=false)
    {
        CanContinue=canContinue;Summary=summary??"";RollbackWarning=rollbackWarning??"";
        SaveStatus=saveStatus??"";ErrorText=errorText??"";CanRetry=canRetry;
    }
}
