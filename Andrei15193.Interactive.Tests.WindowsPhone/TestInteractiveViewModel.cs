﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Andrei15193.Interactive.Validation;

namespace Andrei15193.Interactive.Tests.WindowsPhone
{
    public class TestInteractiveViewModel
        : InteractiveViewModel<object>
    {
        private int _transitionCount;

        private TestItem _selectedItem;
        private readonly ObservableCollection<TestItem> _items;

        public TestInteractiveViewModel()
            : base(new object())
        {
            _transitionCount = 0;

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
                "State1",
                async context =>
                {
                    context.NextState = "State2";
                    await Task.Delay(5000);
                });
            CreateActionState(
                "State2",
                async context =>
                {
                    if (context.PreviousState == "State1")
                        context.NextState = "State3";
                    else
                        context.NextState = "State1";
                    await Task.Delay(2000);
                    _transitionCount++;
                    Errors.Clear();
                    Errors.Add(new ValidationError("Error " + _transitionCount));
                });

            BeginTransitionCommand = GetTransitionCommand("State2").BindTo("State1", "State3");

            TransitionToAsync("State1");
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