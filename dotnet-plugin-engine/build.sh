#!/usr/bin/env bash
# Build script for the dotnet-auth-server repository.
# Restores NuGet packages, builds the solution, and runs tests.

set -euo pipefail

# Determine the repository root (the parent directory of this script)
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

echo "Repository root: $REPO_ROOT"

# Find the solution file (*.sln) in the repository root
SOLUTION=$(find "$REPO_ROOT" -maxdepth 1 -name "*.sln" -print -quit)

if [[ -z "$SOLUTION" ]]; then
  echo "Error: No solution (.sln) file found in $REPO_ROOT"
  exit 1
fi

echo "Using solution: $SOLUTION"

# Restore packages
dotnet restore "$SOLUTION"

# Build the solution
dotnet build "$SOLUTION" --configuration Release --no-restore

# Run tests
dotnet test "$SOLUTION" --configuration Release --no-build --logger "console;verbosity=normal"

echo "Build and tests completed successfully."
