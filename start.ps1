$ErrorActionPreference = 'Stop'
$taskProject = Join-Path $PSScriptRoot 'backend\CareerPortal.Api\CareerPortal.Api.csproj'
$taskData = Join-Path $PSScriptRoot 'backend\CareerPortal.Api\App_Data'
New-Item -ItemType Directory -Force -Path $taskData | Out-Null
$taskSecretFile = Join-Path $taskData 'hr-password.txt'
if (-not (Test-Path -LiteralPath $taskSecretFile)) {
    $taskBytes = New-Object byte[] 24
    $taskRng = [Security.Cryptography.RandomNumberGenerator]::Create()
    $taskRng.GetBytes($taskBytes)
    $taskRng.Dispose()
    [IO.File]::WriteAllText($taskSecretFile, [Convert]::ToBase64String($taskBytes))
}
$env:Sprint1__HrEmail = 'hr@sprint1.local'
$env:Sprint1__HrPassword = [IO.File]::ReadAllText($taskSecretFile).Trim()
$env:ASPNETCORE_URLS = 'http://localhost:5135'
$env:PublicUrl = 'http://localhost:5135'
$env:ConnectionStrings__CareerPortal = 'Server=(localdb)\MSSQLLocalDB;Database=CareerPortalSprint1;Trusted_Connection=True;TrustServerCertificate=True'
Write-Host 'SPRINT 1 ONLY - Open http://localhost:5135'
Write-Host "HR email: $env:Sprint1__HrEmail"
Write-Host "HR password (keep private): $env:Sprint1__HrPassword"
Write-Host 'Demo emails are .eml files in backend\CareerPortal.Api\App_Data\mail.'
dotnet restore $taskProject
if ($LASTEXITCODE -ne 0) { throw 'NuGet restore failed. Check your internet connection and .NET 10 SDK.' }
dotnet ef database update --project $taskProject
if ($LASTEXITCODE -ne 0) { throw 'Migration failed. Check dotnet-ef 10 and SQL Server LocalDB. Do not delete your existing database.' }
dotnet run --project $taskProject --no-launch-profile
