﻿using System.IO;
using Xunit;

namespace ScmBackup.Tests.Integration
{
    public class DirectoryHelperTests
    {
        [Fact]
        public void TestAssemblyDirectoryWorks()
        {
            // Difficult to test, because it's hard to determine the path *without* using the method under test.
            // -> at least make sure it doesn't throw and it's a real path
            string result = DirectoryHelper.TestAssemblyDirectory();

            Assert.False(string.IsNullOrWhiteSpace(result), result);
            Assert.True(Directory.Exists(result), result);
        }
    }
}
