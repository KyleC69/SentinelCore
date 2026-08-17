# Fix TraceLogPage.xaml command binding
$f = 'F:\Solutions\SentinelCore\SentinelCore\projects\SentinelCoreAdmin\Views\TraceLogPage.xaml'
$c = [System.IO.File]::ReadAllText($f)
$c = $c.Replace('RefreshLogAsyncCommand', 'RefreshLogCommand')
[System.IO.File]::WriteAllText($f, $c, (New-Object System.Text.UTF8Encoding $false))
Write-Host 'TraceLogPage.xaml fixed'