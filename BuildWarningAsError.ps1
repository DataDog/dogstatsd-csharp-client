# When there are only warnings, the exit code is 0.
# See https://github.com/dotnet/installer/issues/1708.

# Ignore warning NETSDK1138: The target framework 'netcoreapp2.0' is out of support and will not receive security updates in the future
dotnet build -warnaserror -warnAsMessage:NETSDK1138 *> build.txt
Get-Content build.txt
$noError = Select-String -Path build.txt -pattern "0 Error\(s\)"
Exit $noError -eq $null
