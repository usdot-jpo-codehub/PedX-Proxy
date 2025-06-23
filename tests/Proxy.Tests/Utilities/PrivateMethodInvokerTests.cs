using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using Proxy.Tests.Adapters;
using static Proxy.Tests.Adapters.MaxTimeAdapterTests;

namespace Proxy.Tests.Utilities
{
    [TestClass]
    public class PrivateMethodInvokerTests
    {
        private class TestClass
        {
            private string ReturnString() => "test";
            private int ReturnInt() => 42;
            private Task<string> ReturnStringAsync() => Task.FromResult("test");
            private Task ReturnVoidAsync() => Task.CompletedTask;
            private Task<int> ReturnIntAsync() => Task.FromResult(42);
            private void ThrowException() => throw new InvalidOperationException("Test exception");
            private Task ThrowExceptionAsync() => Task.FromException(new InvalidOperationException("Test exception"));
            private Task<string> ThrowExceptionWithResultAsync() => Task.FromException<string>(new InvalidOperationException("Test exception"));
        }

        private TestClass _testInstance;

        [TestInitialize]
        public void Setup()
        {
            _testInstance = new TestClass();
        }

        [TestMethod]
        public void InvokePrivateMethod_WithValidMethod_ShouldReturnResult()
        {
            // Act
            var result = PrivateMethodInvoker.InvokePrivateMethod<string>(_testInstance, "ReturnString", Array.Empty<object>());

            // Assert
            Assert.AreEqual("test", result);
        }

        [TestMethod]
        public void InvokePrivateMethod_WithDifferentReturnType_ShouldReturnConvertedResult()
        {
            // Act
            var result = PrivateMethodInvoker.InvokePrivateMethod<int>(_testInstance, "ReturnInt", Array.Empty<object>());

            // Assert
            Assert.AreEqual(42, result);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void InvokePrivateMethod_WithNonExistentMethod_ShouldThrowArgumentException()
        {
            // Act - this should throw
            PrivateMethodInvoker.InvokePrivateMethod<string>(_testInstance, "NonExistentMethod", Array.Empty<object>());
        }

        [TestMethod]
        public void InvokePrivateMethod_WithMethodThatThrows_ShouldPropagateException()
        {
            try
            {
                // Act - this should throw
                PrivateMethodInvoker.InvokePrivateMethod<string>(_testInstance, "ThrowException", Array.Empty<object>());
                
                // If we get here, the test fails
                Assert.Fail("Expected exception was not thrown");
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                // When using reflection, exceptions are wrapped in TargetInvocationException
                // Verify that the inner exception is the expected InvalidOperationException
                Assert.IsInstanceOfType(ex.InnerException, typeof(InvalidOperationException));
                Assert.AreEqual("Test exception", ex.InnerException.Message);
            }
        }

        [TestMethod]
        public async Task InvokePrivateMethodAsync_WithValidMethod_ShouldReturnResult()
        {
            // Act
            var result = await PrivateMethodInvoker.InvokePrivateMethodAsync<string>(_testInstance, "ReturnStringAsync", Array.Empty<object>());

            // Assert
            Assert.AreEqual("test", result);
        }

        [TestMethod]
        public async Task InvokePrivateMethodAsync_WithDifferentReturnType_ShouldReturnConvertedResult()
        {
            // Act
            var result = await PrivateMethodInvoker.InvokePrivateMethodAsync<int>(_testInstance, "ReturnIntAsync", Array.Empty<object>());

            // Assert
            Assert.AreEqual(42, result);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task InvokePrivateMethodAsync_WithNonExistentMethod_ShouldThrowArgumentException()
        {
            // Act - this should throw
            await PrivateMethodInvoker.InvokePrivateMethodAsync<string>(_testInstance, "NonExistentMethod", Array.Empty<object>());
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task InvokePrivateMethodAsync_WithMethodThatDoesNotReturnExpectedTaskType_ShouldThrowInvalidOperationException()
        {
            // Act - this should throw because ReturnVoidAsync returns Task, not Task<string>
            await PrivateMethodInvoker.InvokePrivateMethodAsync<string>(_testInstance, "ReturnVoidAsync", Array.Empty<object>());
        }

        [TestMethod]
        public async Task InvokePrivateMethodAsync_WithVoidTaskMethod_ShouldComplete()
        {
            // Act
            await PrivateMethodInvoker.InvokePrivateMethodAsync(_testInstance, "ReturnVoidAsync", Array.Empty<object>());
            
            // Assert - no exception means success
            Assert.IsTrue(true);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task InvokePrivateMethodAsync_WithNonExistentMethodNoReturnType_ShouldThrowArgumentException()
        {
            // Act - this should throw
            await PrivateMethodInvoker.InvokePrivateMethodAsync(_testInstance, "NonExistentMethod", Array.Empty<object>());
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task InvokePrivateMethodAsync_WithMethodThatThrowsAsyncException_ShouldPropagateException()
        {
            // Act - this should throw
            await PrivateMethodInvoker.InvokePrivateMethodAsync(_testInstance, "ThrowExceptionAsync", Array.Empty<object>());
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task InvokePrivateMethodAsync_WithMethodThatThrowsAsyncExceptionWithResult_ShouldPropagateException()
        {
            // Act - this should throw
            await PrivateMethodInvoker.InvokePrivateMethodAsync<string>(_testInstance, "ThrowExceptionWithResultAsync", Array.Empty<object>());
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task InvokePrivateMethodAsync_WithNonAsyncMethod_ShouldThrowInvalidOperationException()
        {
            // Act - this should throw because ReturnString is not async
            await PrivateMethodInvoker.InvokePrivateMethodAsync(_testInstance, "ReturnString", Array.Empty<object>());
        }

        [TestMethod]
        public async Task InvokePrivateMethodAsync_WithOverloadedMethod_ShouldSelectCorrectOverload()
        {
            // For this test, we'd need a class with overloaded methods
            // Let's use our helper class with a dynamic method that can pass parameters

            var dynamicTestClass = new DynamicTestClass();
            await PrivateMethodInvoker.InvokePrivateMethodAsync(dynamicTestClass, "OverloadedMethod", new object[] { "test" });
            
            // Assert that the string overload was called
            Assert.AreEqual("string", dynamicTestClass.LastCalledOverload);
            
            // Try with int parameter
            await PrivateMethodInvoker.InvokePrivateMethodAsync(dynamicTestClass, "OverloadedMethod", new object[] { 42 });
            
            // Assert that the int overload was called
            Assert.AreEqual("int", dynamicTestClass.LastCalledOverload);
        }

        private class DynamicTestClass
        {
            public string LastCalledOverload { get; private set; }

            private Task OverloadedMethod(string value)
            {
                LastCalledOverload = "string";
                return Task.CompletedTask;
            }

            private Task OverloadedMethod(int value)
            {
                LastCalledOverload = "int";
                return Task.CompletedTask;
            }
        }
    }
}
