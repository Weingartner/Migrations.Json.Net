REM If GitVersion is not available then run 
REM cinst GitVersion.Portable
REM to install it
GitVersion.exe /output buildserver  /exec scripts\lib\FAKE\tools\Fake.exe /execArgs "scripts\build.fsx %1" /l console
