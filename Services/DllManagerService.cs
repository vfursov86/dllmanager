using System.Diagnostics;

public class DllManagerService
{
    private readonly Dictionary<string, Process> _runningProcesses = new();

    public bool StartDll(string dllPath, List<DllInfo> dlls, out string error)
    {
        error = string.Empty;
        try
        {
            // Dynamically allocate a port, if required
            int port = 5000; // Adjust dynamically if needed

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"\"{dllPath}\" --urls \"http://localhost:{port}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(dllPath)
                }
            };

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    // Append output to the DLL's log output
                    var dll = dlls.FirstOrDefault(d => d.Path == dllPath);
                    if (dll != null)
                    {
                        dll.LogOutput += $"{e.Data}\n";
                    }
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    // Append error output to the ProblemDetails section
                    var dll = dlls.FirstOrDefault(d => d.Path == dllPath);
                    if (dll != null)
                    {
                        dll.ProblemDetails += $"{e.Data}\n";
                    }
                }
            };

            process.Start();
            process.BeginOutputReadLine(); // Start reading stdout
            process.BeginErrorReadLine(); // Start reading stderr

            _runningProcesses[dllPath] = process;

            // Update the DLL info with port information


            var normalizedDllPath = Path.GetFullPath(dllPath).ToLowerInvariant();
            var runningDllInfo = dlls.FirstOrDefault(d => Path.GetFullPath(d.Path).ToLowerInvariant() == normalizedDllPath);

            if (runningDllInfo == null)
            {
                Console.WriteLine($"No matching DLL found for path: {dllPath}");
            }
            else
            {
                runningDllInfo.LogOutput += $"Application started at http://localhost:{port}\n";
            }

            Console.WriteLine("DLLs in list:");
            foreach (var dll in dlls)
            {
                Console.WriteLine($"- {dll.Path}");
            }
            Console.WriteLine($"Starting DLL: {dllPath}");

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }


    public bool StopDll(string dllPath, out string error)
    {
        error = string.Empty;
        if (_runningProcesses.ContainsKey(dllPath))
        {
            try
            {
                _runningProcesses[dllPath].Kill();
                _runningProcesses.Remove(dllPath);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
        }
        else
        {
            error = "Process not found.";
        }
        return false;
    }

    public bool IsRunning(string dllPath) => _runningProcesses.ContainsKey(dllPath);
}
