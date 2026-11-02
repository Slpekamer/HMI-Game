using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Fixture-only CPU readback of rigid sole material points, not bone-center contact.</summary>
internal sealed class ControlledGaitSkin
{
    private sealed record Sample(Skeleton3D Skeleton, int Bone, Transform3D Bind, Vector3 Vertex, string Side);
    private readonly List<Sample> _samples = new();
    public ControlledGaitSkin(Node root)
    {
        void Walk(Node node)
        {
            if (node is MeshInstance3D mesh && mesh.Mesh != null && mesh.Skin != null)
            {
                var skeleton = mesh.GetNodeOrNull<Skeleton3D>(mesh.Skeleton);
                if (skeleton != null)
                {
                    var candidates = new List<Sample>();
                    for (int surface = 0; surface < mesh.Mesh.GetSurfaceCount(); surface++)
                    {
                        var arrays = mesh.Mesh.SurfaceGetArrays(surface);
                        var vertices = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
                        var bones = arrays[(int)Mesh.ArrayType.Bones].AsInt32Array();
                        var weights = arrays[(int)Mesh.ArrayType.Weights].AsFloat32Array();
                        if (vertices.Length == 0 || bones.Length == 0) continue;
                        int influences = bones.Length / vertices.Length;
                        for (int v = 0; v < vertices.Length; v++)
                            for (int w = 0; w < influences; w++)
                            {
                                int index = v * influences + w;
                                if (weights[index] < .99999f) continue;
                                int bind = bones[index];
                                int bone = mesh.Skin.GetBindBone(bind);
                                if (bone < 0) bone = skeleton.FindBone(mesh.Skin.GetBindName(bind));
                                if (bone < 0) throw new InvalidOperationException("Unresolved skin bind");
                                string name = skeleton.GetBoneName(bone);
                                if (name == "Foot_L" || name == "Foot_R")
                                    candidates.Add(new Sample(skeleton, bone, mesh.Skin.GetBindPose(bind), vertices[v], name));
                            }
                    }
                    foreach (string side in new[] { "Foot_L", "Foot_R" })
                    {
                        var foot = candidates.Where(p => p.Side == side).ToArray();
                        if (foot.Length == 0) continue;
                        // Exact source bottom layer; not pose-selected and unchanged for every frame/A/B.
                        float minimum = foot.Min(p => p.Vertex.Y);
                        _samples.AddRange(foot.Where(p => p.Vertex.Y <= minimum + .0001f));
                    }
                }
            }
            foreach (Node child in node.GetChildren()) Walk(child);
        }
        Walk(root);
        if (_samples.Count == 0) throw new InvalidOperationException("No rigid sole source vertices found");
    }
    public object Read()
    {
        var feet = new List<object>();
        foreach (string side in new[] { "Foot_L", "Foot_R" })
        {
            var points = _samples.Where(p => p.Side == side).Select(p =>
                p.Skeleton.GlobalTransform * (p.Skeleton.GetBoneGlobalPose(p.Bone) * (p.Bind * p.Vertex))).ToArray();
            if (points.Length == 0) throw new InvalidOperationException("Missing side sole points");
            var mean = points.Aggregate(Vector3.Zero, (a, b) => a + b) / points.Length;
            feet.Add(new { side, count = points.Length, minY = points.Min(p => p.Y), maxY = points.Max(p => p.Y),
                centroid = new[] { mean.X, mean.Y, mean.Z },
                points = points.Select(p => new[] { p.X, p.Y, p.Z }).ToArray() });
        }
        return feet;
    }
}
