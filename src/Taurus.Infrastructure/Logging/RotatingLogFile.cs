using System.Text;

namespace Taurus.Infrastructure.Logging;

public sealed class RotatingLogFile : IDisposable
{
    private const int FirstBackupIndex = 1;
    private const string BackupSeparator = ".";

    private readonly object sync = new();
    private readonly string path;
    private readonly ServiceLogOptions options;
    private readonly Encoding encoding = new UTF8Encoding(false);
    private StreamWriter? writer;
    private long bytes;

    public RotatingLogFile(string path, ServiceLogOptions options)
    {
        if (options.MaximumFileBytes <= default(long)
            || options.RetainedFiles <= default(int))
            throw new ArgumentOutOfRangeException(nameof(options));

        this.path = path;
        this.options = options;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
    }

    public void Write(string line)
    {
        lock (sync)
        {
            var length = encoding.GetByteCount(line)
                + encoding.GetByteCount(Environment.NewLine);

            writer ??= Open();

            if (bytes != default && bytes + length > options.MaximumFileBytes)
                Rotate();

            writer.WriteLine(line);
            bytes += length;
        }
    }

    private StreamWriter Open()
    {
        var stream = new FileStream(path, FileMode.Append, FileAccess.Write,
            FileShare.Read);

        bytes = stream.Length;

        return new StreamWriter(stream, encoding) { AutoFlush = true };
    }

    private void Rotate()
    {
        writer!.Dispose();
        writer = null;

        for (var index = options.RetainedFiles; index > default(int); index--)
        {
            var source = index == FirstBackupIndex
                ? path : Backup(index - FirstBackupIndex);
            var destination = Backup(index);

            if (File.Exists(source))
                File.Move(source, destination, true);
        }

        writer = Open();
    }

    private string Backup(int index) => string.Concat(path, BackupSeparator,
        index.ToString(System.Globalization.CultureInfo.InvariantCulture));

    public void Dispose()
    {
        lock (sync)
            writer?.Dispose();
    }
}
