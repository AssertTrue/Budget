using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace budget
{
    public class PotComboItem : INotifyPropertyChanged
    {
        public PotComboItem(Pot aPot)
        {
            mPot = aPot;
            if (mPot != null)
            {
                mPot.PropertyChanged += (s, a) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(""));
            }
        }
        public int? Id
        {
            get
            {
                return mPot != null ? mPot.Id : (int?)null;
            }
        }
        public string Title
        {
            get
            {
                return mPot != null ? mPot.Title : "<-->";
            }
        }

        private Pot mPot;

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
