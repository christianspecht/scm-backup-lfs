﻿using ScmBackup.Scm;
using System;
using System.IO;
using Xunit;

namespace ScmBackup.Tests.Integration.Scm
{
    public abstract class IScmTests
    {
        // this must be set in the child classes' constructor
        internal IScm sut;

        // public and private test repositories
        internal abstract string PublicRepoUrl { get; }
        internal abstract string PrivateRepoUrl { get; }
        internal abstract ScmCredentials PrivateRepoCredentials { get; }
        internal abstract string NonExistingRepoUrl { get; }

        // commit ids that do/do not exist in the public repo
        internal abstract string PublicRepoExistingCommitId { get; }
        internal abstract string PublicRepoNonExistingCommitId { get; }

        // public test repo which contains LFS files / a commit that exists in it / file name of a LFS file in the repo
        internal abstract string LfsRepoUrl { get; }
        internal abstract string LfsRepoExistingCommitId { get; }
        internal abstract string LfsRepoFileName { get; }

        [Fact]
        public void SutWasSetInChildClass()
        {
            Assert.NotNull(this.sut);
        }

        [Fact]
        public void IsOnThisComputerExecutes()
        {
            sut.IsOnThisComputer();
        }

        [Fact]
        public void IsOnThisComputerReturnsTrue()
        {
            // Some of the other integration tests will need to use the SCM -> make sure that it's installed on the machine running the tests
            var result = sut.IsOnThisComputer();

            Assert.True(result);
        }

        [Fact]
        public void GetVersionNumberReturnsVersionNumber()
        {
            // Getting the SCM's version number without the method under test is difficult -> just check whether it executes and returns something
            var result = sut.GetVersionNumber();

            Assert.False(string.IsNullOrWhiteSpace(result));

            // output version number to be sure
            Console.WriteLine("{0} version {1}", sut.DisplayName, result);
        }

        [Fact]
        public void GetVersionNumberDoesntContainSpecialCharacters()
        {
            var result = sut.GetVersionNumber();

            Assert.False(result.Contains("\r"), "contains \\r");
            Assert.False(result.Contains("\n"), "contains \\n");
            Assert.False(result.Contains("\t"), "contains \\t");
        }

        [Fact]
        public void DirectoryIsRepositoryReturnsFalseForNonExistingDir()
        {
            string dir = DirectoryHelper.CreateTempDirectory(DirSuffix("non-existing"));
            string subDir = Path.Combine(dir, "sub");

            Assert.False(sut.DirectoryIsRepository(subDir));
        }

        [Fact]
        public void DirectoryIsRepositoryReturnsFalseForEmptyDir()
        {
            string dir = DirectoryHelper.CreateTempDirectory(DirSuffix("empty"));

            Assert.False(sut.DirectoryIsRepository(dir));
        }

        [Fact]
        public void DirectoryIsRepositoryReturnsFalseForNonEmptyDir()
        {
            string dir = DirectoryHelper.CreateTempDirectory(DirSuffix("non-empty"));
            string subDir = Path.Combine(dir, "sub");
            Directory.CreateDirectory(subDir);
            File.WriteAllText(Path.Combine(dir, "foo.txt"), "foo");

            Assert.False(sut.DirectoryIsRepository(dir));
        }

        [Fact]
        public void CreateRepositoryCreatesNewRepository()
        {
            string dir = DirectoryHelper.CreateTempDirectory(DirSuffix("create"));
            
            sut.IsOnThisComputer();
            sut.CreateRepository(dir);

            Assert.True(sut.DirectoryIsRepository(dir));
        }

        [Fact]
        public void CreateRepositoryDoesNothingWhenDirectoryIsARepository()
        {
            string dir = DirectoryHelper.CreateTempDirectory(DirSuffix("create-2"));
            
            sut.CreateRepository(dir);

            // this should do nothing
            sut.CreateRepository(dir);

            Assert.True(sut.DirectoryIsRepository(dir));
        }

        [Fact]
        public void CreateRepositoryCreatesNonExistingDirectory()
        {
            string dir = DirectoryHelper.CreateTempDirectory(DirSuffix("create-3"));
            string subDir = Path.Combine(dir, "sub");
            
            sut.CreateRepository(subDir);

            Assert.True(Directory.Exists(subDir));
        }

        [Fact]
        public void PullFromRemote_PublicUrl_CreatesNewRepo()
        {
            string dir = DirectoryHelper.CreateTempDirectory(DirSuffix("pull-new"));
            string subDir = Path.Combine(dir, "sub");

            sut.PullFromRemote(this.PublicRepoUrl, subDir);

            Assert.True(Directory.Exists(subDir));
            Assert.True(sut.DirectoryIsRepository(subDir));
        }

