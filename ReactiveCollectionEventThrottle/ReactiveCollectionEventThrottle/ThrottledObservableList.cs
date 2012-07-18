using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveCollectionEventThrottle
{
    public class ThrottledObservableList<T> : IList<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        public static readonly TimeSpan DefaultThrottle = TimeSpan.FromMilliseconds(50);

        private ObservableCollection<T> list;

        private List<T> newItems = new List<T>();
        private List<T> oldItems = new List<T>();

        public ThrottledObservableList() : this(DefaultThrottle) { }
        public ThrottledObservableList(TimeSpan throttle) : this(throttle, null) { }
        public ThrottledObservableList(IEnumerable<T> source) : this(DefaultThrottle, source) { }
        public ThrottledObservableList(TimeSpan throttle, IEnumerable<T> source)
        {
            list = new ObservableCollection<T>(source ?? new List<T>());

            var rawEventSource =
                Observable.FromEventPattern<NotifyCollectionChangedEventArgs>(list, "CollectionChanged")
                .Select(a => a.EventArgs);

            var throttledEventSource = rawEventSource
                .Throttle(throttle);

            rawEventSource.Subscribe(Accumulate);
            throttledEventSource.Subscribe(Publish);
        }

        private void Publish(NotifyCollectionChangedEventArgs obj)
        {
            var newList = System.Threading.Interlocked.Exchange(ref newItems, new List<T>());
            var oldList = System.Threading.Interlocked.Exchange(ref oldItems, new List<T>());

            var collectionChangeHandler = CollectionChanged;
            if (collectionChangeHandler != null)
            {

                if (newList.Any())
                {
                    var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newList);
                    collectionChangeHandler(this, args);
                }

                if (oldList.Any())
                {
                    var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldList);
                    collectionChangeHandler(this, args);
                }
            }

            var propertyChangedHandler = PropertyChanged;
            if(propertyChangedHandler != null) 
            {
                propertyChangedHandler(this, new PropertyChangedEventArgs("Count"));
            }

        }

        private void Accumulate(NotifyCollectionChangedEventArgs obj)
        {
            if (obj.NewItems != null)
                newItems.AddRange(obj.NewItems.Cast<T>());

            if (obj.OldItems != null)
                oldItems.AddRange(obj.OldItems.Cast<T>());
        }

        public int IndexOf(T item)
        {
            return list.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            list.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            list.RemoveAt(index);
        }

        public T this[int index]
        {
            get
            {
                return list[index];
            }
            set
            {
                list[index] = value;
            }
        }

        public void Add(T item)
        {
            list.Add(item);
        }

        public void Clear()
        {
            list.Clear();
        }

        public bool Contains(T item)
        {
            return list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            list.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return list.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            return list.Remove(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
