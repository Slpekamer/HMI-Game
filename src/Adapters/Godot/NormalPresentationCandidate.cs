using Godot;
using Hmi.Core;

/// <summary>Independent normal-game combination candidate, never a Core/gameplay switch.</summary>
internal static class NormalPresentationCandidate
{
    // On by default only on this candidate branch. Explicit 0 exists for identical-authority A/B tests.
    // Controlled fixtures retain their own frozen opt-in paths and cannot silently inherit this candidate.
    internal static bool Enabled => !ControlledGaitDrill.Enabled && OS.GetEnvironment("NORMAL_PRESENTATION_CANDIDATE") != "0";
    internal static bool UsesAcceptedModel(string visualId) => Enabled && visualId == "hero_crit";
    internal static bool AppliesTo(EntityView view) => view.Kind == EntityKind.Player && UsesAcceptedModel(view.VisualId);
}
