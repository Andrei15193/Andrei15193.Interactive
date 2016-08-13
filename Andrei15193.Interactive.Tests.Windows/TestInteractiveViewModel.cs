using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Andrei15193.Interactive.Tests.Windows
{
    public class TestInteractiveViewModel
        : InteractiveViewModel
    {
        private TestItem _selectedItem;
        private readonly ObservableCollection<TestItem> _items;

        public TestInteractiveViewModel()
        {
            _selectedItem = new TestItem
            {
                Id = new Random().Next(0, 20),
                Text = "Selected item"
            };

            _items = new ObservableCollection<TestItem>(from itemNumber in Enumerable.Range(0, 20)
                                                        select new TestItem
                                                        {
                                                            Id = itemNumber,
                                                            Text = $"Item #{itemNumber}"
                                                        });
            Items = new ReadOnlyObservableCollection<TestItem>(_items);

            CreateActionState(
                "State2",
                async context =>
                {
                    if (context.PreviousState == "State1")
                        context.NextState = "State3";
                    else
                        context.NextState = "State1";
                    await Task.Delay(2000);
                });

            BeginTransitionCommand = GetTransitionCommand("State2").BindTo("State1", "State3");

            TransitionToAsync("State1");

            Task.Delay(5000).ContinueWith(delegate { TransitionToAsync("State2"); }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        public ICommand BeginTransitionCommand { get; }

        public TestItem SelectedItem
        {
            get
            {
                return _selectedItem;
            }
            set
            {
                _selectedItem = value;
                NotifyPropertyChanged(nameof(SelectedItem));
            }
        }

        public ReadOnlyObservableCollection<TestItem> Items { get; }
    }
}