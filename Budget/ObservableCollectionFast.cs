using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace budget
{
    public class ObservableCollectionFast<T> : ObservableCollection<T>
    {
        public ObservableCollectionFast() : base() { }

        public ObservableCollectionFast(IEnumerable<T> aCollection)
            : base(aCollection) { }

        public ObservableCollectionFast(List<T> aList)
            : base(aList) { }

        public virtual void AddRange(IEnumerable<T> aCollection)
        {
            if (aCollection == null || aCollection.Count() == 0)
            {
                return;
            }

            foreach (var item in aCollection)
            {
                Items.Add(item);
            }

            OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            // Cannot use NotifyCollectionChangedAction.Add, because Constructor supports only the 'Reset' action.
        }

        public virtual void RemoveRange(IEnumerable<T> aCollection)
        {
            if (isNullOrEmpty(aCollection))
            {
                return;
            }

            var removed = false;
            foreach (var item in aCollection)
            {
                if (Items.Remove(item))
                {
                    removed = true;
                }
            }

            if (removed)
            {
                OnPropertyChanged(new PropertyChangedEventArgs("Count"));
                OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                // Cannot use NotifyCollectionChangedAction.Remove, because Constructor supports only the 'Reset' action.
            }
        }

        public virtual void Reset(T aItem)
        {
            Reset(new List<T> { aItem });
        }

        public virtual void Reset(IEnumerable<T> aCollection)
        {
            if (isNullOrEmpty(aCollection)
                && isNullOrEmpty(Items))
            {
                return;
            }

            var count = Count;

            // Step 1: Clear the old items
            Items.Clear();

            // Step 2: Add new items
            if (!isNullOrEmpty(aCollection))
            {
                foreach (var item in aCollection)
                {
                    Items.Add(item);
                }
            }

            // Step 3: Don't forget the event
            if (Count != count)
            {
                OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            }

            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        private static bool isNullOrEmpty<S>(IEnumerable<S> aCollection)
        {
            return aCollection == null || aCollection.Count() == 0;
        }
    }
}
