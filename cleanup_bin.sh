#!/bin/bash

# Remove build outputs from bin/ directory
# These files are generated during dotnet build/publish and shouldn't be in the repository

cd /home/akira/develop/0.9.3.0_akisim/akisim/bin

echo "Removing build outputs from bin/ directory..."

# Remove all OpenSim DLL files (these are built from source)
echo "Removing OpenSim DLL files..."
rm -f OpenSim*.dll

# Remove SmartThreadPool and ThreadedClasses (built from ThirdParty)
echo "Removing ThirdParty DLLs..."
rm -f SmartThreadPool.dll ThreadedClasses.dll

# Remove Robust.dll (built from source)
echo "Removing Robust.dll..."
rm -f Robust.dll

# Remove all .exe files (built executables)
echo "Removing .exe files..."
rm -f *.exe

# Remove all .pdb files (debug symbols)
echo "Removing .pdb files..."
rm -f *.pdb

# Remove .runtimeconfig.json files (build outputs)
echo "Removing .runtimeconfig.json files..."
rm -f *.runtimeconfig.json

# Remove .deps.json files
echo "Removing .deps.json files..."
rm -f *.deps.json

# Remove mautil (build tool)
echo "Removing mautil..."
rm -f mautil.exe mautil.dll

echo ""
echo "Cleanup complete!"
echo ""
echo "Files that should REMAIN in bin/ (third-party dependencies):"
echo "- C5.dll, CSJ2K.dll, DotNetOpenId.dll, Ionic.Zip.dll"
echo "- LukeSkywalker.IPNetwork.dll"
echo "- log4net.dll, Nini.dll"
echo "- Mono.Addins*.dll, Mono.Cecil.dll, Mono.Security.dll"
echo "- OpenMetaverse*.dll"
echo "- Database drivers (MySQL, PostgreSQL, SQLite)"
echo "- NuGet packages (BouncyCastle, Google.Protobuf, etc.)"
echo "- Configuration files, data directories, assets"
