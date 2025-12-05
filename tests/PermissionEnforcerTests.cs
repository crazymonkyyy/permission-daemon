using System.Collections.Generic;
using Xunit;

namespace PermissionDaemon.Tests
{
    public class PermissionEnforcerTests
    {
        [Fact]
        public void HasPermission_ReadActionWithReadPermission_ReturnsTrue()
        {
            var config = new DaemonConfig
            {
                Rules = new List<Rule>
                {
                    new Rule
                    {
                        Patterns = new List<string> { "**/*.txt" },
                        Permissions = new Dictionary<string, string>
                        {
                            { "test_agent", "r--" }  // read-only
                        }
                    }
                }
            };

            var enforcer = new PermissionEnforcer(config, "test_agent");
            
            // Test read permission
            var result = enforcer.HasPermission("test.txt", System.Security.AccessControl.FileSystemRights.ReadData);
            Assert.True(result);
        }

        [Fact]
        public void HasPermission_WriteActionWithReadPermission_ReturnsFalse()
        {
            var config = new DaemonConfig
            {
                Rules = new List<Rule>
                {
                    new Rule
                    {
                        Patterns = new List<string> { "**/*.txt" },
                        Permissions = new Dictionary<string, string>
                        {
                            { "test_agent", "r--" }  // read-only
                        }
                    }
                }
            };

            var enforcer = new PermissionEnforcer(config, "test_agent");
            
            // Test write permission with only read access
            var result = enforcer.HasPermission("test.txt", System.Security.AccessControl.FileSystemRights.WriteData);
            Assert.False(result);
        }

        [Fact]
        public void HasPermission_ReadActionWithoutPermission_ReturnsFalse()
        {
            var config = new DaemonConfig
            {
                Rules = new List<Rule>
                {
                    new Rule
                    {
                        Patterns = new List<string> { "**/*.txt" },
                        Permissions = new Dictionary<string, string>
                        {
                            { "other_agent", "rwx" }  // different agent has permission
                        }
                    }
                }
            };

            var enforcer = new PermissionEnforcer(config, "test_agent");
            
            // Test access for agent that doesn't have permission
            var result = enforcer.HasPermission("test.txt", System.Security.AccessControl.FileSystemRights.ReadData);
            Assert.False(result);
        }

        [Fact]
        public void HasPermission_MatchingPattern_ReturnsCorrectPermission()
        {
            var config = new DaemonConfig
            {
                Rules = new List<Rule>
                {
                    new Rule
                    {
                        Patterns = new List<string> { "**/*.txt" },
                        Permissions = new Dictionary<string, string>
                        {
                            { "test_agent", "rwx" }  // full access
                        }
                    }
                }
            };

            var enforcer = new PermissionEnforcer(config, "test_agent");
            
            // Test full access
            Assert.True(enforcer.HasPermission("test.txt", System.Security.AccessControl.FileSystemRights.ReadData));
            Assert.True(enforcer.HasPermission("test.txt", System.Security.AccessControl.FileSystemRights.WriteData));
            Assert.True(enforcer.HasPermission("test.txt", System.Security.AccessControl.FileSystemRights.Delete));
        }

        [Fact]
        public void HasPermission_NoMatchingRule_ReturnsTrue()
        {
            var config = new DaemonConfig
            {
                Rules = new List<Rule>
                {
                    new Rule
                    {
                        Patterns = new List<string> { "**/*.cs" }, // different pattern
                        Permissions = new Dictionary<string, string>
                        {
                            { "test_agent", "---" }  // no access
                        }
                    }
                }
            };

            var enforcer = new PermissionEnforcer(config, "test_agent");
            
            // Test file that doesn't match any rule should be allowed by default
            var result = enforcer.HasPermission("test.txt", System.Security.AccessControl.FileSystemRights.ReadData);
            Assert.True(result);
        }

        [Fact]
        public void HasPermission_DefaultPermission_UsedWhenNoAgentSpecificPermission()
        {
            var config = new DaemonConfig
            {
                Rules = new List<Rule>
                {
                    new Rule
                    {
                        Patterns = new List<string> { "**/*.txt" },
                        Permissions = new Dictionary<string, string>
                        {
                            { "default", "r--" }  // default read-only
                        }
                    }
                }
            };

            var enforcer = new PermissionEnforcer(config, "any_agent");
            
            // Test default permission
            Assert.True(enforcer.HasPermission("test.txt", System.Security.AccessControl.FileSystemRights.ReadData));
            Assert.False(enforcer.HasPermission("test.txt", System.Security.AccessControl.FileSystemRights.WriteData));
        }
    }
}