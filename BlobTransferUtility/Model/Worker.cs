using BlobTransferUtility.Helpers;
using BlobTransferUtility.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobTransferUtility.Model
{
    public class Worker : ModelBase
    {
        private BlobJob _BlobJob;
        public BlobJob BlobJob
        {
            get { return _BlobJob; }
            set { SetField(ref _BlobJob, value, () => BlobJob); }
        }

        private double _TransferedInBytes;
        public double TransferedInBytes
        {
            get { return _TransferedInBytes; }
            set
            {
                if (SetField(ref _TransferedInBytes, value, () => TransferedInBytes))
                    OnPropertyChanged("Transfered");
            }
        }

        public string Transfered { get { return SizeUtil.Format(TransferedInBytes); } }

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

        private double _SpeedInBytes;
        public double SpeedInBytes
        {
            get { return _SpeedInBytes; }
            set
            {
                if (SetField(ref _SpeedInBytes, value, () => SpeedInBytes))
                    OnPropertyChanged("Speed");
            }
        }
        public string Speed { get { return SizeUtil.Format(SpeedInBytes, "/s"); } }

        private TimeSpan _TimeRemaining;
        public TimeSpan TimeRemaining
        {
            get { return _TimeRemaining; }
            set { SetField(ref _TimeRemaining, value, () => TimeRemaining); }
        }

        private string _Message;
        public string Message
        {
            get { return _Message; }
            set { SetField(ref _Message, value, () => Message); }
        }

        private string _ErrorMessage;
        public string ErrorMessage
        {
            get { return _ErrorMessage; }
            set { SetField(ref _ErrorMessage, value, () => ErrorMessage); }
        }

        private DateTime _Start;
        public DateTime Start
        {
            get { return _Start; }
            set { SetField(ref _Start, value, () => Start); }
        }

        private DateTime _Finish;
        public DateTime Finish
        {
            get { return _Finish; }
            set { SetField(ref _Finish, value, () => Finish); }
        }

        public System.Threading.Thread Thread { get; set; }
    }
}
