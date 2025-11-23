C# Coding Conventions for AI Agent (Antigravity IDE)

Version: 1.9 (2025-08-05)

Context: This document outlines the mandatory coding standards, naming conventions, and architectural patterns for the "Smart Cities \& Mobility" project (SONDA).

Instruction for AI: Use these rules to validate, refactor, and generate C# and Blazor code.

1\. General Guidelines

Base Standard: Follow the official Microsoft C# Coding Conventions and Naming Guidelines unless specified otherwise below.

Language: All code (projects, classes, methods, variables, properties, comments, regions) must be in English.

Clarity: Names must be explanatory and strictly avoid abbreviations. Do not include data types in names (e.g., avoid strName).

2\. Data Types \& Initialization

Strings

Use the string alias (not System.String).

Use string.Empty for empty assignments.

Use string.Equals for comparisons.

Use string.IsNullOrEmpty to check for null or empty.

Use StringBuilder for loop-based or heavy string modifications ($O(n)$ or higher).

Use String Interpolation ($"{var}") for constructing strings.

Integers \& Numbers

Use the int alias (not System.Int32).

Use int.Parse for conversions.

Use long for potentially large values or values expected to grow.

Booleans

Use the bool alias (not System.Boolean).

Use bool.Parse for conversions.

Naming Anti-pattern: Boolean variables representing a state should not start with "is" (e.g., use showPassword, not isShowPassword).

Initialization

Always initialize variables of simple types.

Correctly handle C# Nullable reference types.

3\. Naming Conventions

Element Type

Convention

Format

Notes

Classes / Methods / Properties

UpperCamelCase

MyClass, MyMethod

PascalCase

Interfaces

'I' + UpperCamelCase

IMyInterface





Local Variables / Parameters

lowerCamelCase

myVariable





Private Fields

'\_' + lowerCamelCase

\_myField

Must use underscore prefix.

Properties (Object)

UpperCamelCase

ObjectNameProperty

e.g., UserEmail.



Specific Method Actions

Delete: Use when permanently eliminating an object (e.g., from DB).

Remove: Use when removing an object from a container/list.

Create: Use when instantiating/creating a new object.

Add: Use when adding an existing object to a container.

Update: Use when editing object data.

4\. Exception Handling \& Logging

Exceptions

Use try-catch or try-catch-finally where necessary.

Catch Block: Must log the error (Log, DB, or Event Viewer).

Finally Block: Ensure consistency of state/resources.

Rethrowing: Use throw; to preserve the original stack trace. DO NOT use throw ex; or throw exception;.

Logging

Use a single standardized method for saving messages (LogInformation, LogDebug, LogWarning, LogError).

Log all "important" actions.

Messages: Must be defined as Constants in a resource class (e.g., Language, StringResource). Do not hardcode strings.

5\. Control Flow \& LINQ

Disposal: Use using statements (preferable) or call Dispose()/Close() explicitly.

Conditionals:

Do not compare explicitly to true or false (e.g., if (isValid), not if (isValid == true)).

Do not perform assignments inside comparison expressions.

Do not re-evaluate fixed methods inside loop iterations.

LINQ: Use Lambda expressions.

6\. Code Structure \& Layout

Class Structure (Ordering)

Elements inside a class/struct/interface must follow this order:

Constant Fields

Fields

Constructors

Finalizers (Destructors)

Delegates

Events

Enums

Interfaces

Properties

Indexers

Methods

Structs

Classes

Inside these groups, sort by Access Modifier:

public

internal / protected internal

protected

private

Then by modifier:

static

non-static

Then by mutability:

readonly

non-readonly

Detailed Design

Single Responsibility Principle (SRP): Classes and methods must have a single responsibility.

One Class Per File.

Regions: Use #region to separate the groups defined in the "Class Structure" section. Do not use regions to hide "functional" divisions within a class.

No Duplication: Reuse code strictly.

Method Parameters: Maximum 5 parameters (unless justified exception).

Braces: Always use {} for control blocks, even for single-line if statements.

No Warnings: The Output window must show 0 errors and 0 warnings.

Comments: Prefer self-explanatory code over excessive comments.

7\. Architecture \& Patterns

Design

Coupling/Cohesion: Low coupling, high cohesion.

