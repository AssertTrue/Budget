using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace budget
{
    internal class MainViewModel : INotifyPropertyChanged
    {
        #region Constructor
        public MainViewModel()
        {
            FileOpen = false;
            Transactions = new ObservableCollectionFast<Transaction>();
            VisiblePots = new ObservableCollectionFast<Pot>();
            VisibleTransactions = new ObservableCollectionFast<Transaction>();
            Pots = new ObservableCollectionFast<Pot>();
            Budgets = new ObservableCollectionFast<Budget>();
            AvailablePots = new ObservableCollectionFast<PotComboItem>();

            Transactions.CollectionChanged += (s, a) =>
            {
                refreshVisibleTransactions();
                activateSave();
            };

            Pots.CollectionChanged += (s, a) =>
            {
                refreshVisiblePots();
                refreshPotComboItems();
                activateSave();
            };

            Budgets.CollectionChanged += (s, a) =>
            {
                refreshBudgetTotalBalanceInPennies();
                activateSave();
            };


        }
        #endregion

        #region File menu

        #region Properties and commands
        public bool FileOpen { get; private set; }
        public RelayCommand New => new RelayCommand((o) => { onNew(); }, (o) => { return !FileOpen; });
        public RelayCommand Open => new RelayCommand((o) => { onOpen(); }, (o) => { return !FileOpen; });
        public RelayCommand Import => new RelayCommand((o) => { onImport(); }, (o) => { return FileOpen; });
        public RelayCommand Save => new RelayCommand((o) => { onSave(); }, (o) => { return FileOpen && mCanSave; });
        public RelayCommand Close => new RelayCommand((o) => { onClose(); }, (o) => { return FileOpen; });
        #endregion

        #region New handling
        private void onNew()
        {
            Debug.Assert(!FileOpen);

            var fileDialog = new Microsoft.Win32.SaveFileDialog();
            fileDialog.DefaultExt = ".sqlite3";
            fileDialog.Filter = "Budget files (*.sqlite3)|*.sqlite3";

            if (fileDialog.ShowDialog() == true)
            {
                if (System.IO.File.Exists(fileDialog.FileName))
                {
                    System.IO.File.Delete(fileDialog.FileName);
                }

                Accounts.createNewFile(fileDialog.FileName);
                openFile(fileDialog.FileName);
            }
        }
        #endregion

        #region Open handling
        private void loadTransactions(IEnumerable<Transaction> aTransactions)
        {
            Util.unregister(Transactions, onTransactionChanged);
            Transactions.Reset(aTransactions);
            Util.register(Transactions, onTransactionChanged);
        }

        private void loadPots(IEnumerable<Pot> aPots)
        {
            var loadedPots = aPots.OrderBy(x => x.Sequence);
            Util.unregister(Pots, onPotChanged);
            foreach (var pot in loadedPots)
            {
                pot.Transactions = Transactions;
            }
            Pots.Reset(loadedPots);
            Util.register(Pots, onPotChanged);
        }

        private void loadBudgets(IEnumerable<Budget> aBudgets)
        {
            Util.unregister(Budgets, onBudgetChanged);
            Budgets.Reset(aBudgets);
            Util.register(Budgets, onBudgetChanged);
        }

        private void onOpen()
        {
            Debug.Assert(!FileOpen);

            var fileDialog = new Microsoft.Win32.OpenFileDialog();
            fileDialog.DefaultExt = ".sqlite3";
            fileDialog.Filter = "Budget files (*.sqlite3)|*.sqlite3";

            if (fileDialog.ShowDialog() == true)
            {
                openFile(fileDialog.FileName);
            }
        }

        private void openFile(string aFilePath)
        {
            mFilePath = aFilePath;
            if (System.IO.File.Exists(mFilePath))
            {
                Tuple<List<Transaction>, List<Pot>, List<Budget>> tuple = Accounts.loadData(mFilePath);

                loadPots(tuple.Item2);
                loadTransactions(tuple.Item1);
                loadBudgets(tuple.Item3);

                refreshPotTransactionProperties();

                FileOpen = true;

                fireAllPropertiesChanged();
            }
        }
        #endregion

        #region Close handling
        private void onClose()
        {
            if (shouldContinueWithClose())
            {
                FileOpen = false;
                mFilePath = null;
                Transactions.Clear();
                Pots.Clear();
                Budgets.Clear();
                fireAllPropertiesChanged();
            }
        }

        private bool shouldContinueWithClose()
        {
            if (mCanSave)
            {
                var result = MessageBox.Show("Do you wish to save changes?", "Save changes?", MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Yes)
                {
                    onSave();
                    return true;
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return false;
                }
            }
            return true;
        }
        #endregion

        #region Save handling
        private void onSave()
        {
            Debug.Assert(FileOpen);

            if (FileOpen)
            {
                Accounts.save(mFilePath, Transactions, Pots, Budgets);
                mCanSave = false;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Save)));
            }
        }


        private void activateSave()
        {
            mCanSave = true;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Save)));
        }
        #endregion

        #region Import handling
        private void onImport()
        {
            var fileDialog = new Microsoft.Win32.OpenFileDialog();
            fileDialog.DefaultExt = ".csv";
            fileDialog.Filter = "Transactions (*.csv)|*.csv";

            if (fileDialog.ShowDialog() == true)
            {
                var path = fileDialog.FileName;
                if (System.IO.File.Exists(path))
                {
                    var dialog = new ImportDialog();
                    var transactions = Util.importTransactions(path);
                    var vm = new ImportViewModel(transactions);
                    dialog.DataContext = vm;
                    if (dialog.ShowDialog() == true)
                    {
                        foreach (var transaction in vm.Transactions)
                        {
                            var newTransactions = addTransaction();
                            newTransactions.Date = transaction.Date;
                            newTransactions.Description = transaction.Description;
                            newTransactions.ValueInPennies = transaction.ValueInPennies;
                            newTransactions.BalanceInPennies = transaction.BalanceInPennies;
                            newTransactions.AccountName = transaction.AccountName;
                            newTransactions.AccountNumber = transaction.AccountNumber;
                        }
                    }
                }
            }
        }
        #endregion

        #endregion

        #region Pot editor tab

        #region Properties and commands
        public ObservableCollectionFast<Pot> Pots { get; private set; }
        public ObservableCollectionFast<PotComboItem> AvailablePots { get; }
        public RelayCommand ReassignPot => new RelayCommand((o) => { onReassignPot(); }, (o) => { return canReassignPot(); });
        public RelayCommand PotUp => new RelayCommand((o) => { onMovePotUp(); }, (o) => { return canMovePotUp(); });
        public RelayCommand PotDown => new RelayCommand((o) => { onMovePotDown(); }, (o) => { return canMovePotDown(); });
        public RelayCommand PotUpTen => new RelayCommand((o) => { onMovePotUpTen(); }, (o) => { return canMovePotUp(); });
        public RelayCommand PotDownTen => new RelayCommand((o) => { onMovePotDownTen(); }, (o) => { return canMovePotDown(); });
        public Pot SelectedPot { get; set; }
        public RelayCommand AddPot => new RelayCommand((o) => { onAddPot(); }, (o) => { return FileOpen; });
        public RelayCommand DeletePot => new RelayCommand((o) => { onDeletePot(); }, (o) => { return canDeletePot(); });
        public RelayCommand ZeroPot => new RelayCommand((o) => { onZeroPot(); }, (o) => { return canZeroPot(); });
        #endregion

        #region Add/delete pot
        private void onAddPot()
        {
            Pot pot = new Pot();
            pot.Id = Pots.Count > 0 ? Pots.Select(x => x.Id).Max() + 1 : 0;
            pot.IsPickable = true;
            pot.IsVisible = true;
            pot.Sequence = Pots.Select(x => x.Sequence).Max() + 1;
            pot.Title = "--new--";
            pot.Transactions = Transactions;
            pot.PropertyChanged += onPotChanged;
            Pots.Add(pot);
        }

        private bool canDeletePot()
        {
            return FileOpen && SelectedPot != null && !potInUse(SelectedPot.Id);
        }

        private void onDeletePot()
        {
            Debug.Assert(canDeletePot());
            var index = Pots.IndexOf(SelectedPot);
            Pots.Remove(SelectedPot);
            SelectedPot = Pots.Count > 0 ? Pots[Math.Min(index, Pots.Count - 1)] : null;
        }

        private bool potInUse(int aPotId)
        {
            return Transactions.Any(x => x.OriginId == aPotId || x.DestinationId == aPotId);
        }
        #endregion

        #region Zero pots
        private bool canZeroPot()
        {
            return FileOpen && SelectedPot != null;
        }

        private void onZeroPot()
        {
            Debug.Assert(FileOpen);
            Debug.Assert(canZeroPot());

            if (FileOpen && canZeroPot())
            {
                zeroPots(new[] { SelectedPot });
            }
        }

        private void zeroPots(IEnumerable<Pot> aSelectedPots)
        {
            var selectPotDialog = new SelectPotDialog();
            var result = selectPotDialog.ShowDialog("Zero with...", AvailablePots);

            if (result.HasValue && result.Value && selectPotDialog.SelectedPotId.HasValue)
            {
                var pot = Pots.First(x => x.Id == selectPotDialog.SelectedPotId.Value);
                foreach (var selectedPot in aSelectedPots)
                {
                    var transaction = addTransaction();
                    transaction.ValueInPennies = selectedPot.BalanceInPennies;
                    if (selectedPot.BalanceInPennies > 0)
                    {
                        transaction.OriginId = selectedPot.Id;
                        transaction.DestinationId = pot.Id;
                    }
                    else
                    {
                        transaction.OriginId = pot.Id;
                        transaction.DestinationId = selectedPot.Id;
                    }
                }
            }
        }
        #endregion

        #region Reassign pots
        private bool canReassignPot()
        {
            return FileOpen && SelectedPot != null;
        }

        private void onReassignPot()
        {
            var selectPotDialog = new SelectPotDialog();
            var result = selectPotDialog.ShowDialog("Reassign to...", AvailablePots);

            if (result.HasValue && result.Value && selectPotDialog.SelectedPotId.HasValue)
            {
                var originalId = SelectedPot.Id;
                var replacementId = selectPotDialog.SelectedPotId.Value;

                foreach (var transaction in Transactions)
                {
                    if (transaction.OriginId == originalId)
                    {
                        transaction.OriginId = replacementId;
                    }
                    if (transaction.DestinationId == originalId)
                    {
                        transaction.DestinationId = replacementId;
                    }
                }
            }
        }
        #endregion

        #region Resequence pots
        private void switchPots(Pot aPot, int aChange)
        {
            var allPots = Pots.OrderBy(x => x.Sequence).ToList();
            var index = allPots.IndexOf(aPot);
            var otherIndex = Math.Min(Math.Max(index + aChange, 0), allPots.Count - 1);
            var t = allPots[index].Sequence;
            allPots[index].Sequence = allPots[otherIndex].Sequence;
            allPots[otherIndex].Sequence = t;
        }

        private bool canMovePotUp()
        {
            return FileOpen && Pots.Count > 1 && SelectedPot != null && Pots.OrderBy(x => x.Sequence).ToList().First() != SelectedPot;
        }

        private void onMovePotUp()
        {
            Debug.Assert(FileOpen);
            Debug.Assert(canMovePotUp());

            if (FileOpen && canMovePotUp())
            {
                switchPots(SelectedPot, -1);
            }
        }

        private void onMovePotUpTen()
        {
            Debug.Assert(FileOpen);
            Debug.Assert(canMovePotUp());

            if (FileOpen && canMovePotUp())
            {
                mRefreshMutex = true;

                foreach (var i in Enumerable.Range(0, 10))
                {
                    switchPots(SelectedPot, -1);
                }

                mRefreshMutex = false;
            }
        }

        private bool canMovePotDown()
        {
            return FileOpen && Pots.Count > 1 && SelectedPot != null && Pots.OrderBy(x => x.Sequence).ToList().Last() != SelectedPot;
        }

        private void onMovePotDown()
        {
            Debug.Assert(FileOpen);
            Debug.Assert(canMovePotDown());

            if (FileOpen && canMovePotDown())
            {
                switchPots(SelectedPot, +1);
            }
        }

        private void onMovePotDownTen()
        {
            Debug.Assert(FileOpen);
            Debug.Assert(canMovePotDown());

            if (FileOpen && canMovePotDown())
            {
                mRefreshMutex = true;

                foreach (var i in Enumerable.Range(0, 10))
                {
                    switchPots(SelectedPot, +1);
                }

                mRefreshMutex = false;
            }
        }
        #endregion

        #endregion

        #region Transaction editor tab

        #region Transaction pots view

        #region Properties and commands
        public int PotTotalBalanceInPennies { get; private set; }
        public ObservableCollectionFast<Pot> VisiblePots { get; private set; }
        public RelayCommand ZeroPotInTransactions => new RelayCommand((aParameter) => { onZeroPotInTransactions(aParameter); }, (aParameter) => { return canZeroPotInTransactions(aParameter); });
        #endregion

        #region Command handlers
        public bool canZeroPotInTransactions(object aSelectedItems)
        {
            var selectedPots = aSelectedItems as ObservableCollection<object>;
            return FileOpen && selectedPots != null && selectedPots.Count() > 0;
        }
        private void onZeroPotInTransactions(object aSelectedItems)
        {
            Debug.Assert(FileOpen);
            Debug.Assert(canZeroPotInTransactions(aSelectedItems));

            if (FileOpen && canZeroPotInTransactions(aSelectedItems))
            {
                zeroPots(((IEnumerable<object>)aSelectedItems).Select(x => (Pot)x));
            }
        }
        #endregion

        #endregion

        #region Transactions view

        #region Properties and commands
        public RelayCommand AddTransaction => new RelayCommand((o) => { onAddTransaction(); }, (o) => { return FileOpen; });
        public RelayCommand DeleteTransaction => new RelayCommand((o) => { onDeleteTransaction(); }, (o) => { return canDeleteTransaction(); });
        public Transaction SelectedTransaction { get; set; }
        public ObservableCollectionFast<Transaction> Transactions { get; private set; }
        public ObservableCollectionFast<Transaction> VisibleTransactions { get; private set; }
        #endregion

        #region Command handlers
        private void onAddTransaction()
        {
            Debug.Assert(FileOpen);

            if (FileOpen)
            {
                addTransaction();
            }
        }

        private bool canDeleteTransaction()
        {
            return FileOpen && SelectedTransaction != null;
        }

        private void onDeleteTransaction()
        {
            Debug.Assert(FileOpen);
            Debug.Assert(canDeleteTransaction());

            if (FileOpen && canDeleteTransaction())
            {
                var index = VisibleTransactions.IndexOf(SelectedTransaction);
                SelectedTransaction.PropertyChanged -= onTransactionChanged;
                Transactions.Remove(SelectedTransaction);
                SelectedTransaction = VisibleTransactions.Count > 0 ? VisibleTransactions[Math.Min(VisibleTransactions.Count - 1, index + 1)] : null;
            }
        }

        private Transaction addTransaction()
        {
            var vm = new Transaction
            {
                Id = Transactions.Count > 0 ? Transactions.Select(x => x.Id).Max() + 1 : 0,
                Description = "-- user adjustment --",
                Date = System.DateTime.Now.ToString("dd/MM/yyyy")
            };
            Transactions.Insert(0, vm);
            vm.PropertyChanged += onTransactionChanged;
            return vm;
        }
        #endregion

        #endregion

        #region Page controls

        #region Properties and commands
        public RelayCommand NextPage => new RelayCommand((o) => { onNextPage(); }, (o) => { return canMoveToNextPage(); });
        public RelayCommand PreviousPage => new RelayCommand((o) => { onPreviousPage(); }, (o) => { return canMoveToPreviousPage(); });
        public RelayCommand FirstPage => new RelayCommand((o) => { onFirstPage(); }, (o) => { return canMoveToFirstPage(); });
        public RelayCommand LastPage => new RelayCommand((o) => { onLastPage(); }, (o) => { return canMoveToLastPage(); });

        public int TransactionsPerPage
        {
            get { return 40; }
        }

        public int CurrentPageIndex
        {
            get
            {
                return mCurrentPageIndex + 1;
            }
            set
            {
                mCurrentPageIndex = Math.Max(value - 1, 0);
                refreshVisibleTransactions();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentPageIndex)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FirstPage)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastPage)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NextPage)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PreviousPage)));
            }
        }

        public int LastPageIndex
        {
            get
            {
                return (int)Math.Ceiling(FilteredTransactions.Count / (double)TransactionsPerPage);
            }
        }
        #endregion

        #region Command handlers

        private void onNextPage()
        {
            CurrentPageIndex = CurrentPageIndex + 1;
        }

        private void onPreviousPage()
        {
            CurrentPageIndex = CurrentPageIndex - 1;
        }

        private void onFirstPage()
        {
            CurrentPageIndex = 1;
        }

        private void onLastPage()
        {
            CurrentPageIndex = LastPageIndex;
        }

        private bool canMoveToNextPage()
        {
            return CurrentPageIndex != LastPageIndex;
        }

        private bool canMoveToPreviousPage()
        {
            return CurrentPageIndex > 1;
        }

        private bool canMoveToLastPage()
        {
            return canMoveToNextPage();
        }

        private bool canMoveToFirstPage()
        {
            return canMoveToPreviousPage();
        }
        #endregion

        #endregion

        #region Fill controls

        #region Properties and commands
        public RelayCommand FillOriginUp => new RelayCommand((o) => { onFillOriginUp(); });
        public RelayCommand FillDestinationUp => new RelayCommand((o) => { onFillDestinationUp(); });
        public RelayCommand FillUp => new RelayCommand((o) => { onFillUp(); });
        public RelayCommand FillAuto => new RelayCommand((o) => { onFillAuto(); });
        #endregion

        #region Command handlers
        private void onFillOriginUp()
        {
            if (FileOpen && SelectedTransaction != null)
            {
                var index = VisibleTransactions.IndexOf(SelectedTransaction);
                if (index >= 0 && index < VisibleTransactions.Count - 1)
                {
                    SelectedTransaction.OriginId = VisibleTransactions[index + 1].OriginId;
                }
            }
        }

        private void onFillDestinationUp()
        {
            if (FileOpen && SelectedTransaction != null)
            {
                var index = VisibleTransactions.IndexOf(SelectedTransaction);
                if (index >= 0 && index < VisibleTransactions.Count - 1)
                {
                    SelectedTransaction.DestinationId = VisibleTransactions[index + 1].DestinationId;
                }
            }
        }

        private void onFillUp()
        {
            if (FileOpen && SelectedTransaction != null)
            {
                var index = VisibleTransactions.IndexOf(SelectedTransaction);
                if (index >= 0 && index < VisibleTransactions.Count - 1)
                {
                    SelectedTransaction.OriginId = VisibleTransactions[index + 1].OriginId;
                    SelectedTransaction.DestinationId = VisibleTransactions[index + 1].DestinationId;
                }
            }
        }

        private void onFillAuto()
        {
            if (FileOpen && SelectedTransaction != null)
            {
                var selectedIndex = Transactions.IndexOf(SelectedTransaction);
                int? bestMatchIndex = null;
                int bestMatchCount = 0;

                for (int index = selectedIndex + 1; index < Transactions.Count; ++index)
                {
                    var possibleTransaction = Transactions[index];
                    if (!possibleTransaction.OriginId.HasValue || !possibleTransaction.DestinationId.HasValue)
                    {
                        continue;
                    }
                    if (possibleTransaction.Description == SelectedTransaction.Description)
                    {
                        SelectedTransaction.OriginId = possibleTransaction.OriginId;
                        SelectedTransaction.DestinationId = possibleTransaction.DestinationId;
                        return;
                    }
                    int count = 0;
                    for (int i = 0; i < SelectedTransaction.Description.Length; ++i)
                    {
                        if (i < possibleTransaction.Description.Length && SelectedTransaction.Description[i] == possibleTransaction.Description[i])
                        {
                            ++count;
                        }
                    }
                    if (!bestMatchIndex.HasValue || count > bestMatchCount)
                    {
                        bestMatchCount = count;
                        bestMatchIndex = index;
                    }
                }

                if (bestMatchIndex.HasValue)
                {
                    var possibleTransaction = Transactions[bestMatchIndex.Value];
                    SelectedTransaction.OriginId = possibleTransaction.OriginId;
                    SelectedTransaction.DestinationId = possibleTransaction.DestinationId;
                }
            }
        }
        #endregion

        #endregion

        #region Filters

        #region Properties and commands
        public string DateFilter { get { return mDateFilter; } set { mDateFilter = value; refreshVisibleTransactions(); } }
        public string DescriptionFilter { get { return mDescriptionFilter; } set { mDescriptionFilter = value; refreshVisibleTransactions(); } }
        public string AmountFilter { get { return mAmountFilter; } set { mAmountFilter = value; refreshVisibleTransactions(); } }
        public string BalanceFilter { get { return mBalanceFilter; } set { mBalanceFilter = value; refreshVisibleTransactions(); } }
        public string FromFilter { get { return mFromFilter; } set { mFromFilter = value; refreshVisibleTransactions(); } }
        public string ToFilter { get { return mToFilter; } set { mToFilter = value; refreshVisibleTransactions(); } }
        public string CommentFilter { get { return mCommentFilter; } set { mCommentFilter = value; refreshVisibleTransactions(); } }
        public bool AreFiltersActive { get { return (DateFilter + DescriptionFilter + AmountFilter + BalanceFilter + FromFilter + ToFilter + CommentFilter).Length > 0; } }
        private List<Transaction> FilteredTransactions
        {
            get
            {
                return Transactions.Where(x => shouldInclude(x)).OrderByDescending(x => x.Id).ToList();
            }
        }
        #endregion

        #region Utility methods
        private bool shouldInclude(Transaction aTransaction)
        {
            return shouldInclude(DateFilter, aTransaction.Date) &&
                shouldInclude(DescriptionFilter, aTransaction.Description) &&
                shouldInclude(AmountFilter, aTransaction.ValueInPennies) &&
                shouldInclude(BalanceFilter, aTransaction.BalanceInPennies) &&
                shouldInclude(CommentFilter, aTransaction.Comment) &&
                shouldInclude(FromFilter, aTransaction.OriginId, Pots) &&
                shouldInclude(ToFilter, aTransaction.DestinationId, Pots);
        }

        private static bool shouldInclude(string aPotFilter, int? aPotId, IEnumerable<Pot> aPots)
        {
            return string.IsNullOrEmpty(aPotFilter) || (aPotId.HasValue && aPots.First(x => x.Id == aPotId.Value).Title.ToLower().Contains(aPotFilter.ToLower()));
        }

        private static bool shouldInclude(string aPenniesFilter, int aPennies)
        {
            return string.IsNullOrEmpty(aPenniesFilter) || !(new PenniesValidationRule()).Validate(aPenniesFilter, null).IsValid || (int)(new PenniesTypeConverter()).ConvertBack(aPenniesFilter, typeof(int), null, null) == aPennies;
        }

        private static bool shouldInclude(string aMatchString, string aSourceString)
        {
            return string.IsNullOrEmpty(aMatchString) || aSourceString.ToLower().Contains(aMatchString.ToLower());
        }
        #endregion

        #endregion

        #endregion

        #region Budget editor tab

        #region Properties and commands
        public ObservableCollectionFast<Budget> Budgets { get; private set; }
        public RelayCommand AddBudget => new RelayCommand((o) => { onAddBudget(); }, (o) => { return FileOpen; });
        public RelayCommand DeleteBudget => new RelayCommand((o) => { onDeleteBudget(); }, (o) => { return canDeleteBudget(); });
        public RelayCommand BudgetTransfer => new RelayCommand((o) => { onTransferBudget(); }, (o) => { return FileOpen; });
        public Budget SelectedBudget { get; set; }
        public int BudgetTotalBalanceInPennies { get; private set; }
        #endregion

        #region Add/delete budget
        private void onAddBudget()
        {
            Debug.Assert(FileOpen);

            if (FileOpen)
            {
                var vm = new Budget { Id = Budgets.Count > 0 ? Budgets.Select(x => x.Id).Max() + 1 : 0, Title = "--new--" };
                Budgets.Add(vm);
                vm.PropertyChanged += onBudgetChanged;
            }
        }

        private bool canDeleteBudget()
        {
            return FileOpen && SelectedBudget != null;
        }

        private void onDeleteBudget()
        {
            Debug.Assert(FileOpen);
            Debug.Assert(canDeleteBudget());

            if (FileOpen && canDeleteBudget())
            {
                var index = Budgets.IndexOf(SelectedBudget);
                SelectedBudget.PropertyChanged -= onBudgetChanged;
                Budgets.Remove(SelectedBudget);
                SelectedBudget = Budgets.Count > 0 ? Budgets[Math.Min(index, Budgets.Count - 1)] : null;
            }
        }
        #endregion

        #region Transfer budget
        private void onTransferBudget()
        {
            Debug.Assert(FileOpen);

            if (FileOpen)
            {
                var selectPotDialog = new SelectPotDialog();
                var result = selectPotDialog.ShowDialog("Transfer from...", AvailablePots);

                if (result.HasValue && result.Value && selectPotDialog.SelectedPotId.HasValue)
                {
                    int fromId = selectPotDialog.SelectedPotId.Value;
                    foreach (var budget in Budgets)
                    {
                        if (budget.PotId.HasValue)
                        {
                            var transaction = addTransaction();
                            transaction.OriginId = fromId;
                            transaction.DestinationId = budget.PotId.Value;
                            transaction.ValueInPennies = budget.AmountInPennies;
                            transaction.Description = $"Budget transfer {budget.Title}";
                        }
                    }
                }
            }
        }
        #endregion

        #endregion

        #region Properties
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Refresh handling
        private void onBudgetChanged(object aSender, PropertyChangedEventArgs aArgs)
        {
            activateSave();
            refreshBudgetTotalBalanceInPennies();
        }

        private void onPotChanged(object aSender, PropertyChangedEventArgs aArgs)
        {
            if (!mRefreshMutex)
            {
                if (aArgs.PropertyName == nameof(Pot.IsVisible) || aArgs.PropertyName == "")
                {
                    refreshVisiblePots();
                }
                if (aArgs.PropertyName == nameof(Pot.IsPickable) || aArgs.PropertyName == "")
                {
                    refreshPotComboItems();
                }
                activateSave();
            }
        }
        private void onTransactionChanged(object aSender, PropertyChangedEventArgs aArgs)
        {
            if (mRefreshMutex)
            {
                return;
            }

            activateSave();
            if (AreFiltersActive)
            {
                refreshVisibleTransactions();
            }
            if ((new[] { nameof(Transaction.ValueInPennies), nameof(Transaction.OriginId), nameof(Transaction.DestinationId) }).ToList().Contains(aArgs.PropertyName))
            {
                refreshPotTransactionProperties();
            }
        }

        private void refreshVisibleTransactions()
        {
            var transactions = FilteredTransactions;
            int maxPages = (int)Math.Ceiling(transactions.Count / (double)TransactionsPerPage);
            int pageIndex = Math.Max(0, Math.Min(mCurrentPageIndex, maxPages - 1));
            int startIndex = pageIndex * TransactionsPerPage;
            int count = Math.Min(transactions.Count - startIndex, TransactionsPerPage);
            transactions = transactions.GetRange(startIndex, count);
            VisibleTransactions.Reset(transactions);
        }
        void refreshBudgetTotalBalanceInPennies()
        {
            BudgetTotalBalanceInPennies = Budgets.Sum(x => x.AmountInPennies);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BudgetTotalBalanceInPennies)));
        }

        private void refreshPotComboItems()
        {
            var pickable = Pots.Where(x => x.IsPickable).OrderBy(x => x.Title).Select(y => new PotComboItem(y));
            AvailablePots.Reset(pickable);
            AvailablePots.Insert(0, new PotComboItem(null));
            refreshTransactionPots();
        }

        private void refreshTransactionPots()
        {
            mRefreshMutex = true;
            foreach (var transaction in Transactions)
            {
                transaction.refreshPotProperties();
            }
            mRefreshMutex = false;
        }

        private void refreshPotTransactionProperties()
        {
            foreach (var pot in Pots)
            {
                pot.refreshDependentProperties();
            }
            refreshPotTotalBalanceInPennies();
        }

        private void refreshPotTotalBalanceInPennies()
        {
            PotTotalBalanceInPennies = VisiblePots.Sum(x => x.BalanceInPennies);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PotTotalBalanceInPennies)));
        }

        private void refreshVisiblePots()
        {
            mRefreshMutex = true;
            var pots = Pots.Where(x => x.IsVisible).OrderBy(x => x.Sequence);
            VisiblePots.Clear();
            VisiblePots.AddRange(pots);
            refreshPotTotalBalanceInPennies();
            mRefreshMutex = false;
        }
        #endregion

        #region Private methods
        private void fireAllPropertiesChanged()
        {
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
        
        #endregion

        #region Private values
        private bool mCanSave = false;
        private int mCurrentPageIndex = 0;
        private bool mRefreshMutex;
        private string mDateFilter;
        private string mDescriptionFilter;
        private string mAmountFilter;
        private string mBalanceFilter;
        private string mFromFilter;
        private string mToFilter;
        private string mCommentFilter;
        private string mFilePath;
        #endregion
    }
}
