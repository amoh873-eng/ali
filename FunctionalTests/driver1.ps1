$ErrorActionPreference = 'Continue'
$log = "d:\driver1_out.txt"
"START $(Get-Date -Format o)" | Out-File $log -Encoding utf8

# 1) build web
$dl = Join-Path $HOME ".dotnet10\dotnet.exe"
& $dl build d:\ERPSystem\src\Web\ERPSystem.Web.csproj -c Debug --nologo -v q 2>&1 | Out-Null
"BUILD_DONE exit=$LASTEXITCODE" | Out-File $log -Append -Encoding utf8

# 2) stop any listener on 5186
$conn = Get-NetTCPConnection -LocalPort 5186 -State Listen -ErrorAction SilentlyContinue
if ($conn) { $conn.OwningProcess | Sort-Object -Unique | ForEach-Object { Stop-Process -Id $_ -Force -ErrorAction SilentlyContinue } }
Start-Sleep -Seconds 2

# 3) start app on fresh DB
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__DefaultConnection = 'Server=(localdb)\mssqllocaldb;Database=ERPSystemDb_FuncTest;Trusted_Connection=True;TrustServerCertificate=True'
$p = Start-Process -FilePath $dl -ArgumentList 'run','--project','d:\ERPSystem\src\Web\ERPSystem.Web.csproj','--no-build','--launch-profile','http' `
    -WorkingDirectory 'd:\ERPSystem\src\Web' -RedirectStandardOutput 'd:\driver1_run.log' -RedirectStandardError 'd:\driver1_err.log' -PassThru -WindowStyle Hidden
"APP_PID=$($p.Id)" | Out-File $log -Append -Encoding utf8
# wait for listening
for ($i=0; $i -lt 40; $i++) {
    Start-Sleep -Seconds 2
    try { $r = Invoke-WebRequest -Uri 'http://localhost:5186/' -UseBasicParsing -TimeoutSec 5; if ($r.StatusCode -eq 200) { break } } catch {}
}
"APP_HTTP_STATUS=$($r.StatusCode)" | Out-File $log -Append -Encoding utf8

# 4) A5 checks
$enter = curl.exe -s -i -m 15 'http://localhost:5186/x-vendor-9f3a1c/enter'
($enter | Select-Object -First 1) | Out-File $log -Append -Encoding utf8
# follow with cookie
curl.exe -s -L -c d:\dc.txt -b d:\dc.txt -m 15 'http://localhost:5186/x-vendor-9f3a1c/enter' -o d:\enter_follow.html -w "ENTER_FOLLOW_HTTP=%{http_code}" | Out-File $log -Append -Encoding utf8
# bad cred handler
$bad = curl.exe -s -i -m 15 -X POST '--data-urlencode' 'Username=hacker' '--data-urlencode' 'Password=wrongxyz' 'http://localhost:5186/x-vendor-9f3a1c/login/handler'
($bad | Select-Object -First 1) | Out-File $log -Append -Encoding utf8
# direct / no cookie
$root = curl.exe -s -i -m 15 'http://localhost:5186/x-vendor-9f3a1c/'
($root | Select-Object -First 1) | Out-File $log -Append -Encoding utf8

"DONE" | Out-File $log -Append -Encoding utf8