using AkkoCore.Commands.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace AkkoTests.Models;

/// <summary>
/// Dummy object used for testing.
/// </summary>
internal record MockObject(int Id, string Name);

[CommandService<ITestModel>(ServiceLifetime.Singleton)]
internal record MockSingleton(int Id, string Name) : ITestModel;

[CommandService<IEmpty>(ServiceLifetime.Singleton)]
internal record MockWrong(int Id, string Name) : ITestModel;