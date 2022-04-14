using AkkoCore.Commands.Abstractions;
using AkkoTests.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using Xunit;

namespace AkkoTests.Core.Attributes;

public sealed class CommandServiceAttributeTests
{
    [Fact]
    internal void ServiceRegistrationTest()
    {
        var ioc = new ServiceCollection();
        var serviceType = typeof(MockSingleton);
        var attribute = serviceType.GetCustomAttribute<CommandServiceAttributeBase>();

        attribute.RegisterService(ioc, serviceType);

        Assert.Single(ioc);
        Assert.Contains(ioc, x => x.ServiceType.IsAssignableFrom(serviceType));
    }

    [Fact]
    internal void ServiceInstanceRegistrationFailureTest()
    {
        var ioc = new ServiceCollection();
        var serviceType = typeof(MockSingleton);
        var attribute = serviceType.GetCustomAttribute<CommandServiceAttributeBase>();

        ioc.AddSingleton(new MockSingleton(1, "A"));

        Assert.Throws<ArgumentException>(() => attribute.RegisterService(ioc, serviceType));
    }

    [Fact]
    internal void ServiceHierarchyRegistrationFailureTest()
    {
        var ioc = new ServiceCollection();
        var serviceType = typeof(MockWrong);
        var attribute = serviceType.GetCustomAttribute<CommandServiceAttributeBase>();

        Assert.Throws<InvalidOperationException>(() => attribute.RegisterService(ioc, serviceType));
    }
}