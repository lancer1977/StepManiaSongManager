using System;
using System.Diagnostics;
using System.Windows.Input;
using PolyhydraGames.Core.ReactiveUI;
using PolyhydraGames.Extensions;
using Prism.Services.Dialogs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Stepmania.Manager.Dialogs.Confirmation;

public class ConfirmDialogViewModel : ViewModelBase, IDialogAware
{
    public ICommand CloseDialogCommand { get; }
    [Reactive] public string Title { get; set; }
    [Reactive] public string Message { get; set; }
    public event Action<IDialogResult> RequestClose;

    public ConfirmDialogViewModel()
    {
        CloseDialogCommand = ReactiveCommand.Create<string>(CloseDialog);
    }
    public bool CanCloseDialog()
    {
        return true;
    }

    public void OnDialogClosed()
    {

    }

    public void OnDialogOpened(IDialogParameters parameters)
    {
        Message = parameters.GetValue<string>(nameof(Message));
        Title = parameters.GetValue<string>(nameof(Title));
    }



    protected virtual void CloseDialog(string response)
    {
        try
        {
            var result = ButtonResult.OK;
            RaiseRequestClose(new DialogResult(result, new DialogParameters($"{nameof(response)}={response.ToBool()}")));
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }

    }

    public virtual void RaiseRequestClose(IDialogResult dialogResult)
    {
        RequestClose?.Invoke(dialogResult);
    }
}