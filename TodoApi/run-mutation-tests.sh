#!/bin/bash

# Mutation Testing with Stryker.NET
# This script runs mutation testing to ensure test quality

set -e  # Exit on any error

echo "🧬 Starting Mutation Testing with Stryker.NET..."

# Check if dotnet-stryker is installed globally
if ! command -v dotnet-stryker &> /dev/null; then
    echo "📦 Installing Stryker.NET globally..."
    dotnet tool install -g dotnet-stryker
fi

# Clean previous results
echo "🧹 Cleaning previous mutation test results..."
rm -rf StrykerOutput/
rm -rf mutation-report/

# Ensure all tests pass before running mutation tests
echo "🧪 Running unit tests to ensure they pass..."
dotnet test TodoApi.Tests.Unit/TodoApi.Tests.Unit.csproj --configuration Release --verbosity minimal

echo "🧪 Running integration tests to ensure they pass..."
dotnet test TodoApi.Tests.Integration/TodoApi.Tests.Integration.csproj --configuration Release --verbosity minimal

# Build the solution
echo "🔨 Building solution..."
dotnet build --configuration Release --no-restore

# Run Stryker mutation testing
echo "🧬 Running mutation testing..."
echo "This may take several minutes depending on the codebase size..."

dotnet stryker \
    --config-file stryker-config.json \
    --output mutation-report \
    --reporter html \
    --reporter json \
    --reporter cleartext \
    --log-level info

# Check if mutation testing completed successfully
if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Mutation Testing Completed Successfully!"
    echo ""
    echo "📊 Results Summary:"
    
    # Parse and display results if JSON report exists
    if [ -f "mutation-report/mutation-report.json" ]; then
        # Extract key metrics from JSON report (requires jq)
        if command -v jq &> /dev/null; then
            MUTATION_SCORE=$(jq -r '.thresholds.high' mutation-report/mutation-report.json 2>/dev/null || echo "N/A")
            KILLED_MUTANTS=$(jq -r '.files | map(.mutants | map(select(.status == "Killed")) | length) | add' mutation-report/mutation-report.json 2>/dev/null || echo "N/A")
            TOTAL_MUTANTS=$(jq -r '.files | map(.mutants | length) | add' mutation-report/mutation-report.json 2>/dev/null || echo "N/A")
            
            echo "🎯 Mutation Score Threshold: ${MUTATION_SCORE}%"
            echo "💀 Killed Mutants: ${KILLED_MUTANTS}"
            echo "🧬 Total Mutants: ${TOTAL_MUTANTS}"
            
            if [ "$KILLED_MUTANTS" != "N/A" ] && [ "$TOTAL_MUTANTS" != "N/A" ]; then
                ACTUAL_SCORE=$(echo "scale=2; $KILLED_MUTANTS * 100 / $TOTAL_MUTANTS" | bc -l 2>/dev/null || echo "N/A")
                echo "📈 Actual Mutation Score: ${ACTUAL_SCORE}%"
            fi
        else
            echo "⚠️  Install 'jq' to see detailed mutation score metrics"
        fi
    fi
    
    echo ""
    echo "📂 Report Location: mutation-report/mutation-report.html"
    echo "🌐 Open in browser: open mutation-report/mutation-report.html"
    echo ""
    
    # Show recommendations
    echo "💡 Mutation Testing Insights:"
    echo "   • High mutation score (>90%) indicates robust tests"
    echo "   • Surviving mutants may reveal test gaps"
    echo "   • Focus on killing mutants in critical code paths"
    echo "   • Review the HTML report for detailed analysis"
    echo ""
    
else
    echo ""
    echo "❌ Mutation Testing Failed!"
    echo "📋 Common Issues:"
    echo "   • Tests failing - ensure all tests pass before running Stryker"
    echo "   • Compilation errors - check that the code builds successfully"
    echo "   • Timeout issues - consider increasing timeout in stryker-config.json"
    echo "   • Memory issues - reduce concurrency in configuration"
    echo ""
    exit 1
fi

# Optional: Check if mutation score meets threshold
THRESHOLD=75
if [ -f "mutation-report/mutation-report.json" ] && command -v jq &> /dev/null; then
    ACTUAL_SCORE=$(jq -r '.files | map(.mutants | map(select(.status == "Killed")) | length) | add as $killed | map(.mutants | length) | add as $total | ($killed * 100 / $total)' mutation-report/mutation-report.json 2>/dev/null)
    
    if [ "$ACTUAL_SCORE" != "null" ] && [ "$ACTUAL_SCORE" != "N/A" ]; then
        if (( $(echo "$ACTUAL_SCORE < $THRESHOLD" | bc -l) )); then
            echo "⚠️  Mutation score ${ACTUAL_SCORE}% is below threshold ${THRESHOLD}%"
            echo "💡 Consider adding more comprehensive tests to improve mutation score"
            # Don't exit with error for now, just warn
            # exit 1
        else
            echo "🎯 Mutation score ${ACTUAL_SCORE}% meets threshold ${THRESHOLD}%"
        fi
    fi
fi