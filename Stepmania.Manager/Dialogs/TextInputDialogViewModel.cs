using System;
using System.Diagnostics;
using System.Windows.Input;
using PolyhydraGames.Core.ReactiveUI;
using Prism.Services.Dialogs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Stepmania.Manager.Dialogs;

public class TextInputDialogViewModel : ViewModelBase, IDialogAware
{
    public ICommand CloseDialogCommand { get; }

    public TextInputDialogViewModel()
    {
        CloseDialogCommand = ReactiveCommand.Create(() =>
        {
            CloseDialog();
        });
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
    }

    [Reactive] public string Value { get; set; }
    [Reactive] public string Message { get; set; }

    public event Action<IDialogResult> RequestClose;

    protected virtual void CloseDialog()
    {
        try
        {
            ButtonResult result = ButtonResult.OK;
            RaiseRequestClose(new DialogResult(result, new DialogParameters($"{nameof(Value)}={Value}")));
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