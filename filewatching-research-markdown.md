# File Watching Research and Implementation Notes

## Goal
The goal is to create a daemon that prevents AI agents from deleting files based on user-defined rules using glob patterns and user permissions.

## Approaches Considered

### 1. File System Watchers
- Use .NET FileSystemWatcher to monitor file operations in real-time
- Can detect file creation, modification, deletion, and renaming
- Works well for monitoring a directory and its subdirectories
- Handles changes to the configuration file dynamically

### 2. Glob Pattern Matching
- Use Microsoft.Extensions.FileSystemGlobbing for pattern matching
- Supports standard glob patterns like `**/*test*.cs`, `**/*.config`, etc.
- More intuitive for users familiar with .gitignore patterns
- Cross-platform compatibility

### 3. Permission Checking
- Determine the effective user running the operation
- Compare against configured rules in the permission file
- Deny operations that don't match the permitted user for a pattern

## Implementation Strategy

### Configuration File Format
```json
{
  "rules": [
    {
      "pattern": "**/*test*.cs",
      "allowedUser": "tester"
    },
    {
      "pattern": "**/*.cs", 
      "allowedUser": "editor"
    }
  ]
}
```

### Daemon Operation
1. Loads configuration at startup
2. Monitors the config file for changes and reloads when modified
3. Watches the current directory and subdirectories for file operations
4. When a file operation occurs:
   - Checks if the file path matches any configured patterns
   - Verifies if the current user is allowed to perform that operation
   - Logs violations and potentially blocks/reverts operations

## Security Considerations
- The daemon must run with sufficient privileges to monitor and potentially prevent operations
- Should not crash if it encounters errors (fail-safe)
- Logging should be verbose to track access violations
- User identification should be robust

## Alternative Approaches
- Using filesystem ACLs (Access Control Lists) for more fine-grained control
- Implementing a custom file system filter driver (more complex)
- Using existing tools like inotify on Linux with custom scripts
- Employing OS-level file protection mechanisms

## Selected Implementation
The implementation uses FileSystemWatcher for monitoring and Microsoft.Extensions.FileSystemGlobbing for pattern matching. This approach is:
- Cross-platform compatible
- Uses well-tested libraries
- Easy to understand and modify
- Fail-safe with proper error handling