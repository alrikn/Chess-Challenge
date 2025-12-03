#!/bin/bash

# Convenience script to run batch mode with 200 parallel games
# Usage: ./run_batch.sh [number_of_games] [parallel_limit]

GAMES=${1:-200}
PARALLEL=${2:-200}

# Use .NET 6 specifically
DOTNET="/opt/homebrew/opt/dotnet@6/bin/dotnet"

echo "Running $GAMES games with max $PARALLEL in parallel..."
echo "Using: $($DOTNET --version)"
echo ""

cd "$(dirname "$0")/Chess-Challenge"
$DOTNET run -- --batch --games=$GAMES --parallel=$PARALLEL
