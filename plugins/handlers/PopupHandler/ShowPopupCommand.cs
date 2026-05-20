using System;
using System.Runtime.InteropServices;
using System.Text;
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

        var script = BuildPopupScript(Request);
        var scriptBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(script));

        dynamic powershell = service.GetService();
        OperationResult result = powershell.Execute(scriptBase64, null, null);

        logger.LogInformation("Popup command completed with result: {Result}", result.Result);
        return Task.FromResult(result);
    }

    public static string BuildPopupScript(ShowPopupRequest request)
    {
        var title = string.IsNullOrWhiteSpace(request.Title) ? "TaskHub" : request.Title!;
        var duration = Clamp(request.DurationMilliseconds, 5000, 500, 60000);
        var width = Clamp(request.Width, 360, 240, 800);
        var height = Clamp(request.Height, 140, 100, 500);
        var margin = Clamp(request.Margin, 16, 0, 120);

        return $$"""
$popupTitle = '{{EscapePowerShellSingleQuotedString(title)}}'
$popupMessage = '{{EscapePowerShellSingleQuotedString(request.Message)}}'
$popupWidth = {{width}}
$popupHeight = {{height}}
$popupMargin = {{margin}}
$popupDuration = {{duration}}

Add-Type -ReferencedAssemblies @('System.Windows.Forms', 'System.Drawing') -TypeDefinition @'
using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

public static class TaskHubPopupWindow
{
    public static void Show(string title, string message, int width, int height, int margin, int durationMilliseconds)
    {
        Exception error = null;
        var popupThread = new Thread(() =>
        {
            try
            {
                Application.EnableVisualStyles();

                using (var form = new Form())
                using (var timer = new System.Windows.Forms.Timer())
                {
                    form.Text = title;
                    form.Width = width;
                    form.Height = height;
                    form.StartPosition = FormStartPosition.Manual;
                    form.FormBorderStyle = FormBorderStyle.FixedSingle;
                    form.MaximizeBox = false;
                    form.MinimizeBox = false;
                    form.ShowInTaskbar = false;
                    form.TopMost = true;
                    form.BackColor = Color.White;

                    var label = new Label
                    {
                        Text = message,
                        Dock = DockStyle.Fill,
                        Padding = new Padding(14),
                        Font = new Font("Segoe UI", 10),
                        AutoEllipsis = true,
                        TextAlign = ContentAlignment.MiddleLeft
                    };
                    form.Controls.Add(label);

                    var workingArea = Screen.PrimaryScreen.WorkingArea;
                    form.Location = new Point(
                        workingArea.Right - form.Width - margin,
                        workingArea.Bottom - form.Height - margin);

                    timer.Interval = durationMilliseconds;
                    timer.Tick += (_, __) =>
                    {
                        timer.Stop();
                        form.Close();
                    };

                    form.Shown += (_, __) =>
                    {
                        timer.Start();
                        form.Activate();
                    };

                    form.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });

        popupThread.SetApartmentState(ApartmentState.STA);
        popupThread.Start();
        popupThread.Join();

        if (error != null)
        {
            throw new InvalidOperationException("Failed to show popup.", error);
        }
    }
}
'@

[TaskHubPopupWindow]::Show($popupTitle, $popupMessage, $popupWidth, $popupHeight, $popupMargin, $popupDuration)
""";
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

    private static string EscapePowerShellSingleQuotedString(string value) =>
        value.Replace("'", "''", StringComparison.Ordinal);
}
