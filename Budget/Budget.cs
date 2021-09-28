using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace budget
{
    class Budget : INotifyPropertyChanged
    {
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
        public int AmountInPennies
        {
            get
            {
                return mAmountInPennies;
            }
            set
            {
                mAmountInPennies = value;
                firePropertyChanged();
                firePropertyChanged(nameof(AnnualAmountInPennies));
            }
        }

        public int AnnualAmountInPennies
        {
            get
            {
                return AmountInPennies * 12;
            }
        }

        public int? PotId
        {
            get
            {
                return mPotId;
            }
            set
            {
                mPotId = value;
                firePropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void firePropertyChanged([CallerMemberName] string aPropertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(aPropertyName));
        }

        private int mId;
        private string mTitle;
        private int mAmountInPennies;
        private int? mPotId;
    }
}
