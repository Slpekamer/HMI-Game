using System;
using System.Collections.Generic;

namespace Hmi.Adapters.Input
{
    public readonly struct MoveIntent
    {
        public readonly float X, Y;
        public MoveIntent(float x,float y){X=x;Y=y;}
        public bool IsZero => X==0 && Y==0;
        public static MoveIntent Zero => new MoveIntent(0,0);
    }
    public readonly struct MoveInputToken
    {
        public readonly string ProviderId;
        public readonly long Generation;
        public MoveInputToken(string id,long generation){ProviderId=id;Generation=generation;}
    }
    public readonly struct MoveProviderOptions
    {
        public readonly int Priority;
        public readonly float Deadzone,Hysteresis;
        public MoveProviderOptions(int priority,float deadzone,float hysteresis=0)
        {
            if(float.IsNaN(deadzone)||float.IsInfinity(deadzone)||deadzone<0||deadzone>=1 ||
               float.IsNaN(hysteresis)||float.IsInfinity(hysteresis)||hysteresis<0||deadzone+hysteresis>=1||hysteresis>deadzone)
                throw new ArgumentOutOfRangeException(nameof(deadzone));
            Priority=priority;Deadzone=deadzone;Hysteresis=hysteresis;
        }
    }
    /// <summary>Bounded, engine-independent adapter routing. No UI, device API or gameplay state.</summary>
    public sealed class MoveInputRouter
    {
        private sealed class Provider
        {
            public string Id;
            public MoveProviderOptions Options;
            public long Generation,Order;
            public bool Enabled,AxisEngaged;
            public MoveIntent Intent;
        }
        public const int MaximumProviders=8;
        private readonly Dictionary<string,Provider> _providers=new Dictionary<string,Provider>(StringComparer.Ordinal);
        private long _nextGeneration,_order;
        public string SelectedProvider {get;private set;}
        public int Count => _providers.Count;
        private long NewGeneration()=>checked(++_nextGeneration);
        public MoveInputToken Register(string id,MoveProviderOptions options,bool enabled=true)
        {
            if(string.IsNullOrWhiteSpace(id))throw new ArgumentException("Provider ID required",nameof(id));
            if(_providers.ContainsKey(id))throw new InvalidOperationException("Provider already registered: "+id);
            if(_providers.Count>=MaximumProviders)throw new InvalidOperationException("Input provider budget reached");
            var p=new Provider{Id=id,Options=options,Enabled=enabled,Generation=NewGeneration(),Order=++_order};
            _providers.Add(id,p);return new MoveInputToken(id,p.Generation);
        }
        public bool IsEnabled(string id)=>_providers.TryGetValue(id,out var p)&&p.Enabled;
        public bool TryToken(string id,out MoveInputToken token){if(_providers.TryGetValue(id,out var p)){token=new MoveInputToken(id,p.Generation);return true;}token=default;return false;}
        public MoveInputToken Token(string id)
        {
            if(!_providers.TryGetValue(id,out var p))throw new InvalidOperationException("Unknown input provider: "+id);
            return new MoveInputToken(id,p.Generation);
        }
        public MoveInputToken Enable(string id)
        {
            if(!_providers.TryGetValue(id,out var p))throw new InvalidOperationException("Unknown input provider: "+id);
            if(!p.Enabled){p.Enabled=true;Clear(p);}
            return Token(id);
        }
        public void Disable(string id)
        {
            if(!_providers.TryGetValue(id,out var p))return;
            if(p.Enabled){p.Enabled=false;Clear(p);}
            if(SelectedProvider==id)SelectedProvider=null;
        }
        public void Unregister(string id)
        {
            if(!_providers.TryGetValue(id,out var p))return;
            Clear(p);_providers.Remove(id);if(SelectedProvider==id)SelectedProvider=null;
        }
        private void Clear(Provider p){p.Intent=MoveIntent.Zero;p.AxisEngaged=false;p.Generation=NewGeneration();}
        public void Reset(string id){if(_providers.TryGetValue(id,out var p))Clear(p);if(SelectedProvider==id)SelectedProvider=null;}
        public void ResetAll(){foreach(var p in _providers.Values)Clear(p);SelectedProvider=null;}
        public bool Submit(MoveInputToken token,MoveIntent raw)
        {
            if(token.ProviderId==null || !_providers.TryGetValue(token.ProviderId,out var p)||!p.Enabled||p.Generation!=token.Generation)return false;
            if(float.IsNaN(raw.X)||float.IsNaN(raw.Y)||float.IsInfinity(raw.X)||float.IsInfinity(raw.Y))
            {p.Intent=MoveIntent.Zero;p.AxisEngaged=false;return false;}
            double magnitude=Math.Sqrt((double)raw.X*raw.X+(double)raw.Y*raw.Y);
            float release=p.Options.Deadzone-p.Options.Hysteresis;
            float threshold=p.AxisEngaged?release:p.Options.Deadzone+p.Options.Hysteresis;
            if(magnitude<=threshold){p.Intent=MoveIntent.Zero;p.AxisEngaged=false;return true;}
            p.AxisEngaged=true;
            double scaled=(Math.Min(1,magnitude)-release)/(1-release);
            p.Intent=new MoveIntent((float)(raw.X/magnitude*scaled),(float)(raw.Y/magnitude*scaled));return true;
        }
        public MoveIntent Resolve()
        {
            Provider selected=null;
            foreach(var p in _providers.Values)
                if(p.Enabled&&!p.Intent.IsZero&&(selected==null||p.Options.Priority>selected.Options.Priority||
                    p.Options.Priority==selected.Options.Priority&&p.Order<selected.Order))selected=p;
            string next=selected?.Id;
            if(SelectedProvider!=next)
            {
                if(SelectedProvider!=null&&_providers.TryGetValue(SelectedProvider,out var prior))Clear(prior);
                SelectedProvider=next;
            }
            return selected?.Intent??MoveIntent.Zero;
        }
    }
}
