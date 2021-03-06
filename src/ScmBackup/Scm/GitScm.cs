using System;
using System.IO;

namespace ScmBackup.Scm
{
    [Scm(Type = ScmType.Git)]
    internal class GitScm : CommandLineScm, IScm
    {
        public GitScm(IFileSystemHelper filesystemhelper, IContext context)
        {
            this.FileSystemHelper = filesystemhelper;
            this.context = context;
        }

        public IFileSystemHelper FileSystemHelper { get; set; }

        public override string ShortName
        {
            get { return "git"; }
        }

        public override string DisplayName
        {
            get { return "Git"; }
        }

        protected override string CommandName
        {
            get { return "git"; }
        }

        public override bool IsOnThisComputer()
        {
            var result = this.ExecuteCommand("--version");

            if (result.Successful && result.StandardOutput.ToLower().Contains("git version"))
            {
                return true;
            }

            return false;
        }

        public override string GetVersionNumber()
        {
            var result = this.ExecuteCommand("--version");

            if (result.Successful)
            {
                const string search = "git version ";
                return result.StandardOutput.Substring(result.StandardOutput.IndexOf(search) + search.Length).Replace("\n", "");
            }

            throw new InvalidOperationException(result.Output);
        }

        public override bool LFSIsOnThisComputer()
        {
            var result = this.ExecuteCommand("lfs version");

            if (result.Successful)
            {
                return true; //git lfs found: run lfs commands
            }
            else
            {
                return false; //git lfs not found: do not run lfs commands
            }
        }

        public override bool DirectoryIsRepository(string directory)
        {
            // SCM Backup uses bare repos only, so we don't need to check for non-bare repos at all
            string cmd = string.Format("-C \"{0}\" rev-parse --is-bare-repository", directory);
            var result = this.ExecuteCommand(cmd);

            if (result.Successful && result.StandardOutput.ToLower().StartsWith("true"))
            {
                return true;
            }

            return false;
        }

        public override void CreateRepository(string directory)
        {
            if (!this.DirectoryIsRepository(directory))
            {
                string cmd = string.Format("init --bare \"{0}\"", directory);
                var result = this.ExecuteCommand(cmd);

                if (!result.Successful)
                {
                    throw new InvalidOperationException(result.Output);
                }
            }
        }

        public override bool RemoteRepositoryExists(string remoteUrl, ScmCredentials credentials)
        {
            if (credentials != null)
            {
                remoteUrl = this.CreateRepoUrlWithCredentials(remoteUrl, credentials);
            }

            string cmd = "ls-remote " + remoteUrl;
            var result = this.ExecuteCommand(cmd);

            return result.Successful;
        }

        public override void PullFromRemote(string remoteUrl, string directory, ScmCredentials credentials)
        {
            if (!this.DirectoryIsRepository(directory))
            {
                if (Directory.Exists(directory) && !this.FileSystemHelper.DirectoryIsEmpty(directory))
                {
                    throw new InvalidOperationException(string.Format(Resource.ScmTargetDirectoryNotEmpty, directory));
                }

                this.CreateRepository(directory);
            }

            if (credentials != null)
            {
                remoteUrl = this.CreateRepoUrlWithCredentials(remoteUrl, credentials);
            }

            string cmd = string.Format("-C \"{0}\" fetch --force --prune {1} refs/heads/*:refs/heads/* refs/tags/*:refs/tags/*", directory, remoteUrl);
            var result = this.ExecuteCommand(cmd);

            if (!result.Successful)
            {
                throw new InvalidOperationException(result.Output);
            }

            if (LFSIsOnThisComputer())
            {
                if (RepositoryContainsLFS(directory))
                {
                    PullLFSFromRemote(remoteUrl, directory, credentials);
                }
            }
        }

        public override void PullLFSFromRemote(string remoteUrl, string directory, ScmCredentials credentials)
        {
            if (credentials != null)
            {
                remoteUrl = this.CreateRepoUrlWithCredentials(remoteUrl, credentials);
            }

            string cmd = string.Format("-C \"{0}\" lfs fetch --all {1}", directory, remoteUrl); // git -C *DIR* lfs fetch --all *REMOTE*
            var result = this.ExecuteCommand(cmd);

            if (!result.Successful)
            {
                throw new InvalidOperationException(result.Output);
            }
        }

        public override bool RepositoryContainsLFS(string directory) //test if repo contains lfs files
        {
            //do not run if LFSIsOnThisComputer = false
            string cmd = string.Format("-C \"{0}\" lfs ls-files", directory); 
            var result = this.ExecuteCommand(cmd);

            if (!result.Successful)
            {
                throw new InvalidOperationException(result.Output);
            }

            if (String.IsNullOrWhiteSpace(result.Output))
            {
                return false; //no lfs files found, continuing
            }
            else
            {
                return true; //lfs files found, backing them up
            }
        }

        public override bool RepositoryContainsCommit(string directory, string commitid)
        {
            if (!Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException(string.Format(Resource.DirectoryDoesntExist, directory));
            }

            if (!this.DirectoryIsRepository(directory))
            {
                throw new InvalidOperationException(string.Format(Resource.DirectoryNoRepo, directory));
            }

            // https://stackoverflow.com/a/21878920/6884
            string cmd = string.Format("-C \"{0}\" rev-parse --quiet --verify {1}^{{commit}}", directory, commitid);
            var result = this.ExecuteCommand(cmd);

            if (result.Successful && result.Output.StartsWith(commitid))
            {
                return true;
            }

            return false;
        }

        public string CreateRepoUrlWithCredentials(string url, ScmCredentials credentials)
        {
            // https://stackoverflow.com/a/10054470/6884
            var uri = new UriBuilder(url);
            uri.UserName = credentials.User;
            uri.Password = credentials.Password;
            return uri.ToString();
        }

        public override bool BackupContainsLFSFile(string directory, string path)
        {
            string subDir = "_scm-backup-gitscm";

            // Source: https://github.com/christianspecht/scm-backup/pull/62#issuecomment-834752336 (bottom)

            // 1. create another bare repo locally
            string tmpRepo = this.FileSystemHelper.CreateTempDirectory(subDir, "tmprepo");
            this.CreateRepository(tmpRepo);

            // 2. push from the original backup to this
            string cmd = string.Format("-C \"{0}\" push --mirror \"{1}\"", directory, tmpRepo);
            var result = this.ExecuteCommand(cmd);

            // 3. push LFS files
            cmd = string.Format("-C \"{0}\" lfs push --all \"file:///{1}\"", directory, tmpRepo);
            result = this.ExecuteCommand(cmd);

            // 4. clone again, the local clone contains all LFS files
            string finalRepo = this.FileSystemHelper.CreateTempDirectory(subDir, "finalrepo");
            cmd = string.Format("clone \"{0}\" \"{1}\"", tmpRepo, finalRepo);

            // note: at the moment, it fails here --> output result?
            result = this.ExecuteCommand(cmd);

            string filename = Path.Combine(finalRepo, path);
            return File.Exists(filename);
        }
    }
}
