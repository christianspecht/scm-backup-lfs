﻿using ScmBackup.Scm;
using ScmBackup.Tests.Hosters;
using System;

namespace ScmBackup.Tests.Integration.Scm
{
    public class GitScmTests : IScmTests
    {
        public GitScmTests()
        {
            this.sut = new GitScm(new FileSystemHelper(), new FakeContext());
        }

        internal override string PublicRepoUrl
        {
            get
            {
                string url = CloneUrlBuilder.GithubCloneUrl("scm-backup-testuser", "scm-backup");
                return url;
            }
        }

        internal override string PrivateRepoUrl
        {
            get { return CloneUrlBuilder.BitbucketCloneUrl(TestHelper.EnvVar("Bitbucket_Name"), TestHelper.EnvVar("Bitbucket_RepoPrivateGit")); }
        }

        internal override ScmCredentials PrivateRepoCredentials
        {
            get { return new ScmCredentials(TestHelper.EnvVar("Bitbucket_Name"), TestHelper.EnvVar("Bitbucket_PW")); }
        }

        internal override string NonExistingRepoUrl
        {
            get { return CloneUrlBuilder.GithubCloneUrl("scm-backup-testuser", "repo-does-not-exist"); }
        }

        internal override string PublicRepoExistingCommitId
        {
            get
            {
                return "7be29139f4cdc4037647fc2f21d9d82c42a96e88";
            }
        }

        internal override string PublicRepoNonExistingCommitId
        {
            get { return "00000"; }
        }

        internal override string LfsRepoUrl
        {
            get { return CloneUrlBuilder.GithubCloneUrl("scm-backup-testuser", "git-lfs-files"); }
        }

        internal override string LfsRepoExistingCommitId
        {
            get { return "6f566cf847dee2dd63adcd37df14127a47e9d94d"; }
        }

        internal override string LfsRepoFileName
        {
            get { return "test1.lfs"; }
        }
    }
}
