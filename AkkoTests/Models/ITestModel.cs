using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AkkoTests.Models;

/// <summary>
/// Test interface.
/// </summary>
internal interface ITestModel
{
    int Id { get; }
    string Name { get; }
}

/// <summary>
/// Test interface.
/// </summary>
internal interface IEmpty
{
}