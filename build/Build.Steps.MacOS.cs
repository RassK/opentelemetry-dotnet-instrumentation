using Nuke.Common;
using Nuke.Common.IO;
using Serilog;
using static Nuke.Common.EnvironmentInfo;

partial class Build
{
    Target CompileNativeSrcMacOs => _ => _
        .Unlisted()
        .After(CreateRequiredDirectories)
        .OnlyWhenStatic(() => IsOsx)
        .Executes(() =>
        {
            var nativeProjectDirectory = NativeProfilerProject.Directory;

            var (major, minor, patch) = VersionHelper.GetVersionParts();

            CMake.Value(
                arguments: $". -DOTEL_AUTO_VERSION={VersionHelper.GetVersionWithoutSuffixes()} -DOTEL_AUTO_VERSION_MAJOR={major} -DOTEL_AUTO_VERSION_MINOR={minor} -DOTEL_AUTO_VERSION_PATCH={patch}",
                workingDirectory: nativeProjectDirectory);
            Make.Value(
                arguments: $" ", //space is needed - see https://github.com/nuke-build/nuke/issues/1417
                workingDirectory: nativeProjectDirectory);
        });

    Target CompileNativeDependenciesForManagedTestsMacOs => _ => _
        .Unlisted()
        .After(CreateRequiredDirectories)
        .OnlyWhenStatic(() => IsOsx)
        .Executes(() =>
        {
            var buildDirectory = Solution.GetContinuousProfilerNativeDep().Directory.ToString();
            CMake.Value(
                arguments: "-S .",
                workingDirectory: buildDirectory);
            Make.Value(
                arguments: $" ", //space is needed - see https://github.com/nuke-build/nuke/issues/1417
                workingDirectory: buildDirectory);
        });

    Target PublishNativeProfilerMacOs => _ => _
        .Unlisted()
        .OnlyWhenStatic(() => IsOsx)
        .After(CompileNativeSrc, PublishManagedProfiler)
        .Executes(() =>
        {
            // Create home directory
            var source = NativeProfilerProject.Directory / "bin" / $"{NativeProfilerProject.Name}.dylib";
            var dest = TracerHomeDirectory / "osx-x64";
            Log.Information($"Copying '{source}' to '{dest}'");

            source.CopyToDirectory(dest, ExistsPolicy.FileOverwrite);
        });
}
