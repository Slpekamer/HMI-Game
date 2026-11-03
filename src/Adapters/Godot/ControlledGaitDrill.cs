using System;
using System.Collections.Generic;

/// <summary>Opt-in asset fixture settings, never enabled by normal game startup.</summary>
internal static class ControlledGaitDrill
{
    public static bool Enabled;
    public static bool Candidate;
    public static bool WeightTransfer;
    public static bool ProportionCandidate;
    public static bool DisplacementRun;
    public static bool TurningRun;
    public static string Clip = "StrafeR";
    public static float Yaw;
    public static readonly HashSet<string> LowerBones = new(StringComparer.Ordinal)
    {
        "Pelvis", "Thigh_L", "Shin_L", "Foot_L", "Thigh_R", "Shin_R", "Foot_R"
    };
}
