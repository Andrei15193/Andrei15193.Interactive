Stuff going in, changing and eventually going out
=================================================

Any type or method that was later discovered as a bad decision will be marked as Obsolete and left in the
library for at least one more release (the release in which they become marked as Obsolete).

Obsolete types or methods are still supported until removed, however their usage is discouraged. The reason
they are still left for at least one release is so that people using this library know that some of the stuff
they are using will be removed and thus they have time to change their code towards an alternative.

Besides the release constraint for Obsolete stuff there is also a time constraint. Obsolete types will not
be removed sooner than 6 months since it has been marked so. Again, this is to allow enough time for
developers to adjust their code.

Version 1.0
-----------

* InteractiveViewModel.
* Attached property for VisualState.
* Page Navigation Command.
* Validation in the form of Constraints.
    * LinearConstraints (a sequence of required Constraints).
    * AnyConstraints (a sequence where at least one needs to hold).
    * Callback based Constraints (for specific constraints that reside in every application).
* INotifyPropertyChanged default implementation (probably like the dozens out there already), see PropertyChangedNotifier.

Version 1.0.1
-------------

* Bug fixes.

Version 1.1
-----------

* Bug fixes.
* DynamicPropertyEquatable, some collection controls, such as combo boxes, require that the selected item that we bind to
to be in the collection we provide as ItemsSource. The **DynamicPropertyEquatable** and **DynamicPropertyEquatableConverter**
help resolve this issue allowing a property path to be specified, in XAML, which should be used to compare items. this
will allow the selected item to not necessarily be "part" (as in the exact same instance) of the collection provided to
ItemsSource.
* ProjectedObservableCollection, which is the equivalent of applying LINQ's Select on an ObservableCollection, used by
**DynamicPropertyEquatableConverter**.
* StatePageNavigators attached property which allows for page transitions to happen when an InteractiveViewModel chnages its
state. The view model itself is obtained from the DataContext of the Page onto which IPageNavigator items are added.
* ConfirmDialogCommand, a wrapper command that first displays a confirmation MessageDialog. If the user confirms then the
wrapped command is executed.
* MappingConverter, this is useful to map various InteractiveViewModel states to the same visual state.
* ResourceMap. Sometimes it is useful to provide resources (e.g.: other interactive view models) through properties
rather than have a huge parameter list on the constructor, not to mention that for store and windows phone apps action
parameter-less public constructor needs to be provided in order specify its instantiation in XAML. Awaitable resources
aim to solve that problem by providing a task that can be awaited in an asynchronous method (e.g. the loading method
of an InteractiveViewModel). The task will resume once a value has been set (which can happen after constructor
has done initializing and the InteractiveViewModel is left in the loading state).

### Stuff going out

* ErrorContext and all methods using it have been marked with **Obsolete**. Allowing InteractiveViewModels to not
transition to any state (because of an uncaught exception) and remain "stuck" in an action state leads to "partial"
transitions and inconsistencies when using commands to trigger transitions.
* ContentDialogPage has been marked with **Obsolete**. Asynchronous commands may trigger page navigation when it is
not desired. Use *StatePageNavigators* instead to trigger page navigation when an *InteractiveViewModel* reaches a
specific *state*.
* Constraints has been marked with **Obsolete**. Having a static class to act as a container for constraints raises
more issues than it solves (When and where should constraint registration happen? How is it ensured that it only happens
once?). Instead of having a static container which increases coupling as it would be used extensively, have each constraint
provided to consumers (dependency injection is one way to do it).