using BlobTransferUtility.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BlobTransferUtility
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        public MainPageViewModel ViewModel { get { return DataContext as MainPageViewModel; } }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = new MainPageViewModel();
            ViewModel.Load();
            ViewModel.OnError += ViewModel_OnError;
        }

        void ViewModel_OnError(object sender, OnErrorEventArgs e)
        {
            errorTextBox.AppendText(e.Message);
            errorTextBox.AppendText(Environment.NewLine);
        }
    }
}
