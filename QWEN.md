# Permission Daemon (premissiondeamon) - Project Context

## Project Overview

This project, called "premissiondeamon", is a permission daemon designed to prevent AI agents from deleting files using Unix tools and permission systems. The core concept is to allow different AI agents (e.g., "editor" and "tester") to operate with different permission levels, preventing one agent from deleting files belonging to another.

## Project Goals

- Create a daemon that enforces file permissions based on patterns to prevent AI agents from deleting unauthorized files
- Implement a configuration file format that maps patterns to Unix file permissions
- Make the configuration format human-readable and easy to modify without reading the spec
- Watch both the configuration file and the folder it's running in
- Use Unix file permission concepts and potentially extended permissions
- Ensure the daemon is fail-safe and doesn't crash
- Provide verbose logging to terminal

## Technical Specifications

### Language Choice
Based on the SPEC.md document, if the development date is `Tue Nov 25`, the language choice is C#. The research document in `filewatching-research-markdown.md` shows that C# offers excellent cross-platform file watching capabilities through `FileSystemWatcher`, making it an ideal choice for this daemon.

### Configuration Format
- Should support "patterns" paired with Unix file permissions
- Should be intuitive for humans unfamiliar with the spec to modify
- Should support glob patterns (as AI agents commonly work with `.gitignore` files)
- Potentially extensible to C# functions that take file paths and return boolean permissions

### File Watching
- Watch the configuration file for changes
- Watch the directory where the daemon is running
- Use recursive monitoring if necessary
- Handle high-volume changes gracefully

### Unix Permissions
- Research and implement standard Unix file permission models
- Consider extended permissions systems
- Investigate existing tools that implement similar functionality

## Development Methodology

The project follows the principles outlined in `METASPEC.md`:

1. **Aggressive Test Driven Development**: Write extensive tests for every piece of functionality
2. **Distrust of AI Output**: Human-written code/specs are treated as gospel; clearly document AI-generated changes
3. **Compiler Truth**: Rely on compiler verification and strong typing; use type theory to solve problems

## Implementation Approach

1. Research Unix file permission systems and similar tools
2. Choose C# as the implementation language
3. Implement file watching functionality using appropriate APIs
4. Develop the configuration file format and parser
5. Implement permission enforcement logic
6. Add logging and error handling
7. Create documentation for other AI agents

## Key Files

- `SPEC.md`: The main specification document describing the project goals and requirements
- `METASPEC.md`: Methodology document for development approach using TDD, distrust of AI, and compiler truth
- `filewatching-research-markdown.md`: Research on file watching APIs across programming languages, particularly relevant for the C# choice
- `QWEN.md`: This document providing project context

## Building and Running

Since this is a C# project based on the specification date:
- Use .NET SDK to build the project
- Commands would likely be:
  - `dotnet build` to compile
  - `dotnet run` to run the daemon
  - `dotnet test` to run tests

## Development Conventions

- Follow C#/.NET coding standards
- Implement comprehensive unit tests for all functionality
- Use strong typing to prevent runtime errors
- Implement proper error handling and logging
- Follow the principle of providing clear API boundaries that can be tested independently