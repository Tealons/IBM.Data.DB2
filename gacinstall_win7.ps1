Param (     
    [switch] $info,
    [switch] $install,   
    [switch] $uninstall,
    [string] $name  
)

$gacUtil = "${Env:ProgramFiles(x86)}\Microsoft SDKs\Windows\v7.0A\Bin\NETFX 4.0 Tools\gacutil.exe";
$dll = "IBM.Data.DB2,Version=1.0.0.1,Culture=neutral,PublicKeyToken=e6f42553a3895695";
function Add-GacItem([string]$path) {

    #Full Path Name or Relative - ex: C:\Temp\Larned.dll
    & $gacutil "/nologo" "/i" "$path"
}

function Remove-GacItem([string]$name) {
    
    #Assembly Name - ex: if Dll was Larned.dll then  Larned
    & $gacutil "/nologo" "/u" "$name"
}

function Search-GacItem([string]$name) {
    
    #Assembly Name - ex: if Dll was Larned.dll then  Larned
    & $gacutil "/nologo" "/l" "$name"
}
if($info)
{
    Search-GacItem $name    
}
elseif ($install) {
    Add-GacItem $name
}    
elseif ($uninstall) {
    Remove-GacItem $dll
}     

#.\gacutil.exe /u IBM.Data.DB2,Version=1.0.0.0,Culture=neutral,PublicKeyToken=e6f42553a3895695