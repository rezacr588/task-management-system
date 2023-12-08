using System;
using System.IO;
using System.Threading.Tasks;

public class FileLogger
{
    private readonly string _filePath;
    private readonly object _lock = new object();

    public FileLogger(string filePath)
    {
        _filePath = filePath;
    }

    public void Log(string message)
    {
        lock (_lock)
        {
            File.AppendAllText(_filePath, $"{DateTime.Now}: {message}\n");
        }
    }

    public async Task LogAsync(string message)
    {
        await Task.Run(() => Log(message));
    }
}
