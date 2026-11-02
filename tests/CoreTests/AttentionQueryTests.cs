using System;
using Hmi.Core;
static partial class Program
{
    static void AttentionQueryTests()
    {
        var g=New("crit");
        Check(g.HasUnobstructedSegment(new Vec2(0,0),new Vec2(1,1)),"attention open wall segment");
        Check(!g.HasUnobstructedSegment(new Vec2(-8,0),new Vec2(-6,0)),"attention wall blocks");
        Check(g.HasUnobstructedSegment(new Vec2(0,0),new Vec2(0,0)),"attention zero open segment");
        Check(!g.HasUnobstructedSegment(new Vec2(float.NaN,0),new Vec2(0,0)),"attention nan fail closed");
        Check(!g.HasUnobstructedSegment(new Vec2(0,0),new Vec2(float.PositiveInfinity,0)),"attention infinite fail closed");
        Check(!g.HasUnobstructedSegment(new Vec2(0,0),new Vec2(1000000,0)),"attention out of arena fail closed");
        Check(!g.HasUnobstructedSegment(new Vec2(-15,0),new Vec2(0,0)),"attention outside start fail closed");
        for(int x=-13;x<=13;x+=2)for(int y=-7;y<=7;y+=2)
            Check(g.HasUnobstructedSegment(new Vec2(0,0),new Vec2(x,y))==Geometry.LineClear(g,new Vec2(0,0),new Vec2(x,y)),"attention exact old wall predicate");
        var control=New("crit");var queried=New("crit");
        for(int tick=0;tick<1200;tick++)
        {
            var move=new Vec2((tick/100)%2==0?.12f:-.12f,.03f);
            control.Enqueue(new GameCommand(CommandKind.Move,x:move.X,y:move.Y,runToken:control.RunToken,wave:control.Wave));
            queried.Enqueue(new GameCommand(CommandKind.Move,x:move.X,y:move.Y,runToken:queried.RunToken,wave:queried.Wave));
            uint rng=queried.Run.Random;
            for(int i=0;i<queried.Capacity;i++)if(queried.TryGetEntityAtSlot(i,out var e))queried.HasUnobstructedSegment(new Vec2(0,0),e.Position);
            Check(rng==queried.Run.Random,"attention query never consumes rng");
            control.Step();queried.Step();
            Check(control.Tick==queried.Tick&&control.Phase==queried.Phase&&control.Coins==queried.Coins&&control.Run.Random==queried.Run.Random&&control.EventCount==queried.EventCount,"attention query authority parity");
            for(int i=0;i<control.Capacity;i++)
            {
                bool a=control.TryGetEntityAtSlot(i,out var av),b=queried.TryGetEntityAtSlot(i,out var bv);
                Check(a==b,"attention entity existence parity");if(a)Check(av.Id==bv.Id&&av.Position.X==bv.Position.X&&av.Position.Y==bv.Position.Y&&av.Health==bv.Health,"attention exact entity parity");
            }
            for(int i=0;i<control.EventCount;i++){var a=control.GetEvent(i);var b=queried.GetEvent(i);Check(a.Kind==b.Kind&&a.Source==b.Source&&a.Target==b.Target&&a.Value==b.Value&&a.From.X==b.From.X&&a.From.Y==b.From.Y&&a.To.X==b.To.X&&a.To.Y==b.To.Y,"attention old event parity");}
        }
        Check(control.Tick>100,"attention parity actually advanced authority");
        Console.WriteLine($"ATTENTION_QUERY paired_updates=1200 actual_tick={control.Tick} phase={control.Phase}; queries observe only");
    }
}
