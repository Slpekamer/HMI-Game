using Hmi.Adapters.Persistence;
using System.Diagnostics;

static partial class Program
{
    static void UpgradeChild(string root,int boundary)
    {
        var physical=new PhysicalRuleMigrationFiles(Path.Combine(root,"registry"),Path.Combine(root,"legacy"));
        var c=Coordinator(new CrashFiles(physical,root,boundary));var view=c.InspectUpgrade();
        c.ConfirmUpgrade(Operation,NewRun,view.Fingerprint,false);
        throw new Exception("Child boundary was not reached");
    }
    static void KillMatrix()
    {
        for(int boundary=1;boundary<=6;boundary++)
        {
            string root=Path.Combine(Path.GetTempPath(),"hmi-p3-kill-"+Guid.NewGuid().ToString("N"));
            var f=Setup();Export(f,root);
            var start=new ProcessStartInfo("dotnet"){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};
            start.ArgumentList.Add(typeof(Program).Assembly.Location);start.ArgumentList.Add("--upgrade-child");start.ArgumentList.Add(root);start.ArgumentList.Add(boundary.ToString());
            try
            {
                using var child=Process.Start(start)!;
                bool waiting=SpinWait.SpinUntil(()=>File.Exists(Path.Combine(root,"kill-ready"))||child.HasExited,TimeSpan.FromSeconds(20));
                if(!waiting||child.HasExited)
                {if(!child.HasExited)child.Kill(true);throw new Exception("Child failed boundary "+boundary+": "+child.StandardError.ReadToEnd());}
                child.Kill(true);Check(child.WaitForExit(10000),"killed child exited");
                Check(child.ExitCode!=0,"actual abnormal process kill");
                var files=new PhysicalRuleMigrationFiles(Path.Combine(root,"registry"),Path.Combine(root,"legacy"));
                var c=Coordinator(files);var recovered=c.InspectUpgrade();
                Check(recovered.Kind is RuleUpgradeKind.Incomplete or RuleUpgradeKind.CurrentSelected,"kill yields explicit pending or selected");
                if(recovered.Kind==RuleUpgradeKind.Incomplete)recovered=c.RetryUpgrade(recovered.Pending!);
                Check(recovered.Kind==RuleUpgradeKind.CurrentSelected&&recovered.Recovery!.Kind==RecoveryKind.Active,"kill recovery preserves intended target");
                foreach(var p in f.NewSlots[OldSlot].Data)
                    Check(File.ReadAllBytes(Path.Combine(root,"registry","slots",OldSlot,p.Key)).SequenceEqual(p.Value),"kill leaves source exact");
                Console.WriteLine($"REAL_PROCESS_KILL boundary={boundary} exit={child.ExitCode} final={recovered.Kind}");
            }
            finally{if(Directory.Exists(root))Directory.Delete(root,true);}
        }
    }
    sealed class CrashFiles : IRuleMigrationFiles
    {
        readonly IRuleMigrationFiles inner;readonly string root;readonly int boundary;int writes;
        public CrashFiles(IRuleMigrationFiles inner,string root,int boundary){this.inner=inner;this.root=root;this.boundary=boundary;}
        public ICheckpointFiles Registry=>new Port(this,inner.Registry);
        public ICheckpointFiles Legacy=>new Port(this,inner.Legacy);
        public string[] Slots()=>inner.Slots();
        public ICheckpointFiles Slot(string id)=>new Port(this,inner.Slot(id));
        void Committed()
        {
            if(++writes!=boundary)return;
            File.WriteAllText(Path.Combine(root,"kill-ready"),boundary.ToString());
            Thread.Sleep(Timeout.Infinite);
        }
        sealed class Port : ICheckpointFiles
        {
            readonly CrashFiles owner;readonly ICheckpointFiles inner;
            public Port(CrashFiles owner,ICheckpointFiles inner){this.owner=owner;this.inner=inner;}
            public IDisposable AcquireWriter()=>inner.AcquireWriter();
            public string[] Names()=>inner.Names();public long Length(string n)=>inner.Length(n);
            public byte[] Read(string n,int max)=>inner.Read(n,max);
            public void AtomicWrite(string n,byte[] b,bool overwrite){inner.AtomicWrite(n,b,overwrite);owner.Committed();}
            public void Delete(string n)=>inner.Delete(n);
        }
    }
}
