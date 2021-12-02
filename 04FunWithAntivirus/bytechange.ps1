$bytes = [System.IO.File]::ReadAllBytes("C:\nonosquare\met.exe")
$bytes[18867] = 0
$bytes[18987] = 0
$bytes[73801] = 0xFF
[System.IO.File]::WriteAllBytes("C:\nonosquare\met_mod.exe", $bytes)