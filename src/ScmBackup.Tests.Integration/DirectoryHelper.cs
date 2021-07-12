using System;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace ScmBackup.Tests.Integration
{
    /// <summary>
    /// helper class to create unique temp directories for integration tests
    /// </summary>
    public class DirectoryHelper
    {
        public static string TestSubDir { get { return "_scm-backup-tests"; } }

        public static string CreateTempDirectory()
        {
            return DirectoryHelper.CreateTempDirectory(string.Empty);
        }

        public static string CreateTempDirectory(string suffix)
        {
            // redirect, because this is called from a lot of places
            var helper = new FileSystemHelper();
            return helper.CreateTempDirectory(DirectoryHelper.TestSubDir, suffix);
        }

        /// <summary>
        /// Returns the directory of the current test assembly (usually bin/debug)
        /// </summary>
        public static string TestAssemblyDirectory()
        {
            string unc = typeof(DirectoryHelper).GetTypeInfo().Assembly.CodeBase;

            // convert from UNC path to "real" path
            var uri = new Uri(unc);
            string file = uri.LocalPath;

            return Path.GetDirectoryName(file);
        }
    }
}
