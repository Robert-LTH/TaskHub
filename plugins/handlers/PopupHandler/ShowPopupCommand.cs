using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace PopupHandler;

public class ShowPopupCommand : ICommand
{
    public ShowPopupCommand(ShowPopupRequest request)
    {
        Request = request;
    }

    public ShowPopupRequest Request { get; }

    public Task<OperationResult> ExecuteAsync(IServicePlugin service, ILogger logger, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Request.Message))
        {
            return Task.FromResult(new OperationResult(null, "message is required"));
        }

        if (!OperatingSystem.IsWindows())
        {
            var payload = JsonSerializer.SerializeToElement(new
            {
                platform = RuntimeInformation.OSDescription
            });
            return Task.FromResult(new OperationResult(payload, "Popup notifications are only supported on Windows"));
        }

        try
        {
            ShowPopup(Normalize(Request));
            logger.LogInformation("Popup command completed successfully");
            return Task.FromResult(new OperationResult(JsonSerializer.SerializeToElement(new
            {
                title = string.IsNullOrWhiteSpace(Request.Title) ? "TaskHub" : Request.Title,
                message = Request.Message
            }), "success"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to show popup");
            return Task.FromResult(new OperationResult(null, $"Failed to show popup: {ex.Message}"));
        }
    }

    public static PopupOptions Normalize(ShowPopupRequest request)
    {
        var title = string.IsNullOrWhiteSpace(request.Title) ? "TaskHub" : request.Title!;
        return new PopupOptions(
            title,
            request.Message,
            Clamp(request.DurationMilliseconds, 5000, 500, 60000),
            Clamp(request.Width, 360, 240, 800),
            Clamp(request.Height, 140, 100, 500),
            Clamp(request.Margin, 16, 0, 120));
    }

    public static Point CalculateLowerRightLocation(Rectangle workingArea, int width, int height, int margin) =>
        new(workingArea.Right - width - margin, workingArea.Bottom - height - margin);

    [SupportedOSPlatform("windows")]
    private static void ShowPopup(PopupOptions options)
    {
        Exception? error = null;

        var popupThread = new Thread(() =>
        {
            try
            {
                var formsAssembly = Assembly.Load("System.Windows.Forms");
                var drawingAssembly = LoadDrawingAssembly();

                InvokeStatic(formsAssembly, "System.Windows.Forms.Application", "EnableVisualStyles");

                using var form = (IDisposable)CreateForm(formsAssembly, drawingAssembly, options);
                using var timer = (IDisposable)CreateInstance(formsAssembly, "System.Windows.Forms.Timer");

                SetProperty(timer, "Interval", options.DurationMilliseconds);

                AddEventHandler(timer, "Tick", new EventHandler((_, _) =>
                {
                    Invoke(timer, "Stop");
                    Invoke(form, "Close");
                }));

                AddEventHandler(form, "Shown", new EventHandler((_, _) =>
                {
                    Invoke(timer, "Start");
                    Invoke(form, "Activate");
                }));

                Invoke(form, "ShowDialog");
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });

        popupThread.SetApartmentState(ApartmentState.STA);
        popupThread.Start();
        popupThread.Join();

        if (error is not null)
        {
            throw new InvalidOperationException("Failed to show popup.", error);
        }
    }

    private static object CreateForm(Assembly formsAssembly, Assembly drawingAssembly, PopupOptions options)
    {
        var form = CreateInstance(formsAssembly, "System.Windows.Forms.Form");
        SetProperty(form, "Text", options.Title);
        SetProperty(form, "Width", options.Width);
        SetProperty(form, "Height", options.Height);
        SetProperty(form, "StartPosition", EnumValue(formsAssembly, "System.Windows.Forms.FormStartPosition", "Manual"));
        SetProperty(form, "FormBorderStyle", EnumValue(formsAssembly, "System.Windows.Forms.FormBorderStyle", "FixedSingle"));
        SetProperty(form, "MaximizeBox", false);
        SetProperty(form, "MinimizeBox", false);
        SetProperty(form, "ShowInTaskbar", false);
        SetProperty(form, "TopMost", true);
        SetProperty(form, "BackColor", GetStaticProperty(drawingAssembly, "System.Drawing.Color", "White"));

        var label = CreateInstance(formsAssembly, "System.Windows.Forms.Label");
        SetProperty(label, "Text", options.Message);
        SetProperty(label, "Dock", EnumValue(formsAssembly, "System.Windows.Forms.DockStyle", "Fill"));
        SetProperty(label, "Padding", CreateInstance(formsAssembly, "System.Windows.Forms.Padding", 14));
        SetProperty(label, "Font", CreateInstance(drawingAssembly, "System.Drawing.Font", "Segoe UI", 10f));
        SetProperty(label, "AutoEllipsis", true);
        SetProperty(label, "TextAlign", EnumValue(drawingAssembly, "System.Drawing.ContentAlignment", "MiddleLeft"));

        var controls = GetProperty(form, "Controls");
        Invoke(controls, "Add", label);

        var workingArea = GetWorkingArea(formsAssembly);
        SetProperty(form, "Location", CalculateLowerRightLocation(workingArea, options.Width, options.Height, options.Margin));
        return form;
    }

    private static Rectangle GetWorkingArea(Assembly formsAssembly)
    {
        var primaryScreen = GetStaticProperty(formsAssembly, "System.Windows.Forms.Screen", "PrimaryScreen");
        if (primaryScreen is not null)
        {
            return (Rectangle)GetProperty(primaryScreen, "WorkingArea")!;
        }

        return (Rectangle)GetStaticProperty(formsAssembly, "System.Windows.Forms.SystemInformation", "WorkingArea")!;
    }

    private static Assembly LoadDrawingAssembly()
    {
        try
        {
            return Assembly.Load("System.Drawing.Common");
        }
        catch
        {
            return Assembly.Load("System.Drawing");
        }
    }

    private static object CreateInstance(Assembly assembly, string typeName, params object[] args) =>
        Activator.CreateInstance(GetRequiredType(assembly, typeName), args)
        ?? throw new InvalidOperationException($"Failed to create {typeName}.");

    private static object? GetProperty(object target, string propertyName) =>
        target.GetType().GetProperty(propertyName)?.GetValue(target);

    private static object? GetStaticProperty(Assembly assembly, string typeName, string propertyName) =>
        GetRequiredType(assembly, typeName).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static)?.GetValue(null);

    private static void SetProperty(object target, string propertyName, object? value) =>
        target.GetType().GetProperty(propertyName)?.SetValue(target, value);

    private static void Invoke(object? target, string methodName, params object?[] args)
    {
        if (target is null)
        {
            throw new InvalidOperationException($"Cannot invoke {methodName} on a null target.");
        }

        var method = target.GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(candidate => candidate.Name == methodName && candidate.GetParameters().Length == args.Length);

        if (method is null)
        {
            throw new MissingMethodException(target.GetType().FullName, methodName);
        }

        method.Invoke(target, args);
    }

    private static void InvokeStatic(Assembly assembly, string typeName, string methodName) =>
        GetRequiredType(assembly, typeName).GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);

    private static void AddEventHandler(object target, string eventName, EventHandler handler) =>
        target.GetType().GetEvent(eventName)?.AddEventHandler(target, handler);

    private static object EnumValue(Assembly assembly, string typeName, string name) =>
        Enum.Parse(GetRequiredType(assembly, typeName), name);

    private static Type GetRequiredType(Assembly assembly, string typeName) =>
        assembly.GetType(typeName, throwOnError: true)
        ?? throw new InvalidOperationException($"Type {typeName} was not found.");

    private static int Clamp(int? value, int defaultValue, int min, int max)
    {
        var normalized = value.GetValueOrDefault(defaultValue);
        if (normalized < min)
        {
            return min;
        }

        if (normalized > max)
        {
            return max;
        }

        return normalized;
    }
}

public sealed record PopupOptions(
    string Title,
    string Message,
    int DurationMilliseconds,
    int Width,
    int Height,
    int Margin);
