using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace budget
{
    class ImportViewModel : INotifyPropertyChanged
    {
        public ImportViewModel(IEnumerable<Transaction> aTransactions)
        {
            Transactions = new ObservableCollection<ImportTransactionViewModel>();
            AvailableAccounts = new ObservableCollection<AvailableAccountViewModel>();

            mTransactions = aTransactions.ToList();

            foreach (var accountNumber in mTransactions.Select(x => x.AccountNumber).Distinct())
            {
                var vm = new AvailableAccountViewModel(accountNumber);
                vm.PropertyChanged += (s, a) => refreshTransactions();
                AvailableAccounts.Add(vm);
            }

            refreshTransactions();
        }

        public class ImportTransactionViewModel
        {
            public string Date { get; set; }
            public string Description { get; set; }
            public int ValueInPennies { get; set; }
            public int BalanceInPennies { get; set; }
            public string AccountName { get; set; }
            public string AccountNumber { get; set; }
        }

        public class AvailableAccountViewModel : INotifyPropertyChanged
        {
            public AvailableAccountViewModel(string aAccountNumber)
            {
                AccountNumber = aAccountNumber;
            }
            public string AccountNumber { get; private set; }
            public bool ShouldImport
            {
                get
                {
                    return mShouldImport;
                }
                set
                {
                    mShouldImport = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShouldImport)));
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            private bool mShouldImport = true;
        }

        public ObservableCollection<ImportTransactionViewModel> Transactions { get; private set; }
        public ObservableCollection<AvailableAccountViewModel> AvailableAccounts { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void refreshTransactions()
        {
            var visibleAccountNumbers = AvailableAccounts.Where(x => x.ShouldImport).Select(x => x.AccountNumber).ToHashSet();

            var releventTransactions = mTransactions.Where(x => visibleAccountNumbers.Contains(x.AccountNumber));

            Transactions.Clear();
            foreach (var transaction in releventTransactions)
            {
                Transactions.Add(new ImportTransactionViewModel
                {
                    Date = transaction.Date,
                    Description = transaction.Description,
                    ValueInPennies = transaction.ValueInPennies,
                    BalanceInPennies = transaction.BalanceInPennies,
                    AccountName = transaction.AccountName,
                    AccountNumber = transaction.AccountNumber
                });
            }
        }

        private List<Transaction> mTransactions;
    }
}
