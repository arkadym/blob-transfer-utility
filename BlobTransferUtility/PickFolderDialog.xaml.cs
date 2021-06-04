using BlobTransferUtility.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
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
    /// Interaction logic for PickFolderDialog.xaml
    /// </summary>
    public partial class PickFolderDialog : Window
    {
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public PickFolderDialog()
        {
            InitializeComponent();
            Loaded += PickFolderDialog_Loaded;
        }

        void PickFolderDialog_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }

        private void SelectButtonClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void PickFolderButtonClick(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog() { ShowNewFolderButton = false, SelectedPath = SelectedFolder };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SelectedFolder = dialog.SelectedPath;
            }
        }

        public List<Model.File> PickFolderAndFiles()
        {
            Owner = Application.Current.MainWindow;
            if (ShowDialog().GetValueOrDefault())
            {
                if (!string.IsNullOrEmpty(SelectedFolder))
                {
                    if (!SelectedFolder.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()))
                        SelectedFolder += System.IO.Path.DirectorySeparatorChar;

                    var searchOption = SearchOption.TopDirectoryOnly;
                    if (IncludeSubfolders)
                        searchOption = SearchOption.AllDirectories;

                    var files = new List<BlobTransferUtility.Model.File>();
                    try
                    {
                        foreach (var file in Directory.GetFiles(SelectedFolder, "*", searchOption))
                        {
                            var fileInfo = new FileInfo(file);
                            files.Add(new Model.File()
                            {
                                Name = file.Substring(SelectedFolder.Length),
                                FullFilePath = file,
                                SizeInBytes = fileInfo.Length,
                                RelativeToFolder = SelectedFolder,
                            });
                        }
                    }
                    catch (Exception e) 
                    {
                        var stringBuilder = new StringBuilder();
                        stringBuilder.AppendFormat("{1:yyyy-MM-dd HH:mm:ss} Error!{0}", Environment.NewLine, DateTime.Now);
                        stringBuilder.AppendFormat("    Listing directories and files{0}", Environment.NewLine);
                        stringBuilder.AppendFormat("    Exception: {1}{0}{0}", Environment.NewLine, e.ToString().Replace(Environment.NewLine, Environment.NewLine + "    "));
                        OnOnError(stringBuilder.ToString());
                    }
                    return files;
                }
                return null;
            }
            return null;
        }

        public event EventHandler<OnErrorEventArgs> OnError;
        private void OnOnError(string message)
        {
            if (OnError != null)
                OnError(this, new OnErrorEventArgs() { Message = message });
        }

        public string SelectedFolder
        {
            get { return (string)GetValue(SelectedFolderProperty); }
            set { SetValue(SelectedFolderProperty, value); }
        }
        public static readonly DependencyProperty SelectedFolderProperty =
            DependencyProperty.Register("SelectedFolder", typeof(string), typeof(PickFolderDialog), new PropertyMetadata(null));

        public bool IncludeSubfolders
        {
            get { return (bool)GetValue(IncludeSubfoldersProperty); }
            set { SetValue(IncludeSubfoldersProperty, value); }
        }
        public static readonly DependencyProperty IncludeSubfoldersProperty =
            DependencyProperty.Register("IncludeSubfolders", typeof(bool), typeof(PickFolderDialog), new PropertyMetadata(false));
    }
}
