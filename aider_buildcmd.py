#!/usr/bin/env python3
"""
Utility script to build and test the DotnetAuthServer solution.

Running this script executes `dotnet test` in the repository root,
captures the output, and returns the exit code of the test runner.
It includes logic to locate the repository root even when the script
resides outside the repository (e.g., in /home/redrocket/task-factory/).
"""

import subprocess
import sys
from pathlib import Path

def find_repo_root(start_dir: Path) -> Path:
    """
    Walks up the directory tree from ``start_dir`` looking for a folder that
    contains a *.sln file or a ``src`` directory, which we treat as the repo root.

    Returns:
        Path: The repository root directory.

    Raises:
        FileNotFoundError: If no suitable directory is found.
    """
    candidate = start_dir
    while candidate != candidate.parent:
        # Look for a solution file (*.sln) or a src folder
        has_sln = any(p.suffix.lower() == ".sln" for p in candidate.iterdir())
        has_src = (candidate / "src").is_dir()
        if has_sln or has_src:
            return candidate
        candidate = candidate.parent
    raise FileNotFoundError("Could not locate repository root containing a .sln file or a 'src' folder.")

def main() -> int:
    """
    Executes `dotnet test` and streams its output.

    Returns:
        int: The exit code returned by the `dotnet test` command.
    """
    # The directory where this script resides
    script_dir = Path(__file__).resolve().parent

    try:
        repo_root = find_repo_root(script_dir)
    except FileNotFoundError as exc:
        print(f"Error: {exc}", file=sys.stderr)
        return 1

    try:
        result = subprocess.run(
            ["dotnet", "test", "--no-build", "--verbosity", "minimal"],
            cwd=repo_root,
            capture_output=True,
            text=True,
            check=False,
        )
    except FileNotFoundError:
        print("Error: 'dotnet' CLI not found. Please install .NET SDK.", file=sys.stderr)
        return 1
    except Exception as exc:
        print(f"Unexpected error while running tests: {exc}", file=sys.stderr)
        return 1

    # Print the test runner output
    print(result.stdout)
    if result.stderr:
        print(result.stderr, file=sys.stderr)

    return result.returncode

if __name__ == "__main__":
    sys.exit(main())