        [Fact]
        public void PullFromRemote_PublicUrl_UpdatesExistingRepo()
        {
            string dir = DirectoryHelper.CreateTempDirectory(DirSuffix("pull-existing"));
            sut.CreateRepository(dir);

            sut.PullFromRemote(this.PublicRepoUrl, dir);

            Assert.True(sut.DirectoryIsRepository(dir));

            // does the local repo contain commits from the remote repo?
            Assert.True(sut.RepositoryContainsCommit(dir, this.PublicRepoExistingCommitId));
        }

        [Fact]
        public void PullFromRemote_PublicUrl_ThrowsWhenDirIsNotEmpty()
        {
            string dir = DirectoryHelper.CreateTempDirectory(DirSuffix("pull-not-empty"));
            File.WriteAllText(Path.Combine(dir, "foo.txt"), "foo");

            Assert.Throws<InvalidOperationException>(() => sut.PullFromRemote(this.PublicRepoUrl, dir)); 
        }

        [Fact]
        public void PullFromRemote_PrivateUrl_CreatesNewRepo()
        {
            string dir = DirectoryHelper.CreateTempDirectory(DirSuffix("private-pull-new"));
            string subDir = Path.Combine(dir, "sub");

            sut.PullFromRemote(this.PrivateRepoUrl, subDir, this.PrivateRepoCredentials);

            Assert.True(Directory.Exists(subDir));
            Assert.True(sut.DirectoryIsRepository(subDir));
        }

        [SkippableFact]
        public void PullFromRemote_Lfs_CreatesNewRepo()
        {
            Skip.If(this.SkipLfsTests());

            string dir = DirectoryHelper.CreateTempDirectory(DirSuffix("pull-lfs-new"));
            sut.PullFromRemote(this.LfsRepoUrl, dir);

            Assert.True(sut.DirectoryIsRepository(dir));
            Assert.True(sut.RepositoryContainsCommit(dir, this.LfsRepoExistingCommitId));

            // TODO
            // string filename = Path.Combine(dir, this.LfsRepoFileName);
            // Assert.True(File.Exists(filename));

        }

        [Fact]
        public void RepositoryContainsCommit_ThrowsWhenDirDoesntExist()
        {
            string dir = DirectoryHelper.CreateTempDirectory(DirSuffix("contains-nodir"));
            string subDir = Path.Combine(dir, "sub");

            Assert.Throws<DirectoryNotFoundException>(() => sut.RepositoryContainsCommit(subDir, "foo"));
        }

        [Fact]
        public void RepositoryContainsCommit_ThrowsWhenDirIsNoRepo()
        {
            string dir = DirectoryHelper.CreateTempDirectory(DirSuffix("contains-norepo"));

            Assert.Throws<InvalidOperationException>(() => sut.RepositoryContainsCommit(dir, "foo"));
        }

        [Fact]
        public void RepositoryContainsCommit_ReturnsTrueWhenCommitExists()
        {
            string dir = DirectoryHelper.CreateTempDirectory(DirSuffix("contains-commit"));

            sut.PullFromRemote(this.PublicRepoUrl, dir);

            Assert.True(sut.RepositoryContainsCommit(dir, this.PublicRepoExistingCommitId));
        }

        [Fact]
        public void RepositoryContainsCommit_ReturnsFalseWhenCommitDoesntExist()
        {
            string dir = DirectoryHelper.CreateTempDirectory(DirSuffix("contains-nocommit"));

            sut.PullFromRemote(this.PublicRepoUrl, dir);

            Assert.False(sut.RepositoryContainsCommit(dir, this.PublicRepoNonExistingCommitId));
        }

        [Fact]
        public void RemoteRepositoryExists_ReturnsTrueForExistingRepo()
        {
            var result = sut.RemoteRepositoryExists(this.PublicRepoUrl);
            Assert.True(result);
        }

        [SkippableFact]
        public void RemoteRepositoryExists_ReturnsFalseForNonExistingRepo()
        {
            Skip.If(TestHelper.RunsOnAppVeyor(), "Doesn't finish on AppVeyor, see #15");

            var result = sut.RemoteRepositoryExists(this.NonExistingRepoUrl);
            Assert.False(result);
        }

        [Fact]
        public void RemoteRepositoryExists_ReturnsTrueForPrivateRepo()
        {
            var result = sut.RemoteRepositoryExists(this.PrivateRepoUrl, this.PrivateRepoCredentials);
            Assert.True(result);
        }


        /// <summary>
        /// helper for directory suffixes
        /// </summary>
        private string DirSuffix(string suffix)
        {
            return "iscm-" + this.sut.ShortName + "-" + suffix;
        }

        private bool SkipLfsTests()
        {
            return string.IsNullOrWhiteSpace(this.LfsRepoUrl) 
                || string.IsNullOrWhiteSpace(this.LfsRepoExistingCommitId)
                || string.IsNullOrWhiteSpace(this.LfsRepoFileName);
        }
    }
}
