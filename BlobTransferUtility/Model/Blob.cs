using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobTransferUtility.Model
{
    public class Blob : ModelBase
    {
        private string _ContentType;
        public string ContentType
        {
            get { return _ContentType; }
            set { SetField(ref _ContentType, value, () => ContentType); }
        }

        private string _Container;
        public string Container
        {
            get { return _Container; }
            set { SetField(ref _Container, value, () => Container); }
        }

        private string _StorageAccount;
        public string StorageAccount
        {
            get { return _StorageAccount; }
            set { SetField(ref _StorageAccount, value, () => StorageAccount); }
        }

        private string _StorageAccountKey;
        public string StorageAccountKey
        {
            get { return _StorageAccountKey; }
            set { SetField(ref _StorageAccountKey, value, () => StorageAccountKey); }
        }

        private string _BlobName;
        public string BlobName
        {
            get { return _BlobName; }
            set { SetField(ref _BlobName, value, () => BlobName); }
        }

        private double _SizeInBytes;
        public double SizeInBytes
        {
            get { return _SizeInBytes; }
            set 
            { 
                if (SetField(ref _SizeInBytes, value, () => SizeInBytes))
                    OnPropertyChanged("Size");
            }
        }

        public string Size { get { return SizeUtil.Format(SizeInBytes); } }
    }
}
