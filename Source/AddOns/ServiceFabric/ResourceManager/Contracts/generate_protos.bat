
set errorlevel=

cd /d %~dp0
IF NOT EXIST Codegen MKDIR Codegen

set PROTOC=..\..\..\..\..\packages\Google.Protobuf.Tools\3.6.0\tools\windows_x64\protoc.exe
set PLUGIN=..\..\..\..\..\packages\Grpc.Tools\1.13.0\tools\windows_x64\grpc_csharp_plugin.exe

%PROTOC% -I %~dp0 --csharp_out Codegen ResourceManager.proto --grpc_out Codegen --plugin=protoc-gen-grpc=%PLUGIN%
exit /b %errorlevel%
