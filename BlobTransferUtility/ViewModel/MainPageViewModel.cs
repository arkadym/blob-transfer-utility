using BlobTransferUtility.Model;
using Microsoft.Win32;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace BlobTransferUtility.ViewModel
{
    public class OnErrorEventArgs : EventArgs
    {
        public string Message { get; set; }
    }

    public class MainPageViewModel : ModelBase
    {
        public event EventHandler<OnErrorEventArgs> OnError;
        private void OnOnError(string message)
        {
            if (OnError != null)
                ExecuteOnUI(()=>OnError(this, new OnErrorEventArgs() { Message = message }));
        }

        public MainPageViewModel()
        {
            Files = new ObservableCollection<BlobTransferUtility.Model.File>();
            Blobs = new ObservableCollection<BlobTransferUtility.Model.Blob>();
            WorkerManager = new WorkerManager();
            WorkerManager.OnError += OnErrorHandler;
        }

        void OnErrorHandler(object sender, OnErrorEventArgs e)
        {
            OnOnError(e.Message);
        }

        public void Load()
        {
            SelectedFolder = ConfigurationManager.AppSettings["DefaultFolder"];
            if (string.IsNullOrEmpty(SelectedFolder))
                SelectedFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            DefaultBlobNameFormat = ConfigurationManager.AppSettings["DefaultBlobNameFormat"];
            DefaultContainerName = ConfigurationManager.AppSettings["DefaultContainerName"];
            DefaultContentType = ConfigurationManager.AppSettings["DefaultContentType"];
            DefaultStorageAccount = ConfigurationManager.AppSettings["DefaultStorageAccount"];
            DefaultStorageAccountKey = ConfigurationManager.AppSettings["DefaultStorageAccountKey"];
        }

        private ICommand _OpenLinkCommand;
        public ICommand OpenLinkCommand
        {
            get
            {
                if (_OpenLinkCommand == null)
                    _OpenLinkCommand = new ActionCommand(OnOpenLinkCommand);
                return _OpenLinkCommand;
            }
        }

        private void OnOpenLinkCommand(object param)
        {
            System.Diagnostics.Process.Start((string)param);
        }

        private ICommand _SelectFolderAndFilesCommand;
        public ICommand SelectFolderAndFilesCommand
        {
            get
            {
                if (_SelectFolderAndFilesCommand == null)
                    _SelectFolderAndFilesCommand = new ActionCommand(OnSelectFolderAndFilesCommand);
                return _SelectFolderAndFilesCommand;
            }
        }

        private void OnSelectFolderAndFilesCommand(object param)
        {
            var dialog = new PickFolderDialog();
            dialog.OnError += OnErrorHandler;
            dialog.SelectedFolder = SelectedFolder;
            var files = dialog.PickFolderAndFiles(); 
            if (files != null)
            {
                SelectedFolder = dialog.SelectedFolder;
                Files.Clear();
                foreach (var file in files)
                    Files.Add(file);
            }
        }

        private ICommand _ListBlobsCommand;
        public ICommand ListBlobsCommand
        {
            get
            {
                if (_ListBlobsCommand == null)
                    _ListBlobsCommand = new ActionCommand(OnListBlobs);
                return _ListBlobsCommand;
            }
        }

        private void OnListBlobs(object param)
        {
            Task.Factory.StartNew(() =>
            {
                ExecuteOnUI(() =>
                {
                    Blobs.Clear();
                });
                try
                {
                    var storageAccount = CloudStorageAccount.Parse(string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", DefaultStorageAccount, DefaultStorageAccountKey));
                    var blobClient = storageAccount.CreateCloudBlobClient();
                    var containerReference = blobClient.GetContainerReference(DefaultContainerName);
                    var blobs = new List<Blob>();
                    foreach (var blobItem in containerReference.ListBlobs(new BlobRequestOptions() {
                         UseFlatBlobListing = true,
                    }))
                    {
                        if (blobItem is CloudBlob)
                        {
                            var blob = blobItem as CloudBlob;
                            blobs.Add(new Blob()
                            {
                                BlobName = blob.Name,
                                Container = DefaultContainerName,
                                ContentType = blob.Properties.ContentType,
                                SizeInBytes = blob.Properties.Length,
                                StorageAccount = DefaultStorageAccount,
                                StorageAccountKey = DefaultStorageAccountKey,
                            });
                        }
                    }


                    ExecuteOnUI(() =>
                    {
                        foreach (var blob in blobs)
                            Blobs.Add(blob);
                    });
                }
                catch (Exception e)
                {
                    var stringBuilder = new StringBuilder();
                    stringBuilder.AppendFormat("{1:yyyy-MM-dd HH:mm:ss} Error!{0}", Environment.NewLine, DateTime.Now);
                    stringBuilder.AppendFormat("    Listing blobs{0}", Environment.NewLine);
                    stringBuilder.AppendFormat("    Account: {1}{0}", Environment.NewLine, DefaultStorageAccount);
                    stringBuilder.AppendFormat("    Container: {1}{0}", Environment.NewLine, DefaultContainerName);
                    stringBuilder.AppendFormat("    Exception: {1}{0}{0}", Environment.NewLine, e.ToString().Replace(Environment.NewLine, Environment.NewLine + "    "));
                    OnOnError(stringBuilder.ToString());
                }
            });
        }

        private ICommand _PickContainerCommand;
        public ICommand PickContainerCommand
        {
            get
            {
                if (_PickContainerCommand == null)
                    _PickContainerCommand = new ActionCommand(OnPickContainer);
                return _PickContainerCommand;
            }
        }

        private async void OnPickContainer(object param)
        {
            var dialog = new SelectItemDialog();
            var dialogResult = await dialog.ShowDialog(Application.Current.MainWindow, "Select Container", () =>
            {
                return Task.Factory.StartNew<IEnumerable<object>>(() =>
                {
                    try
                    {
                        var storageAccount = CloudStorageAccount.Parse(string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", DefaultStorageAccount, DefaultStorageAccountKey));
                        var blobClient = storageAccount.CreateCloudBlobClient();
                        var containers = new List<Container>();
                        foreach (var container in blobClient.ListContainers())
                        {
                            containers.Add(new Container()
                            {
                                Name = container.Name,
                                StorageAccount = DefaultStorageAccount,
                                StorageAccountKey = DefaultStorageAccountKey,
                            });
                        }
                        return containers;
                    }
                    catch (Exception e) 
                    {
                        var stringBuilder = new StringBuilder();
                        stringBuilder.AppendFormat("{1:yyyy-MM-dd HH:mm:ss} Error!{0}", Environment.NewLine, DateTime.Now);
                        stringBuilder.AppendFormat("    Listing containers{0}", Environment.NewLine);
                        stringBuilder.AppendFormat("    Account: {1}{0}", Environment.NewLine, DefaultStorageAccount);
                        stringBuilder.AppendFormat("    Exception: {1}{0}{0}", Environment.NewLine, e.ToString().Replace(Environment.NewLine, Environment.NewLine + "    "));
                        OnOnError(stringBuilder.ToString());
                    }
                    return null;
                });
            }, "Name");
            if (dialogResult)
            {
                DefaultContainerName = ((Container)dialog.SelectedItem).Name;
            }
        }

        private ICommand _SelectFolderCommand;
        public ICommand SelectFolderCommand
        {
            get
            {
                if (_SelectFolderCommand == null)
                    _SelectFolderCommand = new ActionCommand(OnSelectFolder);
                return _SelectFolderCommand;
            }
        }

        private void OnSelectFolder(object param)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog() { ShowNewFolderButton = true, SelectedPath = SelectedFolder };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SelectedFolder = dialog.SelectedPath;
            }
        }

        private ICommand _SelectFilesCommand;
        public ICommand SelectFilesCommand
        {
            get
            {
                if (_SelectFilesCommand == null)
                    _SelectFilesCommand = new ActionCommand(OnSelectFiles);
                return _SelectFilesCommand;
            }
        }

        private void OnSelectFiles(object param)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog() { CheckFileExists = true, InitialDirectory = SelectedFolder, Multiselect = true };
            if (dialog.ShowDialog().GetValueOrDefault())
            {
                SelectedFolder = Path.GetDirectoryName(dialog.FileNames[0]);
                Files.Clear();
                foreach (var file in dialog.FileNames)
                {
                    var fileInfo = new FileInfo(file);
                    Files.Add(new Model.File()
                    {
                        Name = Path.GetFileName(file),
                        FullFilePath = file,
                        SizeInBytes = fileInfo.Length,
                        RelativeToFolder = Path.GetDirectoryName(file) + Path.DirectorySeparatorChar,
                    });
                }
            }
        }

        private ICommand _RemoveSelectedBlobsCommand;
        public ICommand RemoveSelectedBlobsCommand
        {
            get
            {
                if (_RemoveSelectedBlobsCommand == null)
                    _RemoveSelectedBlobsCommand = new ActionCommand(OnRemoveSelectedBlobs);
                return _RemoveSelectedBlobsCommand;
            }
        }

        private void OnRemoveSelectedBlobs(object param)
        {
            var selectedItems = ((IList)param).OfType<object>().ToList();
            foreach (BlobTransferUtility.Model.Blob blob in selectedItems)
            {
                Blobs.Remove(blob);
            }
        }

        private ICommand _RemoveSelectedFilesCommand;
        public ICommand RemoveSelectedFilesCommand
        {
            get
            {
                if (_RemoveSelectedFilesCommand == null)
                    _RemoveSelectedFilesCommand = new ActionCommand(OnRemoveSelectedFiles);
                return _RemoveSelectedFilesCommand;
            }
        }

        private void OnRemoveSelectedFiles(object param)
        {
            var selectedItems = ((IList)param).OfType<object>().ToList();
            foreach (BlobTransferUtility.Model.File file in selectedItems)
            {
                Files.Remove(file);
            }
        }

        private ICommand _RemoveSelectedQueueCommand;
        public ICommand RemoveSelectedQueueCommand
        {
            get
            {
                if (_RemoveSelectedQueueCommand == null)
                    _RemoveSelectedQueueCommand = new ActionCommand(OnRemoveSelectedQueue);
                return _RemoveSelectedQueueCommand;
            }
        }

        private void OnRemoveSelectedQueue(object param)
        {
            var selectedItems = ((IList)param).OfType<object>().ToList();
            lock (WorkerManager.Queue)
            {
                foreach (BlobJob job in selectedItems)
                {
                    WorkerManager.Queue.Remove(job);
                }
            }
        }

        private ICommand _CancelWorkerCommand;
        public ICommand CancelWorkerCommand
        {
            get
            {
                if (_CancelWorkerCommand == null)
                    _CancelWorkerCommand = new ActionCommand(OnCancelWorkerCommand);
                return _CancelWorkerCommand;
            }
        }

        private void OnCancelWorkerCommand(object param)
        {
            var selectedItems = ((IList)param).OfType<object>().ToList();
            lock (WorkerManager.Workers)
            {
                foreach (Worker worker in selectedItems)
                {
                    WorkerManager.CancelWorker(worker);
                }
            }
        }

        private ICommand _EnqueueWorkerCommand;
        public ICommand EnqueueWorkerCommand
        {
            get
            {
                if (_EnqueueWorkerCommand == null)
                    _EnqueueWorkerCommand = new ActionCommand(OnEnqueueWorkerCommand);
                return _EnqueueWorkerCommand;
            }
        }

        private void OnEnqueueWorkerCommand(object param)
        {
            var selectedItems = ((IList)param).OfType<object>().ToList();
            lock (WorkerManager.Queue)
            {
                lock (WorkerManager.Workers)
                {
                    foreach (Worker worker in selectedItems)
                    {
                        WorkerManager.CancelWorker(worker);
                        WorkerManager.Queue.Add(worker.BlobJob);
                    }
                }
            }
        }

        private ICommand _ClearQueueCommand;
        public ICommand ClearQueueCommand
        {
            get
            {
                if (_ClearQueueCommand == null)
                    _ClearQueueCommand = new ActionCommand(OnClearQueueCommand);
                return _ClearQueueCommand;
            }
        }

        private void OnClearQueueCommand(object param)
        {
            lock (WorkerManager.Queue)
            {
                WorkerManager.Queue.Clear();
            }
        }

        private ICommand _AddDownloadToQueueCommand;
        public ICommand AddDownloadToQueueCommand
        {
            get
            {
                if (_AddDownloadToQueueCommand == null)
                {
                    _AddDownloadToQueueCommand = new ActionCommand(OnAddDownloadToQueue);
                }
                return _AddDownloadToQueueCommand;
            }
        }

        private void OnAddDownloadToQueue(object param)
        {
            lock (WorkerManager.Queue)
            {
                foreach (var blob in Blobs)
                {
                    WorkerManager.Queue.Add(new BlobJob()
                    {
                        JobType = BlobJobType.Download,
                        File = new Model.File()
                        {
                            FullFilePath = Path.Combine(SelectedFolder, blob.BlobName.Replace("/","\\")),
                            Name = blob.BlobName.Replace("/", "\\"),
                            SizeInBytes = blob.SizeInBytes,
                            RelativeToFolder = SelectedFolder,
                        },
                        SizeInBytes = blob.SizeInBytes,
                        BlobName = blob.BlobName,
                        Container = blob.Container,
                        ContentType = blob.ContentType,
                        StorageAccount = blob.StorageAccount,
                        StorageAccountKey = blob.StorageAccountKey,
                    });
                }
            }
            Blobs.Clear();
        }

        private ICommand _AddUploadToQueueCommand;
        public ICommand AddUploadToQueueCommand
        {
            get
            {
                if (_AddUploadToQueueCommand == null)
                {
                    _AddUploadToQueueCommand = new ActionCommand(OnAddUploadToQueue);
                }
                return _AddUploadToQueueCommand;
            }
        }

        private void OnAddUploadToQueue(object param)
        {
            lock (WorkerManager.Queue)
            {
                foreach (var file in Files)
                {
                    var fileFolders = file.Name.Split(System.IO.Path.DirectorySeparatorChar);
                    var containerName =
                        (fileFolders.Length > 1) && UseFirstLevelAsContainerName ? 
                        fileFolders[0] : DefaultContainerName;

                    WorkerManager.Queue.Add(new BlobJob()
                    {
                        JobType = BlobJobType.Upload,
                        File = file,
                        SizeInBytes = file.SizeInBytes,
                        BlobName = string.Format(DefaultBlobNameFormat, file.Name).Replace("\\","/"),
                        Container = containerName.ToLower(),
                        ContentType = DefaultContentType,
                        StorageAccount = DefaultStorageAccount,
                        StorageAccountKey = DefaultStorageAccountKey,
                    });
                }
            }
            Files.Clear();
        }

        public WorkerManager WorkerManager { get; private set; }
        public ObservableCollection<BlobTransferUtility.Model.File> Files { get; private set; }
        public ObservableCollection<BlobTransferUtility.Model.Blob> Blobs { get; private set; }

        private string _CurrentFolder;
        public string CurrentFolder
        {
            get { return _CurrentFolder; }
            set { SetField(ref _CurrentFolder, value, () => CurrentFolder); }
        }

        private string _DefaultStorageAccount;
        public string DefaultStorageAccount
        {
            get { return _DefaultStorageAccount; }
            set { SetField(ref _DefaultStorageAccount, value, () => DefaultStorageAccount); }
        }

        private string _DefaultStorageAccountKey;
        public string DefaultStorageAccountKey
        {
            get { return _DefaultStorageAccountKey; }
            set { SetField(ref _DefaultStorageAccountKey, value, () => DefaultStorageAccountKey); }
        }

        private string _DefaultContentType;
        public string DefaultContentType
        {
            get { return _DefaultContentType; }
            set { SetField(ref _DefaultContentType, value, () => DefaultContentType); }
        }

        private string _DefaultContainerName;
        public string DefaultContainerName
        {
            get { return _DefaultContainerName; }
            set { SetField(ref _DefaultContainerName, value, () => DefaultContainerName); }
        }

        private string _DefaultBlobNameFormat;
        public string DefaultBlobNameFormat
        {
            get { return _DefaultBlobNameFormat; }
            set { SetField(ref _DefaultBlobNameFormat, value, () => DefaultBlobNameFormat); }
        }

        private string _SelectedFolder;
        public string SelectedFolder
        {
            get { return _SelectedFolder; }
            set { SetField(ref _SelectedFolder, value, () => SelectedFolder); }
        }

        private bool _UseFirstLevelAsContainerName;
        public bool UseFirstLevelAsContainerName
        {
            get { return _UseFirstLevelAsContainerName; }
            set { SetField(ref _UseFirstLevelAsContainerName, value, () => UseFirstLevelAsContainerName); }
        }
    }
}