Partial Classes: Use only to subdivide massive classes (though massive classes should be avoided).

Culture: Implementation must be CultureInfo independent.

Resources: Use LanguageResource for all UI text.

Colors: Define in a single palette location.

Database: Parameter names for Stored Procedures/Queries must be defined as properties: ObjectNameProperty.

8\. Backend Standards (API \& Controllers)

Instantiation

Use new() shorthand when the type is explicit.

Good: List<string> strings = new();

Bad: List<string> strings = new List<string>();

Controllers

Route Attribute: Use \[Route("api/\[Controller]")] (do not hardcode controller name).

Return Types:

500 InternalServerError: Unhandled exceptions.

400 BadRequest: Input errors. Return BadRequest(Language.InvalidData). Use negative numbers if distinguishing errors is needed.

404 NotFound: Element not found in DB.

409 Conflict: Consistency errors (e.g., deleting an entity with active relations).

9\. Blazor \& Frontend Standards

HTTP Methods

GET: Retrieve data only (no state change).

POST: Create new resources.

PUT: Update existing resources. URL: api/method/{id}. Example: PUT: url/api/roles/4.

DELETE: Remove resources.

Razor Files (.razor)

Self-Closing Tags: Use <Tag /> if there is no content.

Bad: <Tag> </Tag>

Injection:

Inject services in the .razor.cs file (code-behind), NOT in the .razor file.

Keep Injects sorted alphabetically.

Directives Order:

@page

@attribute

@using

@inject

Component Parameters: If a component has generic T or Context parameters, assign them first.

Example: <MyComponent T="Type" Context="ctx" ... />

Clean Up: Remove unused using statements.

Error Handling \& Snackbar

Use the SnackService pattern for feedback.

Server Side:

// Return the Resource Key name, not the raw string

return BadRequest(nameof(Language.InvalidData));





Client Side:

@inject SnackService Snackbar



// Display error from server response

await Snackbar.Add(response);



// Display general message

Snackbar.Add(Language.ExceptionMessage, Severity.Error);





Annex: SnackService Implementation

Ensure the SnackService is registered in Program.cs:

builder.Services.AddScoped<SnackService>();

using MudBlazor;

using System.Net.Http;

using System.Threading.Tasks;

using System.Collections.Generic;

using System;



public class SnackService

{

&nbsp;   private ISnackbar \_snackbar;



&nbsp;   public SnackService(ISnackbar snackbar)

&nbsp;   {

&nbsp;       \_snackbar = snackbar;

&nbsp;   }



&nbsp;   public async Task<Snackbar?> Add(HttpResponseMessage response)

&nbsp;   {

&nbsp;       string? resCode = await response.Content.ReadAsStringAsync();

&nbsp;       string? error = null;



&nbsp;       // Attempt to get localized string

&nbsp;       error = Language.ResourceManager.GetString(resCode);



&nbsp;       if (string.IsNullOrEmpty(error))

&nbsp;       {

&nbsp;           error = Language.ExceptionMessage;

&nbsp;       }



&nbsp;       return \_snackbar.Add(error, Severity.Error);

&nbsp;   }



&nbsp;   #region MudBlazor base Snackbar methods



&nbsp;   public IEnumerable<Snackbar> ShownSnackbars => \_snackbar.ShownSnackbars;



&nbsp;   public SnackbarConfiguration Configuration => \_snackbar.Configuration;



&nbsp;   public Snackbar? Add(string message, Severity severity = Severity.Normal, Action<SnackbarOptions>? configure = null, string key = "")

&nbsp;       => \_snackbar.Add(message, severity, configure, key);



&nbsp;   public Snackbar? Add(RenderFragment message, Severity severity = Severity.Normal, Action<SnackbarOptions>? configure = null, string key = "")

&nbsp;       => \_snackbar.Add(message, severity, configure, key);



&nbsp;   public void Clear() => \_snackbar.Clear();



&nbsp;   public void Dispose() => \_snackbar.Dispose();



&nbsp;   public void Remove(Snackbar snackbar) => \_snackbar.Remove(snackbar);



&nbsp;   public void RemoveByKey(string key) => \_snackbar.RemoveByKey(key);



&nbsp;   #endregion

}



