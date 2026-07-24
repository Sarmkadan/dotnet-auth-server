#!/usr/bin/env python3
"""
Simple build command script for the dotnet-auth-server repository.

Running this script will execute `dotnet test` in the repository root,
collecting and displaying test results. It is intended to be used by
the Aider toolchain or any CI process that expects a build command
script at /home/redrocket/task-factory/aider_buildcmd.py.
"""

import subprocess
import sys
from pathlib import Path

def main() -> int:
    """
    Locate the actual repository root (the directory that contains the
    .sln file or the `src` folder) and run `dotnet test` from there.

    The script itself lives in /home/redrocket/task-factory/, while the
    repository is under `workdir/dotnet-auth-server`.  We therefore
    compute the correct path dynamically.
    """
    # This script resides in /home/redrocket/task-factory/
    script_dir = Path(__file__).resolve().parent

    # The repository root is expected to be at:
    #   <script_dir>/workdir/dotnet-auth-server
    repo_root = script_dir / "workdir" / "dotnet-auth-server"

    # If the expected layout is not found, fall back to the script directory.
    if not (repo_root / "src").is_dir():
        repo_root = script_dir

    # Execute `dotnet test` from the determined repository root.
    try:
        result = subprocess.run(
            ["dotnet", "test", "--no-build", "--verbosity", "minimal"],
            cwd=repo_root,
            check=False,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            text=True,
        )
        print(result.stdout)
        return result.returncode
    except FileNotFoundError:
        print(
            "Error: 'dotnet' executable not found. Ensure the .NET SDK is installed.",
            file=sys.stderr,
        )
        return 1
    except Exception as exc:
        print(f"Unexpected error while running tests: {exc}", file=sys.stderr)
        return 1

if __name__ == "__main__":
    sys.exit(main())
