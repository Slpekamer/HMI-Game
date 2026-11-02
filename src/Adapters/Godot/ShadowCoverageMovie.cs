using Godot;
using Hmi.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

// Standalone diagnostic scene only. Never instantiated by the production scene.
public partial class ShadowCoverageMovie : Node
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    GameHost host;
    T Field<T>(string name) => (T)typeof(GameHost).GetField(name, Flags).GetValue(host);
    void Call(string name, params object[] args) => typeof(GameHost).GetMethod(name, Flags).Invoke(host, args);
    static float[] V(Vector3 v) => new[] { v.X, v.Y, v.Z };
    static Button Find(Node node)
    {
        if (node is Button button && button.Text == "确认准备新局") return button;
        foreach (Node child in node.GetChildren()) { var hit = Find(child); if (hit != null) return hit; }
        return null;
    }
    public override async void _Ready()
    {
        try
        {
            string output = OS.GetEnvironment("SHADOW_COVERAGE_OUTPUT");
            if (string.IsNullOrEmpty(output)) throw new Exception("SHADOW_COVERAGE_OUTPUT required");
            Directory.CreateDirectory(output);
            string aspect = OS.GetEnvironment("SHADOW_COVERAGE_ASPECT");
            if (aspect != "landscape" && aspect != "portrait") throw new Exception("Unknown coverage aspect");
            bool portrait = aspect == "portrait";
            var size = portrait ? new Vector2I(720,1280) : new Vector2I(1280,720);
            GetWindow().Size = new Vector2I(1280,720);
            ControlledGaitDrill.Enabled = true;
            ControlledGaitDrill.Candidate = true;
            ControlledGaitDrill.ProportionCandidate = true;
            ControlledGaitDrill.DisplacementRun = true;
            ControlledGaitDrill.Clip = "Run";
            var container = new SubViewportContainer { Size = size, Stretch = false };
            AddChild(container);
            var view = new SubViewport { Size = size, OwnWorld3D = true, RenderTargetUpdateMode = SubViewport.UpdateMode.Always };
            container.AddChild(view);
            host = new GameHost(); view.AddChild(host); host.SetProcess(false);
            Call("Start",1,(uint)1030);
            var confirm = Find(host); if (confirm == null) throw new Exception("Prepare confirmation missing");
            confirm.EmitSignal(Button.SignalName.Pressed);
            Call("Start",1,(uint)1030); host.SetProcess(false);
            var sim = Field<GameSimulation>("_sim");
            sim.Enqueue(GameCommand.Pause(true));
            host._Process(GameSimulation.FixedDelta);
            if (!sim.Paused || sim.Phase != RunPhase.Combat) throw new Exception("Expected paused Combat");
            // Hide only UI in this labeled environment-coverage diagnostic, not in the gameplay A/B.
            void HideUi(Node node) { foreach (Node child in node.GetChildren()) { if (child is CanvasLayer layer) layer.Visible = false; HideUi(child); } }
            HideUi(host);
            Call("ApplyViewportLayout");
            var camera = Field<Camera3D>("_camera");
            var originalBasis = camera.GlobalBasis;
            var sun = host.GetChildren(); DirectionalLight3D light = null;
            foreach (Node node in sun) if (node is DirectionalLight3D found) light = found;
            if (light == null) throw new Exception("Directional light missing");
            var labelLayer = new CanvasLayer { Layer = 99 }; view.AddChild(labelLayer);
            labelLayer.AddChild(new Label { Text = "静态阴影覆盖诊断（非游玩）", Position = new Vector2(12,8) });
            var rows = new List<object>(); long tick = sim.Tick;
            string[] sides = { "west", "east", "north", "south" };
            foreach (string side in sides)
            {
                Vector3 focus = side switch {
                    "west" => new Vector3(portrait ? -13.5f : -3,0,0),
                    "east" => new Vector3(portrait ? 13.5f : 3,0,0),
                    "north" => new Vector3(0,0,portrait ? -7.5f : -1.5f),
                    _ => new Vector3(0,0,portrait ? 7.5f : 1.5f)
                };
                for (int sample = 0; sample < 5; sample++)
                {
                    // Five neighboring camera poses expose local coverage transitions. These are not played routes.
                    float offset = (sample - 2) * .08f;
                    Vector3 lateral = side == "west" || side == "east" ? new Vector3(0,0,offset) : new Vector3(offset,0,0);
                    camera.Position = new Vector3(12,22,18) + focus + lateral;
                    string[] hashes = new string[3];
                    for (int repeat = 0; repeat < 3; repeat++)
                    {
                        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                        if (sim.Tick != tick || !sim.Paused || camera.GlobalBasis != originalBasis) throw new Exception("Frozen state changed");
                        using var image = view.GetTexture().GetImage();
                        if (image.GetSize() != size) throw new Exception("Coverage viewport dimensions changed");
                        hashes[repeat] = Convert.ToHexString(SHA256.HashData(image.GetData()));
                        if (repeat == 2) image.SavePng(Path.Combine(output,$"{side}-{sample}.png"));
                    }
                    if (hashes[0] != hashes[1] || hashes[1] != hashes[2]) throw new Exception("Repeated paused framebuffer changed");
                    rows.Add(new { side, sample, position = V(camera.Position), basisX = V(camera.Basis.X), basisY = V(camera.Basis.Y), basisZ = V(camera.Basis.Z), camera.Size, camera.Near, camera.Far, projection = (int)camera.Projection, tick, paused = sim.Paused, hashes });
                }
            }
            var report = new { diagnostic = "Frozen paused world; explicit side camera views, not played gameplay or reachability proof", aspect, width = size.X, height = size.Y, rows,
                light = new { light.ShadowEnabled, light.DirectionalShadowSplit1, light.DirectionalShadowSplit2, light.DirectionalShadowSplit3, light.DirectionalShadowMaxDistance, mode = (int)light.DirectionalShadowMode, light.ShadowBias, light.ShadowNormalBias, light.LightEnergy },
                note = "Production camera code, orientation, projection, size policy, near/far and shadow settings except opt-in Split1 are unchanged. UI hidden only for diagnostic coverage." };
            File.WriteAllText(Path.Combine(output,"coverage.json"),JsonSerializer.Serialize(report,new JsonSerializerOptions { WriteIndented = true }));
            GD.Print("SHADOW_COVERAGE_COMPLETE aspect="+aspect+" views=20 repeats=3 paused=true");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
