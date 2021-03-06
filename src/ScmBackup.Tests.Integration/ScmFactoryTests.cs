using ScmBackup.CompositionRoot;
using ScmBackup.Scm;
using SimpleInjector;
using System;
using Xunit;

namespace ScmBackup.Tests.Integration
{
    public class ScmFactoryTests
    {
        private readonly ScmFactory sut;

        public ScmFactoryTests()
        {
            var container = new Container();
            container.Register<IFileSystemHelper, FileSystemHelper>();
            container.Register<IContext, FakeContext>();

            sut = new ScmFactory(container);
            sut.Register(typeof(GitScm));
        }

        [Fact]
        public void NewScmIsAdded()
        {
            Assert.Single(sut);
        }

        [Fact]
        public void CreateReturnScm()
        {
            var result = sut.Create(ScmType.Git);

            Assert.NotNull(result);
            Assert.True(result is IScm);
        }

        [Fact]
        public void RegisterThrowsIfRegisteredTypeIsNotIScm()
        {
            Assert.Throws<InvalidOperationException>(() => sut.Register(typeof(ScmBackup)));
        }
    }
}
