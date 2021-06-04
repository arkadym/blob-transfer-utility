using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobTransferUtility.Model
{
    public enum BlobJobType 
    { 
        Upload,
        Download,
        SetMetadata,
    }

    public class BlobJob : Blob
    {
        private BlobJobType _JobType;
        public BlobJobType JobType
        {
            get { return _JobType; }
            set { SetField(ref _JobType, value, () => JobType); }
        }

        private File _File;
        public File File
        {
            get { return _File; }
            set { SetField(ref _File, value, () => File); }
        }
    }
}
