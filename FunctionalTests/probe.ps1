param([string]$sql)
# probe.ps1 -sql <sqlfile>   runs a query via DbProbe and prints output (UTF-8)
$dn = Join-Path $HOME ".dotnet10\dotnet.exe"
$dll = "d:\ERPSystem\FunctionalTests\DbProbe\bin\Release\net10.0\DbProbe.dll"
if (-not $sql) { "usage: probe.ps1 -sql <file>"; exit }

# ensure conn targets the functional-test DB
$env:DBPROBE_CONN = "Server=(localdb)\mssqllocaldb;Database=ERPSystemDb_FuncTest;Trusted_Connection=True;TrustServerCertificate=True"
$out = "d:\probe_out.txt"
& $dn $dll $sql *> $out 2>&1 | Out-Null
Get-Content $out -Encoding UTF8 | Select-Object -First 200