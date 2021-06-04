using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobTransferUtility.Model
{
    public class Container : ModelBase
    {
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

        private string _Name;
        public string Name
        {
            get { return _Name; }
            set { SetField(ref _Name, value, () => Name); }
        }

    }
}
