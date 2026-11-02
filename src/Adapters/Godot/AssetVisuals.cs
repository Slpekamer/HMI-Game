using Godot;
using System;
using System.Collections.Generic;

public sealed class AssetVisuals
{
    public static readonly string[] HeroIds = { "orbit", "crit", "honey", "summon", "bounce" };
    public static readonly Color[] Colors = { new("ff648a"), new("ed715c"), new("ffdb39"), new("e8aa6a"), new("438cf0") };
    public static readonly string[] HeroNames = { "绯绫", "卷卷", "布叮", "烤团", "脆蓝" };
    public static readonly string[] HeroTraits = { "糖果星环", "有限弹射", "蜂蜜领域", "独立伙伴", "近距冲击" };
    public static Color[] HeroColors => Colors;
    private readonly Dictionary<string, PackedScene> _scenes = new();
    // Drop our strong cache references only. GD.Load resources may be shared by live instances.
    public void ReleaseCachedReferences() => _scenes.Clear();

    public Node3D Create(string id)
    {
        if (!_scenes.TryGetValue(id, out var scene))
        {
            string path = "res://content/models/" + id + ".glb";
            if (ControlledGaitDrill.Enabled && id == "hero_crit")
                path = "res://experiments/crit-directional-gait/runtime/hero_crit-directional-candidate.glb";
            if (ResourceLoader.Exists(path)) { scene = GD.Load<PackedScene>(path); _scenes[id] = scene; }
        }
        if (scene != null) return scene.Instantiate<Node3D>();
        throw new InvalidOperationException("Required runtime model missing or invalid: res://content/models/" + id + ".glb. No placeholder or old-hero fallback is permitted.");
    }
    public static StandardMaterial3D Material(Color color) => new() { AlbedoColor = color, Roughness = 1, SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled };
    public static MeshInstance3D Box(Vector3 size, Vector3 position, Color color)
    {
        using var mesh = new BoxMesh { Size = size };
        using var material = Material(color);
        return new MeshInstance3D { Mesh = mesh, Position = position, MaterialOverride = material };
    }
    public static MeshInstance3D Disc(Color color)
    {
        using var mat = Material(color);
        mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        mat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        using var mesh = new CylinderMesh { TopRadius = 1, BottomRadius = 1, Height = .01f, RadialSegments = 48 };
        return new MeshInstance3D { Mesh = mesh, MaterialOverride = mat, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };
    }
}
