using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

using TupleType = System.Tuple<System.Collections.IList, System.Collections.IList>;
using AccumulatorType = System.Collections.Generic.List<System.Tuple<System.Collections.IList, System.Collections.IList>>;
using System.Collections;
using System.Threading;
using System.ComponentModel;

namespace ReactiveCollectionEventThrottle
{
    class Program
    {
        private static List<string> newItems = new List<string>();
        private static List<string> oldItems = new List<string>();

        static ThrottledObservableList<string> collection;

        static void Main(string[] args)
        {
            var list = new ObservableCollection<string> { "hello", "world" };

            collection = new ThrottledObservableList<string>(list);

            //var seed = new AccumulatorType();

            var rawEventSource = 
                Observable.FromEventPattern<NotifyCollectionChangedEventArgs>(collection, "CollectionChanged")
                .Select(a => a.EventArgs);

            rawEventSource.Subscribe(Publish);

            Observable.FromEventPattern<PropertyChangedEventArgs>(collection, "PropertyChanged")
                .Select(a => a.EventArgs)
                .Where(a => a.PropertyName == "Count")
                .Subscribe(CountChanged);

            ThreadPool.QueueUserWorkItem((s) =>
            {
                foreach (var i in Enumerable.Range(1, 10))
                {
                    GenerateEvents(collection);
                    //GenerateRemoveEvents(collection);
                    System.Threading.Thread.Sleep(105);
                }
            });

            Console.ReadLine();
        }

        private static void CountChanged(PropertyChangedEventArgs obj)
        {
            Console.WriteLine("Current count: {0}", collection.Count);
        }

        private static void GenerateRemoveEvents(ThrottledObservableList<string> collection)
        {
            for (int i = collection.Count - 1; i >= 0; i--)
            {
                collection.RemoveAt(i);
            }
        }

        private static void GenerateEvents(ICollection<string> collection)
        {
            var r = new Random(DateTime.Now.GetHashCode());
            foreach (var i in Enumerable.Range(1, r.Next(100000)))
            {
                collection.Add(i.ToString());
            }
        }


        private static void Publish(NotifyCollectionChangedEventArgs obj)
        {
            
            Console.WriteLine("Collection Modified! action:{0}", obj.Action);
            if(obj.Action == NotifyCollectionChangedAction.Add)
                Console.WriteLine("Modified items: {0}", obj.NewItems.Count);
            else if(obj.Action == NotifyCollectionChangedAction.Remove)
                Console.WriteLine("Modified items: {0}", obj.OldItems.Count);
        }

    }
}
