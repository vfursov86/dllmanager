using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
public class IndexModel : PageModel
{
    private readonly DllManagerService _dllManagerService;

    // List to store DLL information
    public List<DllInfo> Dlls { get; set; } = new();

    public IndexModel(DllManagerService dllManagerService)
    {
        _dllManagerService = dllManagerService;
    }

    public void OnGet()
    {
        var dllsFromSession = HttpContext.Session.Get<List<DllInfo>>("Dlls");
        Dlls = dllsFromSession ?? new List<DllInfo>();
        // Refresh the running status for all DLLs
        foreach (var dll in Dlls)
        {
            dll.IsRunning = _dllManagerService.IsRunning(dll.Path);
        }
    }

    public IActionResult OnPostAddDll(string dllPath)
    {
        if (string.IsNullOrWhiteSpace(dllPath) || !System.IO.File.Exists(dllPath))
        {
            TempData["ErrorMessage"] = "Invalid path. Please ensure the file exists.";
            return RedirectToPage();
        }

        if (!dllPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            TempData["ErrorMessage"] = "Only .dll files are supported.";
            return RedirectToPage();
        }

        var dllsFromSession = HttpContext.Session.Get<List<DllInfo>>("Dlls") ?? new List<DllInfo>();

        dllsFromSession.Add(new DllInfo
        {
            Name = Path.GetFileName(dllPath),
            Path = Path.GetFullPath(dllPath).ToLowerInvariant(),
            IsRunning = false
        });

        HttpContext.Session.Set("Dlls", dllsFromSession);

        return RedirectToPage();
    }

    public IActionResult OnPostStart(string path)
    {

        // Load the DLLs from the session
        Dlls = HttpContext.Session.Get<List<DllInfo>>("Dlls") ?? new List<DllInfo>();

        string error;
        if (!_dllManagerService.StartDll(path, Dlls, out error))
        {
            // Update error details for the DLL
            var dll = Dlls.FirstOrDefault(d => d.Path == path);
            if (dll != null) dll.ProblemDetails = error;
        }

        // Save the updated DLLs back to the session
        HttpContext.Session.Set("Dlls", Dlls);

        return RedirectToPage();
    }

    public IActionResult OnPostStop(string path)
    {

        // Load the DLLs from the session
        Dlls = HttpContext.Session.Get<List<DllInfo>>("Dlls") ?? new List<DllInfo>();

        string error;
        if (!_dllManagerService.StopDll(path, out error))
        {
            // Update error details for the DLL
            var dll = Dlls.FirstOrDefault(d => d.Path == path);
            if (dll != null) dll.ProblemDetails = error;
        }

        // Save the updated DLLs back to the session
        HttpContext.Session.Set("Dlls", Dlls);
        return RedirectToPage();
    }


}
