## Contributing to AkkoBot
We're really happy to accept contributions. However we also ask that you follow a few rules when doing so.

## Proper Base
When opening a pull request, please make sure your branch targets the latest `main` branch. Also make sure your branch is synchronized with the target branch, to avoid unnecessary surprises.

## Proper Titles
When opening pull requests, make sure the title reflects the changes proposed in the request. Prefer past tense, and be brief. If you're fixing an issue, mention it through its ID (like `#123` for issue 123).

## Code Style
Most of the code style is based off [Microsoft C# Coding Conventions][C#-Conventions] and enforced by an `.editorconfig` file. Here are the most noteworthy:
- Private fields of a class are to be prefixed with an underscore. DO NOT prefix them with `this`.
- Other members of a class may be prefixed with `this`, if it helps with readability. Otherwise, avoid them.
- `protected` members, when inherited, are to be prefixed with `base`.
- Give descriptive names to your variables. DO NOT use [Hungarian Notation][HungarianNotation].
- Methods comprised of a single line of code are to be written with a expression body instead of a block body.
- Everything, besides constructors, **must** have XML documentation.
- Prefer guard clauses whenever possible. Example:

```cs
// Prefer this
public void SomeMethod(int number)
{
    if (number < 0)
      return;

    CallA();
    CallB();
    CallC();
}

// Avoid this
public void OtherMethod(int number)
{
    if (number >= 0)
    {
        CallA();
        CallB();
        CallC();
    }
}
```

- Members in classes should be ordered as follows (with few exceptions):
	- Private fields not initialized in the constructor.
	- Private fields initialized in the constructor.
	- Properties not initialized in the constructor.
	- Properties initialized in the constructor.
	- Default constructor.
	- Parameterized constructors (if any).
	- Public methods.
	- Internal methods.
	- Protected methods.
	- Private methods.
- Pull requests that change the `.editorconfig` file in an attempt to circumvent its style rules **will not** be merged.
- Pull requests that add entries to the `GlobalSuppressions.cs` file with no justification **will not** be merged.

## Code Changes
- All pull requests must be successfully built by the CI tests on the repository. Pull requests that fail on them **will not** be merged.
- Pull requests may undergo a manual code review. Pull requests that fail to address the points brought in the review **will not** be merged.

[C#-Conventions]: https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions
[HungarianNotation]: https://en.wikipedia.org/wiki/Hungarian_notation
