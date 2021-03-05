#Use this command to run the script
#&("D:\dev\MSTC\deploymentScripts\beforeBuild.ps1")

Write-Host 'Running before build script'

#$root = 'D:\Temp'
#$root = 'D:\dev\MidSussexTriathlon\build\Mstc\MSTC.Web'
$root =  $env:APPVEYOR_BUILD_FOLDER + '\build\Mstc\MSTC.Web'
Write-Host $root

$configPath = $root + '\Web.config'
$webconfig = get-content $configPath
#Write-Host $webconfig

function ReplaceAppSetting($sourceFile, $keyName, $replacementValue) {
    $sourceFile -replace "<add key=`"$keyName`" value=`".*`"","<add key=`"$keyName`" value=`"$replacementValue`""
}

$webconfig = $webconfig -replace "connectionString=`"([^`"]+)`"","connectionString=`"$env:databaseConnectionString`""
$webconfig = ReplaceAppSetting $webconfig "gmailUserName" $env:gmailUserName
$webconfig = ReplaceAppSetting $webconfig "gmailPassword" $env:gmailPassword
$webconfig = ReplaceAppSetting $webconfig "gocardlessEnvironment" $env:gocardlessEnvironment
$webconfig = ReplaceAppSetting $webconfig "gocardlessAccessToken" $env:gocardlessAccessToken
$webconfig = ReplaceAppSetting $webconfig "environment" $env:gocardlessEnvironment
$webconfig = ReplaceAppSetting $webconfig "Umbraco.ModelsBuilder.Enable" $env:Umbraco.ModelsBuilder.Enable

[System.IO.File]::WriteAllLines($configPath, $webconfig)

Write-Host 'Finished before packaging script'