using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobTransferUtility.Model
{
    public class File : ModelBase
    {
        private string _Name;
        public string Name
        {
            get { return _Name; }
            set { SetField(ref _Name, value, () => Name); }
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

        private string _FullFilePath;
        public string FullFilePath
        {
            get { return _FullFilePath; }
            set { SetField(ref _FullFilePath, value, () => FullFilePath); }
        }

        private string _RelativeToFolder;
        public string RelativeToFolder
        {
            get { return _RelativeToFolder; }
            set { SetField(ref _RelativeToFolder, value, () => RelativeToFolder); }
        }
    }
}
