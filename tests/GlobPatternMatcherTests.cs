using System;
using System.Collections.Generic;
using Xunit;

namespace PermissionDaemon.Tests
{
    public class GlobPatternMatcherTests
    {
        [Fact]
        public void IsMatch_SimplePattern_MatchesCorrectly()
        {
            // Test basic pattern matching
            Assert.True(GlobPatternMatcher.IsMatch("*.txt", "file.txt"));
            Assert.False(GlobPatternMatcher.IsMatch("*.txt", "file.cs"));
        }

        [Fact]
        public void IsMatch_StarPattern_MatchesCorrectly()
        {
            // Test * wildcard (matches any characters except /)
            Assert.True(GlobPatternMatcher.IsMatch("test*", "test123"));
            Assert.True(GlobPatternMatcher.IsMatch("test*", "test"));
            Assert.False(GlobPatternMatcher.IsMatch("test*", "pretest"));
        }

        [Fact]
        public void IsMatch_QuestionMarkPattern_MatchesCorrectly()
        {
            // Test ? wildcard (matches exactly one character)
            Assert.True(GlobPatternMatcher.IsMatch("test?", "test1"));
            Assert.True(GlobPatternMatcher.IsMatch("test?", "testa"));
            Assert.False(GlobPatternMatcher.IsMatch("test?", "test12"));
            Assert.False(GlobPatternMatcher.IsMatch("test?", "test"));
        }

        [Fact]
        public void IsMatch_DoubleStarPattern_MatchesCorrectly()
        {
            // Test ** wildcard (matches zero or more directories)
            Assert.True(GlobPatternMatcher.IsMatch("**/*.txt", "file.txt"));
            Assert.True(GlobPatternMatcher.IsMatch("**/*.txt", "dir/file.txt"));
            Assert.True(GlobPatternMatcher.IsMatch("**/*.txt", "dir/subdir/file.txt"));
        }

        [Fact]
        public void IsMatchAny_ListOfPatterns_MatchesCorrectly()
        {
            var patterns = new List<string> { "*.txt", "*.cs", "*.js" };
            
            Assert.True(GlobPatternMatcher.IsMatchAny(patterns, "file.txt"));
            Assert.True(GlobPatternMatcher.IsMatchAny(patterns, "code.cs"));
            Assert.True(GlobPatternMatcher.IsMatchAny(patterns, "script.js"));
            Assert.False(GlobPatternMatcher.IsMatchAny(patterns, "image.png"));
        }
    }
}