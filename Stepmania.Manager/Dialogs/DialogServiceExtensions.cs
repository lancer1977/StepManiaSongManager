using System;
using System.Threading.Tasks;
using Prism.Dialogs;
using Stepmania.Manager.Dialogs.Confirmation;

namespace Stepmania.Manager.Dialogs;

public static class DialogServiceExtensions
{
    /// <summary>Shows a confirmation dialog; if user confirms, invokes the action.</summary>
    public static async Task IfConfirm(this IDialogService dialog, string message, Action action)
    {
        if (await dialog.GetConfirmation(message))
            action?.Invoke();
    }
    public static async Task<string> GetString(this IDialogService dialog, string message)
    {
        var tcs = new TaskCompletionSource<string>();

        dialog.ShowDialog(nameof(TextInputDialog),
            new DialogParameters($"Message={message}")
            ,x =>
            {
                var response = x.Parameters.GetValue<string>("Value");
                tcs.SetResult(response);
            });
        return await tcs.Task;

    }

    public static async Task<bool> GetConfirmation(this IDialogService dialog, string warning = "Are you REALLY sure you want to do this!!?")
{
        var tcs = new TaskCompletionSource<bool>();

        dialog.ShowDialog(nameof(ConfirmDialog), new DialogParameters($"Message={warning}"), x =>
            {
                var response = x.Parameters.GetValue<bool>("response");
                tcs.SetResult(response);
            });
        return await tcs.Task;

    }
}