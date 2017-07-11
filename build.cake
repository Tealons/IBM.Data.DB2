#addin "Cake.Incubator"

var destination = Argument("destination", "F:\\publish");
///var target = Argument("target", "Compile");
var target = Argument("target", "Default");
var x86destination = Argument("destination", new DirectoryPath(destination).Combine("x86"));
var x64destination = Argument("destination", new DirectoryPath(destination).Combine("x64"));
var anycpudestination = Argument("destination", new DirectoryPath(destination).Combine("anycpu"));

Task("Default")
  .Does(() =>
{
    RunTarget("CleanDirectory");
    RunTarget("CreateSubFolders");
    RunTarget("Compile");
    RunTarget("RemovePDB");
});

Task("CreateSubFolders")
.Does(()=>{
    CreateDirectory(x86destination); 
    CreateDirectory(x64destination); 
});

Task("Compile")
  .Does(() =>
{
      RunTarget("Compilex86");
      RunTarget("Compilex64");
      RunTarget("CompilexAnyCPU");
});

Task("CompilexAnyCPU")
  .Does(() =>
{
  Information("Compiling");
  MSBuild("./IBM.Data.DB2/IBM.Data.DB2.csproj", new MSBuildSettings()
  .WithProperty("OutDir", anycpudestination.ToString())
  .WithProperty("DeployOnBuild", "true")
  .WithProperty("Configuration", "Release")
  .WithProperty("PackageAsSingleFile", "true")
  .WithProperty("Verbosity", "Verbosity.Minimal")
  .WithProperty("ToolVersion", "MSBuildToolVersion.VS2015")
   .SetPlatformTarget(PlatformTarget.x86)  
  .WithProperty("SkipInvalidConfigurations", "true"));
});

Task("Compilex86")
  .Does(() =>
{
  Information("Compiling");
  MSBuild("./IBM.Data.DB2/IBM.Data.DB2.csproj", new MSBuildSettings()
  .WithProperty("OutDir", x86destination.ToString())
  .WithProperty("DeployOnBuild", "true")
  .WithProperty("Configuration", "Release")
  .WithProperty("PackageAsSingleFile", "true")
  .WithProperty("Verbosity", "Verbosity.Minimal")
  .WithProperty("ToolVersion", "MSBuildToolVersion.VS2015")
   .SetPlatformTarget(PlatformTarget.x86)  
  .WithProperty("SkipInvalidConfigurations", "true"));
});

Task("Compilex64")
  .Does(() =>
{
  Information("Compiling");
  MSBuild("./IBM.Data.DB2/IBM.Data.DB2.csproj", new MSBuildSettings()
  .WithProperty("OutDir", x64destination.ToString())
  .WithProperty("DeployOnBuild", "true")
  .WithProperty("Configuration", "Release")
  .WithProperty("PackageAsSingleFile", "true")
  .WithProperty("Verbosity", "Verbosity.Minimal")
  .WithProperty("ToolVersion", "MSBuildToolVersion.VS2015")
  .SetPlatformTarget(PlatformTarget.x64)  
  .WithProperty("SkipInvalidConfigurations", "true"));
});

Task("RemovePDB")
    .Description("Removing all but dll files from" + destination)
    .Does(() =>
{        
    var files = GetFiles(destination + "\\*.pdb");
    DeleteFiles(files);
});

Task("CleanDirectory")
    .Description("cleaning directory " + destination)
    .Does(() =>
{        
    CleanDirectory(destination);
});


RunTarget(target);