# AC-15 verification -- response time under 1 second at 10k-cabin catalogue scale
# Run from the repo root after:
#   1. seed-perf-10k.sql has been executed in Supabase SQL Editor
#   2. dotnet run is running in src/backend/CabinConnect.Api

$url  = "http://localhost:5268/api/cabins/search?checkIn=2026-09-01&checkOut=2026-09-08"
$runs = 5

Write-Host ""
Write-Host "AC-15 -- GET $url"
Write-Host "Sending $runs requests..."
Write-Host ""

$times = @()

for ($i = 1; $i -le $runs; $i++) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 10
        $sw.Stop()
        $ms = $sw.ElapsedMilliseconds
        $times += $ms
        $total = ($response.Content | ConvertFrom-Json).data.totalCount
        $status = $response.StatusCode
        Write-Host "  Run $i`: $ms ms  HTTP $status  totalCount=$total"
    } catch {
        $sw.Stop()
        $elapsed = $sw.ElapsedMilliseconds
        $err = $_.Exception.Message
        Write-Host "  Run $i`: FAILED after $elapsed ms -- $err"
    }
}

if ($times.Count -gt 0) {
    $avg = [math]::Round(($times | Measure-Object -Average).Average)
    $max = ($times | Measure-Object -Maximum).Maximum
    $min = ($times | Measure-Object -Minimum).Minimum

    Write-Host ""
    Write-Host "Results:"
    Write-Host "  Min : $min ms"
    Write-Host "  Max : $max ms"
    Write-Host "  Avg : $avg ms"
    Write-Host ""

    if ($max -lt 1000) {
        Write-Host "  PASS -- all runs under 1000 ms" -ForegroundColor Green
    } else {
        Write-Host "  FAIL -- one or more runs exceeded 1000 ms" -ForegroundColor Red
    }
}
