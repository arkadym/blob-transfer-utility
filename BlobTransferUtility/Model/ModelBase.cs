using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BlobTransferUtility.Model
{
    public class ModelBase : INotifyPropertyChanged
    {
        delegate void ActionDelegate();

        protected void ExecuteOnUI(Action action)
        {
            if (Application.Current == null)
                return;
            Application.Current.Dispatcher.BeginInvoke(new ActionDelegate(action));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                ExecuteOnUI(()=>handler(this, new PropertyChangedEventArgs(propertyName)));
            }
        }

        protected virtual void OnPropertyChanged<T>(Expression<Func<T>> selectorExpression)
        {
            if (selectorExpression == null)
                throw new ArgumentNullException("selectorExpression");
            var body = selectorExpression.Body as MemberExpression;
            if (body == null)
                throw new ArgumentException("The body must be a member expression");
            OnPropertyChanged(body.Member.Name);
        }

        protected bool SetProperty<T>(ref T field, T value, Expression<Func<T>> selectorExpression)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(selectorExpression);
            return true;
        }

        protected bool SetField<T>(ref T field, T value, Expression<Func<T>> selectorExpression)
        {
            return SetProperty<T>(ref field, value, selectorExpression);
        }
    }
}
