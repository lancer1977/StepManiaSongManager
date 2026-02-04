using DryIoc;
using PolyhydraGames.Core.ReactiveUI;
using PolyhydraGames.Extensions;
using Prism.Dialogs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Diagnostics;
using System.Windows.Input;

namespace Stepmania.Manager.Dialogs.Confirmation;

public class ConfirmDialogViewModel : ViewModelAsyncBase, IDialogAware
{
    private DialogCloseListener _requestClose;
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
        _requestClose.Invoke();
    }

    public void OnDialogOpened(IDialogParameters parameters)
    {
        Message = parameters.GetValue<string>(nameof(Message));
        Title = parameters.GetValue<string>(nameof(Title));
    }

    DialogCloseListener IDialogAware.RequestClose => _requestClose;



    protected virtual void CloseDialog(string response)
    {
        try
        {
            //var result = ButtonResult.OK;
            var result = new DialogResult(ButtonResult.OK)
            {
                Parameters = new DialogParameters($"{nameof(response)}={response.ToBool()}")
            };
            
            RaiseRequestClose(result);
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