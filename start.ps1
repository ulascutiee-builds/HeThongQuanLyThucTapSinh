param(
    [int]$Port = 5135,
    [switch]$Source,
    [string]$SmtpHost,
    [int]$SmtpPort = 587,
    [string]$SmtpUsername,
    [string]$SmtpPassword,
    [string]$SmtpFrom,
    [switch]$SmtpNoSsl
)
$ErrorActionPreference = 'Stop'
$taskProject = Join-Path $PSScriptRoot 'backend\CareerPortal.Api\CareerPortal.Api.csproj'
$taskSecrets = Join-Path $PSScriptRoot '.local'
New-Item -ItemType Directory -Force -Path $taskSecrets | Out-Null
function Get-TaskPassword([string]$Name) {
    $taskSecretFile = Join-Path $taskSecrets ($Name + '-password.txt')
    if (-not (Test-Path -LiteralPath $taskSecretFile)) {
        $taskBytes = New-Object byte[] 24
        $taskRng = [Security.Cryptography.RandomNumberGenerator]::Create()
        $taskRng.GetBytes($taskBytes)
        $taskRng.Dispose()
        [IO.File]::WriteAllText($taskSecretFile, [Convert]::ToBase64String($taskBytes))
    }
    return [IO.File]::ReadAllText($taskSecretFile).Trim()
}
if (-not $env:Sprint1__HrEmail) { $env:Sprint1__HrEmail = 'hr@sprint1.local' }
if (-not $env:Accounts__AdminEmail) { $env:Accounts__AdminEmail = 'admin@sprint12.local' }
if (-not $env:Sprint1__HrPassword) { $env:Sprint1__HrPassword = Get-TaskPassword 'hr' }
if (-not $env:Accounts__AdminPassword) { $env:Accounts__AdminPassword = Get-TaskPassword 'admin' }
$env:ASPNETCORE_URLS = 'http://localhost:' + $Port
$env:PublicUrl = 'http://localhost:' + $Port
if (-not $env:ConnectionStrings__CareerPortal) {
    $env:ConnectionStrings__CareerPortal = 'Server=(localdb)\MSSQLLocalDB;Database=CareerPortalSprint12;Trusted_Connection=True;TrustServerCertificate=True'
}
if ($SmtpHost) { $env:Email__SmtpHost = $SmtpHost; $env:Email__Port = [string]$SmtpPort }
if ($SmtpUsername) { $env:Email__Username = $SmtpUsername }
if ($SmtpPassword) { $env:Email__Password = $SmtpPassword }
if ($SmtpFrom) { $env:Email__From = $SmtpFrom }
if ($SmtpNoSsl) { $env:Email__EnableSsl = 'false' }
Write-Host ('SPRINT 1 + 2 + 3 - Open ' + $env:PublicUrl)
Write-Host ('HR: ' + $env:Sprint1__HrEmail + ' / ' + $env:Sprint1__HrPassword)
Write-Host ('Admin: ' + $env:Accounts__AdminEmail + ' / ' + $env:Accounts__AdminPassword)
Write-Host 'Passwords are generated on this computer; do not share .local or App_Data.'
Write-Host 'The application applies migrations automatically without deleting existing data.'
$taskPublishedRoot = Join-Path $PSScriptRoot 'build-sprint3-email'
$taskPublished = Join-Path $taskPublishedRoot 'CareerPortal.Api.exe'
$taskEmailPort = $env:Email__Port
if (-not $taskEmailPort) { $taskEmailPort = '587' }
if ($env:Email__SmtpHost) { Write-Host ('Email SMTP: ' + $env:Email__SmtpHost + ':' + $taskEmailPort) }
else { Write-Host ('Email pickup folder (when SMTP is not configured): ' + (Join-Path $taskPublishedRoot 'App_Data\mail')) }
if ((Test-Path -LiteralPath $taskPublished) -and -not $Source) {
    Push-Location $taskPublishedRoot
    try { & $taskPublished } finally { Pop-Location }
} else {
    dotnet restore $taskProject --configfile (Join-Path $PSScriptRoot 'NuGet.Config')
    if ($LASTEXITCODE -ne 0) { throw 'NuGet restore failed. Check .NET 10 SDK and network access.' }
    dotnet run --project $taskProject --no-restore --no-launch-profile
}
if ($LASTEXITCODE -ne 0) { throw 'Application failed. Check SQL Server LocalDB and terminal error.' }
