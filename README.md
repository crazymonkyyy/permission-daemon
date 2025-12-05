# Permission Daemon

A daemon to prevent AI agents from deleting or modifying files using Unix-style permissions and glob patterns.

## Overview

The Permission Daemon monitors a directory and enforces file access rules based on glob patterns and agent identities. It's designed to prevent scenarios where one AI agent (e.g., an "editor") might delete files important to another AI agent (e.g., a "tester").

## How It Works

1. **Configuration**: Rules are defined in a YAML configuration file mapping file patterns to permissions
2. **Monitoring**: The daemon watches both the configuration file and a specified directory for changes
3. **Enforcement**: When file operations occur, the daemon checks if the requesting agent has permission
4. **Logging**: All access attempts are logged with decision details

## Installation

```bash
# Clone the repository
git clone <repository-url>

# Build the project
cd premissiondeamon
dotnet build

# Run the daemon
dotnet run --project PermissionDaemon.csproj -- --config permissions.yaml --agent editor --directory /path/to/project
```

## Configuration

The daemon uses a YAML configuration file (default: `permissions.yaml`) with the following structure:

```yaml
version: "1.0"

rules:
  # Example: Protect test files from editor AI
  - patterns:
      - "**/*test*.cs"      # All C# test files anywhere in project
      - "**/*test*.py"      # All Python test files anywhere
    permissions:
      tester_ai: "rwx"      # Tester AI: full access (read, write, execute/delete)
      default: "---"        # Everyone else: no access (safe default)

  # Example: Protect configuration files
  - patterns:
      - "**/*.config"       # Configuration files
      - "**/appsettings*.json"  # App settings
    permissions:
      admin_ai: "rwx"       # Admin AI: full access
      config_manager_ai: "rwx"  # Config manager: full access
      default: "r--"        # Others: read-only

logging:
  level: "verbose"          # Show all permission checks and decisions
  audit: true               # Keep audit trail of access attempts
```

### Pattern Format
- Uses glob patterns similar to `.gitignore`
- `*` matches any sequence of characters except `/`
- `?` matches exactly one character except `/`
- `**` matches zero or more directories

### Permission Format
- Permissions are specified as 3-character strings
- Position 1: Read (r/-)
- Position 2: Write (w/-) 
- Position 3: Execute/Delete (x/-)

## Usage

### Command Line Options

```bash
PermissionDaemon [options]

Options:
  -c, --config PATH     Path to the configuration file (default: permissions.yaml)
  -d, --directory PATH  Directory to watch (default: current directory)
  -a, --agent NAME      Agent name to use for permission checks (default: current user)
  -h, --help            Show help message
```

### Example Usage

```bash
# Run with default settings (current directory, current user as agent)
dotnet run --project PermissionDaemon.csproj

# Run with specific configuration and agent
dotnet run --project PermissionDaemon.csproj -- --config mypermissions.yaml --agent editor --directory /path/to/project

# Run with different agent for testing
dotnet run --project PermissionDaemon.csproj -- --config permissions.yaml --agent tester --directory /path/to/project
```

## Architecture

- `FileWatcher`: Monitors the configuration file and target directory for changes
- `GlobPatternMatcher`: Handles glob pattern matching for file paths
- `PermissionEnforcer`: Enforces access rules based on agent and file pattern
- `ConsoleLogger`: Handles logging with configurable levels
- `PermissionDaemon`: Main daemon class that coordinates all components

## Security Model

The daemon implements a permission model that integrates with Unix file permissions:

1. **Agent-based permissions**: Different access levels for different AI agents
2. **Pattern-based rules**: Apply permissions based on glob patterns
3. **Unix permission enforcement**: Uses `chmod` to modify actual Unix file permissions based on rules
4. **Default behavior**: Safe defaults when no rules match (configurable)
5. **Auditing**: Complete logging of access attempts and permission changes

## Limitations

- This implementation logs access violations but cannot prevent them at the OS level
- For true file protection, consider using this in conjunction with Unix chattr +i or similar tools
- Performance may vary with large directory structures and many rules

## Development

### Running Tests

```bash
dotnet test
```

### Building

```bash
dotnet build
```

## Contributing

For AI agents working with this codebase, please ensure:

1. All changes maintain the fail-safe principle (daemon should never crash)
2. New features are covered by unit tests
3. The configuration format remains human-readable
4. Logging remains verbose as specified in requirements