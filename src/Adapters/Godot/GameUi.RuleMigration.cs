#nullable enable annotations
using System;
using System.Linq;
using Godot;

namespace Hmi.GodotAdapter;

internal partial class GameUi
{
    // Presentation only. No migration protocol token is ever rendered by these controls.
    public Action? ResumeReviewRequested;
    public string ResumeReviewTitle = "";
    public void ShowRuleMigrationDialog(string title,string message,string confirmLabel,Action? confirm,Action? cancel=null)
    {
        BeginPersistenceModal(title,message,true,cancel);
        PersistenceAction("返回，不修改旧档",()=>cancel?.Invoke(),285,470,310,true);
        if(confirm!=null)PersistenceAction(confirmLabel,confirm,660,470,310,true,CandyTheme.Purple);
        (_overlay.GetChildren().OfType<Button>().FirstOrDefault())?.GrabFocus();
    }
}
