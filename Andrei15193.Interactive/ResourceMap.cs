using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Andrei15193.Interactive
{
    /// <summary>
    /// <para>
    /// Represents a collection of resources that are mapped to a name. For each resource
    /// a <see cref="Task"/> can be provided that will complete once a resource has been
    /// set under the same name.
    /// </para>
    /// <para>
    /// Resource names are case-insensitive.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Sometimes the <see cref="InteractiveViewModel"/> may transition directly to
    /// a loading state where it is expected that all necessary dependencies have
    /// been provided and they can be used. This means that everything required by
    /// the loading state needs the be already set through the constructor.
    /// </para>
    /// <para>
    /// An alternative is to use resources that can be awaited until they are set.
    /// Instead of having everything in the constructor, it can be awaited until
    /// a value has been provided to a property (e.g.: specified in XAML). Once that
    /// has happened then the awaited task will return the value that has been set.
    /// </para>
    /// <para>
    /// Resources can be used alike <see cref="Windows.UI.Xaml.DependencyProperty"/>,
    /// a wrapper property can be defined that gets the current value of the property
    /// (it does not need to wait the task completion) and through the setter it
    /// provides the resource thus any awaiting method will resume.
    /// </para>
    /// </remarks>
    public class ResourceMap
    {
        private sealed class ResourceMapItem
        {
            private TaskCompletionSource<object> _taskCompletionSource;
            private Task<object> _task;

            public ResourceMapItem()
            {
                _taskCompletionSource = new TaskCompletionSource<object>();
                _task = _taskCompletionSource.Task;
            }

            public ResourceMapItem(object value)
            {
                _taskCompletionSource = null;
                _task = Task.FromResult(value);
            }

            public Task<object> ValueTask
                => _task;

            public void Set(object value)
            {
                _task = Task.FromResult(value);
                if (_taskCompletionSource != null)
                {
                    var taskCompletionSource = _taskCompletionSource;
                    _taskCompletionSource = null;
                    taskCompletionSource.SetResult(value);
                }
            }
        }

        /// <summary>
        /// Gets the current value of the resource. If the resource has not been previously
        /// set then this value is equal to the default value of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the resource to get.
        /// </typeparam>
        /// <param name="name">
        /// The name of the resource to get.
        /// </param>
        /// <returns>
        /// Returns the resource under the provided <paramref name="name"/> if it has been
        /// previously set; otherwise the default value for <typeparamref name="T"/>.
        /// </returns>
        public T Get<T>(string name)
        {
            ResourceMapItem item;
            if (!_items.TryGetValue(name, out item) || !item.ValueTask.IsCompleted)
                return default(T);
            else
                return (T)item.ValueTask.Result;
        }

        private readonly IDictionary<string, ResourceMapItem> _items = new Dictionary<string, ResourceMapItem>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Asynchronously gets a resource.
        /// </summary>
        /// <typeparam name="TResource">
        /// The type of the resource to get.
        /// </typeparam>
        /// <param name="name">
        /// The name of the resource to get.
        /// </param>
        /// <returns>
        /// Returns a <see cref="Task{TResource}"/> that eventually provides a resource
        /// with the given <paramref name="name"/> once it has been set.
        /// </returns>
        public async Task<TResource> GetAsync<TResource>(string name)
        {
            ResourceMapItem item;
            if (!_items.TryGetValue(name, out item))
            {
                item = new ResourceMapItem();
                _items.Add(name, item);
            }

            var resource = (TResource)(await item.ValueTask);
            return resource;
        }

        /// <summary>
        /// Asynchronously gets a resource.
        /// </summary>
        /// <typeparam name="TResource">
        /// The type of the resource to get.
        /// </typeparam>
        /// <param name="name">
        /// The name of the resource to get.
        /// </param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> that can be used to signal cancellation thus
        /// the task may complete (in <see cref="TaskStatus.Canceled"/> state) before a
        /// resource is set.
        /// </param>
        /// <returns>
        /// Returns a <see cref="Task{TResource}"/> that eventually provides a resource
        /// with the given <paramref name="name"/> once it has been set.
        /// </returns>
        public async Task<TResource> GetAsync<TResource>(string name, CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled)
                return await GetAsync<TResource>(name);

            var taskCompletionSource = new TaskCompletionSource<TResource>();
            cancellationToken.Register(() => taskCompletionSource.TrySetCanceled(), true);

            var completedTask = await Task.WhenAny(GetAsync<TResource>(name), taskCompletionSource.Task);
            return await completedTask;
        }

        /// <summary>
        /// Sets the given <paramref name="resource"/> under the provided <paramref name="name"/>.
        /// </summary>
        /// <typeparam name="TResource">
        /// The type of the resource to set.
        /// </typeparam>
        /// <param name="name">
        /// The name of the resource to set.
        /// </param>
        /// <param name="resource">
        /// The resource to set.
        /// </param>
        public void Set<TResource>(string name, TResource resource)
        {
            ResourceMapItem item;
            if (_items.TryGetValue(name, out item))
                item.Set(resource);
            else
                _items.Add(name, new ResourceMapItem(resource));
        }
    }
}