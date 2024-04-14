using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace budget
{
    /// <summary>
    /// Interaction logic for SelectPotDialog.xaml
    /// </summary>
    public partial class SelectPotDialog : Window
    {
        public SelectPotDialog()
        {
            InitializeComponent();

            mSelectButton.Click += (s, a) => DialogResult = true;
        }

        public SelectPotDialog(Window aOwner) : this()
        {
            this.Owner = aOwner;
        }

        public bool? ShowDialog(string aTitle, ObservableCollection<PotComboItem> aPots, PotComboItem aSelectedItem = null)
        {
            this.Title = aTitle;
            mComboBox.ItemsSource = aPots;
            mComboBox.SelectedItem = aSelectedItem;

            return this.ShowDialog();
        }

        public int? SelectedPotId
        {
            get
            {
                if (mComboBox.SelectedItem is PotComboItem pot && pot != null)
                {
                    return pot.Id;
                }
                return null;
            }
        }
    }
}
