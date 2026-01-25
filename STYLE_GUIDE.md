# Code and Commit Style Guide

## Code Style Guide

### Functions
- Use PascalCase
- Name the function based on what it does
- Separate functions by responsibility or task. Functions should not be doing multiple tasks. Instead, delegate tasks to smaller functions for clarity.

### Private Variables
- Use camelCase
- Name variables by what data they hold
- Use [SerializeField] to allow inspector modification
- Precede all private variables with the "private" keyword
- Write floats with at least one decimal point of accuracy and include the f (e.g., write 1.0f, NOT 1f or 1.0)

### Public Variables
- Avoid public variables where possible. Prefer to use setter and getter functions.
- If using public variables, name with PascalCase

### Constants
- Use UPPER_SNAKE_CASE
- Avoid magic numbers. Define constants instead of using hard-coded values throughout your code.

### Enums
- Use PascalCase for both the enum type and its values

### Classes and Files
- Use PascalCase for class names
- One class per file
- File name must match the class name exactly

### Comments and Documentation
- Write comments to explain *why* something is done, not *what* is being done
- Use comments for non-obvious decisions, workarounds, or complex logic
- Use XML documentation comments (`///`) for all public methods and classes. At minimum, fill out the `<summary>` section.
- Document complex or non-obvious private methods
- Simple, self-explanatory functions do not require comments

**Example of a good comment:**
```csharp
// Delay spawn by 0.1s to prevent collision with player spawn animation
yield return new WaitForSeconds(0.1f);
```

### Class Organization
Organize class members in the following order:
1. Serialized fields
2. Constants
3. Private fields
4. Properties
5. Unity lifecycle methods (Awake, Start, Update, etc.)
6. Public methods
7. Private methods

### Whitespace
- Separate methods with one empty line
- The following formatting rules are enforced via the .editorconfig file in the project root
  - Use 4 spaces for indentation (no tabs)
  - Insert a final newline at the end of files
  - Trim trailing whitespace

### Brace Style
- Use Allman style braces (braces always appear on new lines)
  - This is enforced by the .editorconfig file

### Type Declarations
- Prefer explicit types over `var` for clarity

## GitHub Style Guide

### Branches
- Name branches in full lowercase

### Commit Messages
- Start commit messages with an imperative action verb (i.e. a command or action verb)
  - Examples include Add, Fix, Update, Remove, Implement, Refactor, Document, Optimize, Format, Overhaul, Move, Rename, Revert etc.
- Start commit messages with a capital letter and format them as sentences without the trailing period
- Commits should be about one sentence long. For more complex commits (such as full features) add details in the commit description.
