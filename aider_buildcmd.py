#!/usr/bin/env python3
"""
Build command script for the dotnet-auth-server repository.

The script is placed at /home/redrocket/task-factory/aider_buildcmd.py (outside the
repository). It locates the repository root (which lives under
`workdir/dotnet-auth-server`) and runs `dotnet test` from there.

This allows the command `python3 /home/redrocket/task-factory/aider_buildcmd.py`
to work regardless of where the script is invoked.
"""

import subprocess
import sys
from pathlib import Path

def main() -> int:
    # Directory where this script resides
    script_dir = Path(__file__).resolve().parent

    # Expected location of the repository root
    repo_root = script_dir / "workdir" / "dotnet-auth-server"

    # Fallback: if the expected layout is not present, try to locate a folder
    # that contains a *.sln file or a `src` directory.
    if not (repo_root / "src").is_dir():
        # Search upward from script_dir for a directory containing a .sln file
        candidate = script_dir
        while candidate != candidate.parent:
            if any(p.suffix == ".sln" for p in candidate.iterdir()):
                repo_root = candidate
                break
            candidate = candidate.parent

    # Final sanity check – ensure we have a `src` folder
    if not (repo_root / "src").is_dir():
        print(
            f"Error: Could not locate repository root containing a 'src' folder. "
            f"Tried {repo_root}",
            file=sys.stderr,
        )
        return 1

    # Run `dotnet test` in the determined repository root
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
