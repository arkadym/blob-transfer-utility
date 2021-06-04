using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BlobTransferUtility.ViewModel
{
    public class ActionCommand : ICommand
    {
        public ActionCommand()
        {
        }

        public ActionCommand(Action<object> action)
        {
            Action = action;
        }

        public Action<object> Action { get; set; }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            if (Action != null)
                Action(parameter);
        }
    }
}
