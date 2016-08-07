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

### Stuff going out

* ErrorContext and all methods using it have been marked with **Obsolete**. Allowing InteractiveViewModels to not
transition to any state (because of an uncaught exception) and remain "stuck" in an action state leads to "partial"
transitions and inconsistencies when using commands to trigger transitions.