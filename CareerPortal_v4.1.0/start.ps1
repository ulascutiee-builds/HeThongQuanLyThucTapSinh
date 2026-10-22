param(
    [int]$Port = 5135,
    [switch]$Source,
    [string]$SmtpHost,
    [int]$SmtpPort = 587,
    [string]$SmtpUsername,
    [string]$SmtpPassword,
    [string]$SmtpFrom,
    [switch]$SmtpNoSsl,
    [switch]$RepairDemoAccounts,
    [switch]$SeedDemoData
)
$ErrorActionPreference = 'Stop'
$taskProject = Join-Path $PSScriptRoot 'backend\CareerPortal.Api\CareerPortal.Api.csproj'
$taskSecrets = Join-Path $PSScriptRoot '.local'
New-Item -ItemType Directory -Force -Path (Join-Path $taskSecrets 'runtime\keys'), (Join-Path $taskSecrets 'runtime\mail') | Out-Null
$env:Runtime__DataDirectory = Join-Path $taskSecrets 'runtime'
$env:Sprint1__HrEmail = 'hr@abcxyz.com'
$env:Sprint1__HrPassword = '0939232002'
$env:Accounts__AdminEmail = 'admin@abcxyz.com'
$env:Accounts__AdminPassword = '0939232002'
$env:Accounts__MentorEmail = 'mentor@abcxyz.com'
$env:Accounts__MentorPassword = '0939232002'
$env:Accounts__UserEmail = 'user@abcxyz.com'
$env:Accounts__UserPassword = '0939232002'
$env:Accounts__RepairDemoAccounts = [string]$RepairDemoAccounts
$env:DemoData__Seed = [string]$SeedDemoData
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
if ($SmtpHost -and -not $env:Email__Provider) { $env:Email__Provider = 'Smtp' }
if ($env:Email__Provider -and $env:Email__Provider -notin @('Resend', 'Smtp', 'Pickup')) {
    throw 'Email__Provider must be Resend, Smtp or Pickup.'
}
if ($env:Email__Provider -eq 'Resend') {
    if (-not $env:Email__ResendApiKey -or -not $env:Email__From) {
        throw 'Resend needs Email__ResendApiKey and Email__From (sender on a verified domain). Set both in this PowerShell session before running.'
    }
}
Write-Host ('CAREER PORTAL v4.1.0 - Open ' + $env:PublicUrl + '/dang-nhap.html?v=20261012-brand-refresh')
Write-Host ('HR: ' + $env:Sprint1__HrEmail + ' / ' + $env:Sprint1__HrPassword)
Write-Host ('Admin: ' + $env:Accounts__AdminEmail + ' / ' + $env:Accounts__AdminPassword)
Write-Host ('Mentor: ' + $env:Accounts__MentorEmail + ' / ' + $env:Accounts__MentorPassword)
Write-Host ('User: ' + $env:Accounts__UserEmail + ' / ' + $env:Accounts__UserPassword)
Write-Host 'All four demo accounts use the same password. Keep this terminal private.'
Write-Host 'The application applies migrations automatically without deleting existing data.'
if ($SeedDemoData) { Write-Warning 'NAP DU LIEU GIA LAP: chỉ sử dụng trên cơ sở dữ liệu thử nghiệm, không dùng trên môi trường thật.' }
if ($RepairDemoAccounts) {
    Write-Warning 'Repairing demo accounts: configured demo passwords will be reset and missing demo accounts created. Other accounts and database data are preserved.'
}
$taskPublishedRoot = Join-Path $PSScriptRoot 'build-v4.1'
$taskPublished = Join-Path $taskPublishedRoot 'CareerPortal.Api.exe'
$taskEmailPort = $env:Email__Port
if (-not $taskEmailPort) { $taskEmailPort = '587' }
if ($env:Email__Provider -eq 'Resend') { Write-Host 'Email provider: Resend (API key hidden)' }
elseif ($env:Email__Provider -eq 'Pickup') { Write-Host ('Email pickup folder: ' + (Join-Path $taskSecrets 'runtime\mail')) }
elseif ($env:Email__SmtpHost) { Write-Host ('Email SMTP: ' + $env:Email__SmtpHost + ':' + $taskEmailPort) }
else { Write-Host ('Email pickup folder (when a provider is not configured): ' + (Join-Path $taskSecrets 'runtime\mail')) }
if ((Test-Path -LiteralPath $taskPublished) -and -not $Source -and -not $RepairDemoAccounts -and -not $SeedDemoData) {
    Push-Location $taskPublishedRoot
    try { & $taskPublished } finally { Pop-Location }
} else {
    dotnet restore $taskProject --configfile (Join-Path $PSScriptRoot 'NuGet.Config')
    if ($LASTEXITCODE -ne 0) { throw 'NuGet restore failed. Check .NET 10 SDK and network access.' }
    dotnet run --project $taskProject --no-restore --no-launch-profile
}
if ($LASTEXITCODE -ne 0) { throw 'Application failed. Check SQL Server LocalDB and terminal error.' }
