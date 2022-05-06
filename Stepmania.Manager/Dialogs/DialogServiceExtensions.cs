using System;
using System.Threading.Tasks;
using Prism.Services.Dialogs;
using Stepmania.Manager.Dialogs.Confirmation;

namespace Stepmania.Manager.Dialogs;


public static class DialogServiceExtensions
{   
    public static IDialogService DialogSerivce { get; set; }

    public static async Task IfConfirm(this Action act, string message = "Are you sure you want to do this")
    {
        var result = await DialogSerivce.GetConfirmation(message);
        if(result) act.Invoke();
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