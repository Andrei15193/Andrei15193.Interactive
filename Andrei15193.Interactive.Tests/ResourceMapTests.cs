using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Andrei15193.Interactive.Tests
{
    [TestClass]
    public class ResourceMapTests
    {
        private ResourceMap ResourceMap { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            ResourceMap = new ResourceMap();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            ResourceMap = null;
        }

        [TestMethod]
        public async Task TestAwaitingAnAsynchronousResourceWillContinueOnlyAfterItHasBeenSet()
        {
            var resourceName = "testResource";
            var resourceTask = ResourceMap.GetAsync<object>(resourceName);

            Assert.IsFalse(resourceTask.IsCompleted);

            ResourceMap.Set(resourceName, new object());
            await resourceTask;
        }

        [TestMethod]
        public async Task TestTaskResultIsTheSameAsTheProvidedResource()
        {
            var resourceName = "testResource";
            var resourceTask = ResourceMap.GetAsync<object>(resourceName);
            var resource = new object();

            ResourceMap.Set(resourceName, resource);

            var actualResource = await resourceTask;
            Assert.AreSame(resource, actualResource);
        }

        [TestMethod]
        public async Task TestSettingBeforeAwaitingToGetAResourceReturnsACompletedTask()
        {
            var resourceName = "testResource";
            var resource = new object();
            ResourceMap.Set(resourceName, resource);

            var resourceTask = ResourceMap.GetAsync<object>(resourceName);

            Assert.IsTrue(resourceTask.IsCompleted);
            await resourceTask;
        }

        [TestMethod]
        public void TestGettingAResourceThroughSynchronousMethodReturnsTheSameOneThatHasBeenSet()
        {
            var resourceName = "testResource";
            var resource = new object();
            ResourceMap.Set(resourceName, resource);

            var actualResource = ResourceMap.Get<object>(resourceName);

            Assert.AreSame(resource, actualResource);
        }

        [TestMethod]
        public void TestGettingAResourceThatHasNotYetBeenSetReturnsDefaultValue()
        {
            var resourceName = "testResource";

            var resource = ResourceMap.Get<object>(resourceName);

            Assert.IsNull(resource);
        }

        [TestMethod]
        public async Task TestGettingAResourceThatHasNotYetBeenSetButThereIsAPendingTaskReturnsDefaultValue()
        {
            var resourceName = "testResource";
            var resource = new object();

            var resourceTask = ResourceMap.GetAsync<object>(resourceName);

            var actualResource = ResourceMap.Get<object>(resourceName);
            Assert.IsNull(actualResource);

            ResourceMap.Set(resourceName, resource);
            await resourceTask;
        }

        [TestMethod]
        public async Task TestGettingAResourceWithDefaultCancellationTokenProvidesTheResourceThatHasBeenSet()
        {
            var resourceName = "testResource";
            var resource = new object();
            ResourceMap.Set(resourceName, resource);

            var actualResource = await ResourceMap.GetAsync<object>(resourceName, default(CancellationToken));

            Assert.AreSame(resource, actualResource);
        }

        [TestMethod]
        public async Task TestGettingAResourceWithCancellationTokenWithoutCancelingProvidesTheResourceThatHasBeenSet()
        {
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var resourceName = "testResource";
                var resource = new object();
                ResourceMap.Set(resourceName, resource);

                var actualResource = await ResourceMap.GetAsync<object>(resourceName, cancellationTokenSource.Token);

                Assert.AreSame(resource, actualResource);
            }
        }

        [TestMethod]
        public async Task TestGettingAResourceAndSignalingCancelationWillCancelTheReturnedTask()
        {
            Task resourceTask = null;
            using (var cancellationTokenSource = new CancellationTokenSource())
                try
                {
                    var resourceName = "testResource";

                    resourceTask = ResourceMap.GetAsync<object>(resourceName, cancellationTokenSource.Token);
                    cancellationTokenSource.Cancel();

                    await resourceTask;
                    Assert.Fail("Expected exception to be thrown.");
                }
                catch (OperationCanceledException)
                {
                }

            Assert.IsTrue(resourceTask.IsCanceled);
        }
    }
}