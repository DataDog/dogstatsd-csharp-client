# When there are only warnings, the exit code is 0.
# See https://github.com/dotnet/installer/issues/1708.

# Ignore warning NETSDK1138: The target framework 'netcoreapp2.1' is out of support and will not receive security updates in the future
# Ignore warning NU1902: The dependency Microsoft.NETCore.App 2.1.34 has a known high severity vulnerability (CVE-2021-43877) that we will ignore
# Ignore warning NU1903: The dependency Microsoft.NETCore.App 2.1.34 has a known high severity vulnerability (CVE-2021-43877) that we will ignore
dotnet build -warnaserror -warnAsMessage:NETSDK1138,NU1902,NU1903 *> build.txt
Get-Content build.txt
$noError = Select-String -Path build.txt -pattern "0 Error\(s\)"
Exit $noError -eq $null
