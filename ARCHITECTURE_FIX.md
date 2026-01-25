# Architecture Fix for SkiaSharp and Native Libraries

## Problem

The application was failing with the following error:

```
System.DllNotFoundException: Unable to load shared library 'libSkiaSharp' or one of its dependencies.
/home/akira/opensim/grid/akisim/bin/runtimes/linux-x64/native/libSkiaSharp.so: wrong ELF class: ELFCLASS32
```

This error indicates that 32-bit (i386) versions of the native libraries were being used on a 64-bit (x86_64) system.

## Solution

The Makefile has been updated to automatically fix the architecture mismatch during deployment. The fix:

1. **Detects the operating system** - Only applies to Linux (not Windows)
2. **Checks for 64-bit libraries** - Looks for libraries in the `bin/lib64/` directory
3. **Copies correct libraries** - Replaces 32-bit libraries with 64-bit versions in:
   - `bin/runtimes/linux-x64/native/` (primary runtime directory)
   - `bin/` (root directory for compatibility)

### Libraries Fixed

- `libSkiaSharp.so` - Used for image processing
- `libBulletSim.so` - Physics engine
- `libopenjpeg-dotnet.so` - Image compression
- `libubode.so` - Dependency for other libraries

## Usage

The fix is automatically applied during deployment. Simply run:

```bash
make deploy
```

This will automatically copy the 64-bit libraries to the correct locations.

## Verification

### Using the Verification Script

Run the provided verification script:

```bash
bash verify_architecture.sh
```

This will check all native libraries in both the bin directory and the runtime directory.

### Manual Verification

To manually verify individual libraries:

```bash
file /path/to/libSkiaSharp.so
```

Should output:
```
ELF 64-bit LSB shared object, x86-64, version 1 (GNU/Linux), dynamically linked
```

## Technical Details

The issue occurred because:
1. The build process created 32-bit libraries in the root `bin/` directory
2. The deployment copied these 32-bit libraries to the runtime directory
3. The system is 64-bit (x86_64), so it couldn't load the 32-bit libraries

The fix ensures that:
1. 64-bit libraries from `bin/lib64/` are copied to the runtime directory
2. The libraries in the root `bin/` directory are also updated to 64-bit
3. This happens automatically during deployment

## Notes

- This fix only applies to Linux systems
- Windows deployments are not affected
- The fix preserves all other deployment functionality
