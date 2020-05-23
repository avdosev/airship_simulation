xcopy .\AirshipSimulation\bin\Debug\netcoreapp2.1\AirshipSimulation.dll ..\OSMLS\modules /Y /I
cd ../OSMLS
dotnet run
