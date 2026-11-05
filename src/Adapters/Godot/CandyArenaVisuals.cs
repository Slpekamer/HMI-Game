using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Immutable, presentation-only candy scenery. No physics bodies, gameplay reads,
/// clocks, or random state. Meshes/materials belong to this host's instance batches.
/// </summary>
public static class CandyArenaVisuals
{
    private static readonly Color[] BoundaryPalette = { new("b9a5dd"), new("83b9da"), new("edb3cb"), new("efd18d") };
    private static readonly Color[] VillagePalette = { new("f7c3d5"), new("b2ddec"), new("d2b9ed"), new("ffe1a1") };
    private static readonly Color Cream = new("fff3da");

    public static void BuildFloor(Node3D host)
    {
        // This shallow, opaque inlay sits below the existing petals and honey.
        // It is flat geometry, not a new platform, collider, target or radius cue.
        var material = new StandardMaterial3D
        {
            AlbedoTexture = GD.Load<Texture2D>("res://content/environment/arena_icing.svg"),
            Roughness = 1, SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled
        };
        var mesh = new PlaneMesh { Size = new Vector2(28, 16) };
        host.AddChild(new MeshInstance3D { Name = "CandyFloorInlay", Mesh = mesh, Position = new Vector3(0, .0005f, 0),
            MaterialOverride = material, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
    }

    public static void BuildWaferLayers(Node3D host, Vector3 corner, float width, float length)
    {
        string location = corner.X.ToString("R", System.Globalization.CultureInfo.InvariantCulture).Replace(".", "_");
        var batches = new Batches(host, "CandyWaferLayers_" + location);
        // Thin icing reveals sit 1 mm beyond the side faces to prevent z-fighting.
        // No top surface, obstacle height, endpoint, or collision AABB changes.
        batches.Define("cream", RoundedBox(new(width + .002f, .085f, length - .04f), .04f), Cream);
        batches.Define("caramel", RoundedBox(new(width + .002f, .065f, length - .04f), .03f), new("e4bd88"));
        var center = corner + new Vector3(width / 2, 0, length / 2);
        batches.Add("cream", center + new Vector3(0, .29f, 0));
        batches.Add("cream", center + new Vector3(0, .62f, 0));
        batches.Add("caramel", center + new Vector3(0, .13f, 0));
        batches.Commit();
    }

    public static void BuildBoundary(Node3D host)
    {
        var batches = new Batches(host, "CandyBoundary");
        // Keep the original centers, maximum extents, palette order and height.
        // A rounded silhouette replaces hard cubes without expanding the play area.
        for (int color = 0; color < 4; color++)
            batches.Define("bonbon" + color, RoundedBox(new(.95f, .75f, .5f), .12f), BoundaryPalette[color]);
        for (int x = -14; x <= 14; x++)
        {
            batches.Add("bonbon" + ((x + 14) % 4), new(x, .25f, -8.3f));
            batches.Add("bonbon" + ((x + 15) % 4), new(x, .25f, 8.3f));
        }
        for (int z = -7; z <= 7; z++)
        {
            batches.Add("bonbon" + ((z + 7) % 4), new(-14.3f, .25f, z), Vector3.One, Mathf.Pi / 2);
            batches.Add("bonbon" + ((z + 8) % 4), new(14.3f, .25f, z), Vector3.One, Mathf.Pi / 2);
        }
        batches.Commit();
    }

    public static void BuildVillage(Node3D host)
    {
        var batches = new Batches(host, "CandyVillage");
        for (int color = 0; color < 4; color++)
        {
            batches.Define("cake" + color, RoundedBox(new(2.18f, 1, 2.18f), .2f), VillagePalette[color]);
            batches.Define("icing" + color, new SphereMesh { Radius = 1.25f, Height = 1.4f, RadialSegments = 24, Rings = 12 }, VillagePalette[color]);
            batches.Define("sweet" + color, new CapsuleMesh { Radius = .065f, Height = .28f, RadialSegments = 8, Rings = 2 }, VillagePalette[color]);
        }
        batches.Define("creamBand", RoundedBox(new(2.28f, .16f, 2.28f), .075f), Cream);
        batches.Define("biscuitFoot", RoundedBox(new(2.36f, .24f, 2.36f), .1f), new("e8b879"));
        batches.Define("waferStripe", RoundedBox(new(.065f, 1, .025f), .012f), new("f3dcb7"));
        batches.Define("windowRim", new CylinderMesh { TopRadius = .23f, BottomRadius = .23f, Height = .07f, RadialSegments = 24 }, Cream);
        batches.Define("windowGlass", new CylinderMesh { TopRadius = .15f, BottomRadius = .15f, Height = .075f, RadialSegments = 24 }, new("91b4d4"));
        batches.Define("windowGlint", new SphereMesh { Radius = .045f, Height = .09f, RadialSegments = 8, Rings = 4 }, new("fffaf0"));
        batches.Define("roofPearl", new SphereMesh { Radius = .13f, Height = .26f, RadialSegments = 12, Rings = 6 }, Cream);

        for (int side = -1; side <= 1; side += 2)
        for (int i = 0; i < 9; i++)
        {
            // Original village positions and roof envelopes, wholly outside the arena.
            float x = -17 + i * 4;
            float z = side * (12 + (i % 3) * 1.3f);
            float height = 1.5f + (i % 4) * .6f;
            int color = i % 4;
            batches.Add("cake" + color, new(x, height / 2 - .05f, z), new(1, height, 1));
            batches.Add("biscuitFoot", new(x, .07f, z));
            batches.Add("creamBand", new(x, height * .38f, z));
            batches.Add("creamBand", new(x, height - .18f, z));
            batches.Add("icing" + ((i + 1) % 4), new(x, height, z));

            // Quiet wafer grooves and inset portholes make these read as confectionery
            // houses rather than giant untextured cubes. Details face the playfield.
            float front = z - side * 1.102f;
            for (int stripe = -3; stripe <= 3; stripe++)
                batches.Add("waferStripe", new(x + stripe * .24f, height * .18f, front), new(1, height * .24f, 1));
            var windowBasis = new Basis(Vector3.Right, Mathf.Pi / 2);
            batches.AddTransform("windowRim", new Transform3D(windowBasis, new(x, height * .65f, front)));
            batches.AddTransform("windowGlass", new Transform3D(windowBasis, new(x, height * .65f, front - side * .025f)));
            batches.Add("windowGlint", new(x - .045f, height * .65f + .055f, front - side * .065f), new(1, 1, .35f));

            // Three cream pearls and six sprinkles only, with no flashing/animated
            // highlights. The central combat floor remains visually quiet.
            for (int pearl = -1; pearl <= 1; pearl++)
                batches.Add("roofPearl", new(x + pearl * .4f, height + .54f, z));
            for (int sprinkle = 0; sprinkle < 6; sprinkle++)
            {
                float angle = sprinkle * Mathf.Tau / 6 + i * .3f;
                var p = new Vector3(x + Mathf.Cos(angle) * .62f, height + .48f, z + Mathf.Sin(angle) * .62f);
                var basis = new Basis(Vector3.Up, angle) * new Basis(Vector3.Forward, .85f);
                batches.AddTransform("sweet" + ((i + sprinkle + 2) % 4), new Transform3D(basis, p));
            }
        }
        batches.Commit();
    }

    // Project a subdivided box onto its inner cuboid plus a sphere radius. This
    // provides real rounded normals/geometry, without textures or a custom shader.
    internal static ArrayMesh RoundedBox(Vector3 size, float radius)
    {
        if (radius <= 0 || radius * 2 >= Mathf.Min(size.X, Mathf.Min(size.Y, size.Z)))
            throw new ArgumentOutOfRangeException(nameof(radius));
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var indices = new List<int>();
        var half = size / 2;
        var inner = half - Vector3.One * radius;
        float[] Axis(float h) => new[] { -h, -h + radius * .293f, -h + radius, h - radius, h - radius * .293f, h };
        void Face(int axis, float sign)
        {
            int u = (axis + 1) % 3, v = (axis + 2) % 3;
            var us = Axis(half[u]); var vs = Axis(half[v]);
            int start = vertices.Count;
            for (int y = 0; y < 6; y++)
            for (int x = 0; x < 6; x++)
            {
                var p = Vector3.Zero; p[axis] = half[axis] * sign; p[u] = us[x]; p[v] = vs[y];
                var q = p.Clamp(-inner, inner);
                var normal = (p - q).Normalized();
                vertices.Add(q + normal * radius); normals.Add(normal);
            }
            for (int y = 0; y < 5; y++)
            for (int x = 0; x < 5; x++)
            {
                int a = start + y * 6 + x, b = a + 1, c = a + 6, d = c + 1;
                // Godot's front faces use clockwise winding.
                if (sign > 0) { indices.AddRange(new[] { a, c, b, b, c, d }); }
                else { indices.AddRange(new[] { a, b, c, b, d, c }); }
            }
        }
        for (int axis = 0; axis < 3; axis++) { Face(axis, -1); Face(axis, 1); }
        var arrays = new Godot.Collections.Array(); arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
        arrays[(int)Mesh.ArrayType.Normal] = normals.ToArray();
        arrays[(int)Mesh.ArrayType.Index] = indices.ToArray();
        var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays); return mesh;
    }

    private sealed class Batches
    {
        private sealed class Batch
        {
            public Mesh Mesh;
            public readonly List<Transform3D> Transforms = new();
        }
        private readonly Node3D _host;
        private readonly string _prefix;
        private readonly Dictionary<string, Batch> _batches = new();
        public Batches(Node3D host, string prefix) { _host = host; _prefix = prefix; }
        public void Define(string name, Mesh mesh, Color color)
        {
            var material = new StandardMaterial3D { AlbedoColor = color, Roughness = .72f, MetallicSpecular = .18f };
            if (mesh is PrimitiveMesh primitive) primitive.Material = material;
            else mesh.SurfaceSetMaterial(0, material);
            _batches.Add(name, new Batch { Mesh = mesh });
        }
        public void Add(string name, Vector3 position) => Add(name, position, Vector3.One);
        public void Add(string name, Vector3 position, Vector3 scale, float yaw = 0)
            => AddTransform(name, new Transform3D(new Basis(Vector3.Up, yaw).Scaled(scale), position));
        public void AddTransform(string name, Transform3D transform) => _batches[name].Transforms.Add(transform);
        public void Commit()
        {
            foreach (var pair in _batches)
            {
                var batch = pair.Value;
                var multi = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, Mesh = batch.Mesh, InstanceCount = batch.Transforms.Count };
                for (int i = 0; i < batch.Transforms.Count; i++) multi.SetInstanceTransform(i, batch.Transforms[i]);
                _host.AddChild(new MultiMeshInstance3D { Name = _prefix + "_" + pair.Key, Multimesh = multi });
            }
        }
    }
}
