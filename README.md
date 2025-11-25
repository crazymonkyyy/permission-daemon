# Permission Daemon

A daemon that prevents AI agents from deleting files using configurable rules based on glob patterns and user permissions.

## Goal

This daemon monitors file operations in a directory and prevents unauthorized deletions based on user-defined rules. For example, if you have an AI agent running as "editor" and another as "tester", the daemon can ensure the editor doesn't delete test files.

## Features

- Real-time file operation monitoring using FileSystemWatcher
- Configurable rules using intuitive glob patterns
- User-based permission system
- Dynamic configuration reloading
- Comprehensive logging of access violations
- Fail-safe operation (daemon doesn't crash on errors)

## Installation

1. Make sure you have .NET 6.0 or higher installed
2. Clone this repository
3. Build the project: `dotnet build`
4. Run the daemon: `dotnet run`

## Configuration

The daemon uses a `permissions.config` JSON file in the current directory with the following format:

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
    },
    {
      "pattern": "**/*.config",
      "allowedUser": "admin"
    }
  ]
}
```

### Pattern Format
- Uses standard glob patterns similar to `.gitignore`
- `**` matches any number of subdirectories
- `*` matches any sequence of characters except path separators
- `?` matches any single character
- `[abc]` matches any character inside brackets
- `[a-z]` matches any character in the specified range

### Rules
- Rules are processed in order
- The first matching rule determines the permission
- If no rule matches, the operation is allowed
- Each rule has:
  - `pattern`: A glob pattern to match file paths
  - `allowedUser`: The user allowed to perform operations on matching files

## Usage

1. Place the `permissions.config` file in the directory you want to monitor
2. Start the daemon with `dotnet run`
3. The daemon will monitor all file operations in the current directory and subdirectories
4. When an operation is blocked, it will be logged in `access_violations.log`

## Examples

### Prevent Non-Testers from Deleting Test Files
```json
{
  "rules": [
    {
      "pattern": "**/*test*.cs",
      "allowedUser": "tester"
    }
  ]
}
```

### Prevent Non-Editors from Modifying Source Files
```json
{
  "rules": [
    {
      "pattern": "**/*.cs", 
      "allowedUser": "editor"
    }
  ]
}
```

## Logging

The daemon logs all operations to the console with timestamps. Access violations are also written to `access_violations.log` in the same directory.

Example log output:
```
[12:45:23] Created: src/Program.cs
[12:45:24] ACCESS VIOLATION: User 'editor' not allowed to delete 'tests/MyTest.cs' (matches pattern '**/*test*.cs', allowed user: 'tester')
```

## How It Works

1. The daemon loads the configuration file at startup
2. Sets up file system watchers to monitor:
   - The configuration file for changes
   - The current directory and subdirectories for file operations
3. When a file operation occurs:
   - Checks if the file path matches any configured patterns
   - Verifies if the current user is allowed to perform that operation
   - Logs violations if the operation is blocked
4. Automatically reloads the configuration when the config file changes

## Security Notes

- The daemon identifies users based on the environment username
- For production use, ensure the daemon runs with appropriate privileges
- Monitor the `access_violations.log` file regularly for security events

## Troubleshooting

- If the daemon doesn't start, ensure .NET 6.0+ is installed
- If file operations aren't being monitored, check the daemon is running in the correct directory
- If rules aren't working, verify the glob patterns match your intended files
- Check that the user context is correctly identified by the system