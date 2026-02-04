using System;
using System.Diagnostics;
using System.Windows.Input;
using PolyhydraGames.Core.ReactiveUI;
using Prism.Dialogs; 
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Stepmania.Manager.Dialogs;

public class TextInputDialogViewModel : ViewModelAsyncBase, IDialogAware
{
    private DialogCloseListener _requestClose;
    public ICommand CloseDialogCommand { get; }

    public TextInputDialogViewModel()
    {
        CloseDialogCommand = ReactiveCommand.Create(CloseDialog);
    }

    public bool CanCloseDialog() => true;

    public void OnDialogClosed() { }

    public void OnDialogOpened(IDialogParameters parameters)
    {
        Message = parameters.GetValue<string>(nameof(Message));
    }

    DialogCloseListener IDialogAware.RequestClose => _requestClose;

    [Reactive] public string Value { get; set; }
    [Reactive] public string Message { get; set; }

    public event Action<IDialogResult> RequestClose;

    protected virtual void CloseDialog()
    {
        try
        {
            RaiseRequestClose(new DialogResult(ButtonResult.OK)
            {
                Parameters = new DialogParameters($"{nameof(Value)}={Value}")
            });
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
