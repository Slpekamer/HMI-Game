namespace Hmi.Adapters.Persistence;

public sealed class PhysicalCheckpointFiles : ICheckpointFiles
{
    private readonly string directory;
    public PhysicalCheckpointFiles(string directory) { this.directory = Path.GetFullPath(directory); }
    private string Resolve(string name)
    {
        if (string.IsNullOrEmpty(name) || Path.GetFileName(name) != name || name.Contains('/') || name.Contains('\\') || name is "." or "..")
            throw new ArgumentException("Not a store basename");
        string path = Path.Combine(directory, name);
        if (File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Store files cannot be symbolic links");
        return path;
    }
    public IDisposable AcquireWriter()
    {
        Directory.CreateDirectory(directory);
        return new FileStream(Resolve("writer.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    }
    public string[] Names()
    {
        string[] names = Directory.EnumerateFiles(directory).Take(1025).Select(Path.GetFileName).Cast<string>().ToArray();
        if (names.Length > 1024) throw new IOException("Directory entry limit exceeded");
        return names;
    }
    public long Length(string name) => new FileInfo(Resolve(name)).Length;
    public byte[] Read(string name, int maximumBytes)
    {
        using var stream = new FileStream(Resolve(name), FileMode.Open, FileAccess.Read, FileShare.Read);
        long length = stream.Length;
        if (length < 0 || length > maximumBytes) throw new IOException("Read quota exceeded");
        byte[] bytes = new byte[(int)length]; stream.ReadExactly(bytes);
        if (stream.ReadByte() != -1) throw new IOException("File grew during read");
        return bytes;
    }
    public void Delete(string name) => File.Delete(Resolve(name));
    public void AtomicWrite(string name, byte[] bytes, bool overwrite)
    {
        string destination = Resolve(name), temporary = Resolve($"temp-{Guid.NewGuid():N}");
        using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        { stream.Write(bytes); stream.Flush(true); }
        if (!File.ReadAllBytes(temporary).AsSpan().SequenceEqual(bytes)) throw new IOException("Atomic write readback mismatch");
        File.Move(temporary, destination, overwrite);
        // Directory entry durability after sudden power loss is platform-specific and not claimed.
    }
}
