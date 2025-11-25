#!/bin/bash

# Setup script for Permission Daemon

echo "Permission Daemon Setup Script"
echo "==============================="

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null; then
    echo "Error: .NET SDK is not installed."
    echo "Please install .NET SDK 6.0 or higher from https://dotnet.microsoft.com/download"
    echo ""
    echo "On Ubuntu/Debian:"
    echo "  wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb"
    echo "  sudo dpkg -i packages-microsoft-prod.deb"
    echo "  sudo apt-get update && sudo apt-get install -y dotnet-sdk-6.0"
    echo ""
    echo "On CentOS/RHEL:"
    echo "  sudo rpm -Uvh https://packages.microsoft.com/config/centos/7/packages-microsoft-prod.rpm"
    echo "  sudo yum install dotnet-sdk-6.0"
    echo ""
    echo "On macOS:"
    echo "  brew install --cask dotnet-sdk"
    echo ""
    exit 1
fi

echo ".NET SDK is installed: $(dotnet --version)"

# Navigate to the project directory
cd src/PermissionDaemon

# Restore packages
echo "Restoring packages..."
dotnet restore

# Build the project
echo "Building the project..."
if dotnet build; then
    echo ""
    echo "Build successful!"
    echo ""
    echo "To run the daemon:"
    echo "  dotnet run"
    echo ""
    echo "The daemon will:"
    echo "- Look for permissions.config in the current directory"
    echo "- Monitor all files in the current directory and subdirectories"
    echo "- Prevent unauthorized file deletions based on the rules"
    echo "- Log all operations and violations to the console"
    echo ""
else
    echo "Build failed!"
    exit 1
fi