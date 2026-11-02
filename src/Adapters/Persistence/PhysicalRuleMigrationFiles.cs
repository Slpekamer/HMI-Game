namespace Hmi.Adapters.Persistence;

/// <summary>Application-owned registry and UUID slots; legacy path is explicit and never rewritten.</summary>
public sealed class PhysicalRuleMigrationFiles : IRuleMigrationFiles
{
    private readonly string registryDirectory, legacyDirectory, slotsDirectory;
    public ICheckpointFiles Registry { get; }
    public ICheckpointFiles Legacy { get; }
    public PhysicalRuleMigrationFiles(string registryDirectory,string legacyDirectory)
    {
        this.registryDirectory=Path.TrimEndingDirectorySeparator(Path.GetFullPath(registryDirectory));
        this.legacyDirectory=Path.TrimEndingDirectorySeparator(Path.GetFullPath(legacyDirectory));
        if(this.registryDirectory==this.legacyDirectory||this.registryDirectory.StartsWith(this.legacyDirectory+Path.DirectorySeparatorChar,StringComparison.Ordinal)||this.legacyDirectory.StartsWith(this.registryDirectory+Path.DirectorySeparatorChar,StringComparison.Ordinal))throw new ArgumentException("Legacy and registry must be disjoint directories");
        CheckPath(this.registryDirectory);CheckPath(this.legacyDirectory);
        slotsDirectory=Path.Combine(this.registryDirectory,"slots");
        Registry=new GuardedFiles(this.registryDirectory,true);
        Legacy=new GuardedFiles(this.legacyDirectory);
    }
    private static void CheckPath(string path)
    {
        for(string? p=Path.GetFullPath(path);p!=null;p=Path.GetDirectoryName(p))
        {
            if((File.Exists(p)||Directory.Exists(p))&&(File.GetAttributes(p)&FileAttributes.ReparsePoint)!=0)throw new IOException("Symbolic links are not allowed in migration storage paths");
            if(File.Exists(p)&&!Directory.Exists(p))throw new IOException("Storage parent is not a directory");
        }
    }
    public string[] Slots()
    {
        CheckPath(registryDirectory);CheckPath(slotsDirectory);
        if(Directory.Exists(registryDirectory))
            foreach(var directory in Directory.EnumerateDirectories(registryDirectory).Take(3))
                if(directory!=slotsDirectory)throw new IOException("Unknown registry directory");
        if(!Directory.Exists(slotsDirectory))return [];
        var entries=Directory.EnumerateFileSystemEntries(slotsDirectory).Take(3).ToArray();
        if(entries.Length>2)throw new IOException("More than two migration slots");
        foreach(string path in entries)
        { CheckPath(path);if(!Directory.Exists(path))throw new IOException("Unknown slot artifact"); }
        return entries.Select(Path.GetFileName).Cast<string>().ToArray();
    }
    public ICheckpointFiles Slot(string id)
    {
        if(!Guid.TryParseExact(id,"N",out var guid)||guid==Guid.Empty||guid.ToString("N")!=id)throw new ArgumentException("Noncanonical slot UUID");
        CheckPath(slotsDirectory);return new GuardedFiles(Path.Combine(slotsDirectory,id));
    }
    private sealed class GuardedFiles : ICheckpointFiles
    {
        private readonly string root;private readonly PhysicalCheckpointFiles inner;private readonly bool allowSlots;
        internal GuardedFiles(string root,bool allowSlots=false){this.root=root;this.allowSlots=allowSlots;inner=new(root);
        }
        public IDisposable AcquireWriter(){CheckPath(root);return inner.AcquireWriter();}
        public string[] Names(){CheckPath(root);foreach(string dir in Directory.EnumerateDirectories(root).Take(2))if(!allowSlots||Path.GetFileName(dir)!="slots")throw new IOException("Unknown storage directory");return inner.Names();}
        public long Length(string name){CheckPath(root);return inner.Length(name);}
        public byte[] Read(string name,int max){CheckPath(root);return inner.Read(name,max);}
        public void AtomicWrite(string name,byte[] bytes,bool overwrite){CheckPath(root);inner.AtomicWrite(name,bytes,overwrite);}
        public void Delete(string name){CheckPath(root);inner.Delete(name);}
    }
}
