using BlobTransferUtility.Helpers;
using BlobTransferUtility.Model;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace BlobTransferUtility.ViewModel
{
    public class WorkerManager : ModelBase
    {
        public event EventHandler<OnErrorEventArgs> OnError;
        private void OnOnError(string message)
        {
            if (OnError != null)
                OnError(this, new OnErrorEventArgs() { Message = message });
        }

        public WorkerManager()
        {
            Workers = new ObservableCollection<Worker>();
            WorkersHistory = new ObservableCollection<Worker>();
            Queue = new ObservableCollection<BlobJob>();

            _Timer = new Thread(new ThreadStart(() =>
            {
                while (Application.Current != null)
                {
                    CheckForJobs();
                    UpdateStats();
                    Thread.Sleep(500);
                }
            }));
            _Timer.Start();
        }

        private void UpdateStats()
        {
            try
            {
                var totalQueueSizeInBytes = Queue.ToList().Sum((q) => q.File == null ? 0d : q.File.SizeInBytes);
                var totalToTransferInBytes = Workers.ToList().Sum((w) => w.SizeInBytes);

                var speedSample = new List<Worker>(Workers.Where((w) => w.SpeedInBytes > 0));
                speedSample.AddRange(WorkersHistory.Skip(WorkersHistory.Count - 10).Where((w) => w.SpeedInBytes > 0));
                var currentSpeed = speedSample.Count == 0 ? 0d : speedSample.Average((w) => w.SpeedInBytes);

                _SpeedHistory.Add(currentSpeed);
                while (_SpeedHistory.Count > 120)
                    _SpeedHistory.RemoveAt(0);

                var averageSpeed = _SpeedHistory.Average((s) => s);
                if (Workers.Count == 0)
                    averageSpeed = 0;

                AverageSpeed = SizeUtil.Format(averageSpeed, "/s");
                TotalQueueSize = SizeUtil.Format(totalQueueSizeInBytes);
                TotalToTransfer = SizeUtil.Format(totalToTransferInBytes);
                TotalTransfered = SizeUtil.Format(Workers.ToList().Sum((w) => w.TransferedInBytes));
                RemainingTime = Workers.Count == 0 ? TimeSpan.FromSeconds(0) : Workers.ToList().Max((w) => w.TimeRemaining);
                QueueCount = Queue.Count;
                WorkersCount = Workers.Count;
                QueueRemainingTime = TimeSpan.FromSeconds(averageSpeed == 0 ? 0 : Convert.ToInt32((totalQueueSizeInBytes + totalToTransferInBytes) / averageSpeed));
            }
            catch { }
        }

        private Thread _Timer;
        private List<double> _SpeedHistory = new List<double>();
        public ObservableCollection<BlobJob> Queue { get; private set; }
        public ObservableCollection<Worker> Workers { get; private set; }
        public ObservableCollection<Worker> WorkersHistory { get; private set; }

        private TimeSpan _RemainingTime;
        public TimeSpan RemainingTime
        {
            get { return _RemainingTime; }
            set { SetField(ref _RemainingTime, value, () => RemainingTime); }
        }

        private TimeSpan _QueueRemainingTime;
        public TimeSpan QueueRemainingTime
        {
            get { return _QueueRemainingTime; }
            set { SetField(ref _QueueRemainingTime, value, () => QueueRemainingTime); }
        }

        private string _AverageSpeed;
        public string AverageSpeed
        {
            get { return _AverageSpeed; }
            set { SetField(ref _AverageSpeed, value, () => AverageSpeed); }
        }

        private string _TotalTransfered;
        public string TotalTransfered
        {
            get { return _TotalTransfered; }
            set { SetField(ref _TotalTransfered, value, () => TotalTransfered); }
        }

        private string _TotalToTransfer;
        public string TotalToTransfer
        {
            get { return _TotalToTransfer; }
            set { SetField(ref _TotalToTransfer, value, () => TotalToTransfer); }
        }

        private string _TotalQueueSize;
        public string TotalQueueSize
        {
            get { return _TotalQueueSize; }
            set { SetField(ref _TotalQueueSize, value, () => TotalQueueSize); }
        }

        private int _QueueCount;
        public int QueueCount
        {
            get { return _QueueCount; }
            set { SetField(ref _QueueCount, value, () => QueueCount); }
        }

        private int _WorkersCount;
        public int WorkersCount
        {
            get { return _WorkersCount; }
            set { SetField(ref _WorkersCount, value, () => WorkersCount); }
        }

        private int _MaxWorkers;
        public int MaxWorkers
        {
            get { return _MaxWorkers; }
            set
            {
                SetField(ref _MaxWorkers, value, () => MaxWorkers);
                CheckForJobs();
            }
        }

        private void CheckForJobs()
        {
            try
            {
                var availableWorkers = MaxWorkers - Workers.Count;
                var jobs = Queue.Take(availableWorkers).ToList();

                ExecuteOnUI(() =>
                {
                    foreach (var job in jobs)
                    {
                        Queue.Remove(job);
                        CreateWorker(job);
                    }
                });
            }
            catch { }
        }

        private void CreateFakeWorker(BlobJob BlobJob)
        {
            var worker = new Worker()
            {
                BlobJob = BlobJob,
                SizeInBytes = BlobJob.SizeInBytes,
            };
            ExecuteOnUI(() =>
            {
                Workers.Add(worker);
            });

            var random = new Random();

            worker.SizeInBytes = (Convert.ToInt64(1000000 * random.NextDouble()) + 1000000);

            var backgroundWorker = new Thread(new ThreadStart(() =>
                {
                    while (worker.TransferedInBytes < worker.SizeInBytes)
                    {
                        ExecuteOnUI(() =>
                        {
                            worker.SpeedInBytes = random.Next(100000);
                            worker.TransferedInBytes += worker.SpeedInBytes;
                            worker.TimeRemaining = TimeSpan.FromSeconds((worker.SizeInBytes - worker.TransferedInBytes) / worker.SpeedInBytes);
                        });
                        Thread.Sleep(1000);
                    }
                    worker.Message = "Success";
                    ArchiveWorker(worker);
                }));
            backgroundWorker.Start();
        }

        private void CreateWorker(BlobJob job)
        {
            var worker = new Worker()
            {
                BlobJob = job,
                SizeInBytes = job.SizeInBytes,
            };
            ExecuteOnUI(() =>
            {
                Workers.Add(worker);
            });

            try
            {
                worker.Thread = new Thread(new ThreadStart(() =>
                {
                    worker.Start = DateTime.Now;
                    try
                    {
                        var storageAccount = CloudStorageAccount.Parse(string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", job.StorageAccount, job.StorageAccountKey));
                        var blobClient = storageAccount.CreateCloudBlobClient();

                        //var serviceProperties = blobClient.GetServiceProperties();
                        //if (!"2012-02-12".Equals(serviceProperties.DefaultServiceVersion))
                        //{
                        //    serviceProperties.DefaultServiceVersion = "2012-02-12";
                        //    blobClient.SetServiceProperties(serviceProperties);
                        //}

                        var containerReference = blobClient.GetContainerReference(job.Container);

                        containerReference.CreateIfNotExist();
                        var blobReference = containerReference.GetBlobReference(job.BlobName);

                        try
                        {
                            if (System.IO.File.Exists(job.File.FullFilePath))
                            {
                                blobReference.FetchAttributes();
                                var fileInfo = new FileInfo(job.File.FullFilePath);
                                if (blobReference.Attributes.Properties.Length == fileInfo.Length)
                                {
                                    worker.Message = "Same file already exists";
                                    worker.Finish = DateTime.Now;
                                    ArchiveWorker(worker);
                                    return;
                                }
                            }
                        }
                        catch { }

                        if (job.SizeInBytes > 1 * 1024 * 1024)
                        {
                            var blobTransferHelper = new BlobTransferHelper();
                            blobTransferHelper.TransferCompleted += (sender, e) =>
                            {
                                if (!string.IsNullOrEmpty(job.ContentType)
                                    && (job.JobType == BlobJobType.Upload || job.JobType == BlobJobType.SetMetadata))
                                {
                                    blobReference.Properties.ContentType = job.ContentType;
                                    blobReference.SetProperties();
                                }

                                worker.Message = "Success";
                                worker.Finish = DateTime.Now;
                                ArchiveWorker(worker);
                            };
                            blobTransferHelper.TransferProgressChanged += (sender, e) =>
                            {
                                worker.TransferedInBytes = e.BytesSent;
                                worker.SpeedInBytes = e.Speed;
                                worker.TimeRemaining = TimeSpan.FromSeconds(Convert.ToInt32(e.TimeRemaining.TotalSeconds));
                                worker.SizeInBytes = e.TotalBytesToSend;
                            };

                            switch (job.JobType)
                            {
                                case BlobJobType.Upload:
                                    blobTransferHelper.UploadBlobAsync(blobReference, job.File.FullFilePath);
                                    break;
                                case BlobJobType.Download:
                                    Directory.CreateDirectory(Path.GetDirectoryName(job.File.FullFilePath));
                                    blobTransferHelper.DownloadBlobAsync(blobReference, job.File.FullFilePath);
                                    break;
                            }
                        }
                        else
                        {
                            switch (job.JobType)
                            {
                                case BlobJobType.Upload:
                                    blobReference.UploadFile(job.File.FullFilePath);
                                    break;
                                case BlobJobType.Download:
                                    Directory.CreateDirectory(Path.GetDirectoryName(job.File.FullFilePath));
                                    blobReference.DownloadToFile(job.File.FullFilePath);
                                    break;
                            }

                            if (!string.IsNullOrEmpty(job.ContentType)
                                && (job.JobType == BlobJobType.Upload || job.JobType == BlobJobType.SetMetadata))
                            {
                                blobReference.Properties.ContentType = job.ContentType;
                                blobReference.SetProperties();
                            }

                            worker.Message = "Success";
                            worker.TransferedInBytes = worker.SizeInBytes;
                            worker.Finish = DateTime.Now;
                            worker.SpeedInBytes = worker.SizeInBytes / (worker.Finish - worker.Start).TotalSeconds;
                            ArchiveWorker(worker);
                        }
                    }
                    catch (ThreadAbortException)
                    {
                        worker.Message = "Cancelled";
                        worker.Finish = DateTime.Now;
                        ArchiveWorker(worker);
                    }
                    catch (Exception e)
                    {
                        worker.Message = "Error";
                        worker.ErrorMessage = string.Format("{0}", e.ToString());
                        worker.Finish = DateTime.Now;
                        ArchiveWorker(worker);
                        OnJobError(job, e);
                    }
                }));
                worker.Thread.Start();
            }
            catch (Exception e)
            {
                worker.Message = "Error";
                worker.ErrorMessage = string.Format("{0}", e.ToString());
                ArchiveWorker(worker);
                OnJobError(job, e);
            }
        }

        private void OnJobError(BlobJob job, Exception e)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("{1:yyyy-MM-dd HH:mm:ss} Error!{0}", Environment.NewLine, DateTime.Now);
            stringBuilder.AppendFormat("    Job Type: {1}{0}", Environment.NewLine, job.JobType);
            stringBuilder.AppendFormat("    Account: {1}{0}", Environment.NewLine, job.StorageAccount);
            stringBuilder.AppendFormat("    Container: {1}{0}", Environment.NewLine, job.Container);
            stringBuilder.AppendFormat("    Blob: {1}{0}", Environment.NewLine, job.BlobName);
            stringBuilder.AppendFormat("    File: {1}{0}", Environment.NewLine, job.File.FullFilePath);
            stringBuilder.AppendFormat("    Exception: {1}{0}{0}", Environment.NewLine, e.ToString().Replace(Environment.NewLine, Environment.NewLine + "    "));
            OnOnError(stringBuilder.ToString());
        }

        private void ArchiveWorker(Worker worker)
        {
            worker.Thread = null;
            worker.BlobJob.StorageAccountKey = null;
            ExecuteOnUI(() =>
            {
                Workers.Remove(worker);
                WorkersHistory.Add(worker);
            });
        }

        public void CancelWorker(Worker worker)
        {
            try
            {
                worker.Thread.Abort();
            }
            catch { }
            ExecuteOnUI(() =>
            {
                Workers.Remove(worker);
            });
        }
    }
}
