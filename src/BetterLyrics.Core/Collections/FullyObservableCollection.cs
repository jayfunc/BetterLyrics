using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace BetterLyrics.Core.Collections;

// https://stackoverflow.com/a/32013610/11048731
public class FullyObservableCollection<T> : ObservableCollection<T>
    where T : INotifyPropertyChanged
{
    public FullyObservableCollection()
    {
    }

    public FullyObservableCollection(List<T> list) : base(list)
    {
        ObserveAll();
    }

    public FullyObservableCollection(IEnumerable<T> enumerable) : base(enumerable)
    {
        ObserveAll();
    }

    /// <summary>
    ///     Occurs when a property is changed within an item.
    /// </summary>
    public event EventHandler<ItemPropertyChangedEventArgs> ItemPropertyChanged;

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Remove ||
            e.Action == NotifyCollectionChangedAction.Replace)
            foreach (T item in e.OldItems)
                item.PropertyChanged -= ChildPropertyChanged;

        if (e.Action == NotifyCollectionChangedAction.Add ||
            e.Action == NotifyCollectionChangedAction.Replace)
            foreach (T item in e.NewItems)
                item.PropertyChanged += ChildPropertyChanged;

        base.OnCollectionChanged(e);
    }

    protected void OnItemPropertyChanged(ItemPropertyChangedEventArgs e)
    {
        ItemPropertyChanged?.Invoke(this, e);
    }

    protected void OnItemPropertyChanged(int index, PropertyChangedEventArgs e)
    {
        OnItemPropertyChanged(new ItemPropertyChangedEventArgs(index, e));
    }

    protected override void ClearItems()
    {
        foreach (var item in Items)
            item.PropertyChanged -= ChildPropertyChanged;

        base.ClearItems();
    }

    private void ObserveAll()
    {
        foreach (var item in Items)
            item.PropertyChanged += ChildPropertyChanged;
    }

    private void ChildPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        var typedSender = (T)sender;
        var i = Items.IndexOf(typedSender);

        if (i < 0)
            throw new ArgumentException("Received property notification from item not in collection");

        OnItemPropertyChanged(i, e);
    }

    public void Refresh()
    {
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public void RemoveAll(Predicate<T> match)
    {
        for (var i = Count - 1; i >= 0; i--)
            if (match(Items[i]))
                RemoveAt(i);
    }
}

/// <summary>
///     Provides data for the <see cref="FullyObservableCollection{T}.ItemPropertyChanged" /> event.
/// </summary>
public class ItemPropertyChangedEventArgs : PropertyChangedEventArgs
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ItemPropertyChangedEventArgs" /> class.
    /// </summary>
    /// <param name="index">The index in the collection of changed item.</param>
    /// <param name="name">The name of the property that changed.</param>
    public ItemPropertyChangedEventArgs(int index, string name) : base(name)
    {
        CollectionIndex = index;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ItemPropertyChangedEventArgs" /> class.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <param name="args">The <see cref="PropertyChangedEventArgs" /> instance containing the event data.</param>
    public ItemPropertyChangedEventArgs(int index, PropertyChangedEventArgs args) : this(index, args.PropertyName)
    {
    }

    /// <summary>
    ///     Gets the index in the collection for which the property change has occurred.
    /// </summary>
    /// <value>
    ///     Index in parent collection.
    /// </value>
    public int CollectionIndex { get; }
}