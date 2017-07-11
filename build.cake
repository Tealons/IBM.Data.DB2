#addin "Cake.Incubator"

var destination = Argument("destination", "F:\\publish");

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
    //RunTarget("RemovePDB");
});

Task("CreateSubFolders")
.Does(()=>{
    CreateDirectory(x86destination); 
    CreateDirectory(x64destination); 
});

Task("Compile")
  .Does(() =>
{
      RunTarget("AssemblyUpdate");
      RunTarget("Compilex86");
      RunTarget("Compilex64");
      RunTarget("CompilexAnyCPU");

      RunTarget("RemovePDB64");
      RunTarget("RemovePDB86");
      RunTarget("RemovePDBanycpu");
      RunTarget("CopyInstallers");
});
Task("CopyInstallers")
.Does(()=>
{
   CopyFile("././gacinstall_win7.ps1",new DirectoryPath(destination).CombineWithFilePath("gacinstall_win7.ps1"));
   CopyFile("././gacinstall_win10.ps1",new DirectoryPath(destination).CombineWithFilePath("gacinstall_win10.ps1"));
   CopyFile("././install_win7.bat",new DirectoryPath(destination).CombineWithFilePath("install_win7.bat"));
   CopyFile("././install_win10.bat",new DirectoryPath(destination).CombineWithFilePath("install_win10.bat"));
});


Task("AssemblyUpdate")
.Does(()=>{
var file = "./IBM.Data.DB2/Properties/AssemblyInfo.cs";
var assemblyInfo = ParseAssemblyInfo(file);
var data = int.Parse(assemblyInfo.AssemblyVersion.Skip(6).Take(1).First().ToString());
data++;
var version = string.Format("1.0.0.{0}", data);
Information("Version: {0}", version);
Information("File version: {0}", version);

CreateAssemblyInfo(file, new AssemblyInfoSettings {
    Product = "IBM.Data.DB2",
    Version = assemblyInfo.AssemblyVersion,
    FileVersion = version,    
    Copyright = "Copyright ©  2017"});
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
  .WithProperty("SignAssembly", "true")  
  .WithProperty("AssemblyOriginatorKeyFile", "key.snk")
   .SetPlatformTarget(PlatformTarget.MSIL)  
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
    .WithProperty("SignAssembly", "true")  
  .WithProperty("AssemblyOriginatorKeyFile", "key.snk")
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
    .WithProperty("SignAssembly", "true")  
  .WithProperty("AssemblyOriginatorKeyFile", "key.snk")
  .SetPlatformTarget(PlatformTarget.x64)  
  .WithProperty("SkipInvalidConfigurations", "true"));
});

Task("RemovePDB64")
    .Description("Removing all but dll files from" + x64destination)
    .Does(() =>
{        
    var files = GetFiles(x64destination + "\\*.pdb");
    DeleteFiles(files);
});
Task("RemovePDB86")
    .Description("Removing all but dll files from" + x86destination)
    .Does(() =>
{        
    var files = GetFiles(x86destination + "\\*.pdb");
    DeleteFiles(files);
});
Task("RemovePDBanycpu")
    .Description("Removing all but dll files from" + anycpudestination)
    .Does(() =>
{        
    var files = GetFiles(anycpudestination + "\\*.pdb");
    DeleteFiles(files);
});

Task("CleanDirectory")
    .Description("cleaning directory " + destination)
    .Does(() =>
{        
    CleanDirectory(destination);
});


RunTarget(target);