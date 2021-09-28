using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace budget
{
    public class Pot : INotifyPropertyChanged
    {
        public IEnumerable<Transaction> Transactions
        {
            get { return mTransactions; }
            set { mTransactions = value; firePropertyChanged(""); }
        }

        public int Id
        {
            get
            {
                return mId;
            }
            set
            {
                mId = value;
                firePropertyChanged();
            }
        }
        public string Title
        {
            get
            {
                return mTitle;
            }
            set
            {
                mTitle = value;
                firePropertyChanged();
            }
        }
        public int BalanceInPennies
        {
            get
            {
                System.Diagnostics.Debug.Assert(mTransactions != null);
                var outgoing = mTransactions.Where(x => x.OriginId.HasValue && x.OriginId.Value == mId).Sum(x => System.Math.Abs(x.ValueInPennies));
                var incoming = mTransactions.Where(x => x.DestinationId.HasValue && x.DestinationId.Value == mId).Sum(x => System.Math.Abs(x.ValueInPennies));
                return incoming - outgoing;
            }
        }
        public bool IsVisible
        {
            get
            {
                return mIsVisible;
            }
            set
            {
                mIsVisible = value; firePropertyChanged();
            }
        }

        public int Sequence
        {
            get
            {
                return mSequence;
            }
            set
            {
                mSequence = value; firePropertyChanged();
            }
        }

        public bool IsPickable
        {
            get
            {
                return mIsPickable;
            }
            set
            {
                mIsPickable = value; firePropertyChanged();
            }
        }

        public int OriginCount
        {
            get { return mTransactions.Where(x => x.OriginId.HasValue && x.OriginId.Value == mId).Count(); }
        }

        public int DestinationCount
        {
            get { return mTransactions.Where(x => x.DestinationId.HasValue && x.DestinationId.Value == mId).Count(); }
        }

        public void refreshDependentProperties()
        {
            foreach (var propertyName in new[] { nameof(OriginCount),nameof(DestinationCount),nameof(BalanceInPennies) })
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void firePropertyChanged([CallerMemberName] string aPropertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(aPropertyName));
        }

        private int mId;
        private int mSequence;
        private bool mIsVisible;
        private bool mIsPickable;
        private string mTitle;
        private IEnumerable<Transaction> mTransactions;
    }
}
