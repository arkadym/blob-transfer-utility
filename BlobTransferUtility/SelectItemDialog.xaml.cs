using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BlobTransferUtility
{
    /// <summary>
    /// Interaction logic for SelectItemDialog.xaml
    /// </summary>
    public partial class SelectItemDialog : Window
    {
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public SelectItemDialog()
        {
            InitializeComponent();
            Loaded += SelectItemDialog_Loaded;
        }

        async void SelectItemDialog_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);

            if (_GetItemsFunction != null)
                Items = await _GetItemsFunction();

            if (Items != null)
                SelectedItem = Items.FirstOrDefault();
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void SelectButtonClick(object sender, RoutedEventArgs e)
        {
            DialogResult = SelectedItem != null;
        }

        public bool ShowDialog(Window owner, string title, IEnumerable<object> items, string displayMemberPath = null)
        {
            DisplayMemberPath = displayMemberPath;
            Items = items;
            Title = title;
            Owner = owner;
            return ShowDialog().GetValueOrDefault();
        }

        public async Task<bool> ShowDialog(Window owner, string title, Func<Task<IEnumerable<object>>> getItemsFunction, string displayMemberPath = null)
        {
            DisplayMemberPath = displayMemberPath;
            _GetItemsFunction = getItemsFunction;
            Title = title;
            Owner = owner;
            return ShowDialog().GetValueOrDefault();
        }

        public IEnumerable<object> Items
        {
            get { return (IEnumerable<object>)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }
        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register("Items", typeof(IEnumerable<object>), typeof(SelectItemDialog), new PropertyMetadata(null));

        public object SelectedItem
        {
            get { return (object)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(object), typeof(SelectItemDialog), new PropertyMetadata(null));

        public string DisplayMemberPath
        {
            get { return (string)GetValue(DisplayMemberPathProperty); }
            set { SetValue(DisplayMemberPathProperty, value); }
        }
        public static readonly DependencyProperty DisplayMemberPathProperty =
            DependencyProperty.Register("DisplayMemberPath", typeof(string), typeof(SelectItemDialog), new PropertyMetadata(null));
        private Func<Task<IEnumerable<object>>> _GetItemsFunction;

     }
}
