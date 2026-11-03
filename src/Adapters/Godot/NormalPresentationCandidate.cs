using Godot;
using Hmi.Core;
using System;

/// <summary>Independent normal-game combination candidate, never a Core/gameplay switch.</summary>
internal static class NormalPresentationCandidate
{
    // On by default only on this candidate branch. Explicit 0 exists for identical-authority A/B tests.
    // Controlled fixtures retain their own frozen opt-in paths and cannot silently inherit this candidate.
    internal static bool Enabled => !ControlledGaitDrill.Enabled && OS.GetEnvironment("NORMAL_PRESENTATION_CANDIDATE") != "0";
    internal static bool UsesAcceptedModel(string visualId) => Enabled && visualId == "hero_crit";
    internal static bool AppliesTo(EntityView view) => view.Kind == EntityKind.Player && UsesAcceptedModel(view.VisualId);
    internal static int StepCount(float elapsed,float horizontalRootDistance)
    {
        if(!float.IsFinite(elapsed)||elapsed<=0||!float.IsFinite(horizontalRootDistance)||horizontalRootDistance<0)
            throw new ArgumentOutOfRangeException("Invalid presentation step plan");
        double maxStep=1f/60f;
        // The first exponential-Lerp segment is the largest with a fixed target.
        // Keep margin below the frozen gait's .15 m guard; do not modify that guard.
        if(horizontalRootDistance>.12f)
            maxStep=Math.Min(maxStep,-Math.Log(1-.12/horizontalRootDistance)/22);
        int steps=(int)Math.Ceiling(elapsed/maxStep);
        if(steps<1||steps>120)throw new InvalidOperationException("Normal presentation displacement exceeds bounded stepping contract");
        return steps;
    }
}

