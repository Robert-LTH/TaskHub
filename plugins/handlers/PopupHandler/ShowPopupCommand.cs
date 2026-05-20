using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
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
                Application.EnableVisualStyles();
                using var form = CreateForm(options);
                using var timer = new System.Windows.Forms.Timer
                {
                    Interval = options.DurationMilliseconds
                };

                timer.Tick += (_, _) =>
                {
                    timer.Stop();
                    form.Close();
                };

                form.Shown += (_, _) =>
                {
                    timer.Start();
                    form.Activate();
                };

                form.ShowDialog();
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

    private static Form CreateForm(PopupOptions options)
    {
        var form = new Form
        {
            Text = options.Title,
            Width = options.Width,
            Height = options.Height,
            StartPosition = FormStartPosition.Manual,
            FormBorderStyle = FormBorderStyle.FixedSingle,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            TopMost = true,
            BackColor = Color.White
        };

        var label = new Label
        {
            Text = options.Message,
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            Font = new Font("Segoe UI", 10),
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft
        };

        form.Controls.Add(label);
        var workingArea = Screen.PrimaryScreen?.WorkingArea ?? SystemInformation.WorkingArea;
        form.Location = CalculateLowerRightLocation(workingArea, form.Width, form.Height, options.Margin);
        return form;
    }

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
