using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace budget
{
    public class Transaction : INotifyPropertyChanged
    {
        public int Id
        {
            get { return mId; }
            set { mId = value; firePropertyChanged(); }
        }

        public string Date
        {
            get { return mDate; }
            set { mDate = value; firePropertyChanged(); }
        }

        public string Description
        {
            get { return mDescription; }
            set { mDescription = value; firePropertyChanged(); }
        }

        public int ValueInPennies
        {
            get { return mValueInPennies; }
            set { mValueInPennies = value; firePropertyChanged(); }
        }

        public string AccountName
        {
            get { return mAccountName; }
            set { mAccountName = value; firePropertyChanged(); }
        }

        public string AccountNumber
        {
            get { return mAccountNumber; }
            set { mAccountNumber = value; firePropertyChanged(); }
        }

        public int? OriginId { get { return mOriginId; } set { mOriginId = value; firePropertyChanged(); } }
        public int? DestinationId { get { return mDestinationId; } set { mDestinationId = value; firePropertyChanged(); } }

        public int BalanceInPennies
        {
            get { return mBalanceInPennies; }
            set { mBalanceInPennies = value; firePropertyChanged(); }
        }

        public string Comment
        {
            get { return mComment; }
            set { mComment = value; firePropertyChanged(); }
        }

        public void refreshPotProperties()
        {
            firePropertyChanged(nameof(OriginId));
            firePropertyChanged(nameof(DestinationId));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void firePropertyChanged([CallerMemberName] string aPropertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(aPropertyName));
        }

        private int mId;
        private string mComment;
        private string mDate;
        private int mBalanceInPennies;
        private int mValueInPennies;
        private string mDescription;
        private int? mOriginId;
        private int? mDestinationId;
        private string mAccountNumber;
        private string mAccountName;
    }
}
