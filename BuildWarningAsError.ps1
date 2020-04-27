# When there are only warnings, the exit code is 0.
# See https://github.com/dotnet/installer/issues/1708.

dotnet build /warnaserror *> build.txt
Get-Content build.txt
$noError = Select-String -Path build.txt -pattern "0 Error\(s\)"
Exit $noError -eq $null
