//----------------------------------------------
//* This code is taken form Kevin William Blog at MSDN as below: */
/* http://blogs.msdn.com/b/kwill/archive/2011/05/30/asynchronous-parallel-block-blob-transfers-with-progress-change-notification.aspx */
//----------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.IO;
using System.Linq;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage;
using System.Threading.Tasks;

namespace BlobTransferUtility.Helpers
{
    internal class BlobTransferHelper
    {
        // Public async events
        public event AsyncCompletedEventHandler TransferCompleted;
        public event EventHandler<BlobTransferProgressChangedEventArgs> TransferProgressChanged;

        // Private variables
        private bool Working = false;
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        // Used to calculate download speeds
        private Queue<long> timeQueue = new Queue<long>(200);
        private Queue<long> bytesQueue = new Queue<long>(200);

        // Private BlobTransfer properties
        private string m_FileName;
        private BlobClient m_Blob;

        public void UploadBlobAsync(BlobClient blobClient, string LocalFile, string contentType)
        {
            // The class currently stores state in class level variables so calling UploadBlobAsync or DownloadBlobAsync a second time will cause problems.
            // A better long term solution would be to better encapsulate the state, but the current solution works for the needs of my primary client.
            // Throw an exception if UploadBlobAsync or DownloadBlobAsync has already been called.
            lock (this)
            {
                if (!Working)
                    Working = true;
                else
                    throw new Exception("BlobTransfer already initiated. Create new BlobTransfer object to initiate a new file transfer.");
            }

            // Attempt to open the file first so that we throw an exception before getting into the async work
            m_Blob = blobClient;
            m_FileName = LocalFile;

            var file = new FileInfo(m_FileName);
            long fileSize = file.Length;

            FileStream fs = new FileStream(m_FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            ProgressStream pstream = new ProgressStream(fs);
            pstream.ProgressChanged += pstream_ProgressChanged;
            pstream.SetLength(fileSize);

            //var options = new BlobUploadOptions()
            //{
            //    TransferOptions = new StorageTransferOptions
            //    {
            //        MaximumConcurrency = 10,
            //        InitialTransferLength = 0x10000,
            //        MaximumTransferSize = 0x10000
            //    }
            //};
            var task = m_Blob.UploadAsync(pstream, /*options, */cancellationTokenSource.Token);
            task.ContinueWith(t => BlobTransferCompleted(t, pstream));
        }

        public void DownloadBlobAsync(BlobClient blobClient, string LocalFile)
        {
            // The class currently stores state in class level variables so calling UploadBlobAsync or DownloadBlobAsync a second time will cause problems.
            // A better long term solution would be to better encapsulate the state, but the current solution works for the needs of my primary client.
            // Throw an exception if UploadBlobAsync or DownloadBlobAsync has already been called.
            lock (this)
            {
                if (!Working)
                    Working = true;
                else
                    throw new Exception("BlobTransfer already initiated. Create new BlobTransfer object to initiate a new file transfer.");
            }

            m_Blob = blobClient;
            m_FileName = LocalFile;

            var properties = m_Blob.GetProperties();

            FileStream fs = new FileStream(m_FileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
            ProgressStream pstream = new ProgressStream(fs);
            pstream.ProgressChanged += pstream_ProgressChanged;
            pstream.SetLength(properties.Value.ContentLength);

            //var options = new StorageTransferOptions
            //{
            //    MaximumConcurrency = 10,
            //    InitialTransferLength = 0x10000,
            //    MaximumTransferSize = 0x10000
            //};
            var task = m_Blob.DownloadToAsync(pstream, /*null, options,*/ cancellationTokenSource.Token);
            task.ContinueWith(t => BlobTransferCompleted(t, pstream));
        }

        private void pstream_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            BlobTransferProgressChangedEventArgs eArgs = null;
            int progress = (int)((double)e.BytesRead / e.TotalLength * 100);

            // raise the progress changed event on the asyncop thread
            eArgs = new BlobTransferProgressChangedEventArgs(e.BytesRead, e.TotalLength, progress, CalculateSpeed(e.BytesRead), null);
            OnTaskProgressChanged(eArgs);
        }

        private void BlobTransferCompleted(Task task, ProgressStream pstream)
        {
            try
            {
                pstream.Close();
            }
            catch (Exception) { }
            
            AsyncCompletedEventArgs completedArgs = new AsyncCompletedEventArgs(task.IsFaulted ? (task.Exception?.InnerException ?? task.Exception) : null, task.IsCanceled, null);
            OnTaskCompleted(completedArgs);
        }

        // Cancel the async download
        public void CancelAsync()
        {
            cancellationTokenSource.Cancel();
        }

        // Helper function to only raise the event if the client has subscribed to it.
        protected virtual void OnTaskCompleted(AsyncCompletedEventArgs e)
        {
            if (TransferCompleted != null)
                TransferCompleted(this, e);
        }

        // Helper function to only raise the event if the client has subscribed to it.
        protected virtual void OnTaskProgressChanged(BlobTransferProgressChangedEventArgs e)
        {
            if (TransferProgressChanged != null)
                TransferProgressChanged(this, e);
        }

        // Keep the last 200 progress change notifications and use them to calculate the average speed over that duration. 
        private double CalculateSpeed(long BytesSent)
        {
            double speed = 0;

            if (timeQueue.Count >= 200)
            {
                timeQueue.Dequeue();
                bytesQueue.Dequeue();
            }

            timeQueue.Enqueue(DateTime.Now.Ticks);
            bytesQueue.Enqueue(BytesSent);

            if (timeQueue.Count > 2)
            {
                speed = (bytesQueue.Max() - bytesQueue.Min()) / TimeSpan.FromTicks(timeQueue.Max() - timeQueue.Min()).TotalSeconds;
            }

            return speed;
        }

        // A modified version of the ProgressStream from http://blogs.msdn.com/b/paolos/archive/2010/05/25/large-message-transfer-with-wcf-adapters-part-1.aspx
        // This class allows progress changed events to be raised from the blob upload/download.
        private class ProgressStream : Stream
        {
            #region Private Fields
            private Stream stream;
            private long bytesTransferred;
            private long totalLength;
            #endregion

            #region Public Handler
            public event EventHandler<ProgressChangedEventArgs> ProgressChanged;
            #endregion

            #region Public Constructor
            public ProgressStream(Stream file)
            {
                stream = file;
                totalLength = file.Length;
                bytesTransferred = 0;
            }
            #endregion

            #region Public Properties
            public override bool CanRead => stream.CanRead;

            public override bool CanSeek => false; // stream.CanSeek;

            public override bool CanWrite => stream.CanWrite;

            public override void Flush() => stream.Flush();

            public override void Close() => stream.Close();

            public override long Length => stream.Length;

            public override long Position
            {
                get => stream.Position;
                set => stream.Position = value;
            }
            #endregion

            #region Public Methods
            public override int Read(byte[] buffer, int offset, int count)
            {
                int result = stream.Read(buffer, offset, count);
                bytesTransferred += result;
                if (ProgressChanged != null)
                {
                    try
                    {
                        OnProgressChanged(new ProgressChangedEventArgs(bytesTransferred, totalLength));
                    }
                    catch (Exception)
                    {
                        ProgressChanged = null;
                    }
                }
                return result;
            }

            protected virtual void OnProgressChanged(ProgressChangedEventArgs e)
            {
                if (ProgressChanged != null)
                    ProgressChanged(this, e);
            }

            public override long Seek(long offset, SeekOrigin origin) => stream.Seek(offset, origin); //throw new NotSupportedException();

            public override void SetLength(long value) => totalLength = value; //stream.SetLength(value);

            public override void Write(byte[] buffer, int offset, int count)
            {
                stream.Write(buffer, offset, count);
                bytesTransferred += count;
                {
                    try
                    {
                        OnProgressChanged(new ProgressChangedEventArgs(bytesTransferred, totalLength));
                    }
                    catch (Exception)
                    {
                        ProgressChanged = null;
                    }
                }
            }

            protected override void Dispose(bool disposing)
            {
                stream.Dispose();
                base.Dispose(disposing);
            }

            #endregion
        }

        private class ProgressChangedEventArgs : EventArgs
        {
            #region Private Fields
            private long bytesRead;
            private long totalLength;
            #endregion

            #region Public Constructor
            public ProgressChangedEventArgs(long bytesRead, long totalLength)
            {
                this.bytesRead = bytesRead;
                this.totalLength = totalLength;
            }
            #endregion

            #region Public properties

            public long BytesRead
            {
                get
                {
                    return this.bytesRead;
                }
                set
                {
                    this.bytesRead = value;
                }
            }

            public long TotalLength
            {
                get
                {
                    return this.totalLength;
                }
                set
                {
                    this.totalLength = value;
                }
            }
            #endregion
        }

        public class BlobTransferProgressChangedEventArgs : System.ComponentModel.ProgressChangedEventArgs
        {
            private long m_BytesSent = 0;
            private long m_TotalBytesToSend = 0;
            private double m_Speed = 0;

            public long BytesSent
            {
                get { return m_BytesSent; }
            }

            public long TotalBytesToSend
            {
                get { return m_TotalBytesToSend; }
            }

            public double Speed
            {
                get { return m_Speed; }
            }

            public TimeSpan TimeRemaining
            {
                get
                {
                    TimeSpan time = new TimeSpan(0, 0, (int)((TotalBytesToSend - m_BytesSent) / (m_Speed == 0 ? 1 : m_Speed)));
                    return time;
                }
            }

            public BlobTransferProgressChangedEventArgs(long BytesSent, long TotalBytesToSend, int progressPercentage, double Speed, object userState)
                : base(progressPercentage, userState)
            {
                m_BytesSent = BytesSent;
                m_TotalBytesToSend = TotalBytesToSend;
                m_Speed = Speed;
            }
        }
    }
}