﻿using System.IO;
using System.Linq;
using Xunit;

namespace ScmBackup.Tests.Integration
{
    public class ConfigBackupMakerTests
    {
        [Fact]
        public void SubfolderIsSet()
        {
            var sut = new ConfigBackupMaker(new FakeContext());
            Assert.True(!string.IsNullOrWhiteSpace(sut.SubFolder));
        }

        [Fact]
        public void ConfigFileNamesAreSet()
        {
            var sut = new ConfigBackupMaker(new FakeContext());
            Assert.True(sut.ConfigFileNames.Any());
        }
        
        [Fact]
        public void CopiesAllFiles()
        {
            var config = new Config();
            config.LocalFolder = DirectoryHelper.CreateTempDirectory("configbackupmaker1");
        
            var context = new FakeContext();
            context.Config = config;

            var sut = new ConfigBackupMaker(context);
            sut.BackupConfigs();

            foreach (var file in sut.ConfigFileNames)
            {
                string path = Path.Combine(config.LocalFolder, sut.SubFolder, file);

                Assert.True(File.Exists(path), file);
            }
        }
    }
}